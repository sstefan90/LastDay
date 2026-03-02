using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LastDay.Utilities;

namespace LastDay.Audio
{
    /// <summary>
    /// Centralized audio management with separate channels for music, SFX, and dialogue.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource dialogueBlipSource;

        [Header("Music Clips")]
        [SerializeField] private AudioClip ambientLoop;
        [SerializeField] private AudioClip endingSigned;
        [SerializeField] private AudioClip endingTorn;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip clickSfx;
        [SerializeField] private AudioClip hoverSfx;
        [SerializeField] private AudioClip paperRustle;
        [SerializeField] private AudioClip paperTear;

        [Header("Dialogue Blips")]
        [SerializeField] private AudioClip[] textBlips;

        [Header("Settings")]
        [SerializeField] private float musicVolume = 0.5f;
        [SerializeField] private float sfxVolume = 0.7f;
        [SerializeField] private float crossfadeDuration = 1f;

        private Dictionary<string, AudioClip> musicClips;

        protected override void Awake()
        {
            base.Awake();

            musicClips = new Dictionary<string, AudioClip>
            {
                { "ambient_loop", ambientLoop },
                { "ending_signed", endingSigned },
                { "ending_torn", endingTorn }
            };

            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
                musicSource.loop = true;
                musicSource.spatialBlend = 0f;
            }

            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
                sfxSource.spatialBlend = 0f;
            }

            if (dialogueBlipSource != null)
                dialogueBlipSource.spatialBlend = 0f;
        }

        public void PlayMusic(string clipName)
        {
            if (musicSource == null) return;

            if (musicClips.TryGetValue(clipName, out AudioClip clip) && clip != null)
            {
                if (musicSource.isPlaying)
                    StartCoroutine(CrossfadeMusic(clip));
                else
                {
                    musicSource.clip = clip;
                    musicSource.Play();
                }

                Debug.Log($"[Audio] Playing music: {clipName}");
            }
            else
            {
                Debug.LogWarning($"[Audio] Music clip not found or null: {clipName}");
            }
        }

        public void StopMusic(float fadeOut = 1f)
        {
            if (musicSource != null && musicSource.isPlaying)
                StartCoroutine(FadeOutMusic(fadeOut));
        }

        public void PlaySFX(string sfxName)
        {
            if (sfxSource == null) return;

            AudioClip clip = sfxName switch
            {
                "click" => clickSfx,
                "hover" => hoverSfx,
                "paper_rustle" => paperRustle,
                "paper_tear" => paperTear,
                _ => null
            };

            if (clip != null)
                sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void PlaySFX(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
                sfxSource.PlayOneShot(clip, sfxVolume);
        }

        /// <summary>
        /// Play a random text blip for typewriter effect.
        /// </summary>
        public void PlayTextBlip()
        {
            if (dialogueBlipSource == null || textBlips == null || textBlips.Length == 0) return;

            dialogueBlipSource.pitch = Random.Range(0.9f, 1.1f);
            dialogueBlipSource.PlayOneShot(textBlips[Random.Range(0, textBlips.Length)]);
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            float startVolume = musicSource.volume;

            // Fade out
            float elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / crossfadeDuration);
                yield return null;
            }

            // Switch clip
            musicSource.clip = newClip;
            musicSource.Play();

            // Fade in
            elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / crossfadeDuration);
                yield return null;
            }

            musicSource.volume = musicVolume;
        }

        private IEnumerator FadeOutMusic(float duration)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            musicSource.Stop();
            musicSource.volume = musicVolume;
        }
    }
}
