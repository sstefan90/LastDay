using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Threading.Tasks;
using System;

namespace LastDay.Dialogue
{
    /// <summary>
    /// Downloads the Phi-3-mini GGUF model on first run and tracks its location.
    ///
    /// Model is stored at: <persistent data path>/Models/phi3-mini.gguf
    /// (~2.4 GB, downloaded once, persists across app launches)
    ///
    /// Usage — attach to a GameObject and wire to GameManager.
    /// Call EnsureModelReady() before initializing the LLM.
    /// Subscribe to OnProgress for UI feedback.
    /// </summary>
    public class ModelDownloader : MonoBehaviour
    {
        // ── Configuration ──────────────────────────────────────────────────

        private const string MODEL_FILENAME = "phi3-mini.gguf";
        private const string MODEL_URL =
            "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf";

        // ── State ──────────────────────────────────────────────────────────

        public bool IsModelReady { get; private set; }
        public string ModelPath  { get; private set; }

        public float DownloadProgress  { get; private set; }   // 0–1
        public string StatusMessage    { get; private set; } = "Checking model…";
        public bool IsDownloading      { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────

        /// <summary>Fired each frame during download. float = 0–1 progress.</summary>
        public event Action<float, string> OnProgress;

        /// <summary>Fired when the model is ready to use (already present or just downloaded).</summary>
        public event Action<string> OnModelReady;

        /// <summary>Fired if the download fails. string = error message.</summary>
        public event Action<string> OnError;

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Check whether the model file exists.
        /// If it does, fires OnModelReady immediately.
        /// If not, downloads it, then fires OnModelReady.
        /// Returns the model path once ready.
        /// </summary>
        public async Task<string> EnsureModelReady()
        {
            ModelPath = GetModelPath();

            if (File.Exists(ModelPath))
            {
                StatusMessage = "Model found.";
                IsModelReady  = true;
                OnModelReady?.Invoke(ModelPath);
                Debug.Log($"[ModelDownloader] Model already present at: {ModelPath}");
                return ModelPath;
            }

            Debug.Log($"[ModelDownloader] Model not found. Starting download to: {ModelPath}");
            await DownloadModel();
            return IsModelReady ? ModelPath : null;
        }

        // ── Download ───────────────────────────────────────────────────────

        private async Task DownloadModel()
        {
            IsDownloading = true;
            string dir    = Path.GetDirectoryName(ModelPath);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Write to a temp file first; rename on success to avoid partial files.
            string tempPath = ModelPath + ".tmp";

            UpdateStatus(0f, "Connecting to server…");

            using var request = new UnityWebRequest(MODEL_URL, UnityWebRequest.kHttpVerbGET);
            request.downloadHandler = new DownloadHandlerFile(tempPath)
            {
                removeFileOnAbort = true
            };
            request.timeout = 0; // No timeout for large files

            var op = request.SendWebRequest();

            while (!op.isDone)
            {
                float p = request.downloadProgress;
                long  downloaded = request.downloadedBytes;
                string msg = $"Downloading model… {FormatBytes(downloaded)} / ~2.4 GB";
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

            // Rename temp → final
            if (File.Exists(ModelPath))
                File.Delete(ModelPath);
            File.Move(tempPath, ModelPath);

            UpdateStatus(1f, "Download complete.");
            IsModelReady = true;
            Debug.Log($"[ModelDownloader] Model saved to: {ModelPath}");
            OnModelReady?.Invoke(ModelPath);
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static string GetModelPath()
        {
            // Persistent data path survives app updates and is writable on all platforms.
            string dir = Path.Combine(Application.persistentDataPath, "Models");
            return Path.Combine(dir, MODEL_FILENAME);
        }

        private void UpdateStatus(float progress, string message)
        {
            DownloadProgress = progress;
            StatusMessage    = message;
            OnProgress?.Invoke(progress, message);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)         return $"{bytes} B";
            if (bytes < 1024 * 1024)  return $"{bytes / 1024f:F1} KB";
            return                           $"{bytes / (1024f * 1024f):F0} MB";
        }
    }
}
