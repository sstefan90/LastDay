using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System.Reflection;
using LastDay.Dialogue;
using LastDay.UI;

/// <summary>
/// Creates Assets/Scenes/LoadingScene.unity and optionally adds it to Build Settings at index 0.
/// Run via LastDay ▶ Setup: Create Loading Scene.
/// </summary>
public static class LoadingSceneSetup
{
    private const string ScenePath = "Assets/Scenes/LoadingScene.unity";

    [MenuItem("LastDay/Setup: Create Loading Scene", priority = 6)]
    public static void CreateLoadingScene()
    {
        // Confirm
        if (!EditorUtility.DisplayDialog("Create Loading Scene",
            $"This will create (or overwrite) {ScenePath} and optionally add it to Build Settings.\n\nContinue?",
            "Create", "Cancel"))
            return;

        // Ensure Scenes folder exists
        if (!Directory.Exists("Assets/Scenes"))
            Directory.CreateDirectory("Assets/Scenes");
        AssetDatabase.Refresh();

        // New scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        scene.name = "LoadingScene";

        // ── Camera ──────────────────────────────────────────────────────
        var camGo = new GameObject("Main Camera", typeof(UnityEngine.Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<UnityEngine.Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.05f);
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, scene);

        // ── EventSystem ──────────────────────────────────────────────────
        var esGo = new GameObject("EventSystem",
            typeof(EventSystem), typeof(StandaloneInputModule));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(esGo, scene);

        // ── Canvas ───────────────────────────────────────────────────────
        var canvasGo = new GameObject("Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGo, scene);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // ── DownloadProgressPanel hierarchy ──────────────────────────────
        var panelRoot = BuildProgressPanel(canvasGo);

        // ── DownloadProgressPanel component (on Canvas for convenience) ──
        var dlComp = canvasGo.AddComponent<DownloadProgressPanel>();
        WirePanel(dlComp, panelRoot);

        // ── ModelDownloader ───────────────────────────────────────────────
        var downloaderGo = new GameObject("ModelDownloader", typeof(ModelDownloader));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(downloaderGo, scene);

        // ── LoadingSceneController ────────────────────────────────────────
        var controllerGo = new GameObject("LoadingController", typeof(LoadingSceneController));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(controllerGo, scene);
        var controller = controllerGo.GetComponent<LoadingSceneController>();
        SetField(controller, "progressPanel", dlComp);

        // ── Save scene ────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.Refresh();
        Debug.Log($"[LoadingSceneSetup] Saved: {ScenePath}");

        // ── Build Settings ────────────────────────────────────────────────
        AddSceneToBuildSettings();

        EditorUtility.DisplayDialog("Done",
            $"LoadingScene created at {ScenePath}.\n\n" +
            "It has been added to Build Settings at index 0.\n\n" +
            "Make sure MainRoom is at index 1 (or whatever index your game expects).",
            "OK");
    }

    // ── Panel builder ──────────────────────────────────────────────────

    private static GameObject BuildProgressPanel(GameObject canvas)
    {
        // Full-screen dark root
        var root = MakePanel(canvas, "DownloadProgressPanel", new Color(0.05f, 0.05f, 0.05f, 0.97f));
        Stretch(root, Vector2.zero, Vector2.one);

        // Centred card
        var card = MakePanel(root, "Card", new Color(0.1f, 0.1f, 0.15f, 1f));
        Stretch(card, new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f));

        // Title
        var titleGo = MakeText(card, "TitleText", "Last Day", 36, Color.white);
        StretchAnchor(titleGo, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.93f));
        titleGo.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;

        // Model info
        var infoGo = MakeText(card, "ModelInfoText", "Downloading AI model…", 16, new Color(0.7f, 0.7f, 0.7f));
        StretchAnchor(infoGo, new Vector2(0.1f, 0.60f), new Vector2(0.9f, 0.73f));
        infoGo.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        // Progress bar
        var barGo = new GameObject("ProgressBar",
            typeof(RectTransform), typeof(Slider));
        barGo.transform.SetParent(card.transform, false);
        StretchAnchor(barGo, new Vector2(0.1f, 0.47f), new Vector2(0.9f, 0.58f));
        var slider = barGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = 0f;
        slider.interactable = false;
        BuildSliderVisuals(slider);

        // Status text
        var statusGo = MakeText(card, "StatusText", "Connecting…", 15, new Color(0.8f, 0.8f, 0.8f));
        StretchAnchor(statusGo, new Vector2(0.1f, 0.33f), new Vector2(0.75f, 0.46f));
        statusGo.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;

        // Percent text
        var pctGo = MakeText(card, "PercentText", "0%", 15, Color.white);
        StretchAnchor(pctGo, new Vector2(0.76f, 0.33f), new Vector2(0.9f, 0.46f));
        pctGo.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineRight;

        // Note
        var noteGo = MakeText(card, "NoteText",
            "This is a one-time download (~5 GB). The model will be cached for future sessions.",
            12, new Color(0.5f, 0.5f, 0.5f));
        StretchAnchor(noteGo, new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.32f));
        noteGo.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        // Error group (hidden by default)
        var errGroup = MakePanel(card, "ErrorGroup", new Color32(80, 20, 20, 200));
        StretchAnchor(errGroup, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.28f));
        errGroup.SetActive(false);

        var errTextGo = MakeText(errGroup, "ErrorText", "", 13, new Color(1f, 0.6f, 0.6f));
        StretchAnchorOffset(errTextGo, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(8f, 4f), new Vector2(-8f, -4f));
        var errTmp = errTextGo.GetComponent<TMP_Text>();
        errTmp.alignment = TextAlignmentOptions.TopLeft;
        errTmp.enableWordWrapping = true;

        var retryGo = MakeButton(errGroup, "RetryButton", "Try Again", new Color32(60, 100, 60, 230));
        StretchAnchorOffset(retryGo, new Vector2(0f, 0f), new Vector2(0.48f, 0.42f), new Vector2(8f, 4f), new Vector2(-4f, -4f));

        var quitGo = MakeButton(errGroup, "QuitButton", "Quit", new Color32(100, 40, 40, 230));
        StretchAnchorOffset(quitGo, new Vector2(0.52f, 0f), new Vector2(1f, 0.42f), new Vector2(4f, 4f), new Vector2(-8f, -4f));

        return root;
    }

    private static void BuildSliderVisuals(Slider slider)
    {
        var sliderGo = slider.gameObject;

        var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(sliderGo.transform, false);
        Stretch(bgGo, Vector2.zero, Vector2.one);
        bgGo.GetComponent<Image>().color = new Color32(40, 40, 50, 255);

        var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        var faRect = fillAreaGo.GetComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0f, 0.25f);
        faRect.anchorMax = new Vector2(1f, 0.75f);
        faRect.offsetMin = new Vector2(5f, 0f);
        faRect.offsetMax = new Vector2(-5f, 0f);

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        Stretch(fillGo, Vector2.zero, new Vector2(1f, 1f));
        fillGo.GetComponent<Image>().color = new Color32(100, 160, 255, 255);
        slider.fillRect = fillGo.GetComponent<RectTransform>();
    }

    private static void WirePanel(DownloadProgressPanel comp, GameObject root)
    {
        SetField(comp, "panel", root);

        var card = root.transform.Find("Card");
        if (card == null) return;

        Wire(comp, "titleText",    card.Find("TitleText"),     c => c.GetComponent<TMP_Text>());
        Wire(comp, "modelInfoText",card.Find("ModelInfoText"), c => c.GetComponent<TMP_Text>());
        Wire(comp, "statusText",   card.Find("StatusText"),    c => c.GetComponent<TMP_Text>());
        Wire(comp, "percentText",  card.Find("PercentText"),   c => c.GetComponent<TMP_Text>());
        Wire(comp, "progressBar",  card.Find("ProgressBar"),   c => c.GetComponent<Slider>());

        var errGroup = card.Find("ErrorGroup");
        if (errGroup != null)
        {
            SetField(comp, "errorGroup", errGroup.gameObject);
            Wire(comp, "errorText",   errGroup.Find("ErrorText"),   c => c.GetComponent<TMP_Text>());
            Wire(comp, "retryButton", errGroup.Find("RetryButton"), c => c.GetComponent<Button>());
            Wire(comp, "quitButton",  errGroup.Find("QuitButton"),  c => c.GetComponent<Button>());
        }
    }

    // ── Build Settings ──────────────────────────────────────────────────

    private static void AddSceneToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        // Remove existing entry for this scene if any
        scenes.RemoveAll(s => s.path == ScenePath);

        // Insert at index 0
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[LoadingSceneSetup] Added {ScenePath} to Build Settings at index 0.");
    }

    // ── UI helpers ──────────────────────────────────────────────────────

    private static GameObject MakePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static GameObject MakeText(GameObject parent, string name, string text, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        return go;
    }

    private static GameObject MakeButton(GameObject parent, string name, string label, Color32 bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = bgColor;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        Stretch(textGo, Vector2.zero, Vector2.one);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 16;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static void Stretch(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private static void StretchAnchor(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
        => Stretch(go, anchorMin, anchorMax);

    private static void StretchAnchorOffset(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = offsetMin;
        r.offsetMax = offsetMax;
    }

    // ── Reflection helpers ──────────────────────────────────────────────

    private static void SetField(object target, string fieldName, object value)
    {
        var f = target.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (f != null)
            f.SetValue(target, value);
        else
            Debug.LogWarning($"[LoadingSceneSetup] Field not found: {fieldName} on {target.GetType().Name}");
    }

    private static void Wire<T>(object comp, string fieldName, Transform source,
        System.Func<Transform, T> getter) where T : class
    {
        if (source == null) return;
        var val = getter(source);
        if (val != null) SetField(comp, fieldName, val);
    }
}
