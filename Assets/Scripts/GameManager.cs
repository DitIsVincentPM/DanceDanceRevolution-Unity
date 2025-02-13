using UnityEngine;
    using UnityEngine.InputSystem;
    using System.Collections;
    
    public class GameManager : MonoBehaviour
    {
        public static GameManager singleton;
    
        public enum GameState
        {
            Intro,
            ChoseConnection,
            Connecting,
            MainMenu,
            PlayStyle,
            SongSelection,
            Game,
            Results
        }
    
        public GameState currentState;
    
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject connectingScreen;
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
        }
    
        private void UpdateUI()
        {
    
        }
    
        void Update()
        {
            if (currentState == GameState.Intro)
            {
                if (Keyboard.current.anyKey.wasPressedThisFrame)
                {
                    ChoseConnection();
                }
            }
    
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentState == GameState.MainMenu)
                {
                    Application.Quit();
                }
                else
                {
                    ReturnToMainMenu();
                }
            }
        }
    
        void Intro()
        {
            ChangeState(GameState.Intro);
            StartCoroutine(PlaySoundWithDelay(SoundEffectManager.Instance.ddrMenuLineClip, 1.0f));
        }
    
        void ChoseConnection()
        {
            ChangeState(GameState.ChoseConnection);
            menuAnimator.SetTrigger("Connection");
            StartCoroutine(PlaySoundWithDelay(SoundEffectManager.Instance.choseAOptionLineClip, 1.0f));
        }
    
        public void StartConnecting()
        {
            ChangeState(GameState.Connecting);
        }
    
        public void ConnectionSuccessful()
        {
            ChangeState(GameState.SongSelection);
        }
    
        public void StartGame()
        {
            ChangeState(GameState.Game);
        }
    
        public void EndGame()
        {
            ChangeState(GameState.Results);
        }
    
        public void ReturnToMainMenu()
        {
            ChangeState(GameState.MainMenu);
        }
    
        private IEnumerator PlaySoundWithDelay(AudioClip clip, float delay)
        {
            yield return new WaitForSeconds(delay);
            SoundEffectManager.Instance.PlaySound(clip);
        }
    }