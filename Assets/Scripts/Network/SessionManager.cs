using System;
using System.Threading.Tasks;
using Ships;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// CLIENT - ran by every player
// HOST - ran only by host

namespace Network
{
    public class SessionManager : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDoc;
        [SerializeField] private VisualTreeAsset uxmlReadyUp;

        // network connections
        private ISession _session;
        private IHostSession _host;

        // Strings used as variable names in the session
        private const string ReadyName = "ready";
        private const string StateName = "state";
        private const string ShipsName = "ships";
        private const string ModeName = "mode";
        private const string TurnName = "turn";

        // // Player properties
        // ready / not ready
        // used in lobby and placing
        private readonly PlayerProperty _notReady = new("false");
        private readonly PlayerProperty _ready = new("true");

        // // Session properties
        // checks if all players are ready
        // if they are, sets a session property
        // each client detects a session property change and checks what it is
        // if the state is Lobby and all ready is true, then it will head to GameScene
        // private readonly SessionProperty _allReady = new("true");
        // states -
        //      LOBBY - players are connecting
        //      PLACING - players are placing ships
        //      AT WAR - cycling between player turns
        // prefix s for state
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

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            try
            {
                // connect to unity services
                await UnityServices.InitializeAsync();
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                // session events
                MultiplayerService.Instance.SessionAdded += OnSessionAdded;
                MultiplayerService.Instance.SessionRemoved += OnSessionRemoved;

                // debug
                Debug.Log($"Sign in anonymously succeeded! PlayerID: {AuthenticationService.Instance.PlayerId}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnSessionAdded(ISession session)
        {
            // user joined a lobby
            // Debug.Log("OnSessionAdded fired! IsHost: " + session.IsHost);
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
            // _ discards value, but ensures everything is loaded
            _ = OnSessionAddedAsync(session);
        }

        // CLIENT
        private async Task OnSessionAddedAsync(ISession session)
        {
            // adjusts session properties upon session adding
            try
            {
                // Debug.Log($"OnSessionAddedAsync: IsHost={session.IsHost}");
                if (!session.IsHost)
                {
                    await SendReadyAsync(false);
                    return;
                }

                // set up host, only if is the host

                Debug.Log("Setting up host...");

                _host = _session.AsHost();
                _session.PlayerPropertiesChanged += OnPlayerPropertiesChanged;

                // since is host / first player, setup session state property
                // defaults to the lobby
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
            // called when session properties change
            // Debug.Log("Session changed!");

            // make sure session isnt empty
            if (_session == null) return;

            // get the game state
            _session.Properties.TryGetValue(StateName, out SessionProperty gameState);

            if (gameState == null) return; // game state not found

            if (_lastState == null)
            {
                Debug.Log("LastState empty. setting to gameState.Value");

                // last state not set, set to game's state
                SetLastState(gameState.Value);
            }
            else if (_lastState != gameState.Value)
            {
                // Debug.Log("LastState != gameState.Value");
                // Debug.Log($"LastState: {_lastState} / gameState: {gameState.Value}");

                // last state does not match game's state
                if (_lastState == _sLobby.Value)
                {
                    // Last state is lobby
                    // Current state should be Placing

                    // Debug.Log("Last state == sLobby");

                    // switching from lobby to game
                    // ensure 2 players
                    if (_session.PlayerCount != 2) return;
                    // Debug.Log("2 players");

                    // store this state as client's last loaded
                    SetLastState(gameState.Value);
                    
                    // reset ready
                    _ = ResetReadyAsync();
                }
                else if (_lastState == _sPlacing.Value)
                {
                    // last state is Placing
                    // current state should be At War

                    if (_host != null) OnMyTurn?.Invoke();
                    
                    SetLastState(gameState.Value);
                }
            }
        }

        private void SetLastState(string newLastState)
        {
            _lastState = newLastState;
            OnStateChanged?.Invoke(_lastState);
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
                // player left lobby
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
        }

        // CLIENT
        public async void PlaceShipAsync(int shipType, int number, ShipView ship)
        {
            // tells server where ship is placed
            try
            {
                // init empty string
                string shipsString = "";

                // check if ships property exists
                _session.CurrentPlayer.Properties.TryGetValue(ShipsName + shipType, out PlayerProperty ships);
                if (ships != null)
                {
                    shipsString = ships.Value + ";";
                }

                // shipsString format -
                //      / - position and axis separator
                //      ; - ship separator

                // // adjust string to have ship added
                // get ship information
                Axis axis = ship.GetAxes().GetAxis();
                Vector3 pos = ship.transform.position;
                // ship string
                string currentShip = $"{pos.x},{pos.y},{pos.z}/{axis}";

                // update
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
                Debug.Log($"Player {p.Id}: ready={pVal?.Value}");
                if (pVal?.Value == "true") continue;
                return false;
            }

            return true;
        }

        // HOST
        private void OnPlayerPropertiesChanged()
        {
            if (_host == null) return;

            // get state
            _session.Properties.TryGetValue(StateName, out SessionProperty gameState);

            if (gameState?.Value == _sLobby.Value)
            {
                // lobby
                if (!AllPlayersReady()) return; // check all players ready
                StartGame(); // start game if so
            }
            else if (gameState?.Value == _sPlacing.Value)
            {
                // placing
                if (!_gameSceneReady) return; // make sure scene is set up
                if (!AllPlayersReady()) return; // check all players ready
                StartWar();
            }
            else if (gameState?.Value == _sAtWar.Value)
            {
                // at war
                
                // get player who turn it is
                // _session.Properties.TryGetValue(TurnName, out SessionProperty currentPlayer);
                // if (currentPlayer != null)
                // {
                //     // check if this player
                //     if ((currentPlayer.Value == _tOne.Value & _session.IsHost) ||
                //         (currentPlayer.Value == _tTwo.Value & _session.IsHost)
                //     {
                //         
                //     }
                // }
            }
            else
            {
                Debug.LogError("Could not find state!");
            }

            // LoadGame();
        }

        // HOST
        private void StartGame()
        {
            _ = SetStateAsync(_sPlacing);
        }

        private void StartWar()
        {
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
                }
                
                await _host.SavePropertiesAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}