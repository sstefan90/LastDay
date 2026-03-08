using UnityEngine;
using System.Collections;
using LastDay.Utilities;
using LastDay.Dialogue;
using LastDay.Audio;
using LastDay.UI;

namespace LastDay.Core
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("References")]
        [SerializeField] private LocalLLMManager llmManager;
        [SerializeField] private ModelDownloader modelDownloader;

        [Header("Game State")]
        public bool isGameStarted;

        protected override void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            InitializeGame();
        }

        private async void InitializeGame()
        {
            // ── Step 1: ensure the model file is present ──────────────────
            if (modelDownloader == null)
                modelDownloader = FindObjectOfType<ModelDownloader>();

            if (modelDownloader != null)
            {
                string modelPath = await modelDownloader.EnsureModelReady();

                if (string.IsNullOrEmpty(modelPath))
                {
                    Debug.LogError("[GameManager] Model download failed. Dialogue will use fallback.");
                }
                else
                {
                    Debug.Log($"[GameManager] Model ready at: {modelPath}");
                }
            }

            // ── Step 2: initialize LLM ────────────────────────────────────
            if (llmManager == null)
                llmManager = FindObjectOfType<LocalLLMManager>();

            if (llmManager != null)
            {
                string path = modelDownloader != null && modelDownloader.IsModelReady
                    ? modelDownloader.ModelPath
                    : null;
                await llmManager.Initialize(path);
            }
            else
            {
                Debug.LogWarning("[GameManager] No LocalLLMManager found. Dialogue will use fallback responses.");
            }

            StartGame();
        }

        public void StartGame()
        {
            isGameStarted = true;
            GameStateMachine.Instance.ChangeState(GameState.Playing);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic("ambient_loop");
                AudioManager.Instance.StartClockTick();
            }

            StartCoroutine(PlayIntroAfterDelay(0.5f));
            Debug.Log("[GameManager] Game started.");
        }

        private IEnumerator PlayIntroAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            DialogueSession.Current?.OpenForIntro();
        }

        public void EndGame(bool signed)
        {
            // If we're already in Decision (from DocumentInteraction), go straight to Ending.
            // If somehow called from another state, try to get to Ending via Decision first.
            if (GameStateMachine.Instance.CurrentState != GameState.Decision)
                GameStateMachine.Instance.ChangeState(GameState.Decision);

            GameStateMachine.Instance.ChangeState(GameState.Ending);

            GameEvents.EndGame(signed);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopClockTick(0.5f);
                AudioManager.Instance.StopMusic(0.5f);

                if (!signed)
                    AudioManager.Instance.PlaySFX("paper_tear");

                string track = signed ? "ending_signed" : "ending_torn";
                AudioManager.Instance.PlayMusic(track);
            }

            Debug.Log($"[GameManager] Game ended. Signed: {signed}");
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
