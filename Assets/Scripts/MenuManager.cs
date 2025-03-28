using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager singleton;
    
    void Start()
    {
        if (singleton == null) singleton = this; else if (singleton != this) Destroy(gameObject);
        backgroundImage.sprite = backgroundSprite;
        
        // Make sure these are disabled. They will be enabled when needed
        songSelectionScreen.SetActive(false);
        gameScreen.SetActive(false);
    }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject songSelectionScreen;
    [SerializeField] private GameObject gameScreen;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite backgroundSprite;
    [ShowInInspector] public Animator menuAnimator;

    public void OpenSongSelectionScreen()
    {
        songSelectionScreen.SetActive(true);
    }

    public void SongLoaded()
    {
        menuAnimator.SetTrigger("NoAnimation");

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
}