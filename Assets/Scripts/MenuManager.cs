using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager singleton;

    private string currentScreen = "Type";

    void Start()
    {
        if(singleton == null) singleton = this; else if(singleton != this) Destroy(gameObject);
    }
    
    [SerializeField] private ConnectingScreen connectingScreen;
    [SerializeField] private GameObject connectingTypeScreen;
    [SerializeField] private AudioSource audioSource;

    public void ConnectionSuccessful()
    {
        SongManager.instance.LoadSong("over the top");
        audioSource.enabled = true;
        audioSource.Play();
        SongManager.instance.StartSong();
    }
    
    public void StartConnectingScreen()
    {
        if (currentScreen == "Type")
        {
            connectingTypeScreen.SetActive(false);
        }
        
        connectingScreen.StartConnecting();
    }

    public void ConnectedScreen()
    {
        connectingScreen.Connected();
    }

    public void ConnectingFailedScreen()
    {
        connectingScreen.ResetConnectingScreen();
        connectingTypeScreen.SetActive(true);
    }
}
