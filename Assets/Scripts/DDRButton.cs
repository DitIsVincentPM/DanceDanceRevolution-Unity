using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DDRButton : MonoBehaviour
{
    private Image buttonImage;
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color clickedColor = Color.gray;

    public delegate void ButtonPressed();
    public event ButtonPressed OnButtonPressed; // Event for button execution

    void Start()
    {
        buttonImage = GetComponent<Image>();

        if (buttonImage == null)
        {
            Debug.LogError("DDRButton requires an Image component.");
        }

        buttonImage.color = normalColor;
    }

    public void SelectButton()
    {
        buttonImage.color = highlightColor;
        SoundEffectManager.Instance.PlaySelectSound();
    }

    public void DeselectButton()
    {
        buttonImage.color = normalColor;
    }

    public void PressButton()
    {
        buttonImage.color = clickedColor;
        Invoke("ResetColor", 0.2f);
        OnButtonPressed?.Invoke(); // Trigger assigned action
    }
    
    private void ResetColor()
    {
        buttonImage.color = highlightColor; // Keep it highlighted if selected
    }
}