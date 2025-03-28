using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class SongSelectionMenu : MonoBehaviour
{
    [SerializeField] private GameObject songItemPrefab;
    [SerializeField] private Transform songListContent;
    [SerializeField] private ScrollRect scrollRect;

    [SerializeField] private float selectedItemScale = 1.05f;
    [SerializeField] private float scrollSpeed = 10f;

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
        
        if (Input.GetKeyDown(KeyCode.LeftArrow) && Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnSelect();
            return;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            OnNavigateLeft();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnNavigateRight();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            OnNavigateUp();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            OnNavigateDown();
        }
    }

    private void LoadSongsFromResources()
    {
        songs.Clear();
        Song[] loadedSongs = Resources.LoadAll<Song>("Songs");
        foreach (Song song in loadedSongs)
        {
            if (!songs.Contains(song))
            {
                songs.Add(song);
                Debug.Log($"Loaded song: {song.songTitle}");
            }
        }
    }

    private void PopulateSongList()
    {
        foreach (GameObject item in songItems)
        {
            Destroy(item);
        }
        songItems.Clear();

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

        LayoutRebuilder.ForceRebuildLayoutImmediate(songListContent as RectTransform);
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < songItems.Count; i++)
        {
            SongItem item = songItems[i].GetComponent<SongItem>();
            if (item != null)
            {
                item.transform.localScale = (i == selectedIndex) ? Vector3.one * selectedItemScale : Vector3.one;
            }
        }
        CenterOnSelectedItem();
    }

    private void OnNavigateLeft()
    {
        ChangeSelection(-1);
    }

    private void OnNavigateRight()
    {
        ChangeSelection(1);
    }

    private void OnNavigateUp()
    {
        if(selectedIndex < 3) return;
        ChangeSelection(-3); // Move up a row
    }

    private void OnNavigateDown()
    {
        if(selectedIndex > songs.Count - 4) return;
        ChangeSelection(3); // Move down a row
    }

    private void OnSelect()
    {
        StartSelectedSong();
    }

    private void ChangeSelection(int direction)
    {
        int previousIndex = selectedIndex;
        selectedIndex = Mathf.Clamp(selectedIndex + direction, 0, songs.Count - 1);

        if (previousIndex != selectedIndex)
        {
            UpdateSelection();
        }
    }

    private void CenterOnSelectedItem()
    {
        if (scrollRect != null && songItems.Count > 0 && selectedIndex >= 0 && selectedIndex < songItems.Count)
        {
            RectTransform selectedItemRect = songItems[selectedIndex].GetComponent<RectTransform>();
            RectTransform contentRect = songListContent.GetComponent<RectTransform>();
            RectTransform viewportRect = scrollRect.viewport;

            // Calculate the position of the selected item in the content's local space
            Vector2 itemLocalPosition = selectedItemRect.localPosition;

            // Calculate the offset needed to center the selected item
            float offsetY = -itemLocalPosition.y - (viewportRect.rect.height / 2) + (selectedItemRect.rect.height / 2);

            // Clamp the offset to ensure the content doesn't scroll out of bounds
            float clampedY = Mathf.Clamp(offsetY, 0, contentRect.rect.height - viewportRect.rect.height);

            // Start the smooth scrolling coroutine
            StartCoroutine(SmoothScrollToPosition(clampedY));
        }
    }

    private IEnumerator SmoothScrollToPosition(float targetY)
    {
        RectTransform contentRect = songListContent.GetComponent<RectTransform>();
        float startY = contentRect.anchoredPosition.y;
        float elapsedTime = 0f;
        float duration = 0.3f; // Adjust the duration as needed

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newY = Mathf.Lerp(startY, targetY, elapsedTime / duration);
            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, newY);
            yield return null;
        }

        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, targetY);
    }
    
    private void StartSelectedSong()
    {
        if (songs.Count > 0 && selectedIndex >= 0 && selectedIndex < songs.Count)
        {
            Song selectedSong = songs[selectedIndex];
            Debug.Log($"Starting song: {selectedSong.songTitle}");

            try
            {
                GameManager.singleton.StartGame();
                gameObject.SetActive(false);

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
}
