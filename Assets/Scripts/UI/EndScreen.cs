using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using LastDay.Core;

namespace LastDay.UI
{
    /// <summary>
    /// Displays the ending quote and credits after the player's decision.
    /// </summary>
    public class EndScreen : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject endPanel;

        [Header("Text")]
        [SerializeField] private TMP_Text quoteText;
        [SerializeField] private TMP_Text attributionText;

        [Header("Quotes")]
        [SerializeField, TextArea(3, 6)] private string signedQuote =
            "For what is it to die but to stand naked in the wind\nand to melt into the sun?";
        [SerializeField] private string signedAttribution = "- Khalil Gibran, The Prophet";

        [SerializeField, TextArea(3, 6)] private string tornQuote =
            "The wound is the place where the Light enters you.";
        [SerializeField] private string tornAttribution = "- Rumi";

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 2f;
        [SerializeField] private float holdDuration = 5f;
        [SerializeField] private CanvasGroup endCanvasGroup;

        void Awake()
        {
            if (endPanel != null)
                endPanel.SetActive(false);
        }

        void OnEnable()
        {
            GameEvents.OnGameEnded += HandleGameEnded;
        }

        void OnDisable()
        {
            GameEvents.OnGameEnded -= HandleGameEnded;
        }

        private void HandleGameEnded(bool signed)
        {
            if (signed)
                ShowEnding(signedQuote, signedAttribution);
            else
                ShowEnding(tornQuote, tornAttribution);
        }

        private void ShowEnding(string quote, string attribution)
        {
            if (endPanel != null)
                endPanel.SetActive(true);

            if (quoteText != null)
                quoteText.text = quote;

            if (attributionText != null)
                attributionText.text = attribution;

            if (endCanvasGroup != null)
                StartCoroutine(FadeInEndScreen());
        }

        private IEnumerator FadeInEndScreen()
        {
            endCanvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                endCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            endCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(holdDuration);
        }
    }
}
