using UnityEngine;
using System.Collections;
using LastDay.Utilities;

namespace LastDay.Core
{
    public class FadeManager : Singleton<FadeManager>
    {
        [Header("References")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        [Header("Settings")]
        [SerializeField] private float defaultFadeDuration = 1f;

        protected override void Awake()
        {
            base.Awake();

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// Fade the screen to black.
        /// </summary>
        public void FadeOut(float duration = -1f, System.Action onComplete = null)
        {
            if (duration < 0) duration = defaultFadeDuration;
            StartCoroutine(FadeRoutine(0f, 1f, duration, onComplete));
        }

        /// <summary>
        /// Fade the screen from black to clear.
        /// </summary>
        public void FadeIn(float duration = -1f, System.Action onComplete = null)
        {
            if (duration < 0) duration = defaultFadeDuration;
            StartCoroutine(FadeRoutine(1f, 0f, duration, onComplete));
        }

        /// <summary>
        /// Fade out, run an action, then fade back in.
        /// </summary>
        public void FadeOutAndIn(System.Action duringBlack, float outDuration = -1f, float holdDuration = 0.5f, float inDuration = -1f)
        {
            if (outDuration < 0) outDuration = defaultFadeDuration;
            if (inDuration < 0) inDuration = defaultFadeDuration;

            FadeOut(outDuration, () =>
            {
                duringBlack?.Invoke();
                StartCoroutine(HoldThenFadeIn(holdDuration, inDuration));
            });
        }

        private IEnumerator HoldThenFadeIn(float holdDuration, float inDuration)
        {
            yield return new WaitForSeconds(holdDuration);
            FadeIn(inDuration);
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration, System.Action onComplete)
        {
            if (fadeCanvasGroup == null)
            {
                Debug.LogWarning("[FadeManager] No CanvasGroup assigned.");
                onComplete?.Invoke();
                yield break;
            }

            fadeCanvasGroup.blocksRaycasts = true;
            fadeCanvasGroup.alpha = startAlpha;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }

            fadeCanvasGroup.alpha = endAlpha;

            if (endAlpha <= 0f)
                fadeCanvasGroup.blocksRaycasts = false;

            onComplete?.Invoke();
        }
    }
}
