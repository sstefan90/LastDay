using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LastDay.Dialogue;

namespace LastDay.UI
{
    /// <summary>
    /// Displays a full-screen loading overlay while the AI model downloads on first run.
    /// Lives in LoadingScene (and optionally MainRoom as a fallback).
    /// Self-manages via ModelDownloader events; LoadingSceneController drives the scene transition.
    /// </summary>
    public class DownloadProgressPanel : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panel;

        [Header("Text")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text modelInfoText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text percentText;

        [Header("Progress Bar")]
        [SerializeField] private Slider progressBar;

        [Header("Error")]
        [SerializeField] private GameObject errorGroup;
        [SerializeField] private TMP_Text  errorText;
        [SerializeField] private Button    retryButton;
        [SerializeField] private Button    quitButton;

        private ModelDownloader _downloader;
        private bool _downloadComplete;

        void Awake()
        {
            _downloader = FindObjectOfType<ModelDownloader>();
            if (_downloader == null)
            {
                Debug.LogWarning("[DownloadProgressPanel] No ModelDownloader found — panel will stay hidden.");
                Hide();
                return;
            }

            _downloader.OnProgress   += HandleProgress;
            _downloader.OnModelReady += HandleModelReady;
            _downloader.OnError      += HandleError;

            if (errorGroup  != null) errorGroup.SetActive(false);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
            if (quitButton  != null) quitButton.onClick.AddListener(OnQuitClicked);

            if (_downloader.IsSelectedModelCached())
            {
                // Model already on disk — jump straight to warmup message
                ShowWarmupPhase();
            }
            else
            {
                Show();
                if (titleText    != null) titleText.text    = "Last Day";
                if (modelInfoText != null) modelInfoText.text = $"Downloading {_downloader.ActiveModel.Name}  {_downloader.ActiveModel.SizeHint}";
                if (statusText   != null) statusText.text   = "Connecting…";
                if (progressBar  != null) progressBar.value = 0f;
                if (percentText  != null) percentText.text  = "0%";
            }
        }

        void OnDestroy()
        {
            if (_downloader == null) return;
            _downloader.OnProgress   -= HandleProgress;
            _downloader.OnModelReady -= HandleModelReady;
            _downloader.OnError      -= HandleError;
        }

        // ── Public ────────────────────────────────────────────────────────

        /// <summary>Switch the panel to the "Preparing AI characters…" warmup state.</summary>
        public void ShowWarmupPhase()
        {
            Show();
            if (progressBar  != null) progressBar.value = 1f;
            if (statusText   != null) statusText.text   = "Preparing AI characters…";
            if (percentText  != null) percentText.text  = "✓";
            if (modelInfoText != null) modelInfoText.text = "";
            if (titleText    != null) titleText.text    = "Last Day";
            if (errorGroup   != null) errorGroup.SetActive(false);
        }

        // ── Event handlers ────────────────────────────────────────────────

        private void HandleProgress(float progress, string message)
        {
            Show();
            if (progressBar  != null) progressBar.value = progress;
            if (statusText   != null) statusText.text   = message;
            if (percentText  != null) percentText.text  = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

        private void HandleModelReady(string path)
        {
            _downloadComplete = true;
            ShowWarmupPhase();
            // LoadingSceneController observes OnModelReady independently and triggers scene load.
        }

        private void HandleError(string error)
        {
            if (progressBar  != null) progressBar.value = 0f;
            if (statusText   != null) statusText.text   = "Download failed.";
            if (percentText  != null) percentText.text  = "";
            if (errorGroup   != null) errorGroup.SetActive(true);
            if (errorText    != null) errorText.text    = $"{error}\n\nCheck your internet connection and try again.";
            Debug.LogError($"[DownloadProgressPanel] {error}");
        }

        // ── Buttons ───────────────────────────────────────────────────────

        private async void OnRetryClicked()
        {
            if (_downloader == null || _downloadComplete) return;
            if (errorGroup   != null) errorGroup.SetActive(false);
            if (statusText   != null) statusText.text  = "Retrying…";
            if (progressBar  != null) progressBar.value = 0f;
            if (percentText  != null) percentText.text = "0%";
            await _downloader.EnsureModelReady();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void Show() { if (panel != null) panel.SetActive(true); }
        private void Hide() { if (panel != null) panel.SetActive(false); }
    }
}
