using UnityEngine;

public enum SongDifficulty
{
    Beginner,
    Expierinced,
    Expert,
    Hardcore
}

[CreateAssetMenu(fileName = "NewSong", menuName = "Rhythm Game/Song")]
public class Song : ScriptableObject
{
    public string songTitle;
    public string artist;
    public AudioClip songClip;
    public Sprite songImage;
    public float bpm;
    public SongDifficulty songDifficulty;
}