using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager singleton;

    private string currentScreen = "Type";

    void Start()
    {
        if(singleton == null) singleton = this; else if(singleton != this) Destroy(gameObject);
        backgroundImage.sprite = backgroundSprite;
    }
    
    [SerializeField] private ConnectingScreen connectingScreen;
    [SerializeField] private GameObject connectingTypeScreen;
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private GameObject songSelectionScreen;
    [SerializeField] private GameObject gameScreen;

    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite backgroundSprite;

    public void ConnectionSuccessful()
    {
        audioSource.enabled = true;
        audioSource.Play();
        
        OpenSongSelectionScreen();
    }

    public void OpenSongSelectionScreen()
    {
        currentScreen = "Song";
        connectingScreen.gameObject.SetActive(false);
        songSelectionScreen.SetActive(true);
    }

    public void SongLoaded()
    {
        currentScreen = "Game";
        
        connectingScreen.gameObject.SetActive(false);
        songSelectionScreen.SetActive(false);
        gameScreen.SetActive(true);
        audioSource.Stop();
        backgroundImage.sprite = null;
    }

    public void SongFinished()
    {
        audioSource.Play();
        backgroundImage.sprite = backgroundSprite;
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
