using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System.Reflection;
using LastDay.Core;
using LastDay.Player;
using LastDay.Pathfinding;
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
    private const string PatchTriggerFile = "Assets/Editor/run_patch.trigger";

    [InitializeOnLoadMethod]
    static void CheckForTrigger()
    {
        if (File.Exists(PatchTriggerFile))
        {
            File.Delete(PatchTriggerFile);
            if (File.Exists(PatchTriggerFile + ".meta"))
                File.Delete(PatchTriggerFile + ".meta");

            EditorApplication.delayCall += () =>
            {
                Debug.Log("[SceneSetup] Patch trigger detected. Applying all patches...");
                PatchApplyAll();
                SetupLLMComponents();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                AssetDatabase.Refresh();
                Debug.Log("[SceneSetup] All patches + LLM wiring applied and scene saved.");
            };
            return;
        }

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

    [MenuItem("LastDay/Patch: Add Close Button to Dialogue", priority = 2)]
    public static void PatchAddCloseButton()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[Patch] Canvas not found"); return; }

        var dialogueUI = canvas.GetComponent<DialogueUI>();
        if (dialogueUI == null) { Debug.LogError("[Patch] DialogueUI not found on Canvas"); return; }

        var dialoguePanel = canvas.transform.Find("DialoguePanel");
        if (dialoguePanel == null) { Debug.LogError("[Patch] DialoguePanel not found"); return; }

        var existing = dialoguePanel.Find("CloseButton");
        if (existing != null)
        {
            Debug.Log("[Patch] CloseButton already exists.");
            return;
        }

        var closeBtn = CreateUIButton(dialoguePanel.gameObject, "CloseButton", "\u2715  Esc");
        var closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-8, -8);
        closeRect.sizeDelta = new Vector2(80, 30);
        closeBtn.GetComponent<Image>().color = new Color32(80, 40, 40, 200);
        var closeTmp = closeBtn.GetComponentInChildren<TMP_Text>();
        if (closeTmp != null) closeTmp.fontSize = 14;

        SetPrivateField(dialogueUI, "closeButton", closeBtn.GetComponent<Button>());

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[Patch] Close button added to DialoguePanel. Save the scene.");
    }

    [MenuItem("LastDay/Patch: Apply All Fixes", priority = 2)]
    public static void PatchApplyAll()
    {
        PatchAddCloseButton();
        PatchWireCharacterLayer();
        PatchWireNullSafeManagers();
        PatchAddComputerAndSecurityUI();
        PatchFixEventManagerAndDecisionUI();
        PatchWireAudioClips();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[Patch] All patches applied.");
    }

    [MenuItem("LastDay/Patch: Add Computer + Security UI", priority = 2)]
    public static void PatchAddComputerAndSecurityUI()
    {
        int interactLayer = LayerMask.NameToLayer("Interactables");

        // ── 1. Computer interactable ──────────────────────────
        var interactables = GameObject.Find("interactables");
        if (interactables == null)
        {
            Debug.LogError("[Patch] 'interactables' root not found. Run full scene setup first.");
            return;
        }

        var existingComputer = interactables.transform.Find("Computer");
        if (existingComputer != null)
        {
            Debug.Log("[Patch] Computer interactable already exists, skipping creation.");
        }
        else
        {
            var compGo = CreateChild(interactables, "Computer");
            compGo.transform.localPosition = new Vector3(2f, 0.5f, 0);
            SetLayer(compGo, interactLayer, false);

            var sr = compGo.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Objects/computer.png");
            SetSortingLayer(sr, "Objects", 0);

            var col = compGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.5f);

            var glowGo = CreateChild(compGo, "Glow");
            var glowSr = glowGo.AddComponent<SpriteRenderer>();
            glowSr.sprite = LoadSprite("Objects/computer_glow.png");
            SetSortingLayer(glowSr, "Objects", -1);

            var compInteract = compGo.AddComponent<ComputerInteraction>();
            SetPrivateField(compInteract, "objectId", "computer");
            SetPrivateField(compInteract, "displayName", "Computer");
            SetPrivateField(compInteract, "memoryId", "computer");
            SetPrivateField(compInteract, "spriteRenderer", sr);
            SetPrivateField(compInteract, "highlightRenderer", glowSr);

            Debug.Log("[Patch] Computer interactable created under 'interactables'.");
        }

        // ── 2. Computer UI panels under Canvas ────────────────
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[Patch] Canvas not found.");
            return;
        }

        var computerInteraction = interactables.GetComponentInChildren<ComputerInteraction>();
        if (computerInteraction == null)
        {
            Debug.LogError("[Patch] ComputerInteraction component not found.");
            return;
        }

        // ComputerPanel
        if (canvas.transform.Find("ComputerPanel") == null)
        {
            var compPanel = CreateUIPanel(canvas, "ComputerPanel",
                new Color32(10, 12, 18, 240), AnchorPreset.Center, 400f);
            var cpRect = compPanel.GetComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(0.2f, 0.15f);
            cpRect.anchorMax = new Vector2(0.8f, 0.85f);
            cpRect.offsetMin = Vector2.zero;
            cpRect.offsetMax = Vector2.zero;

            var questionText = CreateUIText(compPanel, "QuestionText", "", 22, new Color(0.0f, 0.85f, 0.35f));
            var qtRect = questionText.GetComponent<RectTransform>();
            qtRect.anchorMin = new Vector2(0.05f, 0.55f);
            qtRect.anchorMax = new Vector2(0.95f, 0.92f);
            qtRect.offsetMin = Vector2.zero;
            qtRect.offsetMax = Vector2.zero;
            var qtTmp = questionText.GetComponent<TMP_Text>();
            qtTmp.alignment = TextAlignmentOptions.Center;
            qtTmp.enableWordWrapping = true;

            var answerInput = CreateUIInputField(compPanel, "AnswerInput", "Type your answer...");
            var aiRect = answerInput.GetComponent<RectTransform>();
            aiRect.anchorMin = new Vector2(0.1f, 0.3f);
            aiRect.anchorMax = new Vector2(0.7f, 0.45f);
            aiRect.offsetMin = Vector2.zero;
            aiRect.offsetMax = Vector2.zero;

            var submitBtn = CreateUIButton(compPanel, "SubmitButton", "SUBMIT");
            var sbRect = submitBtn.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(0.72f, 0.3f);
            sbRect.anchorMax = new Vector2(0.9f, 0.45f);
            sbRect.offsetMin = Vector2.zero;
            sbRect.offsetMax = Vector2.zero;
            submitBtn.GetComponent<Image>().color = new Color32(0, 100, 40, 255);

            var feedbackText = CreateUIText(compPanel, "FeedbackText", "", 16, new Color(1f, 0.3f, 0.3f));
            var fbRect = feedbackText.GetComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.1f, 0.18f);
            fbRect.anchorMax = new Vector2(0.9f, 0.28f);
            fbRect.offsetMin = Vector2.zero;
            fbRect.offsetMax = Vector2.zero;
            feedbackText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

            var closeBtn = CreateUIButton(compPanel, "CloseButton", "\u2715");
            var cbRect = closeBtn.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(1, 1);
            cbRect.anchorMax = new Vector2(1, 1);
            cbRect.pivot = new Vector2(1, 1);
            cbRect.anchoredPosition = new Vector2(-8, -8);
            cbRect.sizeDelta = new Vector2(40, 40);
            closeBtn.GetComponent<Image>().color = new Color32(80, 40, 40, 200);

            compPanel.SetActive(false);

            // Wire to ComputerInteraction
            SetPrivateField(computerInteraction, "computerPanel", compPanel);
            SetPrivateField(computerInteraction, "questionText", qtTmp);
            SetPrivateField(computerInteraction, "feedbackText", feedbackText.GetComponent<TMP_Text>());
            SetPrivateField(computerInteraction, "answerInputField", answerInput.GetComponent<TMP_InputField>());
            SetPrivateField(computerInteraction, "submitButton", submitBtn.GetComponent<Button>());
            SetPrivateField(computerInteraction, "closeButton", closeBtn.GetComponent<Button>());

            Debug.Log("[Patch] ComputerPanel created and wired.");
        }
        else
        {
            Debug.Log("[Patch] ComputerPanel already exists, skipping.");
        }

        // FinalPromptPanel
        if (canvas.transform.Find("FinalPromptPanel") == null)
        {
            var finalPanel = CreateUIPanel(canvas, "FinalPromptPanel",
                new Color32(10, 10, 20, 250), AnchorPreset.Center, 300f);
            var fpRect = finalPanel.GetComponent<RectTransform>();
            fpRect.anchorMin = new Vector2(0.25f, 0.25f);
            fpRect.anchorMax = new Vector2(0.75f, 0.75f);
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

            var signBtn = CreateUIButton(finalPanel, "SignButton", "Sign");
            var signRect = signBtn.GetComponent<RectTransform>();
            signRect.anchorMin = new Vector2(0.1f, 0.08f);
            signRect.anchorMax = new Vector2(0.45f, 0.35f);
            signRect.offsetMin = Vector2.zero;
            signRect.offsetMax = Vector2.zero;
            signBtn.GetComponent<Image>().color = new Color32(100, 40, 40, 255);

            var tearBtn = CreateUIButton(finalPanel, "TearButton", "Tear Up");
            var tearRect = tearBtn.GetComponent<RectTransform>();
            tearRect.anchorMin = new Vector2(0.55f, 0.08f);
            tearRect.anchorMax = new Vector2(0.9f, 0.35f);
            tearRect.offsetMin = Vector2.zero;
            tearRect.offsetMax = Vector2.zero;
            tearBtn.GetComponent<Image>().color = new Color32(40, 60, 100, 255);

            finalPanel.SetActive(false);

            SetPrivateField(computerInteraction, "finalPromptPanel", finalPanel);
            SetPrivateField(computerInteraction, "finalPromptText", finalText.GetComponent<TMP_Text>());
            SetPrivateField(computerInteraction, "signButton", signBtn.GetComponent<Button>());
            SetPrivateField(computerInteraction, "tearButton", tearBtn.GetComponent<Button>());

            Debug.Log("[Patch] FinalPromptPanel created and wired.");
        }
        else
        {
            Debug.Log("[Patch] FinalPromptPanel already exists, skipping.");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Patch] Computer + Security UI patch complete. Save the scene.");
    }

    [MenuItem("LastDay/Patch: Fix EventManager + DecisionUI", priority = 2)]
    public static void PatchFixEventManagerAndDecisionUI()
    {
        // Fix DecisionUI prompt message
        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var decisionUI = canvas.GetComponent<DecisionUI>();
            if (decisionUI != null)
            {
                SetPrivateField(decisionUI, "promptMessage", "FINAL SECURITY CHECK\n\nCan you forgive yourself?");
                Debug.Log("[Patch] DecisionUI prompt updated.");
            }
        }

        // EventManager: clear stale serialized fields (Unity drops missing fields on save)
        var emGo = GameObject.Find("EventManager");
        if (emGo != null)
        {
            var em = emGo.GetComponent<EventManager>();
            if (em != null)
            {
                SetPrivateField(em, "activeSecurityQuestion", 0);
                SetPrivateField(em, "marthaShutdownMode", false);
                SetPrivateField(em, "marthaGuitarBreakdown", false);
                Debug.Log("[Patch] EventManager security question fields initialized.");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Patch] EventManager + DecisionUI patch complete.");
    }

    [MenuItem("LastDay/Patch: Wire Audio Clips", priority = 2)]
    public static void PatchWireAudioClips()
    {
        var amGo = GameObject.Find("AudioManager");
        if (amGo == null)
        {
            Debug.LogError("[Patch] AudioManager not found in scene.");
            return;
        }

        var audioMgr = amGo.GetComponent<AudioManager>();
        if (audioMgr == null)
        {
            Debug.LogError("[Patch] AudioManager component not found.");
            return;
        }

        var ambient = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/ambient.mp3");
        var phoneRing = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/phone_ringing.mp3");
        var typingClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/typing.mp3");
        var tearingPaper = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/tearing_paper.wav");
        var clockTick = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/clock_ticking.wav");

        int wired = 0;
        if (ambient != null) { SetPrivateField(audioMgr, "ambientLoop", ambient); wired++; }
        else Debug.LogWarning("[Patch] ambient.mp3 not found at Assets/Audio/Music/ambient.mp3");

        if (phoneRing != null) { SetPrivateField(audioMgr, "phoneRinging", phoneRing); wired++; }
        else Debug.LogWarning("[Patch] phone_ringing.mp3 not found at Assets/Audio/SFX/phone_ringing.mp3");

        if (typingClip != null) { SetPrivateField(audioMgr, "typing", typingClip); wired++; }
        else Debug.LogWarning("[Patch] typing.mp3 not found at Assets/Audio/SFX/typing.mp3");

        if (tearingPaper != null) { SetPrivateField(audioMgr, "paperTear", tearingPaper); wired++; }
        else Debug.LogWarning("[Patch] tearing_paper.wav not found at Assets/Audio/SFX/tearing_paper.wav");

        if (clockTick != null) { SetPrivateField(audioMgr, "clockTicking", clockTick); wired++; }
        else Debug.LogWarning("[Patch] clock_ticking.wav not found at Assets/Audio/SFX/clock_ticking.wav");

        // Also wire phone ring to PhoneInteraction if present
        var phoneGo = GameObject.Find("phone");
        if (phoneGo != null && phoneRing != null)
        {
            var phoneInteract = phoneGo.GetComponent<LastDay.Interaction.PhoneInteraction>();
            if (phoneInteract != null)
            {
                SetPrivateField(phoneInteract, "ringSound", phoneRing);
                Debug.Log("[Patch] PhoneInteraction.ringSound wired.");
            }
        }

        // Ensure AudioManager has AudioSources
        var sources = amGo.GetComponents<AudioSource>();
        if (sources.Length < 3)
        {
            Debug.Log("[Patch] AudioManager missing AudioSources, adding them...");
            while (amGo.GetComponents<AudioSource>().Length < 3)
                amGo.AddComponent<AudioSource>();

            sources = amGo.GetComponents<AudioSource>();
            sources[0].playOnAwake = false;
            sources[0].loop = true;
            sources[1].playOnAwake = false;
            sources[2].playOnAwake = false;

            SetPrivateField(audioMgr, "musicSource", sources[0]);
            SetPrivateField(audioMgr, "sfxSource", sources[1]);
            SetPrivateField(audioMgr, "dialogueBlipSource", sources[2]);
            Debug.Log("[Patch] AudioSources created and wired.");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[Patch] Audio clips wired: {wired}/5.");
    }

    public static void PatchWireCharacterLayer()
    {
        var robert = GameObject.Find("Robert");
        if (robert == null) { Debug.LogWarning("[Patch] Robert not found, skipping characterLayer patch"); return; }

        var clickHandler = robert.GetComponent<ClickToMoveHandler>();
        if (clickHandler == null) { Debug.LogWarning("[Patch] ClickToMoveHandler not found on Robert"); return; }

        int characterLayerMask = 1 << LayerMask.NameToLayer("Characters");
        int interactableLayerMask = 1 << LayerMask.NameToLayer("Interactables");
        int walkableLayerMask = 1 << LayerMask.NameToLayer("Walkable");

        SetPrivateField(clickHandler, "characterLayer", characterLayerMask);
        SetPrivateField(clickHandler, "interactableLayer", interactableLayerMask);
        SetPrivateField(clickHandler, "walkableLayer", walkableLayerMask);

        Debug.Log("[Patch] ClickToMoveHandler: characterLayer, interactableLayer, walkableLayer wired.");
    }

    public static void PatchWireNullSafeManagers()
    {
        var gmGo = GameObject.Find("GameManager");
        var llmGo = GameObject.Find("LocalLLMManager");
        if (gmGo != null && llmGo != null)
        {
            var gm = gmGo.GetComponent<GameManager>();
            var llm = llmGo.GetComponent<LocalLLMManager>();
            if (gm != null && llm != null)
                SetPrivateField(gm, "llmManager", llm);
        }

        // Ensure persistAcrossScenes is false for child Singleton managers only
        string[] singletonManagerNames = { "GameManager", "GameStateMachine", "EventManager",
                                           "FadeManager", "AudioManager" };
        foreach (string name in singletonManagerNames)
        {
            var go = GameObject.Find(name);
            if (go == null || go.transform.parent == null) continue;
            foreach (var comp in go.GetComponents<MonoBehaviour>())
            {
                var field = comp.GetType().GetField("persistAcrossScenes",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(comp, false);
                    break;
                }
            }
        }

        Debug.Log("[Patch] Manager references and singleton settings patched.");
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
        int walkableLayer = LayerMask.NameToLayer("Walkable");

        var bgGo = CreateChild(root, "RoomBackground");
        var bgSr = bgGo.AddComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite("Environment/room_background.png");
        SetSortingLayer(bgSr, "Background", 0);
        bgGo.transform.localScale = new Vector3(10f, 6f, 1f);

        CreateFurniture(root, "Desk", "Environment/furniture_desk.png", new Vector3(2f, -1f, 0),
            "Midground", 0, obstacleLayer, new Vector2(1f, 0.8f));

        CreateFurniture(root, "Chair", "Environment/furniture_chair.png", new Vector3(0f, -1.5f, 0),
            "Midground", 1, obstacleLayer, new Vector2(0.8f, 0.8f));

        CreateFurniture(root, "Bookshelf", "Environment/furniture_bookshelf.png", new Vector3(-3f, 0f, 0),
            "Midground", 0, obstacleLayer, new Vector2(1f, 1.5f));

        var floorGo = CreateChild(root, "WalkableFloor");
        SetLayer(floorGo, walkableLayer);
        var floorCol = floorGo.AddComponent<BoxCollider2D>();
        floorCol.size = new Vector2(8f, 4f);
        floorCol.offset = new Vector2(0f, -1f);
        floorCol.isTrigger = true;

        Debug.Log("[SceneSetup] Environment created.");
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
        int obstacleLayerMask = 1 << LayerMask.NameToLayer("Obstacles");
        int interactableLayerMask = 1 << LayerMask.NameToLayer("Interactables");
        int walkableLayerMask = 1 << LayerMask.NameToLayer("Walkable");

        var robert = new GameObject("Robert");
        robert.transform.position = new Vector3(0f, -1f, 0f);
        SetLayer(robert, charLayer);

        var spriteChild = CreateChild(robert, "Sprite");
        var sr = spriteChild.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("Characters/Robert/robert_placeholder.png");
        SetSortingLayer(sr, "Characters", 0);

        var col = robert.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.5f, 0.75f);

        var rb = robert.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var pathfinder = robert.AddComponent<SimplePathfinder>();
        SetPrivateField(pathfinder, "gridOrigin", new Vector2(-5f, -3f));
        SetPrivateField(pathfinder, "gridSize", new Vector2(10f, 6f));
        SetPrivateField(pathfinder, "cellSize", 0.5f);
        SetPrivateField(pathfinder, "obstacleLayer", obstacleLayerMask);
        SetPrivateField(pathfinder, "showDebugGrid", true);
        SetPrivateField(pathfinder, "showPath", true);

        var charAnim = robert.AddComponent<CharacterAnimator>();
        SetPrivateField(charAnim, "spriteRenderer", sr);

        var idleMove = robert.AddComponent<CharacterIdleMovement>();
        SetPrivateField(idleMove, "spriteRoot", spriteChild.transform);

        var controller = robert.AddComponent<PlayerController2D>();
        SetPrivateField(controller, "pathfinder", pathfinder);
        controller.characterAnimator = charAnim;
        controller.idleMovement = idleMove;

        int characterLayerMask = 1 << LayerMask.NameToLayer("Characters");

        var clickHandler = robert.AddComponent<ClickToMoveHandler>();
        SetPrivateField(clickHandler, "interactableLayer", interactableLayerMask);
        SetPrivateField(clickHandler, "walkableLayer", walkableLayerMask);
        SetPrivateField(clickHandler, "characterLayer", characterLayerMask);

        Debug.Log("[SceneSetup] Robert (player) created.");
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

        // ── Dialogue Panel ────────────────────────────
        var dialoguePanel = CreateUIPanel(canvasGo, "DialoguePanel",
            new Color32(30, 30, 50, 220), AnchorPreset.BottomStretch, 250f);
        SetStretchBottom(dialoguePanel, 250f, 50f);

        var portrait = CreateUIImage(dialoguePanel, "CharacterPortrait", 100, 100);
        SetAnchored(portrait, new Vector2(0, 0), new Vector2(0, 0), new Vector2(70, 130), new Vector2(100, 100));
        var portraitImg = portrait.GetComponent<Image>();
        portraitImg.sprite = LoadSprite("Characters/Martha/martha_portrait.png");

        var charName = CreateUIText(dialoguePanel, "CharacterName", "Martha", 22, Color.white);
        SetAnchored(charName, new Vector2(0, 1), new Vector2(0, 1), new Vector2(200, -20), new Vector2(300, 40));

        var dialogueText = CreateUIText(dialoguePanel, "DialogueText", "", 16, new Color(0.87f, 0.87f, 0.87f));
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
        inputRect.anchorMax = new Vector2(0.85f, 0.2f);
        inputRect.offsetMin = new Vector2(5, 10);
        inputRect.offsetMax = new Vector2(-5, -5);

        var sendBtn = CreateUIButton(dialoguePanel, "SendButton", "Send");
        var sendRect = sendBtn.GetComponent<RectTransform>();
        sendRect.anchorMin = new Vector2(0.86f, 0f);
        sendRect.anchorMax = new Vector2(1f, 0.2f);
        sendRect.offsetMin = new Vector2(5, 10);
        sendRect.offsetMax = new Vector2(-10, -5);

        var closeBtn = CreateUIButton(dialoguePanel, "CloseButton", "\u2715  Esc");
        var closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-8, -8);
        closeRect.sizeDelta = new Vector2(80, 30);
        closeBtn.GetComponent<Image>().color = new Color32(80, 40, 40, 200);
        var closeTmp = closeBtn.GetComponentInChildren<TMP_Text>();
        if (closeTmp != null) closeTmp.fontSize = 14;

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
        monoRect.anchorMin = new Vector2(0.2f, 0.9f);
        monoRect.anchorMax = new Vector2(0.8f, 0.97f);
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

        // ── Computer Panel ────────────────────────────
        var compPanel = CreateUIPanel(canvasGo, "ComputerPanel",
            new Color32(10, 12, 18, 240), AnchorPreset.Center, 400f);
        var cpRect = compPanel.GetComponent<RectTransform>();
        cpRect.anchorMin = new Vector2(0.2f, 0.15f);
        cpRect.anchorMax = new Vector2(0.8f, 0.85f);
        cpRect.offsetMin = Vector2.zero;
        cpRect.offsetMax = Vector2.zero;

        var questionText = CreateUIText(compPanel, "QuestionText", "", 22, new Color(0.0f, 0.85f, 0.35f));
        var qtRect = questionText.GetComponent<RectTransform>();
        qtRect.anchorMin = new Vector2(0.05f, 0.55f);
        qtRect.anchorMax = new Vector2(0.95f, 0.92f);
        qtRect.offsetMin = Vector2.zero;
        qtRect.offsetMax = Vector2.zero;
        questionText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        var answerInput = CreateUIInputField(compPanel, "AnswerInput", "Type your answer...");
        var answerRect = answerInput.GetComponent<RectTransform>();
        answerRect.anchorMin = new Vector2(0.1f, 0.3f);
        answerRect.anchorMax = new Vector2(0.7f, 0.45f);
        answerRect.offsetMin = Vector2.zero;
        answerRect.offsetMax = Vector2.zero;

        var compSubmitBtn = CreateUIButton(compPanel, "SubmitButton", "SUBMIT");
        var csbRect = compSubmitBtn.GetComponent<RectTransform>();
        csbRect.anchorMin = new Vector2(0.72f, 0.3f);
        csbRect.anchorMax = new Vector2(0.9f, 0.45f);
        csbRect.offsetMin = Vector2.zero;
        csbRect.offsetMax = Vector2.zero;
        compSubmitBtn.GetComponent<Image>().color = new Color32(0, 100, 40, 255);

        var feedbackText = CreateUIText(compPanel, "FeedbackText", "", 16, new Color(1f, 0.3f, 0.3f));
        var fbRect = feedbackText.GetComponent<RectTransform>();
        fbRect.anchorMin = new Vector2(0.1f, 0.18f);
        fbRect.anchorMax = new Vector2(0.9f, 0.28f);
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

        // ── Final Prompt Panel ───────────────────────────
        var finalPanel = CreateUIPanel(canvasGo, "FinalPromptPanel",
            new Color32(10, 10, 20, 250), AnchorPreset.Center, 300f);
        var fpRect = finalPanel.GetComponent<RectTransform>();
        fpRect.anchorMin = new Vector2(0.25f, 0.25f);
        fpRect.anchorMax = new Vector2(0.75f, 0.75f);
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
                var compPanel = canvas.transform.Find("ComputerPanel");
                if (compPanel != null)
                {
                    SetPrivateField(compInteract, "computerPanel", compPanel.gameObject);
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

                var finalPanel = canvas.transform.Find("FinalPromptPanel");
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
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = new Color32(60, 60, 80, 255);

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
}
