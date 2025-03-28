using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class NotesManager : MonoBehaviour
{
    [Range(1f, 10f)] public float noteScrollSpeed = 2f; // Adjusted base scroll speed multiplier

    [Range(0.1f, 5f)] public float scrollSpeedMultiplier = 1f; // Additional multiplier for faster songs

    [Range(0.1f, 10f)] public float noteScrollTime = 3f; // Adjusted time before the note should be hit

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
    [SerializeField] private TMP_Text scoreNumberText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private Animator scoreTextAnimator;
    [SerializeField] private Animator comboTextAnimator;

    [Header("Hit Detection Settings")] public float perfectHitThreshold = 0.05f; // 5% of note scroll time
    public float greatHitThreshold = 0.10f; // 10% of note scroll time
    public float goodHitThreshold = 0.15f; // 15% of note scroll time
    public float okHitThreshold = 0.20f; // 20% of note scroll time

    private int score = 0;
    private int combo = 0;

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

    // Check for missed notes
    CheckForMissedNotes();

    // Check for arrow key presses
    if (Input.GetKeyDown(KeyCode.LeftArrow))
    {
        if (!CheckForHitInLane(0))
        {
            RegisterMiss(null);
        }
    }
    if (Input.GetKeyDown(KeyCode.DownArrow))
    {
        if (!CheckForHitInLane(1))
        {
            RegisterMiss(null);
        }
    }
    if (Input.GetKeyDown(KeyCode.UpArrow))
    {
        if (!CheckForHitInLane(2))
        {
            RegisterMiss(null);
        }
    }
    if (Input.GetKeyDown(KeyCode.RightArrow))
    {
        if (!CheckForHitInLane(3))
        {
            RegisterMiss(null);
        }
    }
}

bool CheckForHitInLane(int lane)
{
    Note closestNote = null;
    float bestDistance = float.MaxValue;
    int bestIndex = -1;

    // Find the closest hittable note in this lane
    for (int i = 0; i < activeNotes.Count; i++)
    {
        Note note = activeNotes[i];
        if (note == null || note.noteData.lane != lane)
            continue;

        Transform hitPosition = GetHitPosition(lane);
        float distance = Mathf.Abs(note.transform.position.y - hitPosition.position.y);

        // Only consider notes within hit range
        float maxDistance = 20f; // Adjusted to use the new distance-based threshold

        if (distance < maxDistance && distance < bestDistance)
        {
            bestDistance = distance;
            closestNote = note;
            bestIndex = i;
        }
    }

    // Hit the closest note if found
    if (closestNote != null && bestIndex >= 0)
    {
        float hitAccuracy = CalculateHitAccuracy(closestNote);
        RegisterHit(closestNote, hitAccuracy);
        activeNotes.RemoveAt(bestIndex);
        Destroy(closestNote.gameObject);

        return true;
    }
    else
    {
        return false;
    }
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

    public void InitializeNotes(List<NoteData> noteDataList, AudioSource audioSource)
    {
        this.audioSource = audioSource;

        notes = noteDataList;
        nextNoteIndex = 0;
        activeNotes.Clear();
    }

    void SpawnNote(NoteData note)
    {
        GameObject notePrefab = GetNotePrefab(note.lane);
        if (notePrefab == null)
        {
            Debug.LogError($"Note prefab for lane {note.lane} is null");
            return;
        }

        GameObject spawnPointObj = GetSpawnPoint(note.lane);
        GameObject endPointObj = GetEndPoint(note.lane);
        Transform hitPoint = GetHitPosition(note.lane);

        if (spawnPointObj == null || endPointObj == null || hitPoint == null)
        {
            Debug.LogError($"Missing reference for lane {note.lane}");
            return;
        }

        Transform spawnPoint = spawnPointObj.transform;
        Transform endPoint = endPointObj.transform;
        Vector3 spawnPos = spawnPoint.position;
        Vector3 endPos = endPoint.position;
        Vector3 hitPos = hitPoint.position;

        // Make sure the note is instantiated as a child of the canvas
        GameObject spawnedNote = Instantiate(notePrefab, spawnPos, Quaternion.identity);
        spawnedNote.transform.SetParent(notesParentCanvas, false);

        // Ensure the note component exists and is properly initialized
        Note noteComponent = spawnedNote.GetComponent<Note>();
        if (noteComponent != null)
        {
            noteComponent.Initialize(spawnPos, endPos, hitPos, this, note);
            activeNotes.Add(noteComponent);
        }
        else
        {
            Debug.LogError("Note component missing on prefab!");
            Destroy(spawnedNote);
        }
    }
    
    private float CalculateHitAccuracy(Note note)
    {
        Transform hitPosition = GetHitPosition(note.noteData.lane);
        float distance = Mathf.Abs(note.transform.position.y - hitPosition.position.y);

        // Calculate the maximum allowed distance for a hit
        float maxDistance = noteScrollSpeed * scrollSpeedMultiplier * noteScrollTime;

        // Normalize the distance to a value between 0 and 1
        float normalizedAccuracy = distance / maxDistance;

        // Invert the value so 1.0 = perfect, 0.0 = barely hit
        return 1.0f - Mathf.Clamp01(normalizedAccuracy);
    }

    void CheckForMissedNotes()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            Note note = activeNotes[i];
            if (note == null || note.transform == null)
                continue;

            Transform hitPosition = GetHitPosition(note.noteData.lane);

            // Calculate miss threshold as a distance below the note's position
            float missThreshold = note.transform.position.y -
                                  (noteScrollSpeed * scrollSpeedMultiplier * noteScrollTime * 0.3f);

            if (note.transform.position.y < missThreshold)
            {
                RegisterMiss(note);
                activeNotes.RemoveAt(i);
                Destroy(note.gameObject);
            }
        }
    }


    void RegisterHit(Note note, float accuracy)
    {
        // Convert accuracy to a more readable percentage
        float accuracyPercent = accuracy * 100f;

        if (accuracyPercent >= 80)
        {
            scoreText.text = "PERFECT!";
            scoreTextAnimator.SetInteger("score", 4);
            score += combo == 0 ? 300 : 300 * combo;
            combo++;
        }
        else if (accuracyPercent >= 60)
        {
            scoreText.text = "GREAT!";
            scoreTextAnimator.SetInteger("score", 3);
            score += combo == 0 ? 150 : 150 * combo;
            combo++;
        }
        else if (accuracyPercent >= 30)
        {
            scoreText.text = "GOOD";
            scoreTextAnimator.SetInteger("score", 2);
            score += combo == 0 ? 100 : 100 * combo;
            combo++;
        }
        else if (accuracyPercent >= 10)
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

    void UpdateUI()
    {
        comboText.text = combo > 1 ? combo.ToString() : "";
        scoreNumberText.text = score.ToString();
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