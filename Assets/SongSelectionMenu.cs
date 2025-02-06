using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;

public class SongSelectionMenu : MonoBehaviour
{
    public Transform songListContainer;
    public GameObject songItemPrefab;
    public TMP_Text songTitleText;
    public TMP_Text bpmText;
    public Image songArtwork;
    public VideoPlayer videoPlayer; // Video player for background videos
    public ScrollRect scrollRect; // Reference to the ScrollRect component

    private List<Song> songs = new List<Song>();
    private List<GameObject> songItems = new List<GameObject>();
    private int selectedIndex = 0;

    private bool wasUpPressed = false;
    private bool wasDownPressed = false;
    private bool wasSelectPressed = false;

    void Start()
    {
        LoadSongsFromResources();
        PopulateSongList();
        UpdateSelection();
    }

    void Update()
    {
        bool upPressed = InputManager.singleton._left;
        bool downPressed = InputManager.singleton._right;
        bool selectPressed = InputManager.singleton._up;

        if (upPressed && !wasUpPressed) ChangeSelection(-1);
        if (downPressed && !wasDownPressed) ChangeSelection(1);
        if (selectPressed && !wasSelectPressed) StartSelectedSong();

        wasUpPressed = upPressed;
        wasDownPressed = downPressed;
        wasSelectPressed = selectPressed;
    }

    void StartSelectedSong()
    {
        Song selectedSong = songs[selectedIndex];
        SongManager.instance.LoadSong(selectedSong.songTitle);
        SongManager.instance.StartSong();
    }

    void LoadSongsFromResources()
    {
        songs.Clear();
        string[] songFolders = Directory.GetDirectories(Path.Combine(Application.dataPath, "Resources/Songs"));

        foreach (string folder in songFolders)
        {
            string folderName = new DirectoryInfo(folder).Name;
            string resourcePath = "Songs/" + folderName; // Path relative to Resources/

            Song songData = Resources.Load<Song>(resourcePath + "/Data");

            if (songData != null)
            {
                songData.songImage = Resources.Load<Sprite>(resourcePath + "/Cover");
                songs.Add(songData);
            }
        }
    }

    void PopulateSongList()
    {
        foreach (Transform child in songListContainer)
        {
            Destroy(child.gameObject);
        }
        songItems.Clear();

        foreach (Song song in songs)
        {
            GameObject songItem = Instantiate(songItemPrefab, songListContainer);
            songItem.GetComponentInChildren<TMP_Text>().text = song.songTitle;
            songItem.transform.GetChild(1).GetComponent<Image>().sprite  = song.songImage;
            songItems.Add(songItem);
        }
    }

    void ChangeSelection(int direction)
    {
        selectedIndex = Mathf.Clamp(selectedIndex + direction, 0, songs.Count - 1);
        UpdateSelection();
        ScrollToSelected();
    }

    void UpdateSelection()
    {
        for (int i = 0; i < songItems.Count; i++)
        {
            songItems[i].GetComponentInChildren<TMP_Text>().color = (i == selectedIndex) ? Color.yellow : Color.white;
        }

        Song selectedSong = songs[selectedIndex];
        songTitleText.text = selectedSong.songTitle;
        bpmText.text = "BPM: " + selectedSong.bpm;
        songArtwork.sprite = selectedSong.songImage;

        string videoPath = Path.Combine(Application.streamingAssetsPath, "Songs", selectedSong.name, "Video.mp4");

        if (File.Exists(videoPath))
        {
            videoPlayer.url = "file://" + videoPath;
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.Stop();
        }
    }

    void ScrollToSelected()
    {
        float itemHeight = songItems[0].GetComponent<RectTransform>().rect.height;
        float containerHeight = songListContainer.GetComponent<RectTransform>().rect.height;
        float contentHeight = songItems.Count * itemHeight;

        float scrollPosition = Mathf.Clamp01((selectedIndex * itemHeight - containerHeight / 2) / (contentHeight - containerHeight));
        scrollRect.verticalNormalizedPosition = 1 - scrollPosition;
    }
}