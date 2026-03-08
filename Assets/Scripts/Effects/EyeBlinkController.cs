using System.Collections;
using UnityEngine;

namespace LastDay.Effects
{
    /// <summary>
    /// Simulates a character eye-blink by briefly squashing a Transform's local Y scale.
    ///
    /// IMPORTANT — what to attach this to:
    ///   • Best: a dedicated head/eye child GameObject with its own SpriteRenderer
    ///     (only that sub-sprite squashes, body is untouched).
    ///   • Acceptable for single-sprite characters: attach to the root character
    ///     Transform and set Blink Squash to 0.88–0.94 so the whole-body squash
    ///     is barely perceptible.
    ///
    /// Use the optional "Blink Target" field to drive a *different* Transform than
    /// the one this component sits on — useful when you want to keep the component
    /// on the root but animate only a child "head" Transform.
    /// </summary>
    public class EyeBlinkController : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Transform to squash. Leave empty to use this GameObject's own Transform.")]
        [SerializeField] private Transform blinkTarget = null;

        [Header("Blink Timing")]
        [SerializeField, Range(1f, 15f)]  private float blinkIntervalMin  = 3.5f;
        [SerializeField, Range(1f, 20f)]  private float blinkIntervalMax  = 7.5f;

        [Header("Blink Shape")]
        [Tooltip("How far Y scale squashes at peak blink (0.88–0.94 for subtle whole-body; 0.1–0.3 for isolated head sprite).")]
        [SerializeField, Range(0f, 1f)]   private float blinkSquash       = 0.90f;
        [Tooltip("Seconds to close the eyes.")]
        [SerializeField, Range(0.02f, 0.2f)] private float closeSeconds   = 0.07f;
        [Tooltip("Seconds to reopen the eyes.")]
        [SerializeField, Range(0.02f, 0.2f)] private float openSeconds    = 0.10f;

        [Header("Double Blink")]
        [Tooltip("Chance each blink is followed immediately by a second blink.")]
        [SerializeField, Range(0f, 1f)]   private float doubleBlink       = 0.15f;
        [SerializeField, Range(0.05f, 0.5f)] private float doubleGap      = 0.12f;

        private Transform _target;
        private Vector3   _baseScale;
        private Coroutine _blinkRoutine;

        void OnEnable()
        {
            _target      = blinkTarget != null ? blinkTarget : transform;
            _baseScale   = _target.localScale;
            _blinkRoutine = StartCoroutine(BlinkLoop());
        }

        void OnDisable()
        {
            if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
            if (_target != null) _target.localScale = _baseScale;
        }

        private IEnumerator BlinkLoop()
        {
            // Stagger start so multiple characters don't blink simultaneously
            yield return new WaitForSeconds(Random.Range(0f, blinkIntervalMax));

            while (true)
            {
                yield return PerformBlink();

                if (Random.value < doubleBlink)
                {
                    yield return new WaitForSeconds(doubleGap);
                    yield return PerformBlink();
                }

                float wait = Random.Range(blinkIntervalMin, blinkIntervalMax);
                yield return new WaitForSeconds(wait);
            }
        }

        private IEnumerator PerformBlink()
        {
            // Close
            yield return AnimateScaleY(1f, blinkSquash, closeSeconds);
            // Open
            yield return AnimateScaleY(blinkSquash, 1f, openSeconds);
        }

        private IEnumerator AnimateScaleY(float fromT, float toT, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / duration);
                // Ease-in-out
                t = t * t * (3f - 2f * t);
                float scaleY = Mathf.Lerp(_baseScale.y * fromT, _baseScale.y * toT, t);
                _target.localScale = new Vector3(
                    _target.localScale.x,
                    scaleY,
                    _target.localScale.z
                );
                yield return null;
            }
            _target.localScale = new Vector3(
                _target.localScale.x,
                _baseScale.y * toT,
                _target.localScale.z
            );
        }
    }
}
