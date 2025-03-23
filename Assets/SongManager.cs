using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Video;

[System.Serializable]
public class NoteData
{
    public float time;
    public int lane;
    public string type;
    public float holdDuration;
}

[System.Serializable]
public class NoteChart
{
    public List<NoteData> notes;
}

public class SongManager : MonoBehaviour
{
    public static SongManager instance;
    
    [HideInInspector]
    public string songName = "Butterfly"; // Set dynamically in-game
    [HideInInspector]
    public Song songData; // ScriptableObject reference
    [HideInInspector]
    public List<NoteData> notes;
    [HideInInspector]
    public Sprite coverArt; // Cover art for UI display

    private AudioSource audioSource;
    private NotesManager notesManager;
    public VideoPlayer videoPlayer;

    [HideInInspector]
    public bool isReady = false; // Ensures everything is loaded before starting

    void Start()
    {
        instance = this;
        
        notesManager = FindObjectOfType<NotesManager>();
    }

    public void LoadSong(string songName)
    {
        string songPath = $"Songs/{songName}/"; // Dynamic folder path

        // Load ScriptableObject
        songData = Resources.Load<Song>($"{songPath}Data");
        if (songData == null)
        {
            Debug.LogError($"SongData for {songName} not found in {songPath}");
            return;
        }

        // Load JSON Notes file
        TextAsset noteJsonFile = Resources.Load<TextAsset>($"{songPath}Notes");
        if (noteJsonFile == null)
        {
            Debug.LogError($"Notes.json for {songName} not found in {songPath}");
            return;
        }

        // Parse JSON notes
        NoteChart chart = JsonUtility.FromJson<NoteChart>(noteJsonFile.text);
        notes = chart.notes;
        Debug.Log($"Loaded {notes.Count} notes for {songName}!");

        // Load & Play Song.mp3
        AudioClip songClip = Resources.Load<AudioClip>($"{songPath}Song");
        if (songClip == null)
        {
            Debug.LogError($"Song.mp3 for {songName} not found in {songPath}");
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = songClip;
        audioSource.playOnAwake = false; // Ensures song doesn't start immediately

        // Load Cover Art
        coverArt = Resources.Load<Sprite>($"{songPath}Cover");
        if (coverArt != null)
        {
            Debug.Log($"Loaded cover art for {songName}");
        }

        videoPlayer.Stop();
        // Load & Play Song.mp3
        VideoClip videoClip = Resources.Load<VideoClip>($"{songPath}Clip");
        if (videoClip == null)
        {
            Debug.LogError($"Clip for {songName} not found in {songPath}");
        }
        else
        {
            videoPlayer.clip = videoClip;
        }
        
        isReady = true; // Song is fully loaded and ready to start
        StartSong();
    }

    public void StartSong()
    {
        if (!isReady)
        {
            Debug.LogError("Song is not ready! Load it first.");
            return;
        }

        MenuManager.singleton.SongLoaded();

        Debug.Log($"Starting song: {songName}");
        notesManager.InitializeNotes(notes, audioSource);
        
        videoPlayer.Play();
        audioSource.Play();
    }
}
