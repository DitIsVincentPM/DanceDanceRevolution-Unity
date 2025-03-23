using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public class SongSelectionMenu : MonoBehaviour
{
    [SerializeField] private GameObject songItemPrefab;
    [SerializeField] private Transform songListContent;
    [SerializeField] private ScrollRect scrollRect;

    // Configuration for selection scale and scrolling
    [SerializeField] private float selectedItemScale = 1.05f;
    [SerializeField] private float scrollPaddingFactor = 0.15f;

    private List<Song> songs = new List<Song>();
    private List<GameObject> songItems = new List<GameObject>();
    private int selectedIndex = 0;
    private bool isActive = false;

    void Start()
    {
        LoadSongsFromResources();
        PopulateSongList();
        UpdateSelection();
    }

    void OnEnable()
    {
        isActive = true;
    }

    void OnDisable()
    {
        isActive = false;
    }

    void Update()
    {
        if (!isActive) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            OnNavigateUp();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnNavigateDown();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            OnSelect();
        }
    }

    private void LoadSongsFromResources()
    {
        songs.Clear();

        // Load all Song scriptable objects from the Resources/Songs folder
        Song[] loadedSongs = Resources.LoadAll<Song>("Songs");

        foreach (Song song in loadedSongs)
        {
            // Make sure we have only one instance of each song
            if (!songs.Contains(song))
            {
                songs.Add(song);
                Debug.Log($"Loaded song: {song.songTitle}");
            }
        }
    }

    private void PopulateSongList()
    {
        // Clear existing items
        foreach (GameObject item in songItems)
        {
            Destroy(item);
        }
        songItems.Clear();

        // Create new items
        foreach (Song song in songs)
        {
            GameObject itemObj = Instantiate(songItemPrefab, songListContent);
            SongItem item = itemObj.GetComponent<SongItem>();

            if (item != null)
            {
                item.Initialize(song);
            }

            songItems.Add(itemObj);
        }
    }

    private void UpdateSelection()
    {
        // Update visual selection for all items
        for (int i = 0; i < songItems.Count; i++)
        {
            SongItem item = songItems[i].GetComponent<SongItem>();
            if (item != null)
            {
                if (i == selectedIndex)
                {
                    item.transform.localScale = Vector3.one * selectedItemScale;
                }
                else
                {
                    item.transform.localScale = Vector3.one;
                }
            }
        }
    }

    private void OnNavigateUp()
    {
        ChangeSelection(-1);
        SoundEffectManager.Instance.PlaySelectSound();
    }

    private void OnNavigateDown()
    {
        ChangeSelection(1);
        SoundEffectManager.Instance.PlaySelectSound();
    }

    private void OnSelect()
    {
        // Ensure the game object is active before starting the coroutine
        gameObject.SetActive(true);
        StartSelectedSong();
    }

    private void ChangeSelection(int direction)
    {
        int previousIndex = selectedIndex;
        selectedIndex = Mathf.Clamp(selectedIndex + direction, 0, songs.Count - 1);

        // Only update and scroll if the selection actually changed
        if (previousIndex != selectedIndex)
        {
            UpdateSelection();
            ScrollToSelected();
        }
    }

    private void ScrollToSelected()
    {
        if (scrollRect != null && songItems.Count > 0 && selectedIndex >= 0 && selectedIndex < songItems.Count)
        {
            // Calculate position based on selected index
            float itemHeight = (songItems[0].transform as RectTransform).rect.height;
            float viewportHeight = scrollRect.viewport.rect.height;
            float contentHeight = (songListContent as RectTransform).rect.height;

            // Get the actual position of the selected item
            float itemPosition = (songItems[selectedIndex].transform as RectTransform).anchoredPosition.y;

            // Calculate normalized position (0-1)
            float normalizedPosition = 1f - (itemPosition / (contentHeight - viewportHeight));

            // Add padding to center the item better
            float centeringOffset = (itemHeight * 0.5f) / contentHeight;
            normalizedPosition += centeringOffset;

            // Add additional fine-tuning padding
            normalizedPosition += scrollPaddingFactor * (selectedIndex - songs.Count / 2) / songs.Count;

            // Ensure it stays within valid range
            normalizedPosition = Mathf.Clamp01(normalizedPosition);

            // Smooth scroll to position
            StartCoroutine(SmoothScrollToPosition(normalizedPosition));
        }
    }

    private System.Collections.IEnumerator SmoothScrollToPosition(float targetPosition)
    {
        float duration = 0.3f;
        float elapsedTime = 0f;
        float startPosition = scrollRect.verticalNormalizedPosition;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, smoothT);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = targetPosition;
    }

    private void StartSelectedSong()
    {
        if (songs.Count > 0 && selectedIndex >= 0 && selectedIndex < songs.Count)
        {
            Song selectedSong = songs[selectedIndex];
            Debug.Log($"Starting song: {selectedSong.songTitle}");

            // Play sound effect
            SoundEffectManager.Instance.PlaySound(SoundEffectManager.Instance.audioSelectClip, 1.0f);

            try
            {
                // Change the game state
                GameManager.singleton.StartGame();

                // Deactivate the song selection screen
                gameObject.SetActive(false);

                // Small delay before loading the song
                if (SongManager.instance != null)
                {
                    SongManager.instance.songName = selectedSong.songTitle;
                    SongManager.instance.LoadSong(selectedSong.songTitle);
                }
                else
                {
                    Debug.LogError("SongManager instance is null!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error starting song: {e.Message}");
            }
        }
    }

    private System.Collections.IEnumerator LoadSongWithDelay(string songTitle, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (SongManager.instance != null)
        {
            SongManager.instance.songName = songTitle;
            SongManager.instance.LoadSong(songTitle);
        }
        else
        {
            Debug.LogError("SongManager instance is null!");
        }
    }
}