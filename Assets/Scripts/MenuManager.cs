using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager singleton;

    public string currentScreen = "Type";

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
    
    [ShowInInspector] public Animator menuAnimator;

    public void ConnectionSuccessful()
    {
        audioSource.enabled = true;
        audioSource.Play();
        GameManager.singleton.SelectSong();
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
        menuAnimator.SetTrigger("NoAnimation");

        connectingScreen.gameObject.SetActive(false);
        songSelectionScreen.SetActive(false);
        gameScreen.SetActive(true);
        audioSource.Stop();
        backgroundImage.sprite = null;
    }

    public void SongFinished()
    {
        menuAnimator.SetTrigger("NoAnimation");

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
