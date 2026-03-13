using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UIElements;

namespace network
{
    public class SessionManager : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDoc;
        [SerializeField] private VisualTreeAsset uxmlReadyUp;
        private ISession _session;
        private IHostSession _host;
        private readonly PlayerProperty _notReady = new PlayerProperty("false");
        private readonly PlayerProperty _ready = new PlayerProperty("true");
        private readonly SessionProperty _allReady = new SessionProperty("true");
        private const string ReadyName = "ready";
        private const string AllReadyName = "allReady";
        private const string GameScene = "Game";
        private EventCallback<ChangeEvent<bool>> _readyCallback;
        
        private async void Start()
        {
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

        private void OnSessionChanged()
        {
            if (!_session.Properties.TryGetValue(AllReadyName, out SessionProperty sVal) ||
                sVal.Value != _allReady.Value || _session.PlayerCount != 2) return;
            LoadGame();
        }
        
        private void OnSessionAdded(ISession session)
        {
            uxmlReadyUp.CloneTree(uiDoc.rootVisualElement);
            _session = session;
            
            // toggle
            VisualElement toggle = uiDoc.rootVisualElement.Q<Toggle>("Toggle");
            if (toggle != null)
            {
                _readyCallback = evt => SendReady(evt.newValue);
                toggle.RegisterCallback(_readyCallback);
            }

            _session.Changed += OnSessionChanged;
            SendReady(false);
            
            if (!session.IsHost) return;
            
            _host = _session.AsHost(); 
            _session.PlayerPropertiesChanged += OnPlayerPropertiesChanged;
        }

        private void OnSessionRemoved(ISession session)
        {
            VisualElement toggle = uiDoc.rootVisualElement.Q<Toggle>("Toggle");
            toggle?.UnregisterCallback(_readyCallback);
            _readyCallback = null;
            _session.Changed -= OnSessionChanged;
            _session.PlayerPropertiesChanged -= OnPlayerPropertiesChanged;
            uiDoc.rootVisualElement.Clear();
            _session = null;
            _host = null;
        }

        private bool AllPlayersReady()
        {
            bool allReady = true;
            
            if (_session.PlayerCount != 2) return false;
            
            foreach (IReadOnlyPlayer p in _session.Players)
            {
                if (!p.Properties.TryGetValue(ReadyName, out PlayerProperty pVal) ||
                    pVal.Value != "true")
                {
                    allReady = false;
                    break;
                }
            }

            return allReady;
        }

        private void OnPlayerPropertiesChanged()
        {
            if (_host == null) return;

            if (!AllPlayersReady()) return;
            
            StartGame();
            // LoadGame();
        }

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

        private async void StartGame()
        {
            try
            {
                _host.SetProperty(AllReadyName, new SessionProperty("true"));
                await _host.SavePropertiesAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void LoadGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameScene);
        }
    }
}
