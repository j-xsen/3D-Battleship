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
        private const string AllReadyName = "allReady";
        private const string StateName = "state";
        
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
        private readonly SessionProperty _allReady = new("true");
        // states -
        //      LOBBY - players are connecting
        //      PLACING - players are placing ships
        //      AT WAR - cycling between player turns
        // prefix s for state
        private readonly SessionProperty _sLobby = new("Lobby");
        private readonly SessionProperty _sPlacing = new("Placing");
        private readonly SessionProperty _sAtWar = new("AtWar");
        
        // name of game scene file
        private const string GameScene = "Game";
        private EventCallback<ChangeEvent<bool>> _readyCallback;
        private bool _loaded;

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

        // CLIENT
        private void OnSessionChanged()
        {
            // Debug.Log("Session changed!");
            // called when session properties change
            
            // get the game state
            _session.Properties.TryGetValue(StateName, out SessionProperty gameState);

            if (gameState?.Value == _sLobby.Value)
            {
                // lobby
                // if not AllReady or playerCount != 2, ignore
                if (!_session.Properties.TryGetValue(AllReadyName, out SessionProperty sVal) ||
                    sVal.Value != _allReady.Value || _session.PlayerCount != 2) return;
                if (!_loaded) LoadGame();
            }
            else if (gameState?.Value == _sPlacing.Value)
            {
                //placing
            }
            else if (gameState?.Value == _sAtWar.Value)
            {
                // cycling turns
            }
            else
            {
                Debug.Log("No GameState on Session");
            }
        }

        private void OnSessionAdded(ISession session)
        {
            // Debug.Log("OnSessionAdded fired! IsHost: " + session.IsHost);
            // user joined a lobby
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
                await SendReady(false);
                // Debug.Log("SendReady done");
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
        private void LoadGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameScene);
            _loaded = true;
        }

        // CLIENT
        public async void PlaceShip(int shipType, int number, ShipView ship)
        {
            try
            {
                Axis axis = ship.GetAxes().GetAxis();
                Vector3 pos = ship.transform.position;
                string value = $"{pos.x},{pos.y},{pos.z}/{axis}";
                _session.CurrentPlayer.SetProperty($"{shipType};{number}", new PlayerProperty(value));
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
        private async void StartGame()
        {
            try
            {
                Debug.Log("Starting game...");
                _host.SetProperty(AllReadyName, new SessionProperty("true"));
                await _host.SavePropertiesAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        // HOST
        private async void SetLobby()
        {
            try
            {
                Debug.Log("SetLobby: Attempting to set state...");
                _host.SetProperty(StateName, _sLobby);
                await _host.SavePropertiesAsync();
                Debug.Log("SetLobby: saved. Current state: " + 
                          (_session.Properties.TryGetValue(StateName, out SessionProperty p) ?
                              p.Value : "NOT FOUND"));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
    
}