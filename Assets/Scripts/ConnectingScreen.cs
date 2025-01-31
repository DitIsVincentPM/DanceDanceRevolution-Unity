using TMPro;
using UnityEngine;
using System.Collections;

public class ConnectingScreen : MonoBehaviour
{
    public TMP_Text connectingText;
    public TMP_Text connectedText;

    void Start()
    {
        connectingText.enabled = false;
        connectedText.enabled = false;
    }
    
    public void ResetConnectingScreen()
    {
        connectingText.enabled = false;
        connectedText.enabled = false;
    }

    public void StartConnecting()
    {
        connectingText.enabled = true;
    }

    public void Connected()
    {
        connectingText.enabled = false;
        connectedText.enabled = true;
        StartCoroutine(ShowConnectedText());
    }

    private IEnumerator ShowConnectedText()
    {
        yield return new WaitForSeconds(2f); // Wait for 2 seconds
        
        connectedText.enabled = false;
        GameManager.singleton.StartGame();
    }
}