using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class NotesManager : MonoBehaviour
{
    public float noteScrollSpeed = 5f;  // Base scroll speed multiplier
    public float scrollSpeedMultiplier = 1f;  // Additional multiplier for faster songs
    public RectTransform notesParentCanvas;

    public Transform hitPositionLeft;
    public Transform hitPositionDown;
    public Transform hitPositionUp;
    public Transform hitPositionRight;

    private List<NoteData> notes = new List<NoteData>();
    private List<Note> activeNotes = new List<Note>();
    private int nextNoteIndex = 0;
    public AudioSource audioSource;
    private float songStartTime;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private Animator scoreTextAnimator;
    [SerializeField] private Animator comboTextAnimator;

    private int score = 0;
    private int combo = 0;

    public void InitializeNotes(List<NoteData> loadedNotes, AudioSource source)
    {
        notes = loadedNotes;
        audioSource = source;
        nextNoteIndex = 0;
        songStartTime = Time.time;
        score = 0;
        combo = 0;
        UpdateUI();
        Debug.Log("Notes Manager initialized with loaded song.");
    }

    void Update()
    {
        if (audioSource == null || !audioSource.isPlaying) return;

        // Check for F6 key press to reset the song
        if (Input.GetKeyDown(KeyCode.F6))
        {
            ResetSong();
        }

        float songTime = audioSource.time;

        // Spawn notes exactly "noteScrollTime" before they should be hit
        while (nextNoteIndex < notes.Count && notes[nextNoteIndex].time - songTime <= noteScrollTime)
        {
            SpawnNote(notes[nextNoteIndex]);
            nextNoteIndex++;
        }

        // Scroll notes down based on scroll speed
        ScrollNotes();

        CheckForHits();
    }

    public float noteScrollTime = 0f;

    void ScrollNotes()
    {
        foreach (Note note in activeNotes)
        {
            if (note != null && note.transform != null)
            {
                // Move notes down the screen at the speed of noteScrollSpeed and scrollSpeedMultiplier
                note.transform.position += Vector3.down * noteScrollSpeed * scrollSpeedMultiplier * Time.deltaTime;
            }
        }
    }

    void SpawnNote(NoteData note)
    {
        GameObject notePrefab = GetNotePrefab(note.lane);
        if (notePrefab == null) return;

        Transform spawnPoint = GetSpawnPoint(note.lane).transform;
        Transform endPoint = GetEndPoint(note.lane).transform;
        Transform hitPoint = GetHitPosition(note.lane);

        if (spawnPoint == null || endPoint == null || hitPoint == null) return;
        Vector3 spawnPos = spawnPoint.position;
        Vector3 endPos = endPoint.position;
        Vector3 hitPos = hitPoint.position;

        GameObject spawnedNote = Instantiate(notePrefab, spawnPos, Quaternion.identity, notesParentCanvas);
        spawnedNote.GetComponent<Note>().Initialize(spawnPos, endPos, hitPos, this, note);

        activeNotes.Add(spawnedNote.GetComponent<Note>());
    }

    void CheckForHits()
    {
        bool up = InputManager.singleton._up;
        bool down = InputManager.singleton._down;
        bool left = InputManager.singleton._left;
        bool right = InputManager.singleton._right;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            Note note = activeNotes[i];

            if (note.CanBeHit())
            {
                bool inputMatched = false;

                if (note.noteData.lane == 0 && left)
                {
                    inputMatched = true;
                }
                else if (note.noteData.lane == 1 && down)
                {
                    inputMatched = true;
                }
                else if (note.noteData.lane == 2 && up)
                {
                    inputMatched = true;
                }
                else if (note.noteData.lane == 3 && right)
                {
                    inputMatched = true;
                }

                if (inputMatched)
                {
                    float accuracy = Mathf.Abs(note.transform.position.y - GetHitPosition(note.noteData.lane).position.y);
                    RegisterHit(note, accuracy);
                    activeNotes.RemoveAt(i);
                    Destroy(note.gameObject);
                }
            }
            else if (note != null && note.transform != null && note.transform.position.y < -Screen.height)
            {
                activeNotes.RemoveAt(i);
                Destroy(note.gameObject);
            }
        }
    }

    public void RegisterHit(Note note, float accuracy)
    {
        if (accuracy < 10f)
        {
            scoreText.text = "PERFECT!";
            scoreTextAnimator.SetInteger("score", 4);
            score += combo == 0 ? 300 : 300 * combo;
            combo++;
        }
        else if (accuracy < 20f)
        {
            scoreText.text = "GREAT!";
            scoreTextAnimator.SetInteger("score", 3);
            score += combo == 0 ? 150 : 150 * combo;
            combo++;
        }
        else if (accuracy < 30f)
        {
            scoreText.text = "OK";
            scoreTextAnimator.SetInteger("score", 2);
            score += combo == 0 ? 50 : 50 * combo;
            combo++;
        }
        else
        {
            RegisterMiss(note);
            return;
        }

        scoreTextAnimator.SetTrigger("NewHit");

        if (combo == 2)
        {
            comboTextAnimator.SetTrigger("NewCombo");
        }
        else if (combo > 2)
        {
            comboTextAnimator.SetTrigger("NumberUp");
        }

        UpdateUI();
    }

    public void RegisterMiss(Note note)
    {
        scoreText.text = "MISS";
        scoreTextAnimator.SetInteger("score", 1);
        scoreTextAnimator.SetTrigger("NewHit");
        combo = 0;
        comboTextAnimator.SetTrigger("ComboLost");
        UpdateUI();
    }

    void UpdateUI()
    {
        comboText.text = combo > 1 ? combo.ToString() : "";
    }

    GameObject GetNotePrefab(int lane)
    {
        switch (lane)
        {
            case 0: return ArrowSpawner.instance.prefabLeftArrow;
            case 1: return ArrowSpawner.instance.prefabDownArrow;
            case 2: return ArrowSpawner.instance.prefabUpArrow;
            case 3: return ArrowSpawner.instance.prefabRightArrow;
            default: return null;
        }
    }

    Transform GetHitPosition(int lane)
    {
        switch (lane)
        {
            case 0: return hitPositionLeft;
            case 1: return hitPositionDown;
            case 2: return hitPositionUp;
            case 3: return hitPositionRight;
            default: return null;
        }
    }

    GameObject GetSpawnPoint(int lane)
    {
        switch (lane)
        {
            case 0: return ArrowSpawner.instance.spawnLeftArrow.gameObject;
            case 1: return ArrowSpawner.instance.spawnDownArrow.gameObject;
            case 2: return ArrowSpawner.instance.spawnUpArrow.gameObject;
            case 3: return ArrowSpawner.instance.spawnRightArrow.gameObject;
            default: return null;
        }
    }

    GameObject GetEndPoint(int lane)
    {
        switch (lane)
        {
            case 0: return ArrowSpawner.instance.endLeftArrow.gameObject;
            case 1: return ArrowSpawner.instance.endDownArrow.gameObject;
            case 2: return ArrowSpawner.instance.endUpArrow.gameObject;
            case 3: return ArrowSpawner.instance.endRightArrow.gameObject;
            default: return null;
        }
    }

    void ResetSong()
    {
        // Stop the audio
        audioSource.Stop();

        // Clear active notes
        foreach (Note note in activeNotes)
        {
            Destroy(note.gameObject);
        }
        activeNotes.Clear();

        // Reset song time and notes
        nextNoteIndex = 0;
        songStartTime = Time.time;
        audioSource.Play();
        Debug.Log("Song reset.");
    }
}