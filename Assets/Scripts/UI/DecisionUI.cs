using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LastDay.Core;

namespace LastDay.UI
{
    /// <summary>
    /// The Sign / Tear decision panel that appears when the player interacts
    /// with the unlocked document.
    /// </summary>
    public class DecisionUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject decisionPanel;

        [Header("Buttons")]
        [SerializeField] private Button signButton;
        [SerializeField] private Button tearButton;

        [Header("Text")]
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private string promptMessage = "The document lies before you. What do you do?";

        void Awake()
        {
            if (decisionPanel != null)
                decisionPanel.SetActive(false);
        }

        void Start()
        {
            if (signButton != null)
                signButton.onClick.AddListener(OnSignClicked);
            if (tearButton != null)
                tearButton.onClick.AddListener(OnTearClicked);
        }

        public void Show()
        {
            if (decisionPanel != null)
                decisionPanel.SetActive(true);

            if (promptText != null)
                promptText.text = promptMessage;

            if (signButton != null) signButton.interactable = true;
            if (tearButton != null) tearButton.interactable = true;
        }

        public void Hide()
        {
            if (decisionPanel != null)
                decisionPanel.SetActive(false);
        }

        private void OnSignClicked()
        {
            DisableButtons();
            Debug.Log("[Decision] Player chose to sign.");

            if (FadeManager.Instance != null)
            {
                FadeManager.Instance.FadeOut(2f, () =>
                {
                    Hide();
                    GameManager.Instance.EndGame(signed: true);
                });
            }
            else
            {
                Hide();
                GameManager.Instance.EndGame(signed: true);
            }
        }

        private void OnTearClicked()
        {
            DisableButtons();
            Debug.Log("[Decision] Player chose to tear.");

            if (FadeManager.Instance != null)
            {
                FadeManager.Instance.FadeOut(2f, () =>
                {
                    Hide();
                    GameManager.Instance.EndGame(signed: false);
                });
            }
            else
            {
                Hide();
                GameManager.Instance.EndGame(signed: false);
            }
        }

        private void DisableButtons()
        {
            if (signButton != null) signButton.interactable = false;
            if (tearButton != null) tearButton.interactable = false;
        }
    }
}
