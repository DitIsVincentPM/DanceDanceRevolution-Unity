using UnityEngine;

public class Note : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 hitPosition;
    private NotesManager notesManager;
    public NoteData noteData;
    private float noteHitTime;
    private bool canBeHit = false;
    private bool isHit = false;
    
    public enum NoteState
    {
        Moving,
        ReadyToHit,
        Hit,
        Missed,
        Destroyed
    }

    public NoteState currentState = NoteState.Moving;

    private Quaternion initialRotation;

    private bool isMissed = false;
    private float missTime = 0f;
    private const float MAX_MISS_TIME = 5f;

    public void Initialize(Vector3 spawnPos, Vector3 endPos, Vector3 hitPos, NotesManager manager, NoteData data)
    {
        startPosition = spawnPos;
        endPosition = endPos;
        hitPosition = hitPos;
        notesManager = manager;
        noteData = data;
        noteHitTime = data.time;
        currentState = NoteState.Moving;

        initialRotation = transform.rotation;
    }

    void Update()
    {
        if (notesManager.audioSource == null) return;
        if (currentState == NoteState.Destroyed)
        {
            Destroy(gameObject);
        }

        float songTime = notesManager.audioSource.time;
        float progress = Mathf.Clamp01((songTime - (noteHitTime - notesManager.noteScrollTime)) / notesManager.noteScrollTime);

        // Update position correctly from start to end
        transform.position = Vector3.Lerp(startPosition, endPosition, progress);
        transform.rotation = initialRotation;

        // Check if note has reached the hit zone
        float distanceToHit = Vector3.Distance(transform.position, hitPosition);
        if (currentState == NoteState.Moving && distanceToHit < 70f)
        {
            currentState = NoteState.ReadyToHit;
            canBeHit = true;
        }

        // If note is beyond the end position or missed, mark as missed
        if (currentState == NoteState.Missed || (currentState == NoteState.ReadyToHit && transform.position.y >= endPosition.y))
        {
            missTime += Time.deltaTime;

            // Destroy missed note after delay
            if (missTime > MAX_MISS_TIME || transform.position.y >= endPosition.y)
            {
                notesManager.RegisterMiss(this);
                currentState = NoteState.Destroyed;
            }
        }

        // If note is hit, mark it and destroy after some time
        if (currentState == NoteState.Hit)
        {
            isHit = true;
            
            missTime += Time.deltaTime;
            if (missTime > 0.5f) // Small delay before destroying hit notes
            {
                Destroy(gameObject);
                currentState = NoteState.Destroyed;
            }
        }

        ProvideFeedback();
    }

    private void ProvideFeedback()
    {
        if (isHit)
        {
            // Add visual feedback for hit
            Debug.Log("Note Hit! Accuracy based on position.");
        }
        else if (currentState == NoteState.Missed)
        {
            // Add visual feedback for miss
            Debug.Log("Note Missed!");
        }
    }

    public bool CanBeHit()
    {
        return currentState == NoteState.ReadyToHit;
    }

    public void SetHitState()
    {
        currentState = NoteState.Hit;
        isHit = true;
    }
}