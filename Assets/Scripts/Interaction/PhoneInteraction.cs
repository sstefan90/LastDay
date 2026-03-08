using UnityEngine;
using System.Collections;
using LastDay.Core;

namespace LastDay.Interaction
{
    /// <summary>
    /// The phone object. Starts ringing after enough memories are triggered.
    /// Clicking answers the call and switches dialogue to David.
    /// </summary>
    public class PhoneInteraction : InteractableObject2D
    {
        [Header("Phone Settings")]
        [SerializeField] private AudioClip ringSound;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private float ringInterval = 3f;
        [SerializeField] private float ringTimeout = 30f;

        private bool isRinging;
        private bool hasBeenAnswered;
        private AudioSource ringAudioSource;
        private Coroutine ringCoroutine;

        void OnEnable()
        {
            GameEvents.OnPhoneRing += HandlePhoneRing;
        }

        void OnDisable()
        {
            GameEvents.OnPhoneRing -= HandlePhoneRing;
        }

        private void HandlePhoneRing()
        {
            if (hasBeenAnswered) return;
            StartRinging();
        }

        private void StartRinging()
        {
            if (isRinging) return;
            isRinging = true;

            ringAudioSource = gameObject.AddComponent<AudioSource>();
            ringAudioSource.clip = ringSound;
            ringAudioSource.spatialBlend = 0f;

            ringCoroutine = StartCoroutine(RingRoutine());
            Debug.Log("[Phone] Phone is ringing!");
        }

        private IEnumerator RingRoutine()
        {
            float elapsed = 0f;
            while (isRinging && elapsed < ringTimeout)
            {
                if (ringSound != null)
                    ringAudioSource.PlayOneShot(ringSound);

                yield return new WaitForSeconds(ringInterval);
                elapsed += ringInterval;
            }

            StopRinging();
        }

        private void StopRinging()
        {
            isRinging = false;
            if (ringCoroutine != null)
                StopCoroutine(ringCoroutine);
            if (ringAudioSource != null)
                Destroy(ringAudioSource);
        }

        public override void OnInteract()
        {
            if (isRinging)
                AnswerPhone();
            else
                CallDavidBack();
        }

        private void AnswerPhone()
        {
            hasBeenAnswered = true;
            StopRinging();

            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);

            OpenDavidDialogue();
            Debug.Log("[Phone] Phone answered. Talking to David.");
        }

        private void CallDavidBack()
        {
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);

            OpenDavidDialogue();
            Debug.Log("[Phone] Calling David back.");
        }

        private void OpenDavidDialogue()
        {
            if (GameStateMachine.Instance != null)
                GameStateMachine.Instance.ChangeState(GameState.PhoneCall);

            if (LastDay.UI.DialogueSession.Current != null)
                LastDay.UI.DialogueSession.Current.OpenForPhone();
        }
    }
}
