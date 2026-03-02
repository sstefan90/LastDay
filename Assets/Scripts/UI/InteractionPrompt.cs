using UnityEngine;
using TMPro;

namespace LastDay.UI
{
    /// <summary>
    /// Floating "Click to examine [object]" text that appears on hover.
    /// </summary>
    public class InteractionPrompt : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private TMP_Text promptText;

        [Header("Settings")]
        [SerializeField] private Vector2 offset = new Vector2(0, 50f);

        void Awake()
        {
            if (promptPanel != null)
                promptPanel.SetActive(false);
        }

        public void Show(string text)
        {
            if (promptText != null)
                promptText.text = text;

            if (promptPanel != null)
                promptPanel.SetActive(true);
        }

        public void Hide()
        {
            if (promptPanel != null)
                promptPanel.SetActive(false);
        }

        void Update()
        {
            if (promptPanel != null && promptPanel.activeSelf)
            {
                Vector2 mousePos = Input.mousePosition;
                promptPanel.transform.position = mousePos + offset;
            }
        }
    }
}
