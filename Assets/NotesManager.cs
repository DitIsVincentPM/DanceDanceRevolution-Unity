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
    public float noteScrollTime = 0f;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private Animator scoreTextAnimator;
    [SerializeField] private Animator comboTextAnimator;
    
    [Header("Hit Detection Settings")]
    public float perfectHitThreshold = 0.05f; // 5% of note scroll time
    public float greatHitThreshold = 0.10f;   // 10% of note scroll time
    public float goodHitThreshold = 0.15f;    // 15% of note scroll time
    public float okHitThreshold = 0.20f;      // 20% of note scroll time

    private int score = 0;
    private int combo = 0;

    void OnEnable()
    {
        // Subscribe to input events
        if (InputManager.singleton != null)
        {
            InputManager.singleton.OnLeftPressed += OnLeftPressed;
            InputManager.singleton.OnDownPressed += OnDownPressed;
            InputManager.singleton.OnUpPressed += OnUpPressed;
            InputManager.singleton.OnRightPressed += OnRightPressed;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from input events
        if (InputManager.singleton != null)
        {
            InputManager.singleton.OnLeftPressed -= OnLeftPressed;
            InputManager.singleton.OnDownPressed -= OnDownPressed;
            InputManager.singleton.OnUpPressed -= OnUpPressed;
            InputManager.singleton.OnRightPressed -= OnRightPressed;
        }
    }

    // Input event handlers
    private void OnLeftPressed() => CheckForHitInLane(0);
    private void OnDownPressed() => CheckForHitInLane(1);
    private void OnUpPressed() => CheckForHitInLane(2);
    private void OnRightPressed() => CheckForHitInLane(3);

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
    
    void OnDrawGizmos()
    {
        // Draw debug lines between spawn and hit positions
        if (ArrowSpawner.instance != null)
        {
            // Left arrow
            if (ArrowSpawner.instance.spawnLeftArrow != null && hitPositionLeft != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(ArrowSpawner.instance.spawnLeftArrow.position, hitPositionLeft.position);
            }
        
            // Down arrow
            if (ArrowSpawner.instance.spawnDownArrow != null && hitPositionDown != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(ArrowSpawner.instance.spawnDownArrow.position, hitPositionDown.position);
            }
        
            // Up arrow
            if (ArrowSpawner.instance.spawnUpArrow != null && hitPositionUp != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(ArrowSpawner.instance.spawnUpArrow.position, hitPositionUp.position);
            }
        
            // Right arrow
            if (ArrowSpawner.instance.spawnRightArrow != null && hitPositionRight != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(ArrowSpawner.instance.spawnRightArrow.position, hitPositionRight.position);
            }
        }
    }
    
    void Start()
    {
        // Check prefab references
        Debug.Assert(ArrowSpawner.instance.prefabLeftArrow != null, "Left arrow prefab is not assigned.");
        Debug.Assert(ArrowSpawner.instance.prefabRightArrow != null, "Right arrow prefab is not assigned.");
        Debug.Assert(ArrowSpawner.instance.prefabUpArrow != null, "Up arrow prefab is not assigned.");
        Debug.Assert(ArrowSpawner.instance.prefabDownArrow != null, "Down arrow prefab is not assigned.");

        // Check spawn points
        Debug.Assert(ArrowSpawner.instance.spawnLeftArrow != null, "Left arrow spawn point is not assigned.");
        Debug.Assert(ArrowSpawner.instance.spawnRightArrow != null, "Right arrow spawn point is not assigned.");
        Debug.Assert(ArrowSpawner.instance.spawnUpArrow != null, "Up arrow spawn point is not assigned.");
        Debug.Assert(ArrowSpawner.instance.spawnDownArrow != null, "Down arrow spawn point is not assigned.");

        // Check end points
        Debug.Assert(ArrowSpawner.instance.endLeftArrow != null, "Left arrow end point is not assigned.");
        Debug.Assert(ArrowSpawner.instance.endRightArrow != null, "Right arrow end point is not assigned.");
        Debug.Assert(ArrowSpawner.instance.endUpArrow != null, "Up arrow end point is not assigned.");
        Debug.Assert(ArrowSpawner.instance.endDownArrow != null, "Down arrow end point is not assigned.");

        // Check canvas setup
        Debug.Assert(notesParentCanvas != null, "Notes parent canvas is not assigned.");

        // Check audio source
        Debug.Assert(audioSource != null, "Audio source is not assigned.");
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

        // Check for missed notes
        CheckForMissedNotes();
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
    
// Revert back to the original SpawnNote method - the renamed method might be causing issues
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
            Debug.Log($"Note spawned for lane {note.lane} at time {note.time}, position: {spawnPos}");
        }
        else
        {
            Debug.LogError("Note component missing on prefab!");
            Destroy(spawnedNote);
        }
    }
    void SpawnNoteIfNeeded(NoteData note)
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


    void CheckForHitInLane(int lane)
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
            float maxDistance = noteScrollSpeed * scrollSpeedMultiplier * okHitThreshold * noteScrollTime;
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
        
            Debug.Log($"Note hit with accuracy: {hitAccuracy * 100}%");
        }
    }
    
    float CalculateHitAccuracy(Note note)
    {
        Transform hitPosition = GetHitPosition(note.noteData.lane);
        float distance = Mathf.Abs(note.transform.position.y - hitPosition.position.y);
    
        // Calculate how close to perfect the hit was (0.0 = perfect, 1.0 = max allowed distance)
        float maxDistance = noteScrollSpeed * scrollSpeedMultiplier * okHitThreshold * noteScrollTime;
        float normalizedAccuracy = distance / maxDistance;
    
        // Return inverted value so 1.0 = perfect, 0.0 = barely hit
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
        
            // Calculate miss threshold as a distance below hit position
            // Make this more lenient to prevent immediate misses
            float missDistance = noteScrollSpeed * scrollSpeedMultiplier * 0.5f; // Increased from 0.3f
            float missThreshold = hitPosition.position.y - missDistance;
        
            if (note.transform.position.y < missThreshold)
            {
                // Debug the position to understand what's happening
                Debug.Log($"Miss: Note y={note.transform.position.y}, Threshold={missThreshold}, Difference={note.transform.position.y - missThreshold}");
            
                RegisterMiss(note);
                activeNotes.RemoveAt(i);
                Destroy(note.gameObject);
            }
        }
    }
    
    public void RegisterHit(Note note, float accuracy)
    {
        // Convert accuracy to a more readable percentage
        float accuracyPercent = accuracy * 100f;
    
        if (accuracyPercent >= (1.0f - perfectHitThreshold) * 100f)
        {
            scoreText.text = "PERFECT!";
            scoreTextAnimator.SetInteger("score", 4);
            score += combo == 0 ? 300 : 300 * combo;
            combo++;
        }
        else if (accuracyPercent >= (1.0f - greatHitThreshold) * 100f)
        {
            scoreText.text = "GREAT!";
            scoreTextAnimator.SetInteger("score", 3);
            score += combo == 0 ? 150 : 150 * combo;
            combo++;
        }
        else if (accuracyPercent >= (1.0f - goodHitThreshold) * 100f)
        {
            scoreText.text = "GOOD";
            scoreTextAnimator.SetInteger("score", 2);
            score += combo == 0 ? 100 : 100 * combo;
            combo++;
        }
        else if (accuracyPercent >= (1.0f - okHitThreshold) * 100f)
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