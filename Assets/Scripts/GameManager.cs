using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager singleton;
    
    void Start()
    {
        if(singleton == null) singleton = this; else Destroy(gameObject);
    }

    public bool StartGame() => true;
}
