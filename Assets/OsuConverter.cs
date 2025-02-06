using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class OsuImporterWindow : EditorWindow
{
    private string selectedOszPath = "";
    private string extractPath = "";
    private string osuFilePath = "";

    private string songTitle = "";
    private string artist = "";
    private float bpm = 120f;
    private string audioFilename = "";
    private string coverFilename = "";

    [MenuItem("Tools/Osu Importer")]
    public static void ShowWindow()
    {
        GetWindow<OsuImporterWindow>("Osu Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Osu Mania Importer", EditorStyles.boldLabel);

        if (GUILayout.Button("Select OSZ File"))
        {
            selectedOszPath = EditorUtility.OpenFilePanel("Select OSZ File", "", "osz");
        }
        GUILayout.Label("Selected OSZ: " + (string.IsNullOrEmpty(selectedOszPath) ? "None" : Path.GetFileName(selectedOszPath)));

        GUILayout.Space(10);

        if (GUILayout.Button("Import OSZ File"))
        {
            if (!string.IsNullOrEmpty(selectedOszPath))
            {
                ImportOszFile();
            }
            else
            {
                Debug.LogError("No OSZ file selected!");
            }
        }
    }

    private void ImportOszFile()
    {
        extractPath = Path.Combine(Application.persistentDataPath, "ExtractedOsz");
        
        // Ensure directory is clean
        if (Directory.Exists(extractPath))
            Directory.Delete(extractPath, true);
        Directory.CreateDirectory(extractPath);

        // Rename .osz to .zip and extract
        string zipFilePath = selectedOszPath + ".zip";
        File.Copy(selectedOszPath, zipFilePath, true);
        ZipFile.ExtractToDirectory(zipFilePath, extractPath);
        File.Delete(zipFilePath); // Cleanup

        Debug.Log("Extracted OSZ to: " + extractPath);

        // Find .osu file
        string[] osuFiles = Directory.GetFiles(extractPath, "*.osu");
        if (osuFiles.Length == 0)
        {
            Debug.LogError("No .osu file found!");
            return;
        }
        osuFilePath = osuFiles[0]; // Assuming first .osu is the main file
        Debug.Log("Using OSU file: " + osuFilePath);

        // Parse .osu file
        string osuContent = File.ReadAllText(osuFilePath);
        songTitle = ExtractMetadata(osuContent, "Title:");
        artist = ExtractMetadata(osuContent, "Artist:");
        bpm = float.TryParse(ExtractMetadata(osuContent, "BPM:"), out float parsedBpm) ? parsedBpm : 120f;
        audioFilename = ExtractMetadata(osuContent, "AudioFilename:");

        // Extract cover image from [Events] section
        coverFilename = ExtractCoverImage(osuContent);

        Debug.Log($"Extracted Metadata: Title - {songTitle}, Artist - {artist}, BPM - {bpm}, Audio - {audioFilename}, Cover - {coverFilename}");

        // Find assets (song and cover)
        string audioPath = Path.Combine(extractPath, audioFilename);
        string coverPath = !string.IsNullOrEmpty(coverFilename) ? Path.Combine(extractPath, coverFilename) : "";

        // Save to Unity project
        SaveToProject(audioPath, coverPath);
    }

    private string ExtractMetadata(string content, string key)
    {
        string[] lines = content.Split('\n');
        foreach (string line in lines)
        {
            if (line.StartsWith(key))
            {
                return line.Split(':')[1].Trim();
            }
        }
        return "";
    }

    private string ExtractCoverImage(string content)
    {
        string[] lines = content.Split('\n');
        bool isEventSection = false;

        foreach (string line in lines)
        {
            if (line.StartsWith("[Events]"))
            {
                isEventSection = true;
                continue;
            }

            if (isEventSection && line.Contains("\""))
            {
                int start = line.IndexOf("\"") + 1;
                int end = line.LastIndexOf("\"");
                return line.Substring(start, end - start);
            }
        }

        return "";
    }

    private void SaveToProject(string audioPath, string coverPath)
    {
        string songFolder = $"Assets/Resources/Songs/{songTitle}";
        if (!AssetDatabase.IsValidFolder(songFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources/Songs", songTitle);
        }

        // Copy and import assets
        string audioTargetPath = CopyAndImportAsset(audioPath, songFolder, "Song");
        string coverTargetPath = CopyAndImportAsset(coverPath, songFolder, "Cover");

        // Check for video clip
        string videoPath = Directory.GetFiles(extractPath, "*.mp4").First();
        if (!string.IsNullOrEmpty(videoPath))
        {
            string videoTargetPath = $"{songFolder}/Clip.mp4";
            File.Copy(videoPath, videoTargetPath, true);
            AssetDatabase.ImportAsset(videoTargetPath);
        }

        // Convert .osu to JSON note data
        string jsonFilePath = $"{songFolder}/Notes.json";
        List<OsuNote> notes = ConvertOsuToNotes(File.ReadAllText(osuFilePath));
        File.WriteAllText(jsonFilePath, JsonUtility.ToJson(new OsuNoteList { notes = notes }, true));

        // Create ScriptableObject for the song
        Song songAsset = ScriptableObject.CreateInstance<Song>();
        songAsset.songTitle = songTitle;
        songAsset.artist = artist;
        songAsset.bpm = bpm;
        songAsset.songClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioTargetPath);
        songAsset.songImage = AssetDatabase.LoadAssetAtPath<Sprite>(LoadSpriteFromPath(coverTargetPath)); // Use the LoadSpriteFromPath method

        string assetPath = $"{songFolder}/Data.asset";
        AssetDatabase.CreateAsset(songAsset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Successfully imported: {songTitle}");
    }

    private string CopyAndImportAsset(string sourcePath, string targetFolder, string newName)
    {
        if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
        {
            string extension = Path.GetExtension(sourcePath);
            string targetPath = $"{targetFolder}/{newName}{extension}";
            File.Copy(sourcePath, targetPath, true);
            AssetDatabase.ImportAsset(targetPath);
            return targetPath;
        }
        return "";
    }

    private string LoadSpriteFromPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        Texture2D texture = new Texture2D(2, 2);
        byte[] imageData = File.ReadAllBytes(path);
        texture.LoadImage(imageData);

        // Set the texture import settings to Sprite (2D and UI) and Single mode
        string assetPath = path.Replace(Application.dataPath, "Assets");
        TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (textureImporter != null)
        {
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        return path;
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
                isMania = line.Contains("3");
            }
            else if (isMania && line.StartsWith("[HitObjects]"))
            {
                readingHitObjects = true;
                continue;
            }
            else if (readingHitObjects && line.Contains(","))
            {
                string[] parts = line.Split(',');
                int x = int.Parse(parts[0]);
                float time = int.Parse(parts[2]) / 1000f;
                int lane = (x * columnCount) / 512;
                notes.Add(new OsuNote { time = time, lane = lane, type = "normal" });
            }
        }
        return notes;
    }

    [System.Serializable]
    class OsuNote { public float time; public int lane; public string type; }
    
    [System.Serializable]
    class OsuNoteList { public List<OsuNote> notes; }
}
