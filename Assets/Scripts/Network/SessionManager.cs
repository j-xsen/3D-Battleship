using System;
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
    internal enum GameStates
    {
        Lobby,
    }
    public class SessionManager : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDoc;
        [SerializeField] private VisualTreeAsset uxmlReadyUp;
        private ISession _session;
        private IHostSession _host;
        private readonly PlayerProperty _notReady = new("false");
        private readonly PlayerProperty _ready = new("true");
        private readonly SessionProperty _allReady = new("true");
        private readonly SessionProperty _modePlacing = new("placing");
        // // Session properties
        // states -
        //      LOBBY
        private readonly SessionProperty _state = new(nameof(GameStates.Lobby));
        
        // Strings used as variable names in the session
        private const string ReadyName = "ready";
        private const string AllReadyName = "allReady";
        private const string ModeName = "mode";
        private const string GameScene = "Game";
        private EventCallback<ChangeEvent<bool>> _readyCallback;
        private bool _loaded;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            // connect to unity services
            try
            {
                await UnityServices.InitializeAsync();
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                MultiplayerService.Instance.SessionAdded += OnSessionAdded;
                MultiplayerService.Instance.SessionRemoved += OnSessionRemoved;
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
            // called when session properties change
            
            // if not AllReady or playerCount != 2, ignore
            if (!_session.Properties.TryGetValue(AllReadyName, out SessionProperty sVal) ||
                sVal.Value != _allReady.Value || _session.PlayerCount != 2) return;

            if (!_loaded) LoadGame();
        }

        // CLIENT
        private void OnSessionAdded(ISession session)
        {
            // user joined a lobby
            uxmlReadyUp.CloneTree(uiDoc.rootVisualElement);
            _session = session;

            // toggle
            VisualElement toggle = uiDoc.rootVisualElement.Q<Toggle>("Toggle");
            if (toggle != null)
            {
                _readyCallback = evt => SendReady(evt.newValue);
                toggle.RegisterCallback(_readyCallback);
            }

            // set up session events
            _session.Changed += OnSessionChanged;
            SendReady(false);

            // set up host, only if is the host
            if (!session.IsHost) return;

            _host = _session.AsHost();
            _session.PlayerPropertiesChanged += OnPlayerPropertiesChanged;
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
        private async void SendReady(bool status)
        {
            try
            {
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
        private string CurrentMode()
        {
            _session.Properties.TryGetValue(ModeName, out SessionProperty sVal);
            return sVal != null ? sVal.ToString() : string.Empty;
        }

        // HOST
        private void OnPlayerPropertiesChanged()
        {
            if (_host == null) return;

            if (!AllPlayersReady()) return;

            if (CurrentMode() == string.Empty) StartGame();
            // LoadGame();
        }

        // HOST
        private async void StartGame()
        {
            try
            {
                _host.SetProperty(AllReadyName, new SessionProperty("true"));
                _host.SetProperty(ModeName, _modePlacing);
                await _host.SavePropertiesAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}