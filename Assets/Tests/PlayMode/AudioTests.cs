using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LastDay.Audio;
using LastDay.Core;

namespace LastDay.Tests
{
    public class AudioTests
    {
        private GameObject testRoot;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("__AudioTestRoot__");
        }

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
                Object.Destroy(testRoot);

            foreach (var s in Object.FindObjectsOfType<AudioManager>())
                Object.Destroy(s.gameObject);
            foreach (var s in Object.FindObjectsOfType<GameStateMachine>())
                Object.Destroy(s.gameObject);
            foreach (var s in Object.FindObjectsOfType<EventManager>())
                Object.Destroy(s.gameObject);
            foreach (var s in Object.FindObjectsOfType<GameManager>())
                Object.Destroy(s.gameObject);
        }

        GameObject CreateChild(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(testRoot.transform);
            return go;
        }

        AudioManager CreateAudioManager(bool withSources = true, bool withClips = false)
        {
            var go = CreateChild("AudioManager");
            go.SetActive(false);

            var mgr = go.AddComponent<AudioManager>();

            if (withSources)
            {
                var musicSrc = go.AddComponent<AudioSource>();
                musicSrc.playOnAwake = false;
                musicSrc.loop = true;
                var sfxSrc = go.AddComponent<AudioSource>();
                sfxSrc.playOnAwake = false;
                var blipSrc = go.AddComponent<AudioSource>();
                blipSrc.playOnAwake = false;

                SetField(mgr, "musicSource", musicSrc);
                SetField(mgr, "sfxSource", sfxSrc);
                SetField(mgr, "dialogueBlipSource", blipSrc);
            }

            if (withClips)
            {
                var dummyClip = AudioClip.Create("dummy", 4410, 1, 44100, false);
                SetField(mgr, "ambientLoop", dummyClip);
                SetField(mgr, "clockTicking", dummyClip);
                SetField(mgr, "typing", dummyClip);
                SetField(mgr, "paperTear", dummyClip);
                SetField(mgr, "phoneRinging", dummyClip);
            }

            go.SetActive(true);
            return mgr;
        }

        static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                field.SetValue(target, value);
        }

        static object GetField(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(target);
        }

        // ═══════════════════════════════════════════════════════
        //  AUDIO MANAGER INITIALIZATION
        // ═══════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator AudioManager_Awake_RegistersMusicClips()
        {
            var mgr = CreateAudioManager(withSources: true, withClips: true);
            yield return null;

            var musicClips = GetField(mgr, "musicClips") as System.Collections.Generic.Dictionary<string, AudioClip>;
            Assert.IsNotNull(musicClips, "musicClips dictionary should be initialized in Awake");
            Assert.IsTrue(musicClips.ContainsKey("ambient_loop"), "Should register ambient_loop key");
            Assert.IsTrue(musicClips.ContainsKey("ending_signed"), "Should register ending_signed key");
            Assert.IsTrue(musicClips.ContainsKey("ending_torn"), "Should register ending_torn key");

            Debug.Log("[TEST PASS] AudioManager_Awake_RegistersMusicClips");
        }

        [UnityTest]
        public IEnumerator AudioManager_Awake_ConfiguresSourceSettings()
        {
            var mgr = CreateAudioManager(withSources: true);
            yield return null;

            var musicSrc = GetField(mgr, "musicSource") as AudioSource;
            var sfxSrc = GetField(mgr, "sfxSource") as AudioSource;

            Assert.IsNotNull(musicSrc, "musicSource should be assigned");
            Assert.IsNotNull(sfxSrc, "sfxSource should be assigned");
            Assert.IsTrue(musicSrc.loop, "musicSource should loop");
            Assert.AreEqual(0f, musicSrc.spatialBlend, "musicSource should be 2D");
            Assert.AreEqual(0f, sfxSrc.spatialBlend, "sfxSource should be 2D");

            Debug.Log("[TEST PASS] AudioManager_Awake_ConfiguresSourceSettings");
        }

        // ═══════════════════════════════════════════════════════
        //  SFX NAME MAPPING
        // ═══════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator PlaySFX_AllNamedClips_DoNotThrow()
        {
            var mgr = CreateAudioManager(withSources: true, withClips: true);
            yield return null;

            string[] sfxNames = { "click", "hover", "paper_rustle", "paper_tear",
                                  "phone_ringing", "typing", "clock_ticking" };

            foreach (string name in sfxNames)
            {
                Assert.DoesNotThrow(() => mgr.PlaySFX(name),
                    $"PlaySFX(\"{name}\") should not throw");
            }

            Debug.Log("[TEST PASS] PlaySFX_AllNamedClips_DoNotThrow");
        }

        [UnityTest]
        public IEnumerator PlaySFX_UnknownName_DoesNotThrow()
        {
            var mgr = CreateAudioManager(withSources: true);
            yield return null;

            Assert.DoesNotThrow(() => mgr.PlaySFX("nonexistent_sfx"),
                "PlaySFX with unknown name should not throw");

            Debug.Log("[TEST PASS] PlaySFX_UnknownName_DoesNotThrow");
        }

        [UnityTest]
        public IEnumerator PlaySFX_NullSource_DoesNotThrow()
        {
            var mgr = CreateAudioManager(withSources: false);
            yield return null;

            Assert.DoesNotThrow(() => mgr.PlaySFX("typing"),
                "PlaySFX with null sfxSource should not throw");

            Debug.Log("[TEST PASS] PlaySFX_NullSource_DoesNotThrow");
        }

        // ═══════════════════════════════════════════════════════
        //  CLOCK TICK
        // ═══════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator StartClockTick_CreatesLoopingSource()
        {
            var mgr = CreateAudioManager(withSources: true, withClips: true);
            yield return null;

            mgr.StartClockTick();
            yield return null;

            var clockSrc = GetField(mgr, "clockSource") as AudioSource;
            Assert.IsNotNull(clockSrc, "clockSource should be created after StartClockTick");
            Assert.IsTrue(clockSrc.loop, "clockSource should be set to loop");
            Assert.IsTrue(clockSrc.isPlaying, "clockSource should be playing");

            Debug.Log("[TEST PASS] StartClockTick_CreatesLoopingSource");
        }

        [UnityTest]
        public IEnumerator StartClockTick_NullClip_DoesNothing()
        {
            var mgr = CreateAudioManager(withSources: true, withClips: false);
            yield return null;

            Assert.DoesNotThrow(() => mgr.StartClockTick(),
                "StartClockTick with null clockTicking clip should not throw");

            var clockSrc = GetField(mgr, "clockSource") as AudioSource;
            Assert.IsNull(clockSrc, "clockSource should not be created if clip is null");

            Debug.Log("[TEST PASS] StartClockTick_NullClip_DoesNothing");
        }

        [UnityTest]
        public IEnumerator StopClockTick_StopsPlayback()
        {
            var mgr = CreateAudioManager(withSources: true, withClips: true);
            yield return null;

            mgr.StartClockTick();
            yield return null;

            mgr.StopClockTick(0.01f);
            yield return new WaitForSeconds(0.1f);

            var clockSrc = GetField(mgr, "clockSource") as AudioSource;
            Assert.IsNotNull(clockSrc);
            Assert.IsFalse(clockSrc.isPlaying, "clockSource should stop after StopClockTick");

            Debug.Log("[TEST PASS] StopClockTick_StopsPlayback");
        }

        // ═══════════════════════════════════════════════════════
        //  MUSIC PLAYBACK
        // ═══════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator PlayMusic_ValidClip_PlaysOnSource()
        {
            var mgr = CreateAudioManager(withSources: true, withClips: true);
            yield return null;

            mgr.PlayMusic("ambient_loop");
            yield return null;

            var musicSrc = GetField(mgr, "musicSource") as AudioSource;
            Assert.IsNotNull(musicSrc.clip, "musicSource should have a clip assigned");
            Assert.IsTrue(musicSrc.isPlaying, "musicSource should be playing");

            Debug.Log("[TEST PASS] PlayMusic_ValidClip_PlaysOnSource");
        }

        [UnityTest]
        public IEnumerator PlayMusic_InvalidClipName_LogsWarning()
        {
            var mgr = CreateAudioManager(withSources: true, withClips: true);
            yield return null;

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Music clip not found"));
            mgr.PlayMusic("nonexistent_track");
            yield return null;

            Debug.Log("[TEST PASS] PlayMusic_InvalidClipName_LogsWarning");
        }

        [UnityTest]
        public IEnumerator PlayMusic_NullSource_DoesNotThrow()
        {
            var mgr = CreateAudioManager(withSources: false);
            yield return null;

            Assert.DoesNotThrow(() => mgr.PlayMusic("ambient_loop"),
                "PlayMusic with null musicSource should not throw");

            Debug.Log("[TEST PASS] PlayMusic_NullSource_DoesNotThrow");
        }

        // ═══════════════════════════════════════════════════════
        //  SFX FIELD EXISTENCE (ensures new fields are declared)
        // ═══════════════════════════════════════════════════════

        [Test]
        public void AudioManager_HasPhoneRingingField()
        {
            var field = typeof(AudioManager).GetField("phoneRinging",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "AudioManager should have a 'phoneRinging' AudioClip field");
            Debug.Log("[TEST PASS] AudioManager_HasPhoneRingingField");
        }

        [Test]
        public void AudioManager_HasTypingField()
        {
            var field = typeof(AudioManager).GetField("typing",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "AudioManager should have a 'typing' AudioClip field");
            Debug.Log("[TEST PASS] AudioManager_HasTypingField");
        }

        [Test]
        public void AudioManager_HasClockTickingField()
        {
            var field = typeof(AudioManager).GetField("clockTicking",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "AudioManager should have a 'clockTicking' AudioClip field");
            Debug.Log("[TEST PASS] AudioManager_HasClockTickingField");
        }

        [Test]
        public void AudioManager_HasPaperTearField()
        {
            var field = typeof(AudioManager).GetField("paperTear",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "AudioManager should have a 'paperTear' AudioClip field");
            Debug.Log("[TEST PASS] AudioManager_HasPaperTearField");
        }

        // ═══════════════════════════════════════════════════════
        //  STOP MUSIC
        // ═══════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator StopMusic_StopsPlayback()
        {
            var mgr = CreateAudioManager(withSources: true, withClips: true);
            yield return null;

            mgr.PlayMusic("ambient_loop");
            yield return null;

            var musicSrc = GetField(mgr, "musicSource") as AudioSource;
            Assert.IsTrue(musicSrc.isPlaying, "Music should be playing before stop");

            mgr.StopMusic(0.01f);
            yield return new WaitForSeconds(0.1f);

            Assert.IsFalse(musicSrc.isPlaying, "Music should stop after StopMusic");

            Debug.Log("[TEST PASS] StopMusic_StopsPlayback");
        }

        [UnityTest]
        public IEnumerator StopMusic_WhenNotPlaying_DoesNotThrow()
        {
            var mgr = CreateAudioManager(withSources: true);
            yield return null;

            Assert.DoesNotThrow(() => mgr.StopMusic(),
                "StopMusic when nothing is playing should not throw");

            Debug.Log("[TEST PASS] StopMusic_WhenNotPlaying_DoesNotThrow");
        }
    }
}
