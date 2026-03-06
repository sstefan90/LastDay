using UnityEngine;
using TMPro;
using System.Collections;

namespace LastDay.UI
{
    /// <summary>
    /// Displays dialogue text above a character (e.g. Martha) — white text floating above the NPC,
    /// like the Interview with the Whisperer reference. Uses screen-space positioning.
    /// </summary>
    public class FloatingDialogueDisplay : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform targetCharacter;

        [Header("Display")]
        [SerializeField] private TMP_Text textComponent;
        [SerializeField] private float screenOffsetY = 60f;
        [SerializeField] private float typewriterSpeed = 0.03f;
        [SerializeField] private float autoHideAfterSeconds = 0f;

        private RectTransform rectTransform;
        private UnityEngine.Camera cam;
        private Coroutine typewriterCoroutine;
        private Coroutine autoHideCoroutine;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            cam = UnityEngine.Camera.main;
            if (textComponent != null)
                textComponent.text = "";
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (targetCharacter != null && rectTransform != null && cam != null && gameObject.activeSelf)
                UpdatePosition();
        }

        /// <summary>Show text above the character, optionally with typewriter effect.</summary>
        public void Show(string text, bool useTypewriter = true)
        {
            if (textComponent == null) return;

            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);
            if (autoHideCoroutine != null)
                StopCoroutine(autoHideCoroutine);

            gameObject.SetActive(true);
            textComponent.text = text;
            textComponent.maxVisibleCharacters = useTypewriter ? 0 : text.Length;

            if (useTypewriter && text.Length > 0)
                typewriterCoroutine = StartCoroutine(TypewriterRoutine(text));
            else if (autoHideAfterSeconds > 0)
                autoHideCoroutine = StartCoroutine(AutoHideRoutine());
        }

        /// <summary>Hide the floating dialogue.</summary>
        public void Hide()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }
            gameObject.SetActive(false);
        }

        private void UpdatePosition()
        {
            Vector3 worldPos = targetCharacter.position + Vector3.up * 0.5f;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            screenPos.y += screenOffsetY;

            if (rectTransform.parent is RectTransform parentRect)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, screenPos, null, out Vector2 localPos);
                rectTransform.anchoredPosition = localPos;
            }
        }

        private IEnumerator TypewriterRoutine(string text)
        {
            for (int i = 0; i <= text.Length; i++)
            {
                if (textComponent == null) yield break;
                textComponent.maxVisibleCharacters = i;
                yield return new WaitForSeconds(typewriterSpeed);
            }
            typewriterCoroutine = null;
            if (autoHideAfterSeconds > 0)
                autoHideCoroutine = StartCoroutine(AutoHideRoutine());
        }

        private IEnumerator AutoHideRoutine()
        {
            yield return new WaitForSeconds(autoHideAfterSeconds);
            autoHideCoroutine = null;
            Hide();
        }
    }
}
