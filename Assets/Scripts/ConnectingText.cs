using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ConnectingText : MonoBehaviour
{
    public Text uiText; // For Unity UI Text
    public TextMeshProUGUI tmpText; // For TextMeshPro

    private string baseText = "Connecting";
    private int dotCount = 0;
    private Coroutine textCoroutine;

    void OnEnable()
    {
        textCoroutine = StartCoroutine(AnimateText());
    }

    void OnDisable()
    {
        if (textCoroutine != null)
        {
            StopCoroutine(textCoroutine);
        }
    }

    IEnumerator AnimateText()
    {
        while (true)
        {
            string newText = baseText + new string('.', dotCount);
            
            if (uiText != null) uiText.text = newText;
            if (tmpText != null) tmpText.text = newText;

            dotCount = (dotCount + 1) % 4; // Loops 0 → 1 → 2 → 3 → 0
            yield return new WaitForSeconds(0.5f); // Adjust speed if needed
        }
    }
}