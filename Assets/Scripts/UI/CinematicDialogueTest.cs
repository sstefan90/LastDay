#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Reflection;
using LastDay.UI;

namespace LastDay.Tests
{
    /// <summary>
    /// Play-Mode usability test for CinematicDialogueUI.
    /// Add to the scene via LastDay/Test: Cinematic Dialogue, then enter Play Mode.
    /// All results are written to the Unity Console as pass/fail lines.
    /// Remove this component before shipping a build.
    /// </summary>
    [AddComponentMenu("")]   // hide from Add Component browser
    public class CinematicDialogueTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Seconds to observe each test result before moving to the next.")]
        [SerializeField] private float pauseBetweenTests = 3.5f;
        [SerializeField] private bool runOnStart = false;

        private CinematicDialogueUI _ui;
        private int _passed;
        private int _failed;

        // ── Lifecycle ──────────────────────────────────────────────────────

        void Start()
        {
            _ui = FindObjectOfType<CinematicDialogueUI>();

            if (_ui == null)
            {
                Debug.LogWarning("[CinematicTest] CinematicDialogueUI not found. " +
                    "Run LastDay/Patch: Apply Cinematic Dialogue first, then re-enter Play Mode.");
                return;
            }

            if (runOnStart)
                StartCoroutine(RunAllTests());
        }

        /// <summary>Run all tests from a ContextMenu button in the Inspector.</summary>
        [ContextMenu("Run Tests Now")]
        public void RunTestsNow()
        {
            if (_ui == null) _ui = FindObjectOfType<CinematicDialogueUI>();
            if (_ui != null) StartCoroutine(RunAllTests());
        }

        // ── Test suite ─────────────────────────────────────────────────────

        private IEnumerator RunAllTests()
        {
            _passed = 0;
            _failed = 0;
            Debug.Log("[CinematicTest] ═══════ Starting cinematic dialogue tests ═══════");

            yield return RunTest("1: Short NPC line — top bar visible, fits 1 line",
                Test_ShortNPCLine());

            yield return RunTest("2: Long NPC line — chunks with '...' continuation",
                Test_LongLineOverflow());

            yield return RunTest("3: Phone call — top bar, speaker label changes",
                Test_PhoneCall());

            yield return RunTest("4: Monologue — bottom bar only, top bar hidden",
                Test_Monologue());

            yield return RunTest("5a: Action description — top bar italic [ ] format",
                Test_ActionDescription_Appears());

            yield return RunTest("5b: Action description — auto-closes after display time",
                Test_ActionDescription_AutoCloses());

            yield return RunTest("6: Waiting cue — top bar shows italic cue on SubmitInput",
                Test_WaitingCue());

            yield return RunTest("7: Rapid fire — 3 consecutive opens, no state leak",
                Test_RapidFire());

            string summary = $"[CinematicTest] ═══════ Done: {_passed} passed  {_failed} failed ═══════";
            if (_failed == 0)
                Debug.Log(summary);
            else
                Debug.LogWarning(summary);
        }

        /// Wraps a test coroutine: logs pass/fail and waits between tests.
        private IEnumerator RunTest(string label, IEnumerator test)
        {
            _ui.Close();
            yield return new WaitForSeconds(0.15f);

            Debug.Log($"[CinematicTest] ── TEST {label}");
            yield return StartCoroutine(test);
            yield return new WaitForSeconds(pauseBetweenTests);
            _ui.Close();
            yield return new WaitForSeconds(0.15f);
        }

        // ── Individual tests ───────────────────────────────────────────────

        private IEnumerator Test_ShortNPCLine()
        {
            _ui.OpenForNPC("martha", "Martha");
            yield return new WaitForSeconds(0.5f);
            Pass_If(IsTopBarActive(), "Top bar active after OpenForNPC");
            Pass_If(!IsBottomBarActive(), "Bottom bar hidden during NPC response");
        }

        private IEnumerator Test_LongLineOverflow()
        {
            // ShowAction uses the top bar with a long string; visually confirms chunking
            string longText = "Robert stands by the window for a long moment. " +
                "Outside, the mountains are white. He remembers the ice, the rope, " +
                "Arthur's voice on the radio. He has not thought about that day in years. " +
                "Now he cannot stop.";
            _ui.ShowAction(longText);
            yield return new WaitForSeconds(1f);
            Pass_If(IsTopBarActive(), "Top bar active for long action text");
            Debug.Log("[CinematicTest]   ► Check the top bar visually — text should appear in full, no overflow clipping.");
        }

        private IEnumerator Test_PhoneCall()
        {
            _ui.OpenForPhone();
            yield return new WaitForSeconds(0.5f);
            Pass_If(IsTopBarActive(), "Top bar active for phone call");
            Pass_If(!IsBottomBarActive(), "Bottom bar hidden during phone call response");
        }

        private IEnumerator Test_Monologue()
        {
            _ui.ShowMonologue("There's a massive crack down the back of the neck. It's broken.");
            yield return new WaitForSeconds(0.5f);
            Pass_If(!IsTopBarActive(), "Top bar hidden during monologue");
            Pass_If(IsBottomBarActive(), "Bottom bar active during monologue");
        }

        private IEnumerator Test_ActionDescription_Appears()
        {
            _ui.ShowAction("The phone is silent.");
            yield return new WaitForSeconds(0.4f);
            Pass_If(IsTopBarActive(), "Top bar shows action description");
            Pass_If(!IsBottomBarActive(), "Bottom bar hidden during action description");
        }

        private IEnumerator Test_ActionDescription_AutoCloses()
        {
            // Read actionDisplaySeconds via reflection to set an accurate wait
            float displaySec = GetPrivateFloat("actionDisplaySeconds", 2.5f);
            _ui.ShowAction("The phone is silent.");
            yield return new WaitForSeconds(displaySec + 0.8f);
            Pass_If(!IsTopBarActive(), $"Top bar auto-hides after {displaySec}s action display");
            Pass_If(!IsBottomBarActive(), "Bottom bar also hidden after action display");
        }

        private IEnumerator Test_WaitingCue()
        {
            // Waiting state is shown inside SubmitInput which requires the LLM.
            // We test it indirectly: OpenForNPC triggers NPCResponding (not Waiting).
            // Verify the waitingCues array is populated so ShowWaitingCue won't NRE.
            var cues = GetPrivateArray<string>("waitingCues");
            Pass_If(cues != null && cues.Length > 0,
                $"waitingCues array populated ({(cues != null ? cues.Length : 0)} entries)");
            Debug.Log("[CinematicTest]   ► To test Waiting state live: type something in the input " +
                "field and hit Send while LLM is active. Top bar should show a random italic [ cue ].");
            yield return null;
        }

        private IEnumerator Test_RapidFire()
        {
            _ui.OpenForNPC("martha", "Martha");
            yield return new WaitForSeconds(0.1f);
            _ui.OpenForNPC("martha", "Martha");
            yield return new WaitForSeconds(0.1f);
            _ui.OpenForNPC("martha", "Martha");
            yield return new WaitForSeconds(0.5f);
            Pass_If(IsTopBarActive(), "Top bar still valid after 3 rapid OpenForNPC calls");
            Pass_If(!IsBottomBarActive(), "Bottom bar not leaked by rapid fire");
        }

        // ── Assertion helpers ──────────────────────────────────────────────

        private void Pass_If(bool condition, string label)
        {
            if (condition)
            {
                Debug.Log($"[CinematicTest]   PASS  {label}");
                _passed++;
            }
            else
            {
                Debug.LogWarning($"[CinematicTest]   FAIL  {label}");
                _failed++;
            }
        }

        // ── Reflection helpers to read private CinematicDialogueUI fields ─

        private bool IsTopBarActive()
        {
            var cg = GetPrivateField<CanvasGroup>("topBar");
            return cg != null && cg.gameObject.activeSelf;
        }

        private bool IsBottomBarActive()
        {
            var cg = GetPrivateField<CanvasGroup>("bottomBar");
            return cg != null && cg.gameObject.activeSelf;
        }

        private T GetPrivateField<T>(string fieldName) where T : class
        {
            var fi = typeof(CinematicDialogueUI).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            return fi?.GetValue(_ui) as T;
        }

        private T[] GetPrivateArray<T>(string fieldName)
        {
            var fi = typeof(CinematicDialogueUI).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            return fi?.GetValue(_ui) as T[];
        }

        private float GetPrivateFloat(string fieldName, float fallback)
        {
            var fi = typeof(CinematicDialogueUI).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi == null) return fallback;
            return (float)fi.GetValue(_ui);
        }
    }
}
#endif
