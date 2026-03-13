using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Multiplayer;
using System.Linq.Expressions;
using UnityEngine.UI;
// most of this is taken from Code Monkey https://www.youtube.com/watch?v=-KDlEBfCBiU
public class NewGame : MonoBehaviour
{
    public GameObject Text;
    private TextMeshProUGUI codeinput;
    public Lobby hostlobby;
    public Lobby joinedLobby;
    public Button startlobby;
    public Button endlobby;
    public TMP_InputField input;
    private float heartbeat;
    private string playerName;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        codeinput = Text.GetComponent<TextMeshProUGUI>(); 
        startlobby.onClick.AddListener(() => CreateLobby(true));
        endlobby.onClick.AddListener(() => DeleteLobby());
        input.onSubmit.AddListener(JoinLobby);

        //needs to await or else the code will freeze for server responses
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async void CreateLobby(bool privacy)
    {
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayers = 2;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions()
            {
                //true for custom, false for random
                IsPrivate = privacy
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);
            hostlobby = lobby;
            joinedLobby = lobby;
            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers);
            codeinput.text = "Code: " + lobby.LobbyCode;
        }
        catch (LobbyServiceException ex) 
        {
            Debug.Log(ex);
        }
    }
    private async void lifeline()
    {
        if (hostlobby != null)
        {
            heartbeat -= Time.deltaTime;
            if (heartbeat < 0f)
            {
                float heartbeatmax = 15f;
                heartbeat = heartbeatmax;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostlobby.Id);
            }
        }
    }
    private async void quickjoin()
    {
        await LobbyService.Instance.QuickJoinLobbyAsync();
    }
    private async void JoinLobby(string lobbyCode)
    {
        try
        {
            await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            Debug.Log("Joined Lobby with code " + lobbyCode);
            await LobbyService.Instance.QueryLobbiesAsync();
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }
    private async void DeleteLobby()
    {
        try{
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
            hostlobby = null;
            Debug.Log("Deleted");
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
        }
    }
    public async void Authenticate(string playerName)
    {
        this.playerName = playerName;
        InitializationOptions initizliationOptions = new InitializationOptions();
        initizliationOptions.SetProfile(playerName);
        await UnityServices.InitializeAsync(initizliationOptions);
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in!" + AuthenticationService.Instance.PlayerId);
        };
    }
    void Update()
    {
        //if inactive for 30 seconds, server will become inactive and shut down
        lifeline();
    }
}
