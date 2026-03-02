using UnityEngine;

namespace LastDay.Player
{
    /// <summary>
    /// Adds subtle breathing, sway, and occasional tremor to an idle character.
    /// Attach to the character and assign the sprite transform.
    /// </summary>
    public class SubtleIdleMovement : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform characterSprite;

        [Header("Breathing")]
        [SerializeField] private float breathingScaleAmount = 0.015f;
        [SerializeField] private float breathingSpeed = 0.4f;

        [Header("Sway")]
        [SerializeField] private float swayAmount = 0.003f;
        [SerializeField] private float swaySpeed = 0.2f;

        [Header("Tremor (elderly feel)")]
        [SerializeField] private bool enableTremor = true;
        [SerializeField] private float tremorAmount = 0.01f;
        [SerializeField] private float tremorDuration = 0.3f;

        private Vector3 originalScale;
        private Vector3 originalPosition;
        private float nextTremorTime;
        private float tremorEndTime;
        private bool isTremoring;

        void Start()
        {
            if (characterSprite == null)
                characterSprite = transform;

            originalScale = characterSprite.localScale;
            originalPosition = characterSprite.localPosition;
            nextTremorTime = Time.time + Random.Range(2f, 5f);
        }

        void Update()
        {
            if (characterSprite == null) return;

            Vector3 newScale = originalScale;
            Vector3 newPosition = originalPosition;

            // Breathing
            float breathPhase = Time.time * breathingSpeed * Mathf.PI * 2;
            float breathOffset = Mathf.Sin(breathPhase) * breathingScaleAmount;
            newScale.y = originalScale.y + breathOffset;
            newPosition.y = originalPosition.y + breathOffset * 0.5f;

            // Sway
            float swayPhase = Time.time * swaySpeed * Mathf.PI * 2;
            newPosition.x = originalPosition.x + Mathf.Sin(swayPhase) * swayAmount;

            // Tremor
            if (enableTremor)
                ApplyTremor(ref newPosition);

            characterSprite.localScale = newScale;
            characterSprite.localPosition = newPosition;
        }

        private void ApplyTremor(ref Vector3 position)
        {
            if (!isTremoring && Time.time > nextTremorTime)
            {
                isTremoring = true;
                tremorEndTime = Time.time + tremorDuration;
                nextTremorTime = Time.time + Random.Range(3f, 8f);
            }

            if (isTremoring)
            {
                float tremorX = Mathf.Sin(Time.time * 40f) * tremorAmount;
                tremorX += Mathf.Sin(Time.time * 55f) * tremorAmount * 0.5f;
                position.x += tremorX;

                if (Time.time > tremorEndTime)
                    isTremoring = false;
            }
        }

        public void OnStartWalking()
        {
            enabled = false;
            if (characterSprite != null)
            {
                characterSprite.localScale = originalScale;
                characterSprite.localPosition = originalPosition;
            }
        }

        public void OnStopWalking()
        {
            enabled = true;
        }
    }
}
