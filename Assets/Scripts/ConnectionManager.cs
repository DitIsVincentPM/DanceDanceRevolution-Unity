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
    private Thread connectionThread;
    private bool isRunning = false;

    private ConcurrentQueue<string> serialQueue = new ConcurrentQueue<string>(); // Thread-safe queue

    private string lastPort;

    void Start()
    {
        if (singleton == null) singleton = this;
        else Destroy(this);

        lastPort = PlayerPrefs.GetString("LastSuccessfulPort", null);
    }

    private void AttemptSerialConnection()
    {
        string[] availablePorts = SerialPort.GetPortNames();
        bool connectionSuccessful = false;

        // Try the last successful port first
        if (!string.IsNullOrEmpty(lastPort) && System.Array.Exists(availablePorts, port => port == lastPort))
        {
            if (TryOpenPort(lastPort))
            {
                connectionSuccessful = true;
            }
        }

        // Try other available ports if the last port was not successful
        if (!connectionSuccessful)
        {
            foreach (string port in availablePorts)
            {
                if (port == lastPort) continue; // Skip the last port if already tried

                if (TryOpenPort(port))
                {
                    connectionSuccessful = true;
                    break;
                }
            }
        }

        if (!connectionSuccessful)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => { MenuManager.singleton.ConnectingFailedScreen(); });
        }
    }
    
    public void TryConnection()
    {
        if (connectionType == ConnectionType.SERIAL)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => { MenuManager.singleton.StartConnectingScreen(); });
            connectionThread = new Thread(AttemptSerialConnection);
            connectionThread.Start();
        }
        else if (connectionType == ConnectionType.HOTSPOT)
        {
            // Handle hotspot connection logic here
        }
    }

    private bool TryOpenPort(string port)
    {
        serialPort = new SerialPort(port, baudRate);
        serialPort.ReadTimeout = 600;

        try
        {
            serialPort.Open();
            isRunning = true;

            serialThread = new Thread(ReadSerialData);
            serialThread.Start();

            // Check if the port is sending the correct data format
            if (CheckSerialDataFormat())
            {
                Debug.Log($"Serial port {port} opened successfully!");
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    MenuManager.singleton.ConnectedScreen();
                    MenuManager.singleton.ConnectionSuccessful();

                    // Save the successful port
                    PlayerPrefs.SetString("LastSuccessfulPort", port);
                    PlayerPrefs.Save();
                });

                return true;
            }
            else
            {
                Debug.LogError($"Serial port {port} is not sending the correct data format.");
                HandleDisconnection();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error opening serial port {port}: {e.Message}");
            serialPort.Close();
        }

        return false;
    }

    private bool CheckSerialDataFormat()
    {
        // Wait for a short period to receive data
        Thread.Sleep(600);

        if (serialQueue.TryDequeue(out string data))
        {
            string[] buttonStates = data.Split(',');

            // Check if the data format is correct
            return buttonStates.Length == 4;
        }

        return false;
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
            catch (System.TimeoutException)
            {
            }
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