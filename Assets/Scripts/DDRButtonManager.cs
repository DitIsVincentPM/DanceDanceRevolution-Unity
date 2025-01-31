using UnityEngine;
using System.Collections.Generic;

public class DDRButtonManager : MonoBehaviour
{
    public List<DDRButton> buttons;
    private int selectedIndex = 0;

    void Start()
    {
        if (buttons.Count > 0)
        {
            SelectButton(selectedIndex);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            buttons[selectedIndex].PressButton(); // Execute button action
        }
    }

    public void MoveSelection(int direction)
    {
        buttons[selectedIndex].DeselectButton();
        selectedIndex += direction;

        // Wrap around if out of bounds
        if (selectedIndex < 0) selectedIndex = buttons.Count - 1;
        if (selectedIndex >= buttons.Count) selectedIndex = 0;

        SelectButton(selectedIndex);
    }

    public void SelectNextButton()
    {
        MoveSelection(1);
    }

    public void SelectPreviousButton()
    {
        MoveSelection(-1);
    }

    private void SelectButton(int index)
    {
        selectedIndex = index;
        buttons[selectedIndex].SelectButton();
    }
}