using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongItem : MonoBehaviour
{
    public Image image;
    public TMP_Text songName;
    public TMP_Text songDifficulty;

    public void Initialize(Song song)
    {
        songName.text = song.songTitle;
        image.sprite = song.songImage;
        songDifficulty.text = song.songDifficulty.ToString();
    }

    public void Select()
    {
        this.gameObject.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
    }

    public void Deselect()
    {
        this.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
    }
}
