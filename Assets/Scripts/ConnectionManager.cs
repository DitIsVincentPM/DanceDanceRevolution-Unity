using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;

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
        Debug.Log("Available COM ports: " + string.Join(", ", availablePorts));
    
        if (availablePorts.Length == 0)
        {
            Debug.LogError("No COM ports found!");
            UnityMainThreadDispatcher.Instance().Enqueue(() => { MenuManager.singleton.ConnectingFailedScreen(); });
            return;
        }

        bool connectionSuccessful = false;

        // Try the last successful port first
        if (!string.IsNullOrEmpty(lastPort) && System.Array.Exists(availablePorts, port => port == lastPort))
        {
            Debug.Log("Trying last successful port: " + lastPort);
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

                Debug.Log("Trying port: " + port);
                if (TryOpenPort(port))
                {
                    connectionSuccessful = true;
                    break;
                }
            }
        }

        if (!connectionSuccessful)
        {
            Debug.LogError("Failed to connect to any available port");
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
    try
    {
        // Close any existing connection first
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
        
        // Clear the queue before starting a new connection
        while (serialQueue.TryDequeue(out _)) { }
        
        serialPort = new SerialPort(port, baudRate);
        serialPort.ReadTimeout = 1000; // Increase timeout
        serialPort.WriteTimeout = 1000;
        serialPort.DtrEnable = true; // Try enabling DTR - helps with some Arduino boards
        
        Debug.Log($"Attempting to open port {port} at {baudRate} baud");
        serialPort.Open();
        
        // Give the port a moment to stabilize
        Thread.Sleep(500);
        
        if (!serialPort.IsOpen)
        {
            Debug.LogError($"Port {port} did not open successfully");
            return false;
        }
        
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
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
    
    return false;
}
   
private bool PingSerialPort()
{
    if (serialPort == null || !serialPort.IsOpen)
        return false;
        
    try 
    {
        // Send a simple character to prompt a response
        serialPort.Write("P");
        
        // Wait shortly for a reply
        Thread.Sleep(200);
        
        // Check if any data came back
        return serialQueue.Count > 0;
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Error pinging serial port: {e.Message}");
        return false;
    }
}

    private bool CheckSerialDataFormat()
    {
        // Wait longer and check multiple times for data
        for (int i = 0; i < 5; i++) 
        {
            Thread.Sleep(200);
            Debug.Log($"Check attempt {i+1}/5, Queue count: {serialQueue.Count}");
        
            if (serialQueue.Count > 0 && serialQueue.TryDequeue(out string data))
            {
                Debug.Log($"Data received: {data}");
                string[] buttonStates = data.Split(',');
            
                // Check if the data format is correct and contains only 0 or 1
                if (buttonStates.Length == 4 && buttonStates.All(state => state == "0" || state == "1"))
                {
                    Debug.Log("Data format is correct.");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"Data format is incorrect: '{data}' (length: {buttonStates.Length})");
                    // Continue trying - don't return false yet
                }
            }
        }
    
        Debug.LogError("No valid data received after multiple attempts.");
        return false;
    }

    private void ReadSerialData()
    {
        int errorCount = 0;
        const int MAX_ERRORS = 5;
    
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string data = serialPort.ReadLine();
                if (!string.IsNullOrEmpty(data))
                {
                    serialQueue.Enqueue(data);
                    errorCount = 0; // Reset error count on successful read
                }
            }
            catch (System.TimeoutException)
            {
                // Timeout is normal, just continue
            }
            catch (System.IO.IOException e)
            {
                Debug.LogError($"Serial port IO exception: {e.Message}");
                errorCount++;
                if (errorCount >= MAX_ERRORS)
                {
                    Debug.LogError("Too many serial errors, disconnecting");
                    HandleDisconnection();
                    break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Serial read error: {e.Message}");
                errorCount++;
                if (errorCount >= MAX_ERRORS)
                {
                    Debug.LogError("Too many serial errors, disconnecting");
                    HandleDisconnection();
                    break;
                }
            }
        
            // Short sleep to prevent CPU overuse
            Thread.Sleep(1);
        }
    }

    void Update()
    {
        try
        {
            // Check if we have data in the queue
            if (serialQueue != null && serialQueue.Count > 0)
            {
                if (serialQueue.TryDequeue(out string data))
                {
                    if (string.IsNullOrEmpty(data))
                    {
                        return; // Skip empty data
                    }
                
                    string[] buttonStates = data.Split(',');

                    if (buttonStates.Length == 4)
                    {
                        bool button1 = buttonStates[0] == "1";
                        bool button2 = buttonStates[1] == "1";
                        bool button3 = buttonStates[2] == "1";
                        bool button4 = buttonStates[3] == "1";

                        if (InputManager.singleton != null)
                        {
                            InputManager.singleton.UpdateInput(button1, button2, button3, button4);
                        }
                        else
                        {
                            Debug.LogError("InputManager singleton is null");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Received malformed data: {data} with {buttonStates.Length} elements");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in Update: {e.Message}\n{e.StackTrace}");
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