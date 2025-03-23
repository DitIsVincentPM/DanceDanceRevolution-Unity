using UnityEngine;
using System;

public class InputManager : MonoBehaviour
{
    public static InputManager singleton;
    
    // Define events for button states
    public event Action OnUpPressed;
    public event Action OnUpReleased;
    public event Action OnDownPressed;
    public event Action OnDownReleased;
    public event Action OnLeftPressed;
    public event Action OnLeftReleased;
    public event Action OnRightPressed;
    public event Action OnRightReleased;
    
    // Keep the boolean state for debugging
    private bool _up;
    private bool _down;
    private bool _left;
    private bool _right;
    
    // Previous state to detect changes
    private bool _prevUp;
    private bool _prevDown;
    private bool _prevLeft;
    private bool _prevRight;
    
    void Start()
    {
        if(InputManager.singleton != null) { Destroy(this); return; }
        
        InputManager.singleton = this;
        
        DebugMenu debugMenu = FindObjectOfType<DebugMenu>();
        
        if (debugMenu != null)
        {
            debugMenu.AddDebugVariable("Up", () => _up.ToString());
            debugMenu.AddDebugVariable("Down", () => _down.ToString());
            debugMenu.AddDebugVariable("Left", () => _left.ToString());
            debugMenu.AddDebugVariable("Right", () => _right.ToString());
        }
    }
    
    public void UpdateInput(bool up, bool down, bool left, bool right)
    {
        // Store previous state
        _prevUp = _up;
        _prevDown = _down;
        _prevLeft = _left;
        _prevRight = _right;
        
        // Update current state
        _up = up;
        _down = down;
        _left = left;
        _right = right;
        
        // Fire events when state changes
        CheckAndFireEvents();
    }
    
    private void CheckAndFireEvents()
    {
        // Check for button presses (false -> true)
        if (!_prevUp && _up)
            OnUpPressed?.Invoke();
        if (!_prevDown && _down)
            OnDownPressed?.Invoke();
        if (!_prevLeft && _left)
            OnLeftPressed?.Invoke();
        if (!_prevRight && _right)
            OnRightPressed?.Invoke();
            
        // Check for button releases (true -> false)
        if (_prevUp && !_up)
            OnUpReleased?.Invoke();
        if (_prevDown && !_down)
            OnDownReleased?.Invoke();
        if (_prevLeft && !_left)
            OnLeftReleased?.Invoke();
        if (_prevRight && !_right)
            OnRightReleased?.Invoke();
    }
    
    // Helper methods to get current button states
    public bool IsUpPressed() => _up;
    public bool IsDownPressed() => _down;
    public bool IsLeftPressed() => _left;
    public bool IsRightPressed() => _right;
}