using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor window that helps a new contributor place large binary assets
/// that are excluded from git (audio, video, large art).
/// Open via: LastDay → Setup: Large Assets
/// </summary>
public class LargeAssetSetupWindow : EditorWindow
{
    // ── PASTE YOUR GOOGLE DRIVE LINK HERE ────────────────────────────────
    private const string DRIVE_LINK = "https://drive.google.com/drive/folders/PASTE_YOUR_FOLDER_ID_HERE";
    // ─────────────────────────────────────────────────────────────────────

    // ── Asset manifest ────────────────────────────────────────────────────
    // Each entry: display label, expected path relative to project root, file extension filter
    private static readonly AssetEntry[] Assets = new AssetEntry[]
    {
        new AssetEntry(
            "clock_ticking.wav",
            "Assets/Audio/SFX/clock_ticking.wav",
            "wav"),
        new AssetEntry(
            "ambient.mp3",
            "Assets/Audio/Music/ambient.mp3",
            "mp3"),
        new AssetEntry(
            "Background video (openart ...mp4)",
            "Assets/Art/Environment/openart-02177278163259100000000000000000000ffffc0a8ac5d94a434_1772781739925_a9f35d8b.mp4",
            "mp4"),
        new AssetEntry(
            "Room background (Gemini_...r43rqr.png)",
            "Assets/Art/Environment/Gemini_Generated_Image_r43rqr43rqr43rqr.png",
            "png"),
        new AssetEntry(
            "Robert sprite A (Gemini_...ge6l8f.png)",
            "Assets/Art/Characters/Robert/Gemini_Generated_Image_ge6l8fge6l8fge6l.png",
            "png"),
        new AssetEntry(
            "Robert sprite B (Gemini_...63r91b.png)",
            "Assets/Art/Characters/Robert/Gemini_Generated_Image_63r91b63r91b63r9.png",
            "png"),
    };

    // ── State ─────────────────────────────────────────────────────────────
    private readonly Dictionary<string, string> _browsedPaths = new Dictionary<string, string>();
    private Vector2 _scroll;
    private string _statusMessage = "";
    private MessageType _statusType = MessageType.None;

    // ── Colours ───────────────────────────────────────────────────────────
    private static readonly Color ColorPresent = new Color(0.3f, 0.85f, 0.4f);
    private static readonly Color ColorMissing = new Color(0.95f, 0.35f, 0.35f);
    private static readonly Color ColorBrowsed = new Color(0.4f, 0.75f, 1f);

    // ── Menu item ─────────────────────────────────────────────────────────
    [MenuItem("LastDay/Setup: Large Assets", priority = 20)]
    public static void Open()
    {
        var window = GetWindow<LargeAssetSetupWindow>(utility: false, title: "Large Asset Setup");
        window.minSize = new Vector2(560, 380);
        window.Show();
    }

    // ── GUI ───────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space(4);
        DrawAssetRows();
        EditorGUILayout.Space(8);
        DrawCopyButton();
        if (!string.IsNullOrEmpty(_statusMessage))
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(8);
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft
        };
        EditorGUILayout.LabelField("LastDay — Large Asset Setup", titleStyle);
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField(
            "These files are not in git. Download them from Google Drive, then use Browse to locate each one.",
            EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open Google Drive  ↗", GUILayout.Width(180), GUILayout.Height(28)))
                Application.OpenURL(DRIVE_LINK);
        }

        EditorGUILayout.Space(6);
        DrawSeparator();
    }

    private void DrawAssetRows()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (var entry in Assets)
        {
            string absPath = Path.GetFullPath(entry.ProjectPath);
            bool present = File.Exists(absPath);
            bool browsed = _browsedPaths.ContainsKey(entry.ProjectPath);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                // Status icon
                Color prevColor = GUI.color;
                if (present)
                {
                    GUI.color = ColorPresent;
                    EditorGUILayout.LabelField("✓", GUILayout.Width(18));
                }
                else if (browsed)
                {
                    GUI.color = ColorBrowsed;
                    EditorGUILayout.LabelField("●", GUILayout.Width(18));
                }
                else
                {
                    GUI.color = ColorMissing;
                    EditorGUILayout.LabelField("✗", GUILayout.Width(18));
                }
                GUI.color = prevColor;

                // Labels
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(entry.Label, EditorStyles.boldLabel);
                    var pathStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
                    EditorGUILayout.LabelField(
                        present ? $"  {entry.ProjectPath}" :
                        browsed ? $"  Will copy from: {_browsedPaths[entry.ProjectPath]}" :
                                  $"  Destination: {entry.ProjectPath}",
                        pathStyle);
                }

                // Browse button (only if not yet present)
                if (!present)
                {
                    if (GUILayout.Button("Browse...", GUILayout.Width(80), GUILayout.Height(36)))
                    {
                        string chosen = EditorUtility.OpenFilePanel(
                            $"Select {entry.Label}",
                            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "/Downloads",
                            entry.Extension);

                        if (!string.IsNullOrEmpty(chosen))
                        {
                            _browsedPaths[entry.ProjectPath] = chosen;
                            _statusMessage = "";
                            Repaint();
                        }
                    }
                }
                else
                {
                    GUILayout.Space(84);
                }
            }

            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawCopyButton()
    {
        int readyCount = 0;
        foreach (var entry in Assets)
        {
            bool present = File.Exists(Path.GetFullPath(entry.ProjectPath));
            if (!present && _browsedPaths.ContainsKey(entry.ProjectPath))
                readyCount++;
        }

        bool allDone = AllPresent();

        using (new EditorGUI.DisabledScope(readyCount == 0 && !allDone))
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = allDone ? ColorPresent : (readyCount > 0 ? ColorBrowsed : Color.gray);

            string label = allDone
                ? "✓  All assets present"
                : $"Copy {readyCount} located file{(readyCount != 1 ? "s" : "")} into project";

            if (GUILayout.Button(label, GUILayout.Height(36)) && !allDone)
                CopyBrowsedFiles();

            GUI.backgroundColor = prev;
        }

        EditorGUILayout.Space(4);
    }

    // ── Copy logic ────────────────────────────────────────────────────────
    private void CopyBrowsedFiles()
    {
        int copied = 0;
        int failed = 0;
        var errors = new System.Text.StringBuilder();

        foreach (var entry in Assets)
        {
            string absPath = Path.GetFullPath(entry.ProjectPath);
            if (File.Exists(absPath)) continue;

            if (!_browsedPaths.TryGetValue(entry.ProjectPath, out string src)) continue;

            try
            {
                string destDir = Path.GetDirectoryName(absPath);
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(src, absPath, overwrite: true);
                copied++;
                Debug.Log($"[LargeAssetSetup] Copied → {entry.ProjectPath}");
            }
            catch (System.Exception ex)
            {
                failed++;
                errors.AppendLine($"• {entry.Label}: {ex.Message}");
                Debug.LogError($"[LargeAssetSetup] Failed to copy {entry.Label}: {ex.Message}");
            }
        }

        AssetDatabase.Refresh();
        _browsedPaths.Clear();

        if (failed == 0)
        {
            _statusMessage = $"Done! {copied} file{(copied != 1 ? "s" : "")} copied and imported successfully.";
            _statusType = MessageType.Info;
        }
        else
        {
            _statusMessage = $"Copied {copied}, failed {failed}:\n{errors}";
            _statusType = MessageType.Error;
        }

        Repaint();
    }

    private bool AllPresent()
    {
        foreach (var entry in Assets)
            if (!File.Exists(Path.GetFullPath(entry.ProjectPath)))
                return false;
        return true;
    }

    private static void DrawSeparator()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUILayout.Space(4);
    }

    // ── Data class ────────────────────────────────────────────────────────
    private class AssetEntry
    {
        public readonly string Label;
        public readonly string ProjectPath;
        public readonly string Extension;

        public AssetEntry(string label, string projectPath, string extension)
        {
            Label = label;
            ProjectPath = projectPath;
            Extension = extension;
        }
    }
}
