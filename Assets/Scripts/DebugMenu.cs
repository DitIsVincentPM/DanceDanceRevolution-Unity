using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DebugMenu : MonoBehaviour
{
    [SerializeField] private GameObject textPrefab; // TMP text prefab
    [SerializeField] private GameObject content;    // Parent container for debug texts

    private Dictionary<string, DebugEntry> debugEntries = new Dictionary<string, DebugEntry>();
    private bool isDebugVisible = false; // Track visibility state

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            ToggleDebugMenu();
        }

        if (isDebugVisible)
        {
            // Update all debug texts every frame
            foreach (var entry in debugEntries.Values)
            {
                entry.UpdateText();
            }
        }
    }

    public void AddDebugVariable(string name, System.Func<string> valueProvider)
    {
        if (debugEntries.ContainsKey(name))
        {
            Debug.LogWarning($"DebugMenu: Entry '{name}' already exists!");
            return;
        }

        GameObject newTextObject = Instantiate(textPrefab, content.transform);
        TMP_Text tmpText = newTextObject.GetComponent<TMP_Text>();

        if (tmpText != null)
        {
            DebugEntry newEntry = new DebugEntry(name, valueProvider, tmpText);
            debugEntries.Add(name, newEntry);
        }
        else
        {
            Debug.LogError("DebugMenu: The prefab does not have a TMP_Text component!");
        }
    }

    public void RemoveDebugVariable(string name)
    {
        if (debugEntries.ContainsKey(name))
        {
            Destroy(debugEntries[name].textObject.gameObject);
            debugEntries.Remove(name);
        }
    }

    private void ToggleDebugMenu()
    {
        isDebugVisible = !isDebugVisible; // Toggle state
        content.SetActive(isDebugVisible); // Show/hide content
    }

    private class DebugEntry
    {
        public string name;
        public System.Func<string> valueProvider;
        public TMP_Text textObject;

        public DebugEntry(string name, System.Func<string> valueProvider, TMP_Text textObject)
        {
            this.name = name;
            this.valueProvider = valueProvider;
            this.textObject = textObject;
        }

        public void UpdateText()
        {
            textObject.text = $"{name}: {valueProvider()}";
        }
    }
}
