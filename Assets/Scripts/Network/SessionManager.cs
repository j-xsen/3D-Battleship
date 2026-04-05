using System;
using System.Threading.Tasks;
using Ships;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
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
        
        // // Player properties
        // ready / not ready
        // used in lobby, TODO all done placement?
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
        // keeps track of client's current state
        private string _lastState;
        
        // name of game scene file
        private const string GameScene = "Game";
        private EventCallback<ChangeEvent<bool>> _readyCallback;

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
                _readyCallback = evt => _ = SendReady(evt.newValue);
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
                
                // sends ready player property
                await SendReady(false);
                
                // set up host, only if is the host
                if (!session.IsHost) return;
                
                Debug.Log("Setting up host...");
                
                _host = _session.AsHost();
                _session.PlayerPropertiesChanged += OnPlayerPropertiesChanged;

                // since is host / first player, setup session state property
                // defaults to the lobby
                SetLobby();
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

            // get the game state
            _session.Properties.TryGetValue(StateName, out SessionProperty gameState);

            if (gameState == null) return; // game state not found

            if (_lastState == null)
            {
                Debug.Log("LastState empty. setting to gameState.Value");
                
                // last state not set, set to game's state
                _lastState = gameState.Value;
            } else if (_lastState != gameState.Value)
            {
                // Debug.Log("LastState != gameState.Value");
                // Debug.Log($"LastState: {_lastState} / gameState: {gameState.Value}");
                
                // last state does not match game's state
                // if last state is not lobby, ignore (game already loaded)
                if (_lastState != _sLobby.Value) return;
                
                // Debug.Log("Last state == sLobby");
                
                // switching from lobby to game
                // ensure 2 players
                if (_session.PlayerCount != 2) return;
                
                // Debug.Log("2 players");
                
                _lastState = gameState.Value;
                
                LoadGame();
            }
        }

        // CLIENT
        private void OnSessionRemoved(ISession session)
        {
            // player left lobby
            VisualElement toggle = uiDoc.rootVisualElement.Q<Toggle>("Toggle");
            toggle?.UnregisterCallback(_readyCallback);
            _readyCallback = null;
            _session.Changed -= OnSessionChanged;
            _session.PlayerPropertiesChanged -= OnPlayerPropertiesChanged;
            uiDoc.rootVisualElement.Clear();
            _session = null;
            _host = null;
        }
        
        // CLIENT
        private async Task SendReady(bool status)
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
        private static void LoadGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameScene);
        }

        // CLIENT
        public async void PlaceShip(int shipType, int number, ShipView ship)
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
            bool allReady = true;

            if (_session.PlayerCount != 2) return false;

            foreach (IReadOnlyPlayer p in _session.Players)
            {
                if (p.Properties.TryGetValue(ReadyName, out PlayerProperty pVal) &&
                    pVal.Value == "true") continue;
                allReady = false;
                break;
            }

            return allReady;
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
            } else if (gameState?.Value == _sPlacing.Value)
            {
                // placing
            } else if (gameState?.Value == _sAtWar.Value)
            {
                // at war
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
            try
            {
                Debug.Log("Starting game...");
                SetState(_sPlacing);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private async void SetState(SessionProperty state)
        {
            try
            {
                _host.SetProperty(StateName, state);
                await _host.SavePropertiesAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        // HOST
        private void SetLobby()
        {
            SetState(_sLobby);
        }
    }
    
}