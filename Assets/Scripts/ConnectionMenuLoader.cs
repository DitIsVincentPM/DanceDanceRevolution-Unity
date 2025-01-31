using UnityEngine;

public class ConnectionMenuLoader : MonoBehaviour
{
    void Start()
    {
        DDRButtonManager manager = FindAnyObjectByType<DDRButtonManager>();
        
        // Hotspot
        manager.buttons[0].OnButtonPressed += () => HotSpot();
        
        // Serial
        manager.buttons[1].OnButtonPressed += () => Serial();
    }

    void HotSpot()
    {
        ConnectionManager.singleton.connectionType = ConnectionType.HOTSPOT;
    }
    
    void Serial()
    {
        ConnectionManager.singleton.connectionType = ConnectionType.SERIAL;
        ConnectionManager.singleton.TryConnection();
    }
}
