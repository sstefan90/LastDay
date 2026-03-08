using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System.Reflection;
using LastDay.Core;
using LastDay.Player;
using LastDay.Interaction;
using LastDay.Dialogue;
using LastDay.NPC;
using LastDay.Camera;
using LastDay.Audio;
using LastDay.UI;
using LastDay.Data;
#if LLMUNITY_AVAILABLE
using LLMUnity;
#endif

public class SceneSetupEditor : EditorWindow
{
    private const string TriggerFile = "Assets/Editor/run_scene_setup.trigger";

    [InitializeOnLoadMethod]
    static void CheckForTrigger()
    {
        if (!File.Exists(TriggerFile)) return;

        File.Delete(TriggerFile);
        if (File.Exists(TriggerFile + ".meta"))
            File.Delete(TriggerFile + ".meta");

        EditorApplication.delayCall += () =>
        {
            Debug.Log("[SceneSetup] Trigger file detected. Running scene setup...");
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainRoom.unity");
            SetupFullScene();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();
            Debug.Log("[SceneSetup] Scene setup complete and saved.");
        };
    }

    public static void SetupSceneBatchMode()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainRoom.unity");
        SetupFullScene();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] Scene saved via batch mode.");
    }

    [MenuItem("LastDay/Setup Scene (Full)", priority = 1)]
    public static void SetupFullScene()
    {
        if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
            "Last Day - Full Scene Setup",
            "This will create the complete GameObject hierarchy, add all components, wire Inspector references, and place sprites.\n\nExisting objects named 'managers', 'interactables', 'Martha', 'Canvas' will be replaced.\n\nContinue?",
            "Build It", "Cancel"))
            return;

        CleanExisting();

        SetupCamera();
        var managers = SetupManagers();
        SetupEnvironment();
        SetupRobert();
        SetupInteractables();
        SetupMartha();
        var canvas = SetupCanvas();
        SetupEventSystem();
        CreateMemoryDataAssets();

        WireCrossReferences(managers, canvas);

        EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SceneSetup] Scene setup complete. Save the scene (Ctrl+S / Cmd+S).");
    }

    [MenuItem("LastDay/Scene view: Match Game camera", priority = 2)]
    public static void SceneViewMatchGameCamera()
    {
        var camGo = GameObject.Find("Main Camera");
        UnityEngine.Camera mainCam = camGo != null ? camGo.GetComponent<UnityEngine.Camera>() : null;
        if (mainCam == null)
        {
            Debug.LogWarning("[SceneSetup] Main Camera not found. Create the scene first.");
            return;
        }

        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            Debug.LogWarning("[SceneSetup] No active Scene view.");
            return;
        }

        Vector3 camPos = mainCam.transform.position;
        sceneView.pivot = new Vector3(camPos.x, camPos.y, 0f);
        sceneView.size = mainCam.orthographicSize;
        sceneView.orthographic = true;
        sceneView.rotation = Quaternion.identity;
        sceneView.Repaint();
        Debug.Log("[SceneSetup] Scene view aligned to Game camera (same scale and framing).");
    }


    [MenuItem("LastDay/Create Memory Assets Only", priority = 3)]
    public static void CreateMemoryDataAssets()
    {
        string dataPath = "Assets/Data";
        if (!AssetDatabase.IsValidFolder(dataPath))
            AssetDatabase.CreateFolder("Assets", "Data");

        CreateMemory(dataPath, "Memory_WeddingPhoto", "wedding_photo", "Wedding Photo",
            "A framed photo from our wedding day, 1979. Robert in his father's too-short tie.",
            "47 years of marriage. The day they promised forever, not knowing what forever would mean.",
            "Martha remembers Robert's nervous hands, how he fumbled with the ring. She held his hands steady then, just as she does now.",
            "David was the best man. He remembers the toast he gave — something about Robert being the bravest coward he'd ever met.");

        CreateMemory(dataPath, "Memory_Guitar", "guitar", "Guitar",
            "Robert's acoustic guitar, dusty now. He hasn't played in months — his fingers won't cooperate.",
            "Sunday mornings were for music. The whole house would fill with sound.",
            "Martha misses the music most. She'd hum along from the kitchen. The silence in the house now is the loudest thing she's ever heard.",
            "David and Robert used to jam together. David on harmonica, Robert on guitar. They were terrible, but it didn't matter.");

        CreateMemory(dataPath, "Memory_IcePicks", "ice_picks", "Ice Picks",
            "A pair of old ice climbing picks, mounted on the wall. Scuffed and well-used.",
            "1989 — Robert and David climbed Mount Washington in February. Nearly died. Best weekend of their lives.",
            "Martha was furious when they came home frostbitten. She made Robert promise never again. He kept that promise — she's not sure if that was the right thing to ask.",
            "David still has his pair too. That climb was the day they stopped being friends and became brothers.");

        CreateMemory(dataPath, "Memory_Phone", "phone", "Phone",
            "The house phone. It hasn't rung in a while.",
            "David calls when he can. The conversations get harder.",
            "Martha answers the phone when Robert can't. She and David don't talk about the hard things — they just coordinate care and pretend everything is fine.",
            "David calls because he doesn't know what else to do. Hearing Robert's voice — even diminished — is better than imagining the worst.");

        CreateMemory(dataPath, "Memory_Document", "document", "The Document",
            "A legal document. Medical Assistance in Dying request form.",
            "The choice that changes everything. Or the choice that acknowledges nothing will change.",
            "Martha found the pamphlet three months ago. She hasn't brought it up. She won't. This has to be Robert's decision.",
            "David doesn't know about the document yet. When Robert tells him, there will be a long silence on the phone.");

        CreateMemory(dataPath, "Memory_Computer", "computer", "Computer",
            "Robert's old desktop computer. The screen glows with a security prompt.",
            "The MAID document is locked behind three security questions. Each answer is a key to Robert's darkest secrets.",
            "Martha doesn't like that computer. It knows things she's spent decades trying to bury.",
            "David never trusted machines with secrets. But he respects that some doors need to be opened.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SceneSetup] Memory data assets created in Assets/Data/");
    }

    [MenuItem("LastDay/Setup LLM Components", priority = 4)]
    public static void SetupLLMComponents()
    {
#if LLMUNITY_AVAILABLE
        var llmManagerGo = GameObject.Find("LocalLLMManager");
        if (llmManagerGo == null)
        {
            var managers = GameObject.Find("managers");
            if (managers == null)
            {
                EditorUtility.DisplayDialog("Error", "Run 'Setup Scene (Full)' first.", "OK");
                return;
            }
            llmManagerGo = new GameObject("LocalLLMManager");
            llmManagerGo.transform.SetParent(managers.transform, false);
            llmManagerGo.AddComponent<LocalLLMManager>();
        }

        var localLLM = llmManagerGo.GetComponent<LocalLLMManager>();

        // Remove deprecated LLMCharacter components if present
        foreach (var old in llmManagerGo.GetComponentsInChildren<LLMCharacter>(true))
            Object.DestroyImmediate(old);

        // LLM server component (loads the model)
        var llm = llmManagerGo.GetComponent<LLM>();
        if (llm == null)
            llm = llmManagerGo.AddComponent<LLM>();

        llm.contextSize = 4096;
        llm.numThreads = -1;

        string modelPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Models", "phi3-mini.gguf"));
        if (File.Exists(modelPath))
        {
            llm.model = modelPath;
            Debug.Log($"[LLMSetup] Model set: {modelPath}");
        }
        else
        {
            Debug.LogWarning($"[LLMSetup] Model not found at {modelPath}. Download to <project>/Models/phi3-mini.gguf");
        }

        // Martha character (LLMAgent, not deprecated LLMCharacter)
        var marthaGo = llmManagerGo.transform.Find("MarthaLLM");
        if (marthaGo == null)
        {
            var mg = new GameObject("MarthaLLM");
            mg.transform.SetParent(llmManagerGo.transform, false);
            marthaGo = mg.transform;
        }
        var marthaChar = marthaGo.GetComponent<LLMAgent>();
        if (marthaChar == null)
            marthaChar = marthaGo.gameObject.AddComponent<LLMAgent>();

        marthaChar.llm = llm;
        marthaChar.numPredict = 80;
        marthaChar.temperature = 0.7f;
        marthaChar.topP = 0.9f;
        marthaChar.repeatPenalty = 1.1f;
        marthaChar.systemPrompt = CharacterPrompts.GetMarthaPrompt(new System.Collections.Generic.List<string>());

        // David character
        var davidGo = llmManagerGo.transform.Find("DavidLLM");
        if (davidGo == null)
        {
            var dg = new GameObject("DavidLLM");
            dg.transform.SetParent(llmManagerGo.transform, false);
            davidGo = dg.transform;
        }
        var davidChar = davidGo.GetComponent<LLMAgent>();
        if (davidChar == null)
            davidChar = davidGo.gameObject.AddComponent<LLMAgent>();

        davidChar.llm = llm;
        davidChar.numPredict = 60;
        davidChar.temperature = 0.7f;
        davidChar.topP = 0.9f;
        davidChar.repeatPenalty = 1.1f;
        davidChar.systemPrompt = CharacterPrompts.GetDavidPrompt(new System.Collections.Generic.List<string>());

        // Wire references back to LocalLLMManager
        SetPrivateField(localLLM, "marthaCharacter", marthaChar);
        SetPrivateField(localLLM, "davidCharacter", davidChar);
        SetPrivateField(localLLM, "useLLM", true);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[LLMSetup] LLM + Martha/David characters configured. Model: Phi-3-mini-4k-instruct");
        Debug.Log("[LLMSetup] Make sure to set the model file in the LLM component Inspector if auto-detect didn't work.");
#else
        Debug.LogWarning("[LLMSetup] LLMUnity package not available yet. LLMUNITY_AVAILABLE define not active. Skipping LLM wiring.");
#endif
    }

    static void CleanExisting()
    {
        string[] toRemove = { "managers", "interactables", "Martha", "Canvas", "Environment", "Robert", "EventSystem" };
        foreach (string name in toRemove)
        {
            var obj = GameObject.Find(name);
            if (obj != null)
                Undo.DestroyObjectImmediate(obj);
        }
    }

    static Sprite LoadSprite(string relativePath)
    {
        string path = "Assets/Art/" + relativePath;
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null)
            Debug.LogWarning($"[SceneSetup] Sprite not found: {path}");
        return s;
    }

    static Sprite[] LoadSprites(string relativePath)
    {
        string path = "Assets/Art/" + relativePath;
        Object[] loaded = AssetDatabase.LoadAllAssetsAtPath(path);
        var sprites = new System.Collections.Generic.List<Sprite>();

        foreach (var obj in loaded)
        {
            if (obj is Sprite sprite)
                sprites.Add(sprite);
        }

        if (sprites.Count == 0)
        {
            var single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (single != null)
                sprites.Add(single);
        }

        sprites.Sort((a, b) => ExtractFrameIndex(a.name).CompareTo(ExtractFrameIndex(b.name)));
        return sprites.ToArray();
    }

    static int ExtractFrameIndex(string spriteName)
    {
        int underscore = spriteName.LastIndexOf('_');
        if (underscore >= 0)
        {
            string suffix = spriteName.Substring(underscore + 1);
            if (int.TryParse(suffix, out int index))
                return index;
        }
        return int.MaxValue;
    }

    static void SetPrivateField(Component comp, string fieldName, object value)
    {
        var so = new SerializedObject(comp);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"[SceneSetup] Property '{fieldName}' not found on {comp.GetType().Name}");
            return;
        }

        switch (prop.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                prop.objectReferenceValue = value as Object;
                break;
            case SerializedPropertyType.String:
                prop.stringValue = (string)value;
                break;
            case SerializedPropertyType.Float:
                prop.floatValue = (float)value;
                break;
            case SerializedPropertyType.Integer:
                prop.intValue = (int)value;
                break;
            case SerializedPropertyType.Boolean:
                prop.boolValue = (bool)value;
                break;
            case SerializedPropertyType.Color:
                prop.colorValue = (Color)value;
                break;
            case SerializedPropertyType.Vector2:
                prop.vector2Value = (Vector2)value;
                break;
            case SerializedPropertyType.LayerMask:
                prop.intValue = (int)value;
                break;
            case SerializedPropertyType.Enum:
                prop.enumValueIndex = (int)value;
                break;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetLayer(GameObject go, int layer, bool includeChildren = true)
    {
        go.layer = layer;
        if (includeChildren)
        {
            foreach (Transform child in go.transform)
                SetLayer(child.gameObject, layer, true);
        }
    }

    static void SetSortingLayer(SpriteRenderer sr, string sortingLayerName, int order)
    {
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = order;
    }

    // ─────────────────────────────────────────────────────────────────
    //  CAMERA
    // ─────────────────────────────────────────────────────────────────

    static void SetupCamera()
    {
        var camGo = GameObject.Find("Main Camera");
        if (camGo == null)
        {
            camGo = new GameObject("Main Camera");
            camGo.AddComponent<UnityEngine.Camera>();
            camGo.AddComponent<AudioListener>();
        }

        var cam = camGo.GetComponent<UnityEngine.Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = HexColor("#1A1A2E");
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        camGo.transform.position = new Vector3(0, 0, -10);

        if (camGo.GetComponent<Physics2DRaycaster>() == null)
            camGo.AddComponent<Physics2DRaycaster>();

        var camCtrl = camGo.GetComponent<CameraController2D>();
        if (camCtrl == null)
            camCtrl = camGo.AddComponent<CameraController2D>();

        SetPrivateField(camCtrl, "persistAcrossScenes", false);

        Debug.Log("[SceneSetup] Camera configured.");
    }

    // ─────────────────────────────────────────────────────────────────
    //  MANAGERS
    // ─────────────────────────────────────────────────────────────────

    static GameObject SetupManagers()
    {
        var root = new GameObject("managers");
        root.transform.position = Vector3.zero;

        var gmGo = CreateChild(root, "GameManager");
        gmGo.AddComponent<GameManager>();

        var gsmGo = CreateChild(root, "GameStateMachine");
        gsmGo.AddComponent<GameStateMachine>();

        var emGo = CreateChild(root, "EventManager");
        emGo.AddComponent<EventManager>();

        var fmGo = CreateChild(root, "FadeManager");
        fmGo.AddComponent<FadeManager>();

        var amGo = CreateChild(root, "AudioManager");
        var audioMgr = amGo.AddComponent<AudioManager>();
        var musicSrc = amGo.AddComponent<AudioSource>();
        musicSrc.playOnAwake = false;
        musicSrc.loop = true;
        var sfxSrc = amGo.AddComponent<AudioSource>();
        sfxSrc.playOnAwake = false;
        var blipSrc = amGo.AddComponent<AudioSource>();
        blipSrc.playOnAwake = false;
        SetPrivateField(audioMgr, "musicSource", musicSrc);
        SetPrivateField(audioMgr, "sfxSource", sfxSrc);
        SetPrivateField(audioMgr, "dialogueBlipSource", blipSrc);

        // Wire audio clips from project assets
        var ambient = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/ambient.mp3");
        var phoneRing = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/phone_ringing.mp3");
        var typingClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/typing.mp3");
        var tearingPaper = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/tearing_paper.wav");
        var clockTick = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/clock_ticking.wav");

        if (ambient != null) SetPrivateField(audioMgr, "ambientLoop", ambient);
        if (phoneRing != null) SetPrivateField(audioMgr, "phoneRinging", phoneRing);
        if (typingClip != null) SetPrivateField(audioMgr, "typing", typingClip);
        if (tearingPaper != null) SetPrivateField(audioMgr, "paperTear", tearingPaper);
        if (clockTick != null) SetPrivateField(audioMgr, "clockTicking", clockTick);

        var llmGo = CreateChild(root, "LocalLLMManager");
        llmGo.AddComponent<LocalLLMManager>();

        var mcGo = CreateChild(root, "MemoryContext");
        mcGo.AddComponent<MemoryContext>();

        var dlGo = CreateChild(root, "ModelDownloader");
        var downloader = dlGo.AddComponent<ModelDownloader>();

        // Wire ModelDownloader → GameManager
        var gm = gmGo.GetComponent<GameManager>();
        SetPrivateField(gm, "modelDownloader", downloader);

        Debug.Log("[SceneSetup] Managers hierarchy created.");
        return root;
    }

    // ─────────────────────────────────────────────────────────────────
    //  ENVIRONMENT
    // ─────────────────────────────────────────────────────────────────

    static void SetupEnvironment()
    {
        var root = new GameObject("Environment");
        root.transform.position = Vector3.zero;

        int obstacleLayer = LayerMask.NameToLayer("Obstacles");

        var bgGo = CreateChild(root, "RoomBackground");
        var bgSr = bgGo.AddComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite("Environment/Gemini_Generated_Image_r43rqr43rqr43rqr.png");
        SetSortingLayer(bgSr, "Background", 0);
        bgGo.transform.localScale = Vector3.one;
        bgGo.AddComponent<BackgroundFitCamera>();

        CreateFurniture(root, "Desk", "Environment/furniture_desk.png", new Vector3(2f, -1f, 0),
            "Midground", 0, obstacleLayer, new Vector2(1f, 0.8f));

        CreateFurniture(root, "Chair", "Environment/furniture_chair.png", new Vector3(0f, -1.5f, 0),
            "Midground", 1, obstacleLayer, new Vector2(0.8f, 0.8f));

        CreateFurniture(root, "Bookshelf", "Environment/furniture_bookshelf.png", new Vector3(-3f, 0f, 0),
            "Midground", 0, obstacleLayer, new Vector2(1f, 1.5f));

        Debug.Log("[SceneSetup] Environment created (static scene, no walkable floor).");
    }

    static void CreateFurniture(GameObject parent, string name, string spritePath, Vector3 pos,
        string sortLayer, int sortOrder, int physicsLayer, Vector2 colliderSize)
    {
        var go = CreateChild(parent, name);
        go.transform.localPosition = pos;
        SetLayer(go, physicsLayer, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spritePath);
        SetSortingLayer(sr, sortLayer, sortOrder);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = colliderSize;
    }

    // ─────────────────────────────────────────────────────────────────
    //  ROBERT (Player)
    // ─────────────────────────────────────────────────────────────────

    static void SetupRobert()
    {
        int charLayer = LayerMask.NameToLayer("Characters");
        int interactableLayerMask = 1 << LayerMask.NameToLayer("Interactables");
        int characterLayerMask = 1 << LayerMask.NameToLayer("Characters");

        var robert = new GameObject("Robert");
        robert.transform.position = new Vector3(0f, -1f, 0f);
        SetLayer(robert, charLayer);

        var spriteChild = CreateChild(robert, "Sprite");
        var sr = spriteChild.AddComponent<SpriteRenderer>();
        var robertFrames = LoadSprites("Characters/Robert/Gemini_Generated_Image_ge6l8fge6l8fge6l.png");
        sr.sprite = robertFrames.Length > 0
            ? robertFrames[0]
            : LoadSprite("Characters/Robert/robert_placeholder.png");
        SetSortingLayer(sr, "Characters", 0);

        if (robertFrames.Length > 0)
        {
            var frameAnimator = spriteChild.AddComponent<SpriteFrameAnimator>();
            frameAnimator.Configure(sr, robertFrames, 8f);
        }

        var col = robert.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 0.75f);

        var rb = robert.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var charAnim = robert.AddComponent<CharacterAnimator>();
        SetPrivateField(charAnim, "spriteRenderer", sr);

        var idleMove = robert.AddComponent<CharacterIdleMovement>();
        SetPrivateField(idleMove, "spriteRoot", spriteChild.transform);

        var controller = robert.AddComponent<PlayerController2D>();
        controller.characterAnimator = charAnim;
        controller.idleMovement = idleMove;

        var clickHandler = robert.AddComponent<ClickToMoveHandler>();
        SetPrivateField(clickHandler, "interactableLayer", interactableLayerMask);
        SetPrivateField(clickHandler, "characterLayer", characterLayerMask);

        Debug.Log("[SceneSetup] Robert (player) created — static scene, point-and-click only.");
    }

    // ─────────────────────────────────────────────────────────────────
    //  INTERACTABLES
    // ─────────────────────────────────────────────────────────────────

    static void SetupInteractables()
    {
        int interactLayer = LayerMask.NameToLayer("Interactables");
        var root = new GameObject("interactables");
        root.transform.position = Vector3.zero;

        CreateInteractable<InteractableObject2D>(root, "WeddingPhoto", interactLayer,
            new Vector3(-2f, 0.5f, 0), "Objects/wedding_photo.png", "Objects/wedding_photo_glow.png",
            "wedding_photo", "Wedding Photo", "wedding_photo", new Vector2(0.5f, 0.5f));

        CreateInteractable<InteractableObject2D>(root, "Guitar", interactLayer,
            new Vector3(3f, -0.5f, 0), "Objects/guitar.png", "Objects/guitar_glow.png",
            "guitar", "Guitar", "guitar", new Vector2(0.5f, 0.8f));

        CreateInteractable<InteractableObject2D>(root, "IcePicks", interactLayer,
            new Vector3(-3f, 1f, 0), "Objects/ice_picks.png", "Objects/ice_picks_glow.png",
            "ice_picks", "Ice Picks", "ice_picks", new Vector2(0.6f, 0.4f));

        CreateInteractable<PhoneInteraction>(root, "Phone", interactLayer,
            new Vector3(2f, 0f, 0), "Objects/phone.png", "Objects/phone_glow.png",
            "phone", "Phone", "phone", new Vector2(0.4f, 0.3f));

        CreateInteractable<DocumentInteraction>(root, "Document", interactLayer,
            new Vector3(1f, -0.5f, 0), "Objects/document.png", "Objects/document_glow.png",
            "document", "The Document", "document", new Vector2(0.5f, 0.4f));

        CreateInteractable<ComputerInteraction>(root, "Computer", interactLayer,
            new Vector3(2f, 0.5f, 0), "Objects/computer.png", "Objects/computer_glow.png",
            "computer", "Computer", "computer", new Vector2(0.6f, 0.5f));

        Debug.Log("[SceneSetup] Interactables created.");
    }

    static void CreateInteractable<T>(GameObject parent, string name, int layer,
        Vector3 pos, string spritePath, string glowPath,
        string objectId, string displayName, string memoryId, Vector2 colliderSize)
        where T : InteractableObject2D
    {
        var go = CreateChild(parent, name);
        go.transform.localPosition = pos;
        SetLayer(go, layer, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spritePath);
        SetSortingLayer(sr, "Objects", 0);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = colliderSize;

        var glowGo = CreateChild(go, "Glow");
        var glowSr = glowGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = LoadSprite(glowPath);
        SetSortingLayer(glowSr, "Objects", -1);

        var interactable = go.AddComponent<T>();
        SetPrivateField(interactable, "objectId", objectId);
        SetPrivateField(interactable, "displayName", displayName);
        SetPrivateField(interactable, "memoryId", memoryId);
        SetPrivateField(interactable, "spriteRenderer", sr);
        SetPrivateField(interactable, "highlightRenderer", glowSr);
    }

    // ─────────────────────────────────────────────────────────────────
    //  MARTHA (NPC)
    // ─────────────────────────────────────────────────────────────────

    static void SetupMartha()
    {
        int charLayer = LayerMask.NameToLayer("Characters");

        var martha = new GameObject("Martha");
        martha.transform.position = new Vector3(-1f, -1f, 0f);
        SetLayer(martha, charLayer);

        var spriteChild = CreateChild(martha, "Sprite");
        var sr = spriteChild.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Characters/Martha/martha_placeholder.png");
        SetSortingLayer(sr, "Characters", 0);

        var col = martha.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 0.75f);

        var charAnim = martha.AddComponent<CharacterAnimator>();
        SetPrivateField(charAnim, "spriteRenderer", sr);

        var idleMove = martha.AddComponent<CharacterIdleMovement>();
        SetPrivateField(idleMove, "spriteRoot", spriteChild.transform);
        // Disable tremor for NPC — Martha doesn't have ALS
        var tremorConfig = new CharacterIdleMovement.TremorConfig
        {
            enabled     = false,
            amount      = 0f,
            duration    = 0.3f,
            intervalMin = 5f,
            intervalMax = 10f
        };
        SetPrivateField(idleMove, "tremor", tremorConfig);

        var npcCtrl = martha.AddComponent<NPCController>();
        SetPrivateField(npcCtrl, "npcId", "martha");
        SetPrivateField(npcCtrl, "displayName", "Martha");
        SetPrivateField(npcCtrl, "facePlayer", true);
        SetPrivateField(npcCtrl, "spriteRenderer", sr);
        SetPrivateField(npcCtrl, "characterAnimator", charAnim);
        SetPrivateField(npcCtrl, "idleMovement", idleMove);

        Debug.Log("[SceneSetup] Martha (NPC) created.");
    }

    // ─────────────────────────────────────────────────────────────────
    //  CANVAS / UI
    // ─────────────────────────────────────────────────────────────────

    static GameObject SetupCanvas()
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Dialogue Panel (bottom, full width) ──────────────────────
        const float dialoguePanelHeight = 160f;
        var dialoguePanel = CreateUIPanel(canvasGo, "DialoguePanel",
            new Color32(30, 30, 50, 220), AnchorPreset.BottomStretch, dialoguePanelHeight);
        SetStretchBottom(dialoguePanel, dialoguePanelHeight, 24f);
        dialoguePanel.transform.SetAsLastSibling();

        var portrait = CreateUIImage(dialoguePanel, "CharacterPortrait", 64, 64);
        SetAnchored(portrait, new Vector2(0, 0), new Vector2(0, 0), new Vector2(44, 78), new Vector2(64, 64));
        var portraitImg = portrait.GetComponent<Image>();
        portraitImg.sprite = LoadSprite("Characters/Martha/martha_portrait.png");

        var charName = CreateUIText(dialoguePanel, "CharacterName", "Martha", 17, Color.white);
        SetAnchored(charName, new Vector2(0, 1), new Vector2(0, 1), new Vector2(146, -12), new Vector2(220, 28));

        var dialogueText = CreateUIText(dialoguePanel, "DialogueText", "", 13, new Color(0.87f, 0.87f, 0.87f));
        var dialogueRect = dialogueText.GetComponent<RectTransform>();
        dialogueRect.anchorMin = new Vector2(0.12f, 0.25f);
        dialogueRect.anchorMax = new Vector2(0.98f, 0.85f);
        dialogueRect.offsetMin = Vector2.zero;
        dialogueRect.offsetMax = Vector2.zero;
        var dtText = dialogueText.GetComponent<TMP_Text>();
        dtText.alignment = TextAlignmentOptions.TopLeft;
        dtText.enableWordWrapping = true;

        var inputFieldGo = CreateUIInputField(dialoguePanel, "InputField", "What do you want to say...");
        var inputRect = inputFieldGo.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.12f, 0f);
        inputRect.anchorMax = new Vector2(0.82f, 0.24f);
        inputRect.offsetMin = new Vector2(4, 6);
        inputRect.offsetMax = new Vector2(-4, -4);

        var sendBtn = CreateUIButton(dialoguePanel, "SendButton", "Send");
        var sendRect = sendBtn.GetComponent<RectTransform>();
        sendRect.anchorMin = new Vector2(0.84f, 0f);
        sendRect.anchorMax = new Vector2(1f, 0.24f);
        sendRect.offsetMin = new Vector2(4, 6);
        sendRect.offsetMax = new Vector2(-8, -4);

        var closeBtn = CreateUIButton(dialoguePanel, "CloseButton", "\u2715  Esc");
        var closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-6, -6);
        closeRect.sizeDelta = new Vector2(60, 24);
        closeBtn.GetComponent<Image>().color = new Color32(80, 40, 40, 200);
        var closeTmp = closeBtn.GetComponentInChildren<TMP_Text>();
        if (closeTmp != null) closeTmp.fontSize = 12;

        var thinkingGo = new GameObject("ThinkingIndicator", typeof(RectTransform));
        thinkingGo.transform.SetParent(dialoguePanel.transform, false);
        var thinkText = CreateUIText(thinkingGo, "ThinkingText", "Thinking...", 14, new Color(0.7f, 0.7f, 0.8f));
        var thinkRect = thinkText.GetComponent<RectTransform>();
        thinkRect.anchorMin = Vector2.zero;
        thinkRect.anchorMax = Vector2.one;
        thinkRect.offsetMin = Vector2.zero;
        thinkRect.offsetMax = Vector2.zero;
        thinkingGo.SetActive(false);

        dialoguePanel.SetActive(false);

        // ── Monologue Panel ───────────────────────────
        var monoPanel = CreateUIPanel(canvasGo, "MonologuePanel",
            new Color32(0, 0, 0, 180), AnchorPreset.TopCenter, 80f);
        var monoRect = monoPanel.GetComponent<RectTransform>();
        monoRect.anchorMin = new Vector2(0.24f, 0.88f);
        monoRect.anchorMax = new Vector2(0.76f, 0.96f);
        monoRect.offsetMin = Vector2.zero;
        monoRect.offsetMax = Vector2.zero;

        var monoText = CreateUIText(monoPanel, "MonologueText", "", 16, new Color(0.67f, 0.67f, 0.8f));
        StretchFill(monoText, 10f);
        var monoTmp = monoText.GetComponent<TMP_Text>();
        monoTmp.fontStyle = FontStyles.Italic;
        monoTmp.alignment = TextAlignmentOptions.Center;

        monoPanel.SetActive(false);

        // ── Decision Panel ────────────────────────────
        var decPanel = CreateUIPanel(canvasGo, "DecisionPanel",
            new Color32(20, 20, 40, 240), AnchorPreset.Center, 300f);
        var decRect = decPanel.GetComponent<RectTransform>();
        decRect.anchorMin = new Vector2(0.3f, 0.3f);
        decRect.anchorMax = new Vector2(0.7f, 0.7f);
        decRect.offsetMin = Vector2.zero;
        decRect.offsetMax = Vector2.zero;

        var decPrompt = CreateUIText(decPanel, "PromptText", "The document lies before you. What do you do?", 20, Color.white);
        var dpRect = decPrompt.GetComponent<RectTransform>();
        dpRect.anchorMin = new Vector2(0.05f, 0.5f);
        dpRect.anchorMax = new Vector2(0.95f, 0.95f);
        dpRect.offsetMin = Vector2.zero;
        dpRect.offsetMax = Vector2.zero;
        decPrompt.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        var signBtn = CreateUIButton(decPanel, "SignButton", "Sign");
        var signRect = signBtn.GetComponent<RectTransform>();
        signRect.anchorMin = new Vector2(0.1f, 0.1f);
        signRect.anchorMax = new Vector2(0.45f, 0.4f);
        signRect.offsetMin = Vector2.zero;
        signRect.offsetMax = Vector2.zero;

        var tearBtn = CreateUIButton(decPanel, "TearButton", "Tear Up");
        var tearRect = tearBtn.GetComponent<RectTransform>();
        tearRect.anchorMin = new Vector2(0.55f, 0.1f);
        tearRect.anchorMax = new Vector2(0.9f, 0.4f);
        tearRect.offsetMin = Vector2.zero;
        tearRect.offsetMax = Vector2.zero;

        decPanel.SetActive(false);

        // ── End Panel ─────────────────────────────────
        var endPanel = CreateUIPanel(canvasGo, "EndPanel",
            new Color32(0, 0, 0, 255), AnchorPreset.Stretch, 0f);
        StretchFill(endPanel);

        var endCg = endPanel.GetComponent<CanvasGroup>();
        if (endCg == null)
            endCg = endPanel.AddComponent<CanvasGroup>();
        endCg.alpha = 0f;

        var quoteText = CreateUIText(endPanel, "QuoteText", "", 24, Color.white);
        var qRect = quoteText.GetComponent<RectTransform>();
        qRect.anchorMin = new Vector2(0.1f, 0.35f);
        qRect.anchorMax = new Vector2(0.9f, 0.65f);
        qRect.offsetMin = Vector2.zero;
        qRect.offsetMax = Vector2.zero;
        var qTmp = quoteText.GetComponent<TMP_Text>();
        qTmp.fontStyle = FontStyles.Italic;
        qTmp.alignment = TextAlignmentOptions.Center;

        var attribText = CreateUIText(endPanel, "AttributionText", "", 16, new Color(0.6f, 0.6f, 0.6f));
        var aRect = attribText.GetComponent<RectTransform>();
        aRect.anchorMin = new Vector2(0.3f, 0.25f);
        aRect.anchorMax = new Vector2(0.7f, 0.35f);
        aRect.offsetMin = Vector2.zero;
        aRect.offsetMax = Vector2.zero;
        attribText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        endPanel.SetActive(false);

        // ── Computer screen overlay (darken scene + centered old-window UI) ─
        var computerOverlay = CreateUIPanel(canvasGo, "ComputerOverlay",
            new Color32(0, 0, 0, 150), AnchorPreset.Stretch, 0f);
        StretchFill(computerOverlay);

        // ── Computer Panel ────────────────────────────
        var compPanel = CreateUIPanel(computerOverlay, "ComputerPanel",
            new Color32(22, 28, 44, 248), AnchorPreset.Center, 400f);
        var cpRect = compPanel.GetComponent<RectTransform>();
        cpRect.anchorMin = new Vector2(0.25f, 0.2f);
        cpRect.anchorMax = new Vector2(0.75f, 0.8f);
        cpRect.offsetMin = Vector2.zero;
        cpRect.offsetMax = Vector2.zero;

        var headerBar = CreateUIPanel(compPanel, "HeaderBar",
            new Color32(49, 82, 170, 255), AnchorPreset.TopCenter, 40f);
        var hbRect = headerBar.GetComponent<RectTransform>();
        hbRect.anchorMin = new Vector2(0f, 0.9f);
        hbRect.anchorMax = new Vector2(1f, 1f);
        hbRect.offsetMin = Vector2.zero;
        hbRect.offsetMax = Vector2.zero;
        var hbText = CreateUIText(headerBar, "TitleText", "SECURE ACCESS TERMINAL", 16, Color.white);
        StretchFill(hbText, 8f);
        hbText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        var questionText = CreateUIText(compPanel, "QuestionText", "", 22, new Color(0.0f, 0.85f, 0.35f));
        var qtRect = questionText.GetComponent<RectTransform>();
        qtRect.anchorMin = new Vector2(0.06f, 0.56f);
        qtRect.anchorMax = new Vector2(0.94f, 0.9f);
        qtRect.offsetMin = Vector2.zero;
        qtRect.offsetMax = Vector2.zero;
        questionText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        var answerInput = CreateUIInputField(compPanel, "AnswerInput", "Type your answer...");
        var answerRect = answerInput.GetComponent<RectTransform>();
        answerRect.anchorMin = new Vector2(0.1f, 0.29f);
        answerRect.anchorMax = new Vector2(0.69f, 0.43f);
        answerRect.offsetMin = Vector2.zero;
        answerRect.offsetMax = Vector2.zero;

        var compSubmitBtn = CreateUIButton(compPanel, "SubmitButton", "SUBMIT");
        var csbRect = compSubmitBtn.GetComponent<RectTransform>();
        csbRect.anchorMin = new Vector2(0.71f, 0.29f);
        csbRect.anchorMax = new Vector2(0.9f, 0.43f);
        csbRect.offsetMin = Vector2.zero;
        csbRect.offsetMax = Vector2.zero;
        compSubmitBtn.GetComponent<Image>().color = new Color32(0, 100, 40, 255);

        var feedbackText = CreateUIText(compPanel, "FeedbackText", "", 16, new Color(1f, 0.3f, 0.3f));
        var fbRect = feedbackText.GetComponent<RectTransform>();
        fbRect.anchorMin = new Vector2(0.1f, 0.16f);
        fbRect.anchorMax = new Vector2(0.9f, 0.26f);
        fbRect.offsetMin = Vector2.zero;
        fbRect.offsetMax = Vector2.zero;
        feedbackText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        var compCloseBtn = CreateUIButton(compPanel, "CloseButton", "\u2715");
        var ccbRect = compCloseBtn.GetComponent<RectTransform>();
        ccbRect.anchorMin = new Vector2(1, 1);
        ccbRect.anchorMax = new Vector2(1, 1);
        ccbRect.pivot = new Vector2(1, 1);
        ccbRect.anchoredPosition = new Vector2(-8, -8);
        ccbRect.sizeDelta = new Vector2(40, 40);
        compCloseBtn.GetComponent<Image>().color = new Color32(80, 40, 40, 200);

        compPanel.SetActive(false);
        computerOverlay.SetActive(false);

        // ── Final Prompt Panel ───────────────────────────
        var finalPanel = CreateUIPanel(computerOverlay, "FinalPromptPanel",
            new Color32(10, 10, 20, 250), AnchorPreset.Center, 300f);
        var fpRect = finalPanel.GetComponent<RectTransform>();
        fpRect.anchorMin = new Vector2(0.28f, 0.24f);
        fpRect.anchorMax = new Vector2(0.72f, 0.76f);
        fpRect.offsetMin = Vector2.zero;
        fpRect.offsetMax = Vector2.zero;

        var finalText = CreateUIText(finalPanel, "FinalPromptText",
            "FINAL SECURITY CHECK\n\nCan you forgive yourself?", 24, Color.white);
        var ftRect = finalText.GetComponent<RectTransform>();
        ftRect.anchorMin = new Vector2(0.05f, 0.5f);
        ftRect.anchorMax = new Vector2(0.95f, 0.95f);
        ftRect.offsetMin = Vector2.zero;
        ftRect.offsetMax = Vector2.zero;
        finalText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        var finalSignBtn = CreateUIButton(finalPanel, "SignButton", "Sign");
        var fsRect = finalSignBtn.GetComponent<RectTransform>();
        fsRect.anchorMin = new Vector2(0.1f, 0.08f);
        fsRect.anchorMax = new Vector2(0.45f, 0.35f);
        fsRect.offsetMin = Vector2.zero;
        fsRect.offsetMax = Vector2.zero;
        finalSignBtn.GetComponent<Image>().color = new Color32(100, 40, 40, 255);

        var finalTearBtn = CreateUIButton(finalPanel, "TearButton", "Tear Up");
        var ftbRect = finalTearBtn.GetComponent<RectTransform>();
        ftbRect.anchorMin = new Vector2(0.55f, 0.08f);
        ftbRect.anchorMax = new Vector2(0.9f, 0.35f);
        ftbRect.offsetMin = Vector2.zero;
        ftbRect.offsetMax = Vector2.zero;
        finalTearBtn.GetComponent<Image>().color = new Color32(40, 60, 100, 255);

        finalPanel.SetActive(false);

        // ── Interaction Prompt ────────────────────────
        var promptRoot = new GameObject("InteractionPrompt", typeof(RectTransform));
        promptRoot.transform.SetParent(canvasGo.transform, false);

        var promptPanel = CreateUIPanel(promptRoot, "PromptPanel",
            new Color32(0, 0, 0, 160), AnchorPreset.Center, 40f);
        var ppRect = promptPanel.GetComponent<RectTransform>();
        ppRect.sizeDelta = new Vector2(300, 40);

        var promptText = CreateUIText(promptPanel, "PromptText", "", 14, Color.white);
        StretchFill(promptText, 5f);
        promptText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        promptRoot.SetActive(false);

        // ── Fade Overlay ──────────────────────────────
        var fadeOverlay = CreateUIPanel(canvasGo, "FadeOverlay",
            new Color32(0, 0, 0, 255), AnchorPreset.Stretch, 0f);
        StretchFill(fadeOverlay);

        var fadeCg = fadeOverlay.GetComponent<CanvasGroup>();
        if (fadeCg == null)
            fadeCg = fadeOverlay.AddComponent<CanvasGroup>();
        fadeCg.alpha = 0f;
        fadeCg.blocksRaycasts = false;

        // ── Download Progress Panel (first-run model download) ────────
        var dlPanel = CreateDownloadProgressPanel(canvasGo);

        // ── Game view layout (full-screen camera, dialogue at bottom) ─
        var layout = canvasGo.AddComponent<GameViewLayout>();
        SetPrivateField(layout, "dialoguePanelHeight", dialoguePanelHeight);

        // ── Attach UI scripts to Canvas ───────────────
        var dialogueUI = canvasGo.AddComponent<DialogueUI>();
        SetPrivateField(dialogueUI, "dialoguePanel", dialoguePanel);
        SetPrivateField(dialogueUI, "characterPortrait", portraitImg);
        SetPrivateField(dialogueUI, "characterNameText", charName.GetComponent<TMP_Text>());
        SetPrivateField(dialogueUI, "dialogueText", dialogueText.GetComponent<TMP_Text>());
        SetPrivateField(dialogueUI, "inputField", inputFieldGo.GetComponent<TMP_InputField>());
        SetPrivateField(dialogueUI, "sendButton", sendBtn.GetComponent<Button>());
        SetPrivateField(dialogueUI, "closeButton", closeBtn.GetComponent<Button>());
        SetPrivateField(dialogueUI, "thinkingIndicator", thinkingGo);
        SetPrivateField(dialogueUI, "monologuePanel", monoPanel);
        SetPrivateField(dialogueUI, "monologueText", monoText.GetComponent<TMP_Text>());
        SetPrivateField(dialogueUI, "marthaPortrait", LoadSprite("Characters/Martha/martha_portrait.png"));

        var decisionUI = canvasGo.AddComponent<DecisionUI>();
        SetPrivateField(decisionUI, "decisionPanel", decPanel);
        SetPrivateField(decisionUI, "signButton", signBtn.GetComponent<Button>());
        SetPrivateField(decisionUI, "tearButton", tearBtn.GetComponent<Button>());
        SetPrivateField(decisionUI, "promptText", decPrompt.GetComponent<TMP_Text>());
        SetPrivateField(decisionUI, "promptMessage", "FINAL SECURITY CHECK\n\nCan you forgive yourself?");

        var endScreen = canvasGo.AddComponent<EndScreen>();
        SetPrivateField(endScreen, "endPanel", endPanel);
        SetPrivateField(endScreen, "quoteText", quoteText.GetComponent<TMP_Text>());
        SetPrivateField(endScreen, "attributionText", attribText.GetComponent<TMP_Text>());
        SetPrivateField(endScreen, "endCanvasGroup", endCg);

        var interactionPrompt = canvasGo.AddComponent<LastDay.UI.InteractionPrompt>();
        SetPrivateField(interactionPrompt, "promptPanel", promptPanel);
        SetPrivateField(interactionPrompt, "promptText", promptText.GetComponent<TMP_Text>());

        var downloadPanel = canvasGo.AddComponent<LastDay.UI.DownloadProgressPanel>();
        SetPrivateField(downloadPanel, "panel", dlPanel);
        var dlTitle     = dlPanel.transform.Find("TitleText");
        var dlModelInfo = dlPanel.transform.Find("ModelInfoText");
        var dlStatus    = dlPanel.transform.Find("StatusText");
        var dlPercent   = dlPanel.transform.Find("PercentText");
        var dlBar       = dlPanel.transform.Find("ProgressBar");
        var dlErrGroup  = dlPanel.transform.Find("ErrorGroup");
        var dlErrText   = dlErrGroup?.Find("ErrorText");
        var dlRetry     = dlErrGroup?.Find("RetryButton");
        var dlQuit      = dlErrGroup?.Find("QuitButton");
        if (dlTitle     != null) SetPrivateField(downloadPanel, "titleText",     dlTitle.GetComponent<TMP_Text>());
        if (dlModelInfo != null) SetPrivateField(downloadPanel, "modelInfoText", dlModelInfo.GetComponent<TMP_Text>());
        if (dlStatus    != null) SetPrivateField(downloadPanel, "statusText",    dlStatus.GetComponent<TMP_Text>());
        if (dlPercent   != null) SetPrivateField(downloadPanel, "percentText",   dlPercent.GetComponent<TMP_Text>());
        if (dlBar       != null) SetPrivateField(downloadPanel, "progressBar",   dlBar.GetComponent<Slider>());
        if (dlErrGroup  != null) SetPrivateField(downloadPanel, "errorGroup",    dlErrGroup.gameObject);
        if (dlErrText   != null) SetPrivateField(downloadPanel, "errorText",     dlErrText.GetComponent<TMP_Text>());
        if (dlRetry     != null) SetPrivateField(downloadPanel, "retryButton",   dlRetry.GetComponent<Button>());
        if (dlQuit      != null) SetPrivateField(downloadPanel, "quitButton",    dlQuit.GetComponent<Button>());

        // Wire computer interaction overlay references
        var compInteract = GameObject.Find("interactables/Computer")?.GetComponent<ComputerInteraction>();
        if (compInteract != null)
        {
            SetPrivateField(compInteract, "computerOverlay", computerOverlay);
            SetPrivateField(compInteract, "computerPanel", compPanel);
            SetPrivateField(compInteract, "computerWindowRect", cpRect);
            SetPrivateField(compInteract, "questionText", questionText.GetComponent<TMP_Text>());
            SetPrivateField(compInteract, "feedbackText", feedbackText.GetComponent<TMP_Text>());
            SetPrivateField(compInteract, "answerInputField", answerInput.GetComponent<TMP_InputField>());
            SetPrivateField(compInteract, "submitButton", compSubmitBtn.GetComponent<Button>());
            SetPrivateField(compInteract, "closeButton", compCloseBtn.GetComponent<Button>());
            SetPrivateField(compInteract, "finalPromptPanel", finalPanel);
            SetPrivateField(compInteract, "finalPromptText", finalText.GetComponent<TMP_Text>());
            SetPrivateField(compInteract, "signButton", finalSignBtn.GetComponent<Button>());
            SetPrivateField(compInteract, "tearButton", finalTearBtn.GetComponent<Button>());
        }

        Debug.Log("[SceneSetup] Canvas and all UI created.");
        return canvasGo;
    }

    // ─────────────────────────────────────────────────────────────────
    //  EVENT SYSTEM
    // ─────────────────────────────────────────────────────────────────

    static void SetupEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        Debug.Log("[SceneSetup] EventSystem created.");
    }

    // ─────────────────────────────────────────────────────────────────
    //  CROSS-REFERENCES (wired after all objects exist)
    // ─────────────────────────────────────────────────────────────────

    static void WireCrossReferences(GameObject managers, GameObject canvas)
    {
        var gmGo = managers.transform.Find("GameManager");
        var llmGo = managers.transform.Find("LocalLLMManager");
        if (gmGo != null && llmGo != null)
        {
            var gm = gmGo.GetComponent<GameManager>();
            var llm = llmGo.GetComponent<LocalLLMManager>();
            SetPrivateField(gm, "llmManager", llm);
        }

        var fmGo = managers.transform.Find("FadeManager");
        var fadeOverlay = canvas.transform.Find("FadeOverlay");
        if (fmGo != null && fadeOverlay != null)
        {
            var fm = fmGo.GetComponent<FadeManager>();
            var fadeCg = fadeOverlay.GetComponent<CanvasGroup>();
            SetPrivateField(fm, "fadeCanvasGroup", fadeCg);
        }

        var mcGo = managers.transform.Find("MemoryContext");
        if (mcGo != null)
        {
            WireMemoryAssets(mcGo.GetComponent<MemoryContext>());
        }

        // Wire ComputerInteraction to its UI panels
        var computerGo = GameObject.Find("Computer");
        if (computerGo != null)
        {
            var compInteract = computerGo.GetComponent<ComputerInteraction>();
            if (compInteract != null)
            {
                var overlay = canvas.transform.Find("ComputerOverlay");
                if (overlay != null)
                    SetPrivateField(compInteract, "computerOverlay", overlay.gameObject);

                var compPanel = canvas.transform.Find("ComputerOverlay/ComputerPanel");
                if (compPanel == null)
                    compPanel = canvas.transform.Find("ComputerPanel");
                if (compPanel != null)
                {
                    SetPrivateField(compInteract, "computerPanel", compPanel.gameObject);
                    SetPrivateField(compInteract, "computerWindowRect", compPanel.GetComponent<RectTransform>());
                    var qt = compPanel.Find("QuestionText");
                    if (qt != null) SetPrivateField(compInteract, "questionText", qt.GetComponent<TMP_Text>());
                    var fb = compPanel.Find("FeedbackText");
                    if (fb != null) SetPrivateField(compInteract, "feedbackText", fb.GetComponent<TMP_Text>());
                    var ai = compPanel.Find("AnswerInput");
                    if (ai != null) SetPrivateField(compInteract, "answerInputField", ai.GetComponent<TMP_InputField>());
                    var sb = compPanel.Find("SubmitButton");
                    if (sb != null) SetPrivateField(compInteract, "submitButton", sb.GetComponent<Button>());
                    var cb = compPanel.Find("CloseButton");
                    if (cb != null) SetPrivateField(compInteract, "closeButton", cb.GetComponent<Button>());
                }

                var finalPanel = canvas.transform.Find("ComputerOverlay/FinalPromptPanel");
                if (finalPanel == null)
                    finalPanel = canvas.transform.Find("FinalPromptPanel");
                if (finalPanel != null)
                {
                    SetPrivateField(compInteract, "finalPromptPanel", finalPanel.gameObject);
                    var ft = finalPanel.Find("FinalPromptText");
                    if (ft != null) SetPrivateField(compInteract, "finalPromptText", ft.GetComponent<TMP_Text>());
                    var fsb = finalPanel.Find("SignButton");
                    if (fsb != null) SetPrivateField(compInteract, "signButton", fsb.GetComponent<Button>());
                    var ftb = finalPanel.Find("TearButton");
                    if (ftb != null) SetPrivateField(compInteract, "tearButton", ftb.GetComponent<Button>());
                }
            }
        }

        // Wire phone ring and pickup clips
        var phoneGo = GameObject.Find("Phone");
        if (phoneGo != null)
        {
            var phoneInteract = phoneGo.GetComponent<PhoneInteraction>();
            if (phoneInteract != null)
            {
                var ringClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/phone_ringing.mp3");
                if (ringClip != null) SetPrivateField(phoneInteract, "ringSound", ringClip);
            }
        }

        Debug.Log("[SceneSetup] Cross-references wired.");
    }

    static void WireMemoryAssets(MemoryContext mc)
    {
        string[] guids = AssetDatabase.FindAssets("t:MemoryData", new[] { "Assets/Data" });
        if (guids.Length == 0) return;

        var so = new SerializedObject(mc);
        var listProp = so.FindProperty("allMemories");
        listProp.ClearArray();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<MemoryData>(path);
            if (asset != null)
            {
                listProp.InsertArrayElementAtIndex(listProp.arraySize);
                listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = asset;
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ─────────────────────────────────────────────────────────────────
    //  MEMORY DATA SCRIPTABLE OBJECTS
    // ─────────────────────────────────────────────────────────────────

    static void CreateMemory(string folder, string assetName, string memId, string objName,
        string shortDesc, string fullStory, string marthaCtx, string davidCtx)
    {
        string path = $"{folder}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<MemoryData>(path);
        if (existing != null) return;

        var mem = ScriptableObject.CreateInstance<MemoryData>();
        mem.memoryId = memId;
        mem.objectName = objName;
        mem.shortDescription = shortDesc;
        mem.fullStory = fullStory;
        mem.marthaContext = marthaCtx;
        mem.davidContext = davidCtx;

        mem.objectSprite = LoadSprite($"Objects/{memId}.png");
        mem.glowSprite = LoadSprite($"Objects/{memId}_glow.png");

        AssetDatabase.CreateAsset(mem, path);
    }

    // ─────────────────────────────────────────────────────────────────
    //  UI HELPERS
    // ─────────────────────────────────────────────────────────────────

    enum AnchorPreset { BottomStretch, TopCenter, Center, Stretch }

    static GameObject CreateChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = Vector3.zero;
        return go;
    }

    static GameObject CreateUIPanel(GameObject parent, string name, Color32 color,
        AnchorPreset anchor, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static GameObject CreateUIImage(GameObject parent, string name, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
        return go;
    }

    static GameObject CreateUIText(GameObject parent, string name, string text, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.enableWordWrapping = true;
        return go;
    }

    static GameObject CreateUIButton(GameObject parent, string name, string label)
        => CreateUIButton(parent, name, label, new Color32(60, 60, 80, 255));

    static GameObject CreateUIButton(GameObject parent, string name, string label, Color32 bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = bgColor;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        StretchFill(textGo);

        return go;
    }

    static GameObject CreateUIInputField(GameObject parent, string name, string placeholder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = new Color32(40, 40, 60, 255);

        var textArea = new GameObject("Text Area", typeof(RectTransform));
        textArea.transform.SetParent(go.transform, false);
        var taRect = textArea.GetComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(10, 2);
        taRect.offsetMax = new Vector2(-10, -2);
        textArea.AddComponent<RectMask2D>();

        var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
        placeholderGo.transform.SetParent(textArea.transform, false);
        var phTmp = placeholderGo.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.fontSize = 14;
        phTmp.color = new Color(0.5f, 0.5f, 0.6f);
        phTmp.fontStyle = FontStyles.Italic;
        StretchFill(placeholderGo);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(textArea.transform, false);
        var txtTmp = textGo.AddComponent<TextMeshProUGUI>();
        txtTmp.fontSize = 14;
        txtTmp.color = Color.white;
        StretchFill(textGo);

        var inputField = go.AddComponent<TMP_InputField>();
        inputField.textViewport = taRect;
        inputField.textComponent = txtTmp;
        inputField.placeholder = phTmp;
        inputField.fontAsset = txtTmp.font;

        return go;
    }

    static void SetAnchored(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }


    static void SetStretchBottom(GameObject go, float height, float margin)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.offsetMin = new Vector2(margin, 10);
        rect.offsetMax = new Vector2(-margin, height);
    }

    static void StretchFill(GameObject go, float padding = 0f)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    // ── Cinematic Dialogue Tests ──────────────────────────────────────────

    [MenuItem("LastDay/Test: Cinematic Dialogue", priority = 10)]
    public static void TestCinematicDialogue()
    {
        if (!Application.isPlaying)
        {
            bool enter = UnityEditor.EditorUtility.DisplayDialog(
                "Cinematic Dialogue Tests",
                "Tests run in Play Mode.\n\nEnter Play Mode now?",
                "Enter Play Mode", "Cancel");
            if (enter)
            {
                // Attach component before entering play — it will run on Start()
                AttachTestComponent();
                UnityEditor.EditorApplication.isPlaying = true;
            }
            return;
        }

        AttachTestComponent();
    }

    private static void AttachTestComponent()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[Test] Canvas not found. Run 'LastDay/Patch: Apply Cinematic Dialogue' first.");
            return;
        }

        // Avoid duplicates
        var existing = canvas.GetComponent<LastDay.Tests.CinematicDialogueTest>();
        if (existing != null)
        {
            Debug.Log("[Test] CinematicDialogueTest already attached — triggering run.");
            existing.RunTestsNow();
            return;
        }

        var test = canvas.AddComponent<LastDay.Tests.CinematicDialogueTest>();
        Debug.Log("[Test] CinematicDialogueTest attached to Canvas. " +
            (Application.isPlaying ? "Running now." : "Will auto-run on Play Mode start."));

        if (Application.isPlaying)
            test.RunTestsNow();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Setup: Download Progress Panel
    // ─────────────────────────────────────────────────────────────────────────────

    [MenuItem("LastDay/Setup: Download Progress Panel", priority = 5)]
    public static void SetupDownloadProgressPanel()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[Setup] Canvas not found. Run 'Setup Scene (Full)' first.");
            return;
        }

        var existing = canvas.transform.Find("DownloadProgressPanel");
        if (existing != null)
        {
            Debug.Log("[Setup] DownloadProgressPanel already exists.");
            return;
        }

        CreateDownloadProgressPanel(canvas);

        var dlPanel = canvas.transform.Find("DownloadProgressPanel");
        var dlComp = canvas.GetComponent<LastDay.UI.DownloadProgressPanel>();
        if (dlComp == null)
            dlComp = canvas.AddComponent<LastDay.UI.DownloadProgressPanel>();

        if (dlPanel != null)
        {
            SetPrivateField(dlComp, "panel", dlPanel.gameObject);
            var dlTitle     = dlPanel.Find("TitleText");
            var dlModelInfo = dlPanel.Find("ModelInfoText");
            var dlStatus    = dlPanel.Find("StatusText");
            var dlPercent   = dlPanel.Find("PercentText");
            var dlBar       = dlPanel.Find("ProgressBar");
            var dlErrGroup  = dlPanel.Find("ErrorGroup");
            var dlErrText   = dlErrGroup?.Find("ErrorText");
            var dlRetry     = dlErrGroup?.Find("RetryButton");
            var dlQuit      = dlErrGroup?.Find("QuitButton");
            if (dlTitle     != null) SetPrivateField(dlComp, "titleText",     dlTitle.GetComponent<TMP_Text>());
            if (dlModelInfo != null) SetPrivateField(dlComp, "modelInfoText", dlModelInfo.GetComponent<TMP_Text>());
            if (dlStatus    != null) SetPrivateField(dlComp, "statusText",    dlStatus.GetComponent<TMP_Text>());
            if (dlPercent   != null) SetPrivateField(dlComp, "percentText",   dlPercent.GetComponent<TMP_Text>());
            if (dlBar       != null) SetPrivateField(dlComp, "progressBar",   dlBar.GetComponent<Slider>());
            if (dlErrGroup  != null) SetPrivateField(dlComp, "errorGroup",    dlErrGroup.gameObject);
            if (dlErrText   != null) SetPrivateField(dlComp, "errorText",     dlErrText.GetComponent<TMP_Text>());
            if (dlRetry     != null) SetPrivateField(dlComp, "retryButton",   dlRetry.GetComponent<Button>());
            if (dlQuit      != null) SetPrivateField(dlComp, "quitButton",    dlQuit.GetComponent<Button>());
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Setup] DownloadProgressPanel created and wired on Canvas.");
    }

    /// <summary>Builds the DownloadProgressPanel hierarchy under a canvas parent and returns the root GameObject.</summary>
    static GameObject CreateDownloadProgressPanel(GameObject canvas)
    {
        // Full-screen dark overlay (starts active so it shows on first launch if needed)
        var root = CreateUIPanel(canvas, "DownloadProgressPanel",
            new Color32(10, 10, 18, 240), AnchorPreset.Stretch, 0f);
        StretchFill(root);
        root.SetActive(false); // DownloadProgressPanel.Awake() activates it when needed

        // Centre column — card
        var card = CreateUIPanel(root, "Card", new Color32(22, 28, 44, 255), AnchorPreset.Center, 0f);
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.25f, 0.3f);
        cardRect.anchorMax = new Vector2(0.75f, 0.7f);
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;

        // Title
        var titleGo = CreateUIText(card, "TitleText", "Last Day — Preparing AI Characters…", 22, Color.white);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.74f);
        titleRect.anchorMax = new Vector2(0.95f, 0.94f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleTmp = titleGo.GetComponent<TMP_Text>();
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.fontStyle = FontStyles.Bold;

        // Model info
        var modelInfoGo = CreateUIText(card, "ModelInfoText", "", 15, new Color(0.7f, 0.7f, 0.7f));
        var miRect = modelInfoGo.GetComponent<RectTransform>();
        miRect.anchorMin = new Vector2(0.05f, 0.62f);
        miRect.anchorMax = new Vector2(0.95f, 0.74f);
        miRect.offsetMin = Vector2.zero;
        miRect.offsetMax = Vector2.zero;
        modelInfoGo.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        // Progress bar (Slider — value-only, no handle)
        var barGo = new GameObject("ProgressBar", typeof(RectTransform));
        barGo.transform.SetParent(card.transform, false);
        var barRect = barGo.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.06f, 0.44f);
        barRect.anchorMax = new Vector2(0.94f, 0.54f);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;

        var slider = barGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        var bgImg = barGo.AddComponent<Image>();
        bgImg.color = new Color32(40, 40, 60, 255);
        slider.targetGraphic = bgImg;

        var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGo.transform.SetParent(barGo.transform, false);
        var faRect = fillAreaGo.GetComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero;
        faRect.anchorMax = Vector2.one;
        faRect.offsetMin = Vector2.zero;
        faRect.offsetMax = Vector2.zero;

        var fillGo = new GameObject("Fill", typeof(RectTransform));
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color32(49, 120, 200, 255);
        slider.fillRect = fillRect;

        // Status text
        var statusGo = CreateUIText(card, "StatusText", "Connecting…", 14, new Color(0.8f, 0.8f, 0.8f));
        var stRect = statusGo.GetComponent<RectTransform>();
        stRect.anchorMin = new Vector2(0.05f, 0.30f);
        stRect.anchorMax = new Vector2(0.78f, 0.42f);
        stRect.offsetMin = Vector2.zero;
        stRect.offsetMax = Vector2.zero;
        statusGo.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;

        // Percent text
        var pctGo = CreateUIText(card, "PercentText", "0%", 16, Color.white);
        var pctRect = pctGo.GetComponent<RectTransform>();
        pctRect.anchorMin = new Vector2(0.80f, 0.30f);
        pctRect.anchorMax = new Vector2(0.95f, 0.42f);
        pctRect.offsetMin = Vector2.zero;
        pctRect.offsetMax = Vector2.zero;
        var pctTmp = pctGo.GetComponent<TMP_Text>();
        pctTmp.alignment = TextAlignmentOptions.MidlineRight;
        pctTmp.fontStyle = FontStyles.Bold;

        // Error group (hidden by default)
        var errGroup = CreateUIPanel(card, "ErrorGroup", new Color32(80, 20, 20, 200), AnchorPreset.Center, 0f);
        var egRect = errGroup.GetComponent<RectTransform>();
        egRect.anchorMin = new Vector2(0.05f, 0.04f);
        egRect.anchorMax = new Vector2(0.95f, 0.28f);
        egRect.offsetMin = Vector2.zero;
        egRect.offsetMax = Vector2.zero;
        errGroup.SetActive(false);

        var errTextGo = CreateUIText(errGroup, "ErrorText", "", 13, new Color(1f, 0.6f, 0.6f));
        var errTxtRect = errTextGo.GetComponent<RectTransform>();
        errTxtRect.anchorMin = new Vector2(0f, 0.45f);
        errTxtRect.anchorMax = new Vector2(1f, 1f);
        errTxtRect.offsetMin = new Vector2(8f, 4f);
        errTxtRect.offsetMax = new Vector2(-8f, -4f);
        var errTmp = errTextGo.GetComponent<TMP_Text>();
        errTmp.alignment = TextAlignmentOptions.TopLeft;
        errTmp.enableWordWrapping = true;

        // Retry / Quit buttons inside ErrorGroup
        var retryGo = CreateUIButton(errGroup, "RetryButton", "Try Again", new Color32(60, 100, 60, 230));
        var retryRect = retryGo.GetComponent<RectTransform>();
        retryRect.anchorMin = new Vector2(0f, 0f);
        retryRect.anchorMax = new Vector2(0.48f, 0.42f);
        retryRect.offsetMin = new Vector2(8f, 4f);
        retryRect.offsetMax = new Vector2(-4f, -4f);

        var quitGo = CreateUIButton(errGroup, "QuitButton", "Quit", new Color32(100, 40, 40, 230));
        var quitRect = quitGo.GetComponent<RectTransform>();
        quitRect.anchorMin = new Vector2(0.52f, 0f);
        quitRect.anchorMax = new Vector2(1f, 0.42f);
        quitRect.offsetMin = new Vector2(4f, 4f);
        quitRect.offsetMax = new Vector2(-8f, -4f);

        // Note
        var noteGo = CreateUIText(root, "NoteText",
            "This is a one-time download. The model will be cached for future sessions.", 12,
            new Color(0.5f, 0.5f, 0.5f));
        var noteRect = noteGo.GetComponent<RectTransform>();
        noteRect.anchorMin = new Vector2(0.1f, 0.22f);
        noteRect.anchorMax = new Vector2(0.9f, 0.28f);
        noteRect.offsetMin = Vector2.zero;
        noteRect.offsetMax = Vector2.zero;
        noteGo.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        Debug.Log("[Setup] DownloadProgressPanel created.");
        return root;
    }


    // ─────────────────────────────────────────────────────────────────────────────
    // Patch: Wire Character Idle Movement
    // ─────────────────────────────────────────────────────────────────────────────

    [MenuItem("LastDay/Patch: Wire Character Idle Movement", priority = 3)]
    public static void PatchWireCharacterIdleMovement()
    {
        int fixed_ = 0;

        // Robert
        var robert = GameObject.Find("Robert");
        if (robert == null)
        {
            Debug.LogWarning("[Patch] 'Robert' GameObject not found in scene.");
        }
        else
        {
            var idleR = robert.GetComponent<LastDay.Player.CharacterIdleMovement>();
            if (idleR == null)
            {
                idleR = robert.AddComponent<LastDay.Player.CharacterIdleMovement>();
                Debug.Log("[Patch] Added CharacterIdleMovement to Robert.");
            }

            var pc = robert.GetComponent<LastDay.Player.PlayerController2D>();
            if (pc != null)
            {
                var so = new UnityEditor.SerializedObject(pc);
                var prop = so.FindProperty("idleMovement");
                if (prop != null)
                {
                    prop.objectReferenceValue = idleR;
                    so.ApplyModifiedProperties();
                    Debug.Log("[Patch] Wired Robert's PlayerController2D.idleMovement → CharacterIdleMovement.");
                    fixed_++;
                }
            }
        }

        // Martha
        var martha = GameObject.Find("Martha");
        if (martha == null)
        {
            Debug.LogWarning("[Patch] 'Martha' GameObject not found in scene.");
        }
        else
        {
            var idleM = martha.GetComponent<LastDay.Player.CharacterIdleMovement>();
            if (idleM == null)
            {
                idleM = martha.AddComponent<LastDay.Player.CharacterIdleMovement>();
                Debug.Log("[Patch] Added CharacterIdleMovement to Martha.");
            }

            var npc = martha.GetComponent<LastDay.NPC.NPCController>();
            if (npc != null)
            {
                var so = new UnityEditor.SerializedObject(npc);
                var prop = so.FindProperty("idleMovement");
                if (prop != null)
                {
                    prop.objectReferenceValue = idleM;
                    so.ApplyModifiedProperties();
                    Debug.Log("[Patch] Wired Martha's NPCController.idleMovement → CharacterIdleMovement.");
                    fixed_++;
                }
            }
        }

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
        Debug.Log($"[Patch] Wire Character Idle Movement complete — {fixed_} reference(s) updated. Scene saved.");
    }
}
