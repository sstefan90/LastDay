using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Threading.Tasks;
using System;

namespace LastDay.Dialogue
{
    /// <summary>
    /// Downloads a GGUF model on first run and tracks its location.
    /// Model is stored at: &lt;persistent data path&gt;/Models/&lt;filename&gt;
    ///
    /// Switch models at dev time via: LastDay → Switch Model
    /// The selected model index is saved via EditorPrefs (editor) or PlayerPrefs (runtime).
    ///
    /// Usage — attach to a GameObject and wire to GameManager.
    /// Call EnsureModelReady() before initialising the LLM.
    /// Subscribe to OnProgress for UI feedback.
    /// </summary>
    public class ModelDownloader : MonoBehaviour
    {
        // ── Model manifest ─────────────────────────────────────────────────

        [System.Serializable]
        public struct ModelDefinition
        {
            public string Name;
            public string Description;
            public string Filename;
            public string Url;
            public string SizeHint;
        }

        public static readonly ModelDefinition[] AvailableModels = new ModelDefinition[]
        {
            new ModelDefinition
            {
                Name        = "Phi-3 Mini (3.8B)",
                Description = "Fast, lightweight. Good for quick iteration and low-memory machines.",
                Filename    = "phi3-mini.gguf",
                Url         = "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf",
                SizeHint    = "~2.4 GB"
            },
            new ModelDefinition
            {
                Name        = "Llama 3 8B Instruct (Q4_K_M)",
                Description = "Stronger instruction following and character constraint adherence. Recommended for play-testing.",
                Filename    = "llama3-8b-instruct.gguf",
                Url         = "https://huggingface.co/bartowski/Meta-Llama-3-8B-Instruct-GGUF/resolve/main/Meta-Llama-3-8B-Instruct-Q4_K_M.gguf",
                SizeHint    = "~4.9 GB"
            },
        };

        private const string PREFS_KEY = "LastDay.SelectedModelIndex";

        // ── Inspector ──────────────────────────────────────────────────────

        [Header("Model Selection")]
        [Tooltip("Which model to use. Switch via LastDay > Switch Model for a friendlier UI.")]
        [SerializeField] private int selectedModelIndex = 1;

        // ── State ──────────────────────────────────────────────────────────

        public bool   IsModelReady      { get; private set; }
        public string ModelPath         { get; private set; }
        public float  DownloadProgress  { get; private set; }
        public string StatusMessage     { get; private set; } = "Checking model…";
        public bool   IsDownloading     { get; private set; }

        public ModelDefinition ActiveModel => AvailableModels[Mathf.Clamp(selectedModelIndex, 0, AvailableModels.Length - 1)];

        // ── Events ─────────────────────────────────────────────────────────

        /// <summary>Fired each frame during download. float = 0–1 progress.</summary>
        public event Action<float, string> OnProgress;

        /// <summary>Fired when the model is ready to use.</summary>
        public event Action<string> OnModelReady;

        /// <summary>Fired if the download fails.</summary>
        public event Action<string> OnError;

        // ── Unity lifecycle ────────────────────────────────────────────────

        void Awake()
        {
            // Runtime: honour any index saved by the Editor switcher window.
            if (PlayerPrefs.HasKey(PREFS_KEY))
                selectedModelIndex = PlayerPrefs.GetInt(PREFS_KEY);

            selectedModelIndex = Mathf.Clamp(selectedModelIndex, 0, AvailableModels.Length - 1);
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Ensures the active model file exists on disk, downloading it if necessary.
        /// Returns the model path when ready, or null on failure.
        /// </summary>
        public async Task<string> EnsureModelReady()
        {
            ModelPath = GetModelPath();

            if (File.Exists(ModelPath))
            {
                StatusMessage = $"Model ready: {ActiveModel.Name}";
                IsModelReady  = true;
                OnModelReady?.Invoke(ModelPath);
                Debug.Log($"[ModelDownloader] Using {ActiveModel.Name} at: {ModelPath}");
                return ModelPath;
            }

            Debug.Log($"[ModelDownloader] {ActiveModel.Name} not found. Starting download to: {ModelPath}");
            await DownloadModel();
            return IsModelReady ? ModelPath : null;
        }

        /// <summary>
        /// Returns true if the currently selected model file already exists on disk.
        /// Useful for UI to warn before a long download.
        /// </summary>
        public bool IsSelectedModelCached()
        {
            return File.Exists(GetModelPath());
        }

        /// <summary>
        /// Switch to a different model by index.
        /// Persists the choice to PlayerPrefs so it survives Play Mode restarts.
        /// </summary>
        public void SelectModel(int index)
        {
            index = Mathf.Clamp(index, 0, AvailableModels.Length - 1);
            selectedModelIndex = index;
            PlayerPrefs.SetInt(PREFS_KEY, index);
            PlayerPrefs.Save();
            IsModelReady = false;
            Debug.Log($"[ModelDownloader] Model selection changed to: {ActiveModel.Name}");
        }

        // ── Download ───────────────────────────────────────────────────────

        private async Task DownloadModel()
        {
            IsDownloading = true;
            string dir = Path.GetDirectoryName(ModelPath);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string tempPath = ModelPath + ".tmp";
            string sizeHint = ActiveModel.SizeHint;

            UpdateStatus(0f, $"Connecting… ({ActiveModel.Name})");

            using var request = new UnityWebRequest(ActiveModel.Url, UnityWebRequest.kHttpVerbGET);
            request.downloadHandler = new DownloadHandlerFile(tempPath)
            {
                removeFileOnAbort = true
            };
            request.timeout = 0;

            var op = request.SendWebRequest();

            while (!op.isDone)
            {
                float  p   = request.downloadProgress;
                ulong  dl  = request.downloadedBytes;
                string msg = $"Downloading {ActiveModel.Name}… {FormatBytes(dl)} / {sizeHint}";
                UpdateStatus(p, msg);
                await Task.Yield();
            }

            IsDownloading = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                string err = $"Download failed: {request.error}";
                StatusMessage = err;
                Debug.LogError($"[ModelDownloader] {err}");
                OnError?.Invoke(err);

                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                return;
            }

            if (File.Exists(ModelPath))
                File.Delete(ModelPath);
            File.Move(tempPath, ModelPath);

            UpdateStatus(1f, $"Download complete: {ActiveModel.Name}");
            IsModelReady = true;
            Debug.Log($"[ModelDownloader] Saved to: {ModelPath}");
            OnModelReady?.Invoke(ModelPath);
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private string GetModelPath() => GetPathForFilename(ActiveModel.Filename);

        /// <summary>
        /// Resolves a model filename to an absolute path.
        /// Checks: (1) project root Models/, (2) persistentDataPath/Models/.
        /// For Llama 3 8B the manifest filename differs from the file on disk — both variants are tried.
        /// Returns the first path where the file exists, or the persistentDataPath fallback if neither found.
        /// </summary>
        public static string GetPathForFilename(string filename)
        {
            // Llama 3 8B was manually downloaded under a different name — map it here.
            string[] candidates = filename == "llama3-8b-instruct.gguf"
                ? new[] { "llama3-8b-instruct.gguf", "Meta-Llama-3-8B-Instruct-Q4_K_M.gguf" }
                : new[] { filename };

            // Priority 1: project root Models/ (developer / manual placement)
            string projectModels = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Models"));
            foreach (string name in candidates)
            {
                string p = Path.Combine(projectModels, name);
                if (File.Exists(p)) return p;
            }

            // Priority 2: persistent data path (downloaded via EnsureModelReady)
            string persistentModels = Path.Combine(Application.persistentDataPath, "Models");
            foreach (string name in candidates)
            {
                string p = Path.Combine(persistentModels, name);
                if (File.Exists(p)) return p;
            }

            // Fallback: expected persistent path (may trigger download on next EnsureModelReady)
            return Path.Combine(persistentModels, filename);
        }

        private void UpdateStatus(float progress, string message)
        {
            DownloadProgress = progress;
            StatusMessage    = message;
            OnProgress?.Invoke(progress, message);
        }

        private static string FormatBytes(ulong bytes)
        {
            if (bytes < 1024)             return $"{bytes} B";
            if (bytes < 1024ul * 1024ul)  return $"{bytes / 1024f:F1} KB";
            return                               $"{bytes / (1024f * 1024f):F0} MB";
        }
    }
}
