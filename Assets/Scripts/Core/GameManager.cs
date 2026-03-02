using UnityEngine;
using LastDay.Utilities;
using LastDay.Dialogue;
using LastDay.Audio;

namespace LastDay.Core
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("References")]
        [SerializeField] private LocalLLMManager llmManager;

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
            if (llmManager == null)
                llmManager = FindObjectOfType<LocalLLMManager>();

            if (llmManager != null)
            {
                await llmManager.Initialize();
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
                AudioManager.Instance.PlayMusic("ambient_loop");

            Debug.Log("[GameManager] Game started.");
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
                AudioManager.Instance.StopMusic(0.5f);
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
