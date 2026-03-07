using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;

/// <summary>
/// Editor-only tests for LargeAssetSetupWindow.
/// Run via: LastDay/Test: Large Asset Setup
/// All results are written to the Unity Console as PASS / FAIL lines.
/// Does not modify any project files — safe to run at any time.
/// </summary>
public static class LargeAssetSetupTests
{
    private const string PLACEHOLDER = "PASTE_YOUR_FOLDER_ID_HERE";

    [MenuItem("LastDay/Test: Large Asset Setup", priority = 11)]
    public static void RunAll()
    {
        int passed = 0;
        int failed = 0;

        Debug.Log("[LargeAssetSetupTest] ═══════ Starting Large Asset Setup tests ═══════");

        Run("1: Drive link is set (not placeholder)",       Test_DriveLinkConfigured,    ref passed, ref failed);
        Run("2: Drive link is a valid Google Drive URL",    Test_DriveLinkFormat,         ref passed, ref failed);
        Run("3: Asset manifest has exactly 6 entries",      Test_ManifestCount,           ref passed, ref failed);
        Run("4: All manifest project paths are non-empty",  Test_ManifestPathsNonEmpty,   ref passed, ref failed);
        Run("5: All manifest paths start with Assets/",     Test_ManifestPathsUnderAssets,ref passed, ref failed);
        Run("6: .meta files exist in git for each asset",   Test_MetaFilesPresent,        ref passed, ref failed);
        Run("7: File.Copy logic works (temp file roundtrip)", Test_FileCopyRoundtrip,     ref passed, ref failed);
        Run("8: Window opens without exception",            Test_WindowOpens,             ref passed, ref failed);
        Run("9: .gitignore excludes *.mp4",                 Test_GitignoreExcludesMP4,    ref passed, ref failed);
        Run("10: .gitignore excludes large audio files",    Test_GitignoreExcludesAudio,  ref passed, ref failed);

        string summary = $"[LargeAssetSetupTest] ═══════ Done: {passed} passed  {failed} failed ═══════";
        if (failed == 0) Debug.Log(summary);
        else             Debug.LogWarning(summary);
    }

    // ── Runners ───────────────────────────────────────────────────────────

    private static void Run(string label, System.Func<string> test, ref int passed, ref int failed)
    {
        string result;
        try   { result = test(); }
        catch (System.Exception ex) { result = $"EXCEPTION: {ex.Message}"; }

        if (result == null)
        {
            Debug.Log($"[LargeAssetSetupTest]   PASS  {label}");
            passed++;
        }
        else
        {
            Debug.LogWarning($"[LargeAssetSetupTest]   FAIL  {label} — {result}");
            failed++;
        }
    }

    // ── Individual tests ──────────────────────────────────────────────────
    // Convention: return null = PASS, return string = FAIL (reason)

    private static string Test_DriveLinkConfigured()
    {
        string link = GetDriveLink();
        if (string.IsNullOrEmpty(link))      return "DRIVE_LINK is null or empty";
        if (link.Contains(PLACEHOLDER))      return $"DRIVE_LINK still contains placeholder text: {PLACEHOLDER}";
        return null;
    }

    private static string Test_DriveLinkFormat()
    {
        string link = GetDriveLink();
        if (link == null) return "could not read DRIVE_LINK via reflection";
        if (!link.StartsWith("https://drive.google.com/"))
            return $"Expected 'https://drive.google.com/…', got: {link}";
        return null;
    }

    private static string Test_ManifestCount()
    {
        var assets = GetManifest();
        if (assets == null) return "could not read Assets manifest via reflection";
        if (assets.Length != 6) return $"Expected 6 entries, found {assets.Length}";
        return null;
    }

    private static string Test_ManifestPathsNonEmpty()
    {
        var assets = GetManifest();
        if (assets == null) return "could not read Assets manifest";
        foreach (var entry in assets)
        {
            string path = GetProjectPath(entry);
            if (string.IsNullOrEmpty(path))
                return $"Entry '{GetLabel(entry)}' has empty ProjectPath";
        }
        return null;
    }

    private static string Test_ManifestPathsUnderAssets()
    {
        var assets = GetManifest();
        if (assets == null) return "could not read Assets manifest";
        foreach (var entry in assets)
        {
            string path = GetProjectPath(entry);
            if (!path.StartsWith("Assets/"))
                return $"Path '{path}' does not start with Assets/";
        }
        return null;
    }

    private static string Test_MetaFilesPresent()
    {
        var assets = GetManifest();
        if (assets == null) return "could not read Assets manifest";

        var missing = new System.Text.StringBuilder();
        foreach (var entry in assets)
        {
            string projPath = GetProjectPath(entry);
            string metaAbs  = Path.GetFullPath(projPath + ".meta");
            if (!File.Exists(metaAbs))
                missing.AppendLine($"  missing: {projPath}.meta");
        }

        string result = missing.ToString();
        if (!string.IsNullOrEmpty(result))
            return $".meta files missing from git checkout:\n{result.TrimEnd()}";
        return null;
    }

    private static string Test_FileCopyRoundtrip()
    {
        // Write a temp source file, copy it to a temp destination, verify it landed, clean up.
        string src  = Path.Combine(Path.GetTempPath(), "lastday_test_src.tmp");
        string dest = Path.Combine(Path.GetTempPath(), "lastday_test_dest.tmp");

        try
        {
            File.WriteAllText(src,  "test content 12345");
            File.Copy(src, dest, overwrite: true);

            if (!File.Exists(dest))
                return "File.Copy did not create destination file";

            string content = File.ReadAllText(dest);
            if (content != "test content 12345")
                return $"Destination file content mismatch: '{content}'";

            return null;
        }
        finally
        {
            if (File.Exists(src))  File.Delete(src);
            if (File.Exists(dest)) File.Delete(dest);
        }
    }

    private static string Test_WindowOpens()
    {
        try
        {
            var window = EditorWindow.GetWindow<LargeAssetSetupWindow>(
                utility: false, title: "Large Asset Setup [Test]");
            if (window == null) return "GetWindow returned null";
            window.Close();
            return null;
        }
        catch (System.Exception ex)
        {
            return $"Window threw on open: {ex.Message}";
        }
    }

    private static string Test_GitignoreExcludesMP4()
    {
        return GitignoreContains("*.mp4");
    }

    private static string Test_GitignoreExcludesAudio()
    {
        string wavLine = GitignoreContains("clock_ticking.wav");
        if (wavLine != null) return wavLine;
        return GitignoreContains("ambient.mp3");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string GitignoreContains(string pattern)
    {
        string path = Path.GetFullPath(".gitignore");
        if (!File.Exists(path)) return ".gitignore not found at project root";
        string content = File.ReadAllText(path);
        if (!content.Contains(pattern))
            return $".gitignore does not contain '{pattern}'";
        return null;
    }

    // Reflection helpers — read private static members of LargeAssetSetupWindow

    private static string GetDriveLink()
    {
        var fi = typeof(LargeAssetSetupWindow).GetField(
            "DRIVE_LINK", BindingFlags.NonPublic | BindingFlags.Static);
        return fi?.GetValue(null) as string;
    }

    private static System.Array GetManifest()
    {
        var fi = typeof(LargeAssetSetupWindow).GetField(
            "Assets", BindingFlags.NonPublic | BindingFlags.Static);
        return fi?.GetValue(null) as System.Array;
    }

    private static string GetProjectPath(object entry)
    {
        var fi = entry.GetType().GetField("ProjectPath",
            BindingFlags.Public | BindingFlags.Instance);
        return fi?.GetValue(entry) as string;
    }

    private static string GetLabel(object entry)
    {
        var fi = entry.GetType().GetField("Label",
            BindingFlags.Public | BindingFlags.Instance);
        return fi?.GetValue(entry) as string ?? "(unknown)";
    }
}
