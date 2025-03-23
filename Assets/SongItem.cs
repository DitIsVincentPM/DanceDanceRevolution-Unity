using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongItem : MonoBehaviour
{
    public Image image;
    public TMP_Text songName;
    public TMP_Text songDifficulty;
    
    private Color defaultColor = Color.white;
    private Color selectedColor = Color.yellow;

    public void Initialize(Song song)
    {
        songName.text = song.songTitle;
        image.sprite = song.songImage;
        songDifficulty.text = song.songDifficulty.ToString();
    }

    public void Select()
    {
        // Don't change scale here - it's handled by SongSelectionMenu
        if (image != null)
            image.color = selectedColor;
    }

    public void Deselect()
    {
        // Don't change scale here - it's handled by SongSelectionMenu
        if (image != null)
            image.color = defaultColor;
    }
}