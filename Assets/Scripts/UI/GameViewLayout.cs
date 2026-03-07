using UnityEngine;

namespace LastDay.UI
{
    /// <summary>
    /// Configures the game view and dialogue panel layout.
    /// Camera uses full screen; dialogue box sits at the bottom.
    /// </summary>
    public class GameViewLayout : MonoBehaviour
    {
        [Header("Dialogue panel")]
        [SerializeField] [Range(140f, 260f)] private float dialoguePanelHeight = 180f;

        private void Awake()
        {
            ApplyCameraViewport();
            ApplyDialoguePanelAnchors();
        }

        private void ApplyCameraViewport()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            cam.rect = new Rect(0f, 0f, 1f, 1f);
        }

        private void ApplyDialoguePanelAnchors()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            var dialoguePanel = canvas.transform.Find("DialoguePanel");
            if (dialoguePanel == null) return;

            var rect = dialoguePanel.GetComponent<RectTransform>();
            if (rect == null) return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(24f, 8f);
            rect.offsetMax = new Vector2(-24f, dialoguePanelHeight);
            dialoguePanel.SetAsLastSibling();
        }
    }
}
