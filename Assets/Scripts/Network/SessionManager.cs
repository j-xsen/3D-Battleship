using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ships;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

//shared network state
//whose turn is it?
//what phase are we in


// CLIENT - ran by every player
// HOST - ran only by host

namespace Network
{
    public class SessionManager : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDoc;
        [SerializeField] private VisualTreeAsset uxmlReadyUp;

        // host-side authoritative combat board data.
        // they are rebuilt from each player's networked ship placement data.
        private CombatBoardState _playerOneBoardState;
        private CombatBoardState _playerTwoBoardState;

        // board dimensions for authoritative board reconstruction.
        // set these to match your actual game board dimensions.
        [Header("Authoritative Board Size")]
        [SerializeField] private int boardWidth = 10;
        [SerializeField] private int boardHeight = 10;
        [SerializeField] private int boardDepth = 10;

        // network connections
        private ISession _session;
        private IHostSession _host;

        // Strings used as variable names in the session
        private const string ReadyName = "ready";
        private const string StateName = "state";
        private const string ShipsName = "ships";
        private const string ModeName = "mode";
        private const string TurnName = "turn";
        private const string ShotName = "shot";
        private const string ResultName = "result";
        private const string PlayerNameKey = "playerName";
        // NEW:
        // prevents duplicate client-side visual application
        private string _lastVisualShot;

        // NEW:
        // prevents duplicate host-side shot processing for the same turn/player/shot
        private string _lastProcessedTurnShot;

        // NEW:
        // simple host-side reentry lock while one shot is being resolved
        private bool _isResolvingShot;

        // // Player properties
        // ready / not ready
        // used in lobby and placing
        private readonly PlayerProperty _notReady = new("false");
        private readonly PlayerProperty _ready = new("true");



        // // Session properties
        // states -
        //      LOBBY - players are connecting
        //      PLACING - players are placing ships
        //      AT WAR - cycling between player turns
        private readonly SessionProperty _sLobby = new("Lobby");
        private readonly SessionProperty _sPlacing = new("Placing");
        private readonly SessionProperty _sAtWar = new("AtWar");

        // turns
        private readonly SessionProperty _tOne = new("1");
        private readonly SessionProperty _tTwo = new("2");

        // keeps track of client's current state
        private string _lastState;
        private bool _gameSceneReady;

        // name of game scene file
        private const string GameScene = "Game";
        private EventCallback<ChangeEvent<bool>> _readyCallback;

        // events
        public event Action<string> OnStateChanged;
        public event Action OnMyTurn;
        public event Action OnTheirTurn;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            try
            {

                MultiplayerService.Instance.SessionAdded += OnSessionAdded;
                MultiplayerService.Instance.SessionRemoved += OnSessionRemoved;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnSessionAdded(ISession session)
        {
            // user joined a lobby
            uxmlReadyUp.CloneTree(uiDoc.rootVisualElement);
            _session = session;

            // toggle ready button
            VisualElement toggle = uiDoc.rootVisualElement.Q<Toggle>("Toggle");
            if (toggle != null)
            {
                _readyCallback = evt => _ = SendReadyAsync(evt.newValue);
                toggle.RegisterCallback(_readyCallback);
            }

            // set up session events
            _session.Changed += OnSessionChanged;

            // sends to async function that can edit session values
            _ = OnSessionAddedAsync(session);
        }

        // CLIENT
        private async Task OnSessionAddedAsync(ISession session)
        {
            try
            {
                await EnsurePlayerNameAsync();

                // optional safety so the player-name property is saved before other actions
                await Task.Delay(50);

                if (!session.IsHost)
                {
                    await SendReadyAsync(false);
                    return;
                }

                Debug.Log("Setting up host...");

                _host = _session.AsHost();
                _session.PlayerPropertiesChanged += OnPlayerPropertiesChanged;

                // since is host / first player, setup session state property
                await SetStateAsync(_sLobby);
                await SendReadyAsync(false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // CLIENT
        private void OnSessionChanged()
        {
            if (_session == null) return;

            _session.Properties.TryGetValue(StateName, out SessionProperty gameState);
            if (gameState == null) return;

            if (_lastState == null)
            {
                Debug.Log("LastState empty. setting to gameState.Value");
                SetLastState(gameState.Value);
            }
            else if (_lastState != gameState.Value)
            {
                if (_lastState == _sLobby.Value)
                {
                    // switching from lobby to game
                    if (_session.PlayerCount != 2) return;

                    SetLastState(gameState.Value);

                    // reset ready
                    _ = ResetReadyAsync();
                }
                else if (_lastState == _sPlacing.Value)
                {
                    // last state is Placing
                    // current state should be At War
                    SetLastState(gameState.Value);
                    
                    BuildAuthoritativeBoardStates();

                    // NEW:
                    // clear any stale player shot when combat begins for this client
                    _ = ClearMyShotAsync();
                }
            }

            // while in AtWar, every client must react to TurnName changes.
            // this keeps combat input cycling correctly after each shot.
            if (gameState.Value == _sAtWar.Value)
            {
                // process shot visuals and update board state BEFORE firing the turn event.
                // UpdateTurnFromSession triggers the radio switch which calls ClearBoard + redraws,
                // so the board state must already include the new shot when that redraw happens.
                _session.Properties.TryGetValue(ShotName, out SessionProperty shotProp);
                _session.Properties.TryGetValue(ResultName, out SessionProperty resultProp);

                if (shotProp != null && resultProp != null &&
                    !string.IsNullOrWhiteSpace(shotProp.Value) &&
                    !string.IsNullOrWhiteSpace(resultProp.Value))
                {
                    string combined = shotProp.Value + "|" + resultProp.Value;

                    if (_lastVisualShot != combined)
                    {
                        _lastVisualShot = combined;

                        Vector3Int cell = ParseShot(shotProp.Value);

                        if (Enum.TryParse(resultProp.Value, out AttackResult result))
                        {
                            Debug.Log($"CLIENT: Shot result at {cell} = {result}");
                            ApplyShotVisual(cell, result);

                            // record attack into local board state so ShowTheirShips can re-draw
                            // after a ClearBoard (e.g. radio button switch).
                            // host already does this via RegisterAttack in OnPlayerPropertiesChanged.
                            if (!_session.IsHost)
                            {
                                _session.Properties.TryGetValue(TurnName, out SessionProperty turnNow);
                                // turn is already flipped to next player;
                                // if next=1, player 2 just shot -> player 1's board was attacked
                                CombatBoardState attackedBoard = (turnNow?.Value == _tOne.Value)
                                    ? _playerOneBoardState
                                    : _playerTwoBoardState;
                                attackedBoard?.RecordAttackResult(cell, result);
                            }
                        }

                        // NEW:
                        // if my local player-shot property still matches the resolved shot,
                        // clear it so it cannot be reused on a later callback/turn.
                        _session.CurrentPlayer.Properties.TryGetValue(ShotName, out PlayerProperty myShot);
                        if (myShot != null && myShot.Value == shotProp.Value)
                        {
                            _ = ClearMyShotAsync();
                        }
                    }
                }

                // fire turn event after board state is updated so the radio-switch redraw
                // already has the latest shot data.
                UpdateTurnFromSession();
            }
        }

        private void SetLastState(string newLastState)
        {
            _lastState = newLastState;
            OnStateChanged?.Invoke(_lastState);
        }

        public async Task SetShotTarget(Vector3Int coords)
        {
            // store shot as x,y,z
            string shotValue = $"{coords.x},{coords.y},{coords.z}";

            _session.CurrentPlayer.SetProperty(ShotName, new PlayerProperty(shotValue));
            await _session.SaveCurrentPlayerDataAsync();
        }

        // NEW:
        // clear this client's player-shot property after a resolved shot
        private async Task ClearMyShotAsync()
        {
            try
            {
                if (_session == null) return;

                // Only save if the shot property is actually set to something
                _session.CurrentPlayer.Properties.TryGetValue(ShotName, out PlayerProperty currentShot);
                if (currentShot == null || string.IsNullOrWhiteSpace(currentShot.Value)) return;

                _session.CurrentPlayer.SetProperty(ShotName, new PlayerProperty(string.Empty));
                await _session.SaveCurrentPlayerDataAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // parse a shot stored as "x,y,z"
        private Vector3Int ParseShot(string value)
        {
            string[] parts = value.Split(',');

            return new Vector3Int(
                int.Parse(parts[0]),
                int.Parse(parts[1]),
                int.Parse(parts[2])
            );
        }

        // helper to evaluate the current TurnName session property
        // and raise the correct local turn event for this client.
        private void UpdateTurnFromSession()
        {
            if (_session == null) return;

            _session.Properties.TryGetValue(TurnName, out SessionProperty turn);
            if (turn == null) return;

            bool amHost = _session.IsHost;
            bool isMyTurn =
                (amHost && turn.Value == _tOne.Value) ||
                (!amHost && turn.Value == _tTwo.Value);

            if (isMyTurn)
            {
                Debug.Log("Session says it is MY turn.");
                OnMyTurn?.Invoke();
            }
            else
            {
                Debug.Log("Session says it is THEIR turn.");
                OnTheirTurn?.Invoke();
            }
        }

        // helper for identifying player 1 / player 2.
        // current assumption remains:
        //      host = player 1
        //      non-host = player 2
        private IReadOnlyPlayer GetPlayerOne()
        {
            if (_session == null) return null;
            return _session.Players.FirstOrDefault(p => p.Id == _session.Host);
        }

        private IReadOnlyPlayer GetPlayerTwo()
        {
            if (_session == null) return null;
            return _session.Players.FirstOrDefault(p => p.Id != _session.Host);
        }

        // gets the player whose turn it currently is
        private IReadOnlyPlayer GetCurrentTurnPlayer(string turnValue)
        {
            if (turnValue == _tOne.Value) return GetPlayerOne();
            if (turnValue == _tTwo.Value) return GetPlayerTwo();
            return null;
        }

        public CombatBoardState GetMyBoardState()
        {
            return _session.IsHost ? _playerOneBoardState : _playerTwoBoardState;
        }

        public CombatBoardState GetTheirBoardState()
        {
            if (_playerOneBoardState == null || _playerTwoBoardState == null)
            {
                Debug.LogError("Unable to find Their Board State");
            }
            return _session.IsHost ? _playerTwoBoardState : _playerOneBoardState;
        }

        // returns the authoritative board state that should receive the attack.
        // if player 1 is attacking, player 2 is defending, and vice versa.
        private CombatBoardState GetDefenderBoardState(string turnValue)
        {
            if (turnValue == _tOne.Value)
            {
                return _playerTwoBoardState;
            }

            if (turnValue == _tTwo.Value)
            {
                return _playerOneBoardState;
            }

            return null;
        }

        // parse stored ship placement position string "x,y,z"
        private Vector3Int ParseStartCell(string positionPart)
        {
            string[] coords = positionPart.Split(',');

            return new Vector3Int(
                Mathf.RoundToInt(float.Parse(coords[0])),
                Mathf.RoundToInt(float.Parse(coords[1])),
                Mathf.RoundToInt(float.Parse(coords[2]))
            );
        }

        // convert saved axis string to a grid direction.
        private Vector3Int AxisToDirection(string axisString)
        {
            string a = axisString.Trim();

            switch (a)
            {
                case "X":
                    return new Vector3Int(1, 0, 0);

                case "Y":
                    return new Vector3Int(0, 1, 0);

                case "Z":
                    return new Vector3Int(0, 0, 1);

                default:
                    Debug.LogWarning($"Unknown axis string '{axisString}', defaulting to X");
                    return new Vector3Int(1, 0, 0);
            }
        }

        // helper for rebuilding ship occupied cells from saved placement info.
        private List<Vector3Int> GetOccupiedCells(Vector3Int startCell, Vector3Int direction, int length)
        {
            List<Vector3Int> cells = new List<Vector3Int>();

            for (int i = 0; i < length; i++)
            {
                cells.Add(startCell + direction * i);
            }

            return cells;
        }

        // central lookup for ship length by ship type.
        private int GetShipLengthFromType(int shipType)
        {
            switch (shipType)
            {
                case 0: return 2;
                case 1: return 3;
                case 2: return 4;
                case 3: return 5;
                default:
                    Debug.LogWarning($"Unknown shipType {shipType}, defaulting length to 2");
                    return 2;
            }
        }

        // build a host-authoritative board state for one player from that player's saved ship properties.
        private CombatBoardState BuildBoardStateForPlayer(IReadOnlyPlayer player)
        {
            CombatBoardState boardState = new CombatBoardState(boardWidth, boardHeight, boardDepth);

            foreach (var kvp in player.Properties)
            {
                string key = kvp.Key;
                PlayerProperty prop = kvp.Value;

                if (!key.StartsWith(ShipsName) || prop == null || string.IsNullOrWhiteSpace(prop.Value))
                    continue;

                string shipTypeSuffix = key.Substring(ShipsName.Length);
                if (!int.TryParse(shipTypeSuffix, out int shipType))
                {
                    Debug.LogWarning($"Could not parse ship type from key {key}");
                    continue;
                }

                // each ship in the property is separated by ';'
                string[] ships = prop.Value.Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (string shipEntry in ships)
                {
                    // format: "x,y,z/axis"
                    string[] parts = shipEntry.Split('/');
                    if (parts.Length != 2)
                    {
                        Debug.LogWarning($"Invalid ship entry format: {shipEntry}");
                        continue;
                    }

                    Vector3Int startCell = ParseStartCell(parts[0]);
                    Vector3Int direction = AxisToDirection(parts[1]);
                    int length = GetShipLengthFromType(shipType);

                    List<Vector3Int> occupiedCells = GetOccupiedCells(startCell, direction, length);
                    boardState.RegisterShipData(shipType, length, occupiedCells);
                }
            }

            return boardState;
        }

        // host reconstructs both players' boards before combat starts.
        private void BuildAuthoritativeBoardStates()
        {
            IReadOnlyPlayer playerOne = GetPlayerOne();
            IReadOnlyPlayer playerTwo = GetPlayerTwo();

            if (playerOne == null || playerTwo == null)
            {
                Debug.LogError("Cannot build board states: missing players.");
                return;
            }

            _playerOneBoardState = BuildBoardStateForPlayer(playerOne);
            _playerTwoBoardState = BuildBoardStateForPlayer(playerTwo);

            Debug.Log("Built authoritative board states for both players.");
        }

        // CLIENT
        private async Task ResetReadyAsync()
        {
            _session.CurrentPlayer.SetProperty(ReadyName, _notReady);
            await _session.SaveCurrentPlayerDataAsync();
            LoadGame();
        }

        // CLIENT
        private void OnSessionRemoved(ISession session)
        {
            if (uiDoc)
            {
                VisualElement toggle = uiDoc.rootVisualElement.Q<Toggle>("Toggle");
                toggle?.UnregisterCallback(_readyCallback);
                uiDoc.rootVisualElement.Clear();
            }

            _readyCallback = null;
            _session.Changed -= OnSessionChanged;
            _session.PlayerPropertiesChanged -= OnPlayerPropertiesChanged;

            _session = null;
            _host = null;
        }

        // CLIENT
        public async Task SendReadyAsync(bool status)
        {
            try
            {
                Debug.Log("Sending ready...");
                _session.CurrentPlayer.SetProperty(ReadyName, status ? _ready : _notReady);
                await _session.SaveCurrentPlayerDataAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // CLIENT
        private void LoadGame()
        {
            if (uiDoc)
            {
                uiDoc.rootVisualElement.Clear();
                uiDoc = null;
            }

            SceneManager.sceneLoaded += OnGameSceneLoaded;
            SceneManager.LoadScene(GameScene);
        }

        private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
            _gameSceneReady = true;

            if (_session is { IsHost: true }) _host = _session.AsHost();
        }

        // CLIENT
        public async void PlaceShipAsync(int shipType, int number, Vector3Int startCell, Axis axis)
        {
            try
            {
                string shipsString = "";

                _session.CurrentPlayer.Properties.TryGetValue(ShipsName + shipType, out PlayerProperty ships);
                if (ships != null)
                {
                    shipsString = ships.Value + ";";
                }

                string currentShip = $"{startCell.x},{startCell.y},{startCell.z}/{axis}";

                string value = shipsString + currentShip;
                _session.CurrentPlayer.SetProperty(ShipsName + shipType, new PlayerProperty(value));
                await _session.SaveCurrentPlayerDataAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // HOST
        private bool AllPlayersReady()
        {
            if (_session.PlayerCount != 2) return false;

            foreach (IReadOnlyPlayer p in _session.Players)
            {
                p.Properties.TryGetValue(ReadyName, out PlayerProperty pVal);
                // Debug.Log($"Player {p.Id}: ready={pVal?.Value}");
                if (pVal?.Value == "true") continue;
                return false;
            }

            return true;
        }

        // HOST
        private void OnPlayerPropertiesChanged()
        {
            if (_host == null) return;

            _session.Properties.TryGetValue(StateName, out SessionProperty gameState);

            if (gameState?.Value == _sLobby.Value)
            {
                if (!AllPlayersReady()) return;
                StartGame();
            }
            else if (gameState?.Value == _sPlacing.Value)
            {
                if (!_gameSceneReady) return;
                if (!AllPlayersReady()) return;
                StartWar();
            }
            else if (gameState?.Value == _sAtWar.Value)
            {
                if (_isResolvingShot)
                {
                    return;
                }

                _session.Properties.TryGetValue(TurnName, out SessionProperty turn);
                if (turn == null) return;

                IReadOnlyPlayer shooter = GetCurrentTurnPlayer(turn.Value);
                if (shooter == null)
                {
                    Debug.LogError("Could not determine shooter during AtWar.");
                    return;
                }

                shooter.Properties.TryGetValue(ShotName, out PlayerProperty shotProp);
                if (shotProp == null || string.IsNullOrWhiteSpace(shotProp.Value))
                {
                    return;
                }

                string processedKey = $"{turn.Value}|{shooter.Id}|{shotProp.Value}";
                if (_lastProcessedTurnShot == processedKey)
                {
                    Debug.Log($"Duplicate host shot ignored: {processedKey}");
                    return;
                }

                _session.Properties.TryGetValue(ShotName, out SessionProperty lastResolvedShot);
                _session.Properties.TryGetValue(ResultName, out SessionProperty lastResolvedResult);

                if (lastResolvedShot != null &&
                    lastResolvedResult != null &&
                    !string.IsNullOrWhiteSpace(lastResolvedShot.Value) &&
                    !string.IsNullOrWhiteSpace(lastResolvedResult.Value) &&
                    lastResolvedShot.Value == shotProp.Value)
                {
                    Debug.Log($"Shot {shotProp.Value} was already resolved. Ignoring duplicate callback.");
                    return;
                }

                _isResolvingShot = true;
                _lastProcessedTurnShot = processedKey;

                try
                {
                    Debug.Log($"Host received shot from turn player {turn.Value}: {shotProp.Value}");

                    Vector3Int shotCell = ParseShot(shotProp.Value);

                    CombatBoardState defenderBoardState = GetDefenderBoardState(turn.Value);
                    if (defenderBoardState == null)
                    {
                        Debug.LogError("Defender board state is missing in SessionManager.");
                        return;
                    }

                    AttackResult result = defenderBoardState.RegisterAttack(shotCell);

                    switch (result)
                    {
                        case AttackResult.Miss:
                            Debug.Log($"Host: Miss at {shotCell}");
                            break;

                        case AttackResult.Hit:
                            Debug.Log($"Host: Hit at {shotCell}");
                            break;

                        case AttackResult.Destroyed:
                            Debug.Log($"Host: Ship destroyed at {shotCell}");
                            break;

                        case AttackResult.AlreadyAttacked:
                            Debug.Log($"Host: {shotCell} was already attacked");
                            break;
                    }

                    if (defenderBoardState.AllShipsDestroyed())
                    {
                        Debug.Log("All defender ships destroyed! Game over.");
                    }

                    _ = SaveResolvedShotAsync(shotCell, result);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    _isResolvingShot = false;
                }
            }
            else
            {
                Debug.LogError("Could not find state!");
            }
        }

        // HOST
        private void StartGame()
        {
            _ = SetStateAsync(_sPlacing);
        }

        private void StartWar()
        {
            // before combat begins, host reconstructs both players' board states
            // from their saved ship placement properties.
            BuildAuthoritativeBoardStates();

            // NEW:
            // reset host duplicate tracking at combat start
            _lastProcessedTurnShot = null;
            _lastVisualShot = null;
            _isResolvingShot = false;

            _ = SetStateAsync(_sAtWar);
        }

        private async Task SetStateAsync(SessionProperty state)
        {
            try
            {
                _host.SetProperty(StateName, state);

                if (state == _sAtWar)
                {
                    // Going to war, add turn property (default to host player 1)
                    _host.SetProperty(TurnName, _tOne);

                    // clear the last resolved shot/result when combat starts.
                    _host.SetProperty(ShotName, new SessionProperty(string.Empty));
                    _host.SetProperty(ResultName, new SessionProperty(string.Empty));
                }

                await _host.SavePropertiesAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void ApplyShotVisual(Vector3Int cell, AttackResult result)
        {
            SpaceBuilder board = FindFirstObjectByType<SpaceBuilder>();

            if (board == null)
            {
                Debug.LogError("No SpaceBuilder found for visuals.");
                return;
            }

            switch (result)
            {
                case AttackResult.Miss:
                    board.MarkMiss(cell);
                    break;

                case AttackResult.Hit:
                    board.MarkHit(cell);
                    break;

                case AttackResult.Destroyed:
                    board.MarkHit(cell);
                    break;

                case AttackResult.AlreadyAttacked:
                    Debug.Log($"VISUAL: {cell} was already attacked");
                    break;
            }
        }

        private async Task SaveResolvedShotAsync(Vector3Int shotCell, AttackResult result)
        {
            try
            {
                _session.Properties.TryGetValue(TurnName, out SessionProperty currentTurn);
                if (currentTurn == null) return;

                string nextTurn = currentTurn.Value == _tOne.Value ? _tTwo.Value : _tOne.Value;

                _host.SetProperty(ShotName, new SessionProperty($"{shotCell.x},{shotCell.y},{shotCell.z}"));
                _host.SetProperty(ResultName, new SessionProperty(result.ToString()));
                _host.SetProperty(TurnName, new SessionProperty(nextTurn));

                Debug.Log($"Saving resolved shot {shotCell} result {result} and switching turn to {nextTurn}");

                await _host.SavePropertiesAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private async Task EnsurePlayerNameAsync()
        {
            try
            {
                string playerName = PlayerPrefs.GetString("PlayerName", "").Trim();

                if (string.IsNullOrEmpty(playerName))
                {
                    string playerId = AuthenticationService.Instance.PlayerId;

                    string suffix = (!string.IsNullOrEmpty(playerId) && playerId.Length >= 4)
                        ? playerId.Substring(playerId.Length - 4)
                        : UnityEngine.Random.Range(1000, 9999).ToString();

                    playerName = $"Player_{suffix}";

                    PlayerPrefs.SetString("PlayerName", playerName);
                    PlayerPrefs.Save();
                }

                // Only save if the property isn't already set to this name
                _session.CurrentPlayer.Properties.TryGetValue(PlayerNameKey, out PlayerProperty existingName);
                if (existingName?.Value == playerName)
                {
                    Debug.Log($"Player name already set to: {playerName}");
                    return;
                }

                _session.CurrentPlayer.SetProperty(PlayerNameKey, new PlayerProperty(playerName));
                await _session.SaveCurrentPlayerDataAsync();

                Debug.Log($"Player name set to: {playerName}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }




    }
}