using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager singleton;

    public bool _up;
    public bool _down;
    public bool _left;
    public bool _right;
    
    void Start() {
        if(InputManager.singleton != null) { Destroy(this); }
        
        InputManager.singleton = this;
        
        DebugMenu debugMenu = FindObjectOfType<DebugMenu>();

        debugMenu.AddDebugVariable("Up", () => _up.ToString());
        debugMenu.AddDebugVariable("Down", () => _down.ToString());
        debugMenu.AddDebugVariable("Left", () => _left.ToString());
        debugMenu.AddDebugVariable("Right", () => _right.ToString());
    }

    public void UpdateInput(bool up, bool down, bool left, bool right)
    {
        _up = up;
        _down = down;
        _left = left;
        _right = right;
    }
}
