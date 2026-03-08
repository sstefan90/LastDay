using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LLMUnity;

/// <summary>
/// One-time scene setup: creates a dedicated "DavidModel" GameObject (Phi-3 Mini, port 13334)
/// and wires it to the DavidLLM agent and LocalLLMManager.
///
/// Run via: LastDay → Setup: David Model (Phi-3 Mini)
/// Safe to re-run — skips work that is already done.
/// </summary>
public static class DavidModelSetup
{
    private const string DAVID_GO_NAME = "DavidModel";
    private const int    DAVID_PORT    = 13334;

    [MenuItem("LastDay/Setup: David Model (Llama 3 8B)", priority = 15)]
    public static void Run()
    {
        int scenesPatched = 0;
        int scenesSkipped = 0;

        string[] scenePaths = new[]
        {
            "Assets/Scenes/MainRoom.unity",
            "Assets/Scenes/abeyRoom.unity",
            "Assets/Scenes/MainRoom_InterviewStyle.unity",
        };

        foreach (string scenePath in scenePaths)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"[DavidModelSetup] Scene not found, skipping: {scenePath}");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            bool didWork = PatchScene(scene);

            if (didWork)
            {
                EditorSceneManager.SaveScene(scene);
                scenesPatched++;
                Debug.Log($"[DavidModelSetup] Patched and saved: {scenePath}");
            }
            else
            {
                scenesSkipped++;
                Debug.Log($"[DavidModelSetup] Already set up, skipped: {scenePath}");
            }

            EditorSceneManager.CloseScene(scene, removeScene: true);
        }

        Debug.Log($"[DavidModelSetup] Done. Patched: {scenesPatched}  Already done: {scenesSkipped}");
        EditorUtility.DisplayDialog(
            "David Model Setup",
            $"Complete.\n\n" +
            $"Patched: {scenesPatched} scene(s)\n" +
            $"Already done: {scenesSkipped} scene(s)\n\n" +
            "David now has his own Phi-3 Mini server (port 13334).\n" +
            "Martha keeps Llama 3 8B on port 13333.",
            "OK");
    }

    private static bool PatchScene(UnityEngine.SceneManagement.Scene scene)
    {
        // ── Find LocalLLMManager ─────────────────────────────────────────────
        var managerGO = FindInScene<LastDay.Dialogue.LocalLLMManager>(scene);
        if (managerGO == null)
        {
            Debug.LogWarning($"[DavidModelSetup] No LocalLLMManager found in {scene.name}. Skipping.");
            return false;
        }
        var manager = managerGO.GetComponent<LastDay.Dialogue.LocalLLMManager>();

        // ── Find existing Martha LLM (on LocalLLMManager's GO) ───────────────
        var marthaLLM = managerGO.GetComponent<LLM>();
        if (marthaLLM == null)
        {
            Debug.LogWarning($"[DavidModelSetup] No LLM component on LocalLLMManager in {scene.name}.");
            return false;
        }

        // ── Find DavidLLM agent ──────────────────────────────────────────────
        var davidAgent = FindInScene<LLMAgent>(scene, go => go.name == "DavidLLM");
        if (davidAgent == null)
        {
            Debug.LogWarning($"[DavidModelSetup] No DavidLLM agent found in {scene.name}.");
            return false;
        }

        // ── Check if already set up ──────────────────────────────────────────
        var existingDavidGO = FindGameObjectInScene(scene, DAVID_GO_NAME);
        var managerSO = new SerializedObject(manager);
        var davidLLMProp = managerSO.FindProperty("davidLLM");

        if (existingDavidGO != null && davidLLMProp != null && davidLLMProp.objectReferenceValue != null)
            return false; // already done

        // ── Create DavidModel GameObject ────────────────────────────────────
        var davidModelGO = existingDavidGO ?? new GameObject(DAVID_GO_NAME);

        // Parent under the same parent as LocalLLMManager (or scene root)
        if (managerGO.transform.parent != null)
            davidModelGO.transform.SetParent(managerGO.transform.parent);

        // ── Add LLM component ────────────────────────────────────────────────
        var davidLLM = davidModelGO.GetComponent<LLM>() ?? davidModelGO.AddComponent<LLM>();

        // Copy settings from Martha's LLM, then override per-character values
        davidLLM.contextSize     = 4096;
        davidLLM.numGPULayers    = marthaLLM.numGPULayers;
        davidLLM.numThreads      = marthaLLM.numThreads;
        davidLLM.flashAttention  = marthaLLM.flashAttention;
        davidLLM.parallelPrompts = 1;
        davidLLM.dontDestroyOnLoad = true;

        // Set port via SerializedObject to bypass the public setter's validation
        var davidLLMSO  = new SerializedObject(davidLLM);
        var portProp    = davidLLMSO.FindProperty("_port");
        if (portProp != null) portProp.intValue = DAVID_PORT;
        davidLLMSO.ApplyModifiedProperties();

        EditorUtility.SetDirty(davidModelGO);

        // ── Re-link DavidLLM agent to the new LLM ───────────────────────────
        var agentSO   = new SerializedObject(davidAgent);
        var llmProp   = agentSO.FindProperty("_llm");
        if (llmProp != null)
        {
            llmProp.objectReferenceValue = davidLLM;
            agentSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(davidAgent);
        }

        // ── Wire davidLLM into LocalLLMManager ───────────────────────────────
        if (davidLLMProp != null)
        {
            davidLLMProp.objectReferenceValue = davidLLM;
            managerSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);
        }

        // ── Also ensure marthaLLM field is wired ─────────────────────────────
        var marthaLLMProp = managerSO.FindProperty("marthaLLM");
        if (marthaLLMProp != null && marthaLLMProp.objectReferenceValue == null)
        {
            marthaLLMProp.objectReferenceValue = marthaLLM;
            managerSO.ApplyModifiedProperties();
        }

        return true;
    }

    // ── Scene search helpers ─────────────────────────────────────────────────

    private static GameObject FindInScene<T>(
        UnityEngine.SceneManagement.Scene scene,
        System.Func<GameObject, bool> predicate = null) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var result = FindInHierarchy<T>(root, predicate);
            if (result != null) return result;
        }
        return null;
    }

    private static GameObject FindInHierarchy<T>(
        GameObject go,
        System.Func<GameObject, bool> predicate) where T : Component
    {
        if (go.GetComponent<T>() != null && (predicate == null || predicate(go)))
            return go;
        foreach (Transform child in go.transform)
        {
            var result = FindInHierarchy<T>(child.gameObject, predicate);
            if (result != null) return result;
        }
        return null;
    }

    private static GameObject FindGameObjectInScene(
        UnityEngine.SceneManagement.Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
            var found = FindByNameInHierarchy(root, name);
            if (found != null) return found;
        }
        return null;
    }

    private static GameObject FindByNameInHierarchy(GameObject go, string name)
    {
        if (go.name == name) return go;
        foreach (Transform child in go.transform)
        {
            var result = FindByNameInHierarchy(child.gameObject, name);
            if (result != null) return result;
        }
        return null;
    }
}
