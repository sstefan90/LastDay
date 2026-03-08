using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

/// <summary>
/// One-click macOS standalone build for Last Day.
///
/// Menu:  LastDay / Build: macOS Standalone
/// CLI:   Unity -executeMethod BuildScript.BuildMacOS -quit -batchmode
///
/// Output: Builds/macOS/Last Day.app
/// The Builds/ folder is gitignored.
/// </summary>
public static class BuildScript
{
    private const string ProductName     = "Last Day";
    private const string BundleId        = "com.lastday.game";
    private const string BuildOutputDir  = "Builds/macOS";
    private const string AppName         = "Last Day";

    // ── Menu entry ────────────────────────────────────────────────────────

    [MenuItem("LastDay/Build: macOS Standalone", priority = 50)]
    public static void BuildMacOSMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Build Last Day — macOS",
            $"This will configure Player Settings and build:\n\n  {BuildOutputDir}/{AppName}.app\n\nContinue?",
            "Build", "Cancel");

        if (!confirmed) return;

        bool ok = RunBuild(headless: false);
        if (ok)
            EditorUtility.DisplayDialog("Build Complete",
                $"Build succeeded!\n\n{Path.GetFullPath(BuildOutputDir)}/{AppName}.app", "Open Folder");

        if (ok)
            EditorUtility.RevealInFinder(BuildOutputDir);
    }

    // ── Called by Unity CLI via -executeMethod ────────────────────────────

    public static void BuildMacOS()
    {
        RunBuild(headless: true);
    }

    // ── Core build logic ──────────────────────────────────────────────────

    /// <summary>
    /// Applies Player Settings and runs the build pipeline.
    /// Returns true on success.
    /// </summary>
    private static bool RunBuild(bool headless = false)
    {
        ApplyPlayerSettings();

        string outputPath = Path.Combine(BuildOutputDir, AppName + ".app");

        Directory.CreateDirectory(BuildOutputDir);

        var buildOptions = new BuildPlayerOptions
        {
            scenes          = GetEnabledScenes(),
            locationPathName = outputPath,
            target          = BuildTarget.StandaloneOSX,
            options         = headless ? BuildOptions.None : BuildOptions.ShowBuiltPlayer,
        };

        Debug.Log($"[Build] Starting macOS build → {outputPath}");

        BuildReport  report  = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Build] SUCCESS — {summary.totalSize / (1024 * 1024)} MB  ({summary.totalTime.TotalSeconds:F1}s)");
            return true;
        }
        else
        {
            Debug.LogError($"[Build] FAILED — result: {summary.result}  errors: {summary.totalErrors}");
            return false;
        }
    }

    // ── Player Settings ───────────────────────────────────────────────────

    private static void ApplyPlayerSettings()
    {
        // Identity
        PlayerSettings.productName        = ProductName;
        PlayerSettings.companyName        = "LastDay";
        PlayerSettings.bundleVersion      = "1.0.0";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, BundleId);

        // macOS standalone display
        PlayerSettings.defaultScreenWidth  = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode      = FullScreenMode.Windowed;
        PlayerSettings.allowFullscreenSwitch = true;

        // Rendering — pixel-art game; keep Gamma colour space
        PlayerSettings.colorSpace = ColorSpace.Gamma;

        // macOS graphics: Metal only (Apple Silicon)
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.Metal });

        // Architecture: Apple Silicon only (arm64)
        // UnityEditor.OSXStandalone.UserBuildSettings.architecture = MacOSArchitecture.AppleSilicon
        // (done via reflection to avoid hard dependency on the OSX standalone module)
        SetMacArchitecture();

        // Scripting backend: Mono for faster iteration; switch to IL2CPP for release if needed
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Standard);

        // Disable splash screen (pro licence) — if not pro, this is silently ignored
        PlayerSettings.SplashScreen.show = false;

        Debug.Log("[Build] Player Settings applied.");
    }

    /// <summary>Sets macOS build architecture to Apple Silicon via reflection to avoid compile issues on non-macOS hosts.</summary>
    private static void SetMacArchitecture()
    {
        try
        {
            var type = System.Type.GetType("UnityEditor.OSXStandalone.UserBuildSettings, UnityEditor.OSXStandaloneSupport");
            if (type == null) return;

            var prop = type.GetProperty("architecture",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (prop == null) return;

            // MacOSArchitecture enum: 0 = x64, 1 = ARM64, 2 = x64ARM64 (universal)
            var enumType = prop.PropertyType;
            var arm64    = System.Enum.ToObject(enumType, 1); // ARM64
            prop.SetValue(null, arm64);

            Debug.Log("[Build] macOS architecture set to Apple Silicon (ARM64).");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Build] Could not set macOS architecture via reflection: {e.Message}. Set it manually in File > Build Settings.");
        }
    }

    // ── Scene list ────────────────────────────────────────────────────────

    private static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        var paths  = new System.Collections.Generic.List<string>();
        foreach (var scene in scenes)
        {
            if (scene.enabled)
                paths.Add(scene.path);
        }
        return paths.ToArray();
    }
}
