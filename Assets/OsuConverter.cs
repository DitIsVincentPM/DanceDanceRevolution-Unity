using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class OsuImporterWindow : EditorWindow
{
    private string osuFileContent = "";
    private string songTitle = "";
    private string artist = "";
    private float bpm = 120f;
    private SongDifficulty difficulty = SongDifficulty.Beginner;

    private string selectedAudioPath = "";
    private string selectedCoverPath = "";
    private string selectedVideoPath = "";

    [MenuItem("Tools/Osu Importer")]
    public static void ShowWindow()
    {
        GetWindow<OsuImporterWindow>("Osu Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Osu Mania Importer", EditorStyles.boldLabel);
        
        // Input field to paste .osu file content directly
        osuFileContent = EditorGUILayout.TextArea(osuFileContent, GUILayout.Height(200));
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Select Song File (.mp3, .wav)"))
        {
            selectedAudioPath = EditorUtility.OpenFilePanel("Select Song File", "", "mp3,wav");
        }
        GUILayout.Label("Selected Audio: " + (string.IsNullOrEmpty(selectedAudioPath) ? "None" : Path.GetFileName(selectedAudioPath)));

        if (GUILayout.Button("Select Cover Art (.png, .jpg)"))
        {
            selectedCoverPath = EditorUtility.OpenFilePanel("Select Cover Image", "", "png,jpg");
        }
        GUILayout.Label("Selected Cover: " + (string.IsNullOrEmpty(selectedCoverPath) ? "None" : Path.GetFileName(selectedCoverPath)));

        if (GUILayout.Button("Select Video File (.mp4)"))
        {
            selectedVideoPath = EditorUtility.OpenFilePanel("Select Video File", "", "mp4");
        }
        GUILayout.Label("Selected Video: " + (string.IsNullOrEmpty(selectedVideoPath) ? "None" : Path.GetFileName(selectedVideoPath)));

        GUILayout.Space(10);
        
        if (GUILayout.Button("Import OSU File"))
        {
            if (!string.IsNullOrEmpty(osuFileContent))
            {
                ImportOsuFile();
            }
            else
            {
                Debug.LogError("No OSU content provided!");
            }
        }
    }

    private void ImportOsuFile()
    {
        if (string.IsNullOrEmpty(osuFileContent)) return;

        // Extract song title and artist from the .osu content
        songTitle = ExtractSongTitle(osuFileContent);
        artist = ExtractArtist(osuFileContent);

        string songFolder = $"Assets/Resources/Songs/{songTitle}";
        if (!AssetDatabase.IsValidFolder(songFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources/Songs", songTitle);
        }

        string jsonFilePath = $"{songFolder}/Notes.json";
        List<OsuNote> notes = ConvertOsuToNotes(osuFileContent);
        string jsonData = JsonUtility.ToJson(new OsuNoteList { notes = notes }, true);
        File.WriteAllText(jsonFilePath, jsonData);

        // Handle Audio File
        if (!string.IsNullOrEmpty(selectedAudioPath))
        {
            string audioFileName = "Song" + Path.GetExtension(selectedAudioPath); // Rename to "Song"
            string audioDestPath = $"{songFolder}/{audioFileName}";
            File.Copy(selectedAudioPath, audioDestPath, true);
            AssetDatabase.ImportAsset(audioDestPath);
        }

        // Handle Cover Art
        if (!string.IsNullOrEmpty(selectedCoverPath))
        {
            string coverFileName = "Cover" + Path.GetExtension(selectedCoverPath); // Rename to "Cover"
            string coverDestPath = $"{songFolder}/{coverFileName}";
            File.Copy(selectedCoverPath, coverDestPath, true);
            AssetDatabase.ImportAsset(coverDestPath);
        }

        // Handle Video File
        if (!string.IsNullOrEmpty(selectedVideoPath))
        {
            string videoFileName = "Clip" + Path.GetExtension(selectedVideoPath); // Rename to "Video"
            string videoDestPath = $"{songFolder}/{videoFileName}";
            File.Copy(selectedVideoPath, videoDestPath, true);
            AssetDatabase.ImportAsset(videoDestPath);
        }

        // Create Song ScriptableObject
        Song songAsset = ScriptableObject.CreateInstance<Song>();
        songAsset.songTitle = songTitle;
        songAsset.artist = artist;
        songAsset.bpm = bpm;
        songAsset.songDifficulty = difficulty;

        // Assign AudioClip
        if (!string.IsNullOrEmpty(selectedAudioPath))
        {
            string audioFileName = "Song" + Path.GetExtension(selectedAudioPath);
            songAsset.songClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{songFolder}/{audioFileName}");
        }

        // Assign Cover Image
        if (!string.IsNullOrEmpty(selectedCoverPath))
        {
            string coverFileName = "Cover" + Path.GetExtension(selectedCoverPath);
            songAsset.songImage = AssetDatabase.LoadAssetAtPath<Sprite>($"{songFolder}/{coverFileName}");
        }

        // Create the asset for the song
        string assetPath = $"{songFolder}/Data.asset";
        AssetDatabase.CreateAsset(songAsset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Song '{songTitle}' imported successfully.");
    }

    private List<OsuNote> ConvertOsuToNotes(string osuText)
    {
        List<OsuNote> notes = new List<OsuNote>();
        bool isMania = false;
        bool readingHitObjects = false;
        int columnCount = 4;
        string[] lines = osuText.Split('\n');

        foreach (string line in lines)
        {
            if (line.StartsWith("Mode:"))
            {
                string modeValue = line.Split(':')[1].Trim();
                isMania = modeValue == "3";
                Debug.Log("Game mode detected: " + (isMania ? "Mania" : "Other") + " (Raw mode value: " + modeValue + ")");
            }
            else if (isMania && line.StartsWith("[HitObjects]"))
            {
                readingHitObjects = true;
                Debug.Log("[HitObjects] section found. Starting parsing.");
                continue;
            }
            else if (readingHitObjects && line.Contains(","))
            {
                Debug.Log("Parsing hit object: " + line);
                ParseHitObject(line, notes, columnCount);
            }
        }

        return notes;
    }

    private void ParseHitObject(string line, List<OsuNote> notes, int columnCount)
    {
        string[] parts = line.Split(',');
        int x = int.Parse(parts[0]);
        float time = int.Parse(parts[2]) / 1000f;
        int type = int.Parse(parts[3]);
        bool isHold = (type & 128) != 0;

        int lane = (x * columnCount) / 512;
        Debug.Log("Converted X: " + x + " to lane: " + lane);

        if (isHold && parts.Length > 5)
        {
            int endTime = int.Parse(parts[5].Split(':')[0]);
            float holdDuration = (endTime - int.Parse(parts[2])) / 1000f;
            Debug.Log("Hold note detected at lane: " + lane + " with duration: " + holdDuration);
            notes.Add(new OsuNote { time = time, lane = lane, type = "hold", holdDuration = holdDuration });
        }
        else
        {
            Debug.Log("Normal note detected at lane: " + lane);
            notes.Add(new OsuNote { time = time, lane = lane, type = "normal" });
        }
    }

    private string ExtractSongTitle(string osuText)
    {
        string titleLine = GetLineWithPrefix(osuText, "Title:");
        return titleLine?.Split(':')[1].Trim() ?? "Unknown Title";
    }

    private string ExtractArtist(string osuText)
    {
        string artistLine = GetLineWithPrefix(osuText, "Artist:");
        return artistLine?.Split(':')[1].Trim() ?? "Unknown Artist";
    }

    private string GetLineWithPrefix(string osuText, string prefix)
    {
        string[] lines = osuText.Split('\n');
        foreach (string line in lines)
        {
            if (line.StartsWith(prefix))
            {
                return line;
            }
        }
        return null;
    }

    [System.Serializable]
    class OsuNote
    {
        public float time;
        public int lane;
        public string type;
        public float holdDuration;
    }

    [System.Serializable]
    class OsuNoteList
    {
        public List<OsuNote> notes;
    }
}
