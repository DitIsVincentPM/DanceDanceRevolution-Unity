using UnityEngine;

public class ArrowSpawner : MonoBehaviour
{
    public static ArrowSpawner instance;

    void Start()
    {
        instance = this;
    }
    
    public Transform spawnLeftArrow;
    public Transform spawnRightArrow;
    public Transform spawnUpArrow;
    public Transform spawnDownArrow;
    
    public Transform endLeftArrow;
    public Transform endRightArrow;
    public Transform endUpArrow;
    public Transform endDownArrow;
    
    public GameObject prefabLeftArrow;
    public GameObject prefabRightArrow;
    public GameObject prefabUpArrow;
    public GameObject prefabDownArrow;
}