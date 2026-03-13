using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class Connection : MonoBehaviour
{
    [Header ("Buttons")] 
    public Button backButton;
    public Button creategame;
    public Button joinknown;
    public Button joinrand;

    [Header ("Files")]
    public GameObject begin;
    public GameObject random;
    public GameObject known;
    public GameObject create;

    [Header("Server")]
    public GameObject server;
    private NewGame newgame;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        backButton.onClick.AddListener(() => Back());
        if (server != null)
        {
            newgame = server.GetComponent<NewGame>();
        }
            if (creategame != null)
        {
            creategame.onClick.AddListener(() => CreateConnection());
        }
        if (joinknown!= null)
        {
            joinknown.onClick.AddListener(() => KnownConnection());
        }
        if (joinrand != null)
        {
            joinrand.onClick.AddListener(() => RandomConnection());
        }
    }

    // Update is called once per frame
    private void Back()
    {
        begin.SetActive(true);
       
        gameObject.SetActive(false);
    }
    
    private void RandomConnection()
    {
        random.SetActive(true);
        gameObject.SetActive(false);
    }

    private void KnownConnection()
    {
        known.SetActive(true);
        gameObject.SetActive(false);
    }

    private void CreateConnection()
    { 
        create.SetActive(true);
        gameObject.SetActive(false);
    }
}
