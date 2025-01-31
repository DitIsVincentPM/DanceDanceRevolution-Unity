using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;

public enum ConnectionType
{
    HOTSPOT,
    SERIAL
}

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager singleton;
    public ConnectionType connectionType = ConnectionType.SERIAL;
    
    public string portName = "COM3";
    public int baudRate = 115200;
    private SerialPort serialPort;
    private Thread serialThread;
    private bool isRunning = false;

    private ConcurrentQueue<string> serialQueue = new ConcurrentQueue<string>(); // Thread-safe queue

    void Start()
    {
        if(singleton == null) singleton = this; else Destroy(this);
            
        Debug.Log("Starting connection manager, waiting for user input...");
    }

    public void TryConnection()
    {
        if (connectionType == ConnectionType.SERIAL)
        {
            MenuManager.singleton.StartConnectingScreen();
            
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 1000;

            try
            {
                serialPort.Open();
                isRunning = true;

                serialThread = new Thread(ReadSerialData);
                serialThread.Start();

                Debug.Log("Serial port opened successfully!");
                MenuManager.singleton.ConnectedScreen();
                MenuManager.singleton.ConnectionSuccessful();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error opening serial port: {e.Message}");
                MenuManager.singleton.ConnectingFailedScreen();
            }   
        } 
        else if (connectionType == ConnectionType.HOTSPOT)
        {
            // Handle hotspot connection logic here
        }
    } 

    private void ReadSerialData()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string data = serialPort.ReadLine();
                serialQueue.Enqueue(data);
            }
            catch (System.TimeoutException) { }
            catch (System.IO.IOException)
            {
                Debug.LogError("Serial port disconnected!");
                HandleDisconnection();
                break;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Serial read error: {e.Message}");
                break;
            }
        }
    }

    void Update()
    {
        if (serialQueue.TryDequeue(out string data)) // Process data on the main thread
        {
            string[] buttonStates = data.Split(',');

            if (buttonStates.Length == 4)
            {
                bool button1 = buttonStates[0] == "1";
                bool button2 = buttonStates[1] == "1";
                bool button3 = buttonStates[2] == "1";
                bool button4 = buttonStates[3] == "1";
                
                InputManager.singleton.UpdateInput(button1, button2, button3, button4);
            }
        }
    }

    void HandleDisconnection()
    {
        isRunning = false;
        if (serialThread != null && serialThread.IsAlive)
        {
            serialThread.Join(); // Ensure the thread stops safely
        }

        if (serialPort != null)
        {
            serialPort.Close();
            serialPort = null;
        }
        
        MenuManager.singleton.ConnectingFailedScreen();
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (serialThread != null && serialThread.IsAlive)
        {
            serialThread.Join();
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            Debug.Log("Closing serial port");
            serialPort.Close();
        }
    }
}
