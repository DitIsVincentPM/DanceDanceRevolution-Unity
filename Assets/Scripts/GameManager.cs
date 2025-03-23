using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager singleton;

    public enum GameState
    {
        Intro,
        SongSelection,
        Game,
        Results
    }

    public GameState currentState;

    [SerializeField] private GameObject songSelectionScreen;
    [SerializeField] private GameObject gameScreen;
    [SerializeField] private GameObject resultsScreen;

    [SerializeField] private Animator menuAnimator;

    void Start()
    {
        if (singleton == null)
        {
            singleton = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        Intro();
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        UpdateUI();

        if (newState == GameState.SongSelection)
        {
            SelectSong();
        }
    }

    private void UpdateUI()
    {
        songSelectionScreen.SetActive(currentState == GameState.SongSelection);
        gameScreen.SetActive(currentState == GameState.Game);
    }

    void Update()
    {
        if (currentState == GameState.Intro)
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                ChangeState(GameState.SongSelection);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.SongSelection)
            {
                Application.Quit();
            }
            else
            {
                ReturnToSongSelection();
            }
        }
    }

    void Intro()
    {
        ChangeState(GameState.Intro);
        StartCoroutine(PlaySoundWithDelay(SoundEffectManager.Instance.ddrMenuLineClip, 1.0f));
    }

    public void SelectSong()
    {
        menuAnimator.SetTrigger("SelectSong");
        StartCoroutine(PlaySoundWithDelay(SoundEffectManager.Instance.choseAOptionLineClip, 1.0f));
    }

    public void StartGame()
    {
        ChangeState(GameState.Game);
    }

    public void EndGame()
    {
        ChangeState(GameState.Results);
    }

    public void ReturnToSongSelection()
    {
        ChangeState(GameState.SongSelection);
    }

    private IEnumerator PlaySoundWithDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        SoundEffectManager.Instance.PlaySound(clip);
    }
}