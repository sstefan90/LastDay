using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using LastDay.Dialogue;

namespace LastDay.UI
{
    /// <summary>
    /// Drives the LoadingScene flow:
    ///   1. Waits for ModelDownloader.EnsureModelReady() (shows progress via DownloadProgressPanel)
    ///   2. Shows a brief "Preparing AI characters…" warmup message
    ///   3. Loads the main game scene
    ///
    /// Attach to a GameObject in LoadingScene alongside a ModelDownloader and a Canvas
    /// that contains a DownloadProgressPanel component.
    /// </summary>
    public class LoadingSceneController : MonoBehaviour
    {
        [Header("Scene")]
        [Tooltip("Name of the main game scene to load after the model is ready.")]
        [SerializeField] private string gameSceneName = "MainRoom";

        [Tooltip("Seconds to display the warmup message before transitioning.")]
        [SerializeField] private float warmupDisplaySeconds = 1f;

        [Header("References")]
        [SerializeField] private DownloadProgressPanel progressPanel;

        private ModelDownloader _downloader;
        private bool _transitioning;

        void Start()
        {
            _downloader = FindObjectOfType<ModelDownloader>();

            if (_downloader == null)
            {
                Debug.LogWarning("[Loading] No ModelDownloader found — loading game directly.");
                Invoke(nameof(LoadGameScene), 0.1f);
                return;
            }

            _downloader.OnModelReady += HandleModelReady;
            _downloader.OnError      += HandleError;

            StartCoroutine(RunLoadingFlow());
        }

        void OnDestroy()
        {
            if (_downloader == null) return;
            _downloader.OnModelReady -= HandleModelReady;
            _downloader.OnError      -= HandleError;
        }

        // ── Loading flow ──────────────────────────────────────────────────

        private IEnumerator RunLoadingFlow()
        {
            // Bridge async Task → coroutine so Unity's main thread isn't blocked.
            var task = _downloader.EnsureModelReady();
            yield return new WaitUntil(() => task.IsCompleted);
            // OnModelReady or OnError will have fired inside the task.
        }

        private void HandleModelReady(string path)
        {
            if (_transitioning) return;
            _transitioning = true;

            if (progressPanel != null)
                progressPanel.ShowWarmupPhase();

            Invoke(nameof(LoadGameScene), warmupDisplaySeconds);
        }

        private void HandleError(string error)
        {
            // DownloadProgressPanel already shows Retry / Quit buttons.
            // Nothing extra needed here — we just don't transition.
            _transitioning = false;

            // Re-enable retry by re-subscribing to OnModelReady after a potential retry.
            // (The panel's Retry button calls EnsureModelReady() again, which re-fires OnModelReady.)
            _downloader.OnModelReady -= HandleModelReady;
            _downloader.OnModelReady += HandleModelReady;
        }

        private void LoadGameScene()
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
