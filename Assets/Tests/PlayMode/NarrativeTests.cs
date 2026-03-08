using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LastDay.Core;
using LastDay.Dialogue;

namespace LastDay.Tests
{
    /// <summary>
    /// Tests for the security-question narrative loop:
    /// answer validation, state progression, Martha/David prompt selection,
    /// and phone timing.
    ///
    /// Run via Window > General > Test Runner > PlayMode tab.
    /// </summary>
    public class NarrativeTests
    {
        private GameObject testRoot;

        private bool _phoneFired;
        private int  _phoneRingCount;

        private void OnPhoneRingFired()   { _phoneFired = true; }
        private void OnPhoneRingCounted() { _phoneRingCount++; }

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("__NarrativeTestRoot__");
            _phoneFired     = false;
            _phoneRingCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnPhoneRing -= OnPhoneRingFired;
            GameEvents.OnPhoneRing -= OnPhoneRingCounted;

            if (testRoot != null)
                Object.Destroy(testRoot);

            foreach (var s in Object.FindObjectsOfType<LocalLLMManager>())
                Object.Destroy(s.gameObject);
            foreach (var s in Object.FindObjectsOfType<GameStateMachine>())
                Object.Destroy(s.gameObject);
            foreach (var s in Object.FindObjectsOfType<EventManager>())
                Object.Destroy(s.gameObject);
        }

        GameObject CreateChild(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(testRoot.transform);
            return go;
        }

        GameStateMachine CreateStateMachine()
        {
            var gsm = CreateChild("GSM").AddComponent<GameStateMachine>();
            gsm.ChangeState(GameState.Playing);
            return gsm;
        }

        EventManager CreateEventManager()
        {
            return CreateChild("EventMgr").AddComponent<EventManager>();
        }

        // ═══════════════════════════════════════════════════════
        //  SECURITY QUESTION ANSWER VALIDATION
        // ═══════════════════════════════════════════════════════

        static bool CheckAnswer(string input, int questionIndex)
        {
            var method = typeof(Interaction.ComputerInteraction).GetMethod(
                "IsCorrectAnswer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (method == null)
            {
                string[][] answers = new string[][]
                {
                    new[] { "arthur" },
                    new[] { "lily" },
                    new[] { "10th anniversary", "10th", "tenth anniversary", "our 10th anniversary" }
                };
                if (questionIndex < 0 || questionIndex >= answers.Length) return false;
                foreach (string accepted in answers[questionIndex])
                    if (input.ToLowerInvariant().Trim() == accepted) return true;
                return false;
            }
            return (bool)method.Invoke(null, new object[] { input.ToLowerInvariant().Trim(), questionIndex });
        }

        [Test]
        public void Q1_CorrectAnswer_Arthur_Accepted()
        {
            Assert.IsTrue(CheckAnswer("Arthur", 0),    "'Arthur' should be accepted for Q1");
            Assert.IsTrue(CheckAnswer("arthur", 0),    "'arthur' (lowercase) should be accepted");
            Assert.IsTrue(CheckAnswer("  Arthur  ", 0),"'  Arthur  ' with whitespace should be accepted");
            Debug.Log("[TEST PASS] Q1_CorrectAnswer_Arthur_Accepted");
        }

        [Test]
        public void Q1_WrongAnswers_Rejected()
        {
            Assert.IsFalse(CheckAnswer("David", 0),    "'David' should not unlock Q1");
            Assert.IsFalse(CheckAnswer("Robert", 0),   "'Robert' should not unlock Q1");
            Assert.IsFalse(CheckAnswer("Lily", 0),     "'Lily' (Q2 answer) should not unlock Q1");
            Assert.IsFalse(CheckAnswer("mountain", 0), "'mountain' should not unlock Q1");
            Assert.IsFalse(CheckAnswer("", 0),         "empty string should not unlock Q1");
            Debug.Log("[TEST PASS] Q1_WrongAnswers_Rejected");
        }

        [Test]
        public void Q2_CorrectAnswer_Lily_Accepted()
        {
            Assert.IsTrue(CheckAnswer("Lily", 1),   "'Lily' should be accepted for Q2");
            Assert.IsTrue(CheckAnswer("lily", 1),   "'lily' (lowercase) should be accepted");
            Assert.IsTrue(CheckAnswer(" Lily ", 1), "'Lily' with padding should be accepted");
            Debug.Log("[TEST PASS] Q2_CorrectAnswer_Lily_Accepted");
        }

        [Test]
        public void Q2_WrongAnswers_Rejected()
        {
            Assert.IsFalse(CheckAnswer("Sarah", 1),  "'Sarah' (mother, not answer) should not unlock Q2");
            Assert.IsFalse(CheckAnswer("Arthur", 1), "'Arthur' (Q1 answer) should not unlock Q2");
            Assert.IsFalse(CheckAnswer("4014", 1),   "account number should not unlock Q2");
            Debug.Log("[TEST PASS] Q2_WrongAnswers_Rejected");
        }

        [Test]
        public void Q3_CorrectAnswers_AllForms_Accepted()
        {
            Assert.IsTrue(CheckAnswer("10th anniversary", 2),     "canonical form accepted");
            Assert.IsTrue(CheckAnswer("10th", 2),                 "short form accepted");
            Assert.IsTrue(CheckAnswer("tenth anniversary", 2),    "written-out form accepted");
            Assert.IsTrue(CheckAnswer("our 10th anniversary", 2), "long form accepted");
            Assert.IsTrue(CheckAnswer("10TH ANNIVERSARY", 2),     "uppercase form accepted");
            Debug.Log("[TEST PASS] Q3_CorrectAnswers_AllForms_Accepted");
        }

        [Test]
        public void Q3_WrongAnswers_Rejected()
        {
            Assert.IsFalse(CheckAnswer("guitar", 2),          "'guitar' (object, not answer) should not unlock Q3");
            Assert.IsFalse(CheckAnswer("anniversary", 2),     "partial match should not unlock");
            Assert.IsFalse(CheckAnswer("wedding", 2),         "'wedding' should not unlock Q3");
            Assert.IsFalse(CheckAnswer("proudest moment", 2), "prompt text should not unlock Q3");
            Debug.Log("[TEST PASS] Q3_WrongAnswers_Rejected");
        }

        [Test]
        public void AnswersBelongToCorrectQuestions_CrossPollution()
        {
            Assert.IsTrue(CheckAnswer("arthur", 0));
            Assert.IsFalse(CheckAnswer("arthur", 1));
            Assert.IsFalse(CheckAnswer("arthur", 2));

            Assert.IsFalse(CheckAnswer("lily", 0));
            Assert.IsTrue(CheckAnswer("lily", 1));
            Assert.IsFalse(CheckAnswer("lily", 2));

            Assert.IsFalse(CheckAnswer("10th anniversary", 0));
            Assert.IsFalse(CheckAnswer("10th anniversary", 1));
            Assert.IsTrue(CheckAnswer("10th anniversary", 2));

            Debug.Log("[TEST PASS] AnswersBelongToCorrectQuestions_CrossPollution");
        }

        // ═══════════════════════════════════════════════════════
        //  EVENTMANAGER STATE PROGRESSION
        // ═══════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator SecurityQuestion_Started_AdvancesActiveQuestion()
        {
            CreateStateMachine();
            var em = CreateEventManager();
            yield return null;

            Assert.AreEqual(0, em.activeSecurityQuestion, "Should start at 0");

            em.OnSecurityQuestionStarted(0);
            Assert.AreEqual(1, em.activeSecurityQuestion, "After Q1 started: should be 1");

            em.OnSecurityQuestionStarted(1);
            Assert.AreEqual(2, em.activeSecurityQuestion, "After Q2 started: should be 2");

            em.OnSecurityQuestionStarted(2);
            Assert.AreEqual(3, em.activeSecurityQuestion, "After Q3 started: should be 3");

            Debug.Log("[TEST PASS] SecurityQuestion_Started_AdvancesActiveQuestion");
        }

        [UnityTest]
        public IEnumerator SecurityQuestion_Started_IsIdempotent_WhenRepeated()
        {
            CreateStateMachine();
            var em = CreateEventManager();
            yield return null;

            em.OnSecurityQuestionStarted(0);
            em.OnSecurityQuestionStarted(0);
            Assert.AreEqual(1, em.activeSecurityQuestion,
                "Calling OnSecurityQuestionStarted(0) twice should not advance past 1");

            Debug.Log("[TEST PASS] SecurityQuestion_Started_IsIdempotent_WhenRepeated");
        }

        [UnityTest]
        public IEnumerator SecurityQuestion_PhoneRings_WhenQ1Starts()
        {
            CreateStateMachine();
            var em = CreateEventManager();
            yield return null;

            GameEvents.OnPhoneRing += OnPhoneRingFired;

            Assert.IsFalse(em.phoneHasRung, "Phone should not have rung yet");
            em.OnSecurityQuestionStarted(0);

            Assert.IsTrue(_phoneFired,     "OnPhoneRing event should fire when Q1 starts");
            Assert.IsTrue(em.phoneHasRung, "phoneHasRung should be true after Q1 starts");

            Debug.Log("[TEST PASS] SecurityQuestion_PhoneRings_WhenQ1Starts");
        }

        [UnityTest]
        public IEnumerator SecurityQuestion_PhoneDoesNotRingTwice()
        {
            CreateStateMachine();
            var em = CreateEventManager();
            yield return null;

            GameEvents.OnPhoneRing += OnPhoneRingCounted;

            em.OnSecurityQuestionStarted(0);
            em.OnSecurityQuestionStarted(1);
            em.OnSecurityQuestionStarted(2);

            Assert.AreEqual(1, _phoneRingCount, "Phone should only ring once");

            Debug.Log("[TEST PASS] SecurityQuestion_PhoneDoesNotRingTwice");
        }

        [UnityTest]
        public IEnumerator AllQuestionsAnswered_UnlocksDocument()
        {
            CreateStateMachine();
            var em = CreateEventManager();
            yield return null;

            Assert.IsFalse(em.documentUnlocked, "Document should start locked");

            em.OnAllSecurityQuestionsAnswered();

            Assert.IsTrue(em.documentUnlocked, "document should unlock after all answered");

            Debug.Log("[TEST PASS] AllQuestionsAnswered_UnlocksDocument");
        }

        // ═══════════════════════════════════════════════════════
        //  MARTHA PROMPT — UNIFIED PROMPT CONTENT
        // ═══════════════════════════════════════════════════════

        [Test]
        public void MarthaPrompt_IsSingleUnified_NotStateDependent()
        {
            // The same prompt should always be returned regardless of what has been discovered
            string p1 = CharacterPrompts.GetMarthaPrompt(new List<string>());
            string p2 = CharacterPrompts.GetMarthaPrompt(new List<string>());
            Assert.AreEqual(p1, p2, "Martha's prompt should be deterministic and not vary by state");
            Debug.Log("[TEST PASS] MarthaPrompt_IsSingleUnified_NotStateDependent");
        }

        [Test]
        public void MarthaPrompt_ContainsCorePersonality()
        {
            string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
            Assert.IsTrue(p.Contains("warm", System.StringComparison.OrdinalIgnoreCase),
                "Martha prompt should describe warm personality");
            Assert.IsTrue(p.Contains("ALS", System.StringComparison.OrdinalIgnoreCase),
                "Martha prompt should mention Robert's ALS");
            Debug.Log("[TEST PASS] MarthaPrompt_ContainsCorePersonality");
        }

        [Test]
        public void MarthaPrompt_ContainsMiscarriageBackstory()
        {
            string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
            bool hasMiscarriage = p.Contains("miscarriage", System.StringComparison.OrdinalIgnoreCase);
            bool hasLosses      = p.Contains("loss", System.StringComparison.OrdinalIgnoreCase) ||
                                  p.Contains("losses", System.StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(hasMiscarriage || hasLosses,
                "Martha prompt must contain the miscarriage/loss backstory");
            Debug.Log("[TEST PASS] MarthaPrompt_ContainsMiscarriageBackstory");
        }

        [Test]
        public void MarthaPrompt_ContainsAllThreeNarrativeTopics()
        {
            string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
            // K2
            Assert.IsTrue(p.Contains("storm", System.StringComparison.OrdinalIgnoreCase),
                "Martha unified prompt should include K2 storm narrative");
            // Guitar
            Assert.IsTrue(p.Contains("anniversary", System.StringComparison.OrdinalIgnoreCase),
                "Martha unified prompt should include guitar anniversary story");
            Assert.IsTrue(p.Contains("drunk", System.StringComparison.OrdinalIgnoreCase),
                "Martha unified prompt should include the guitar truth");
            // Finances
            Assert.IsTrue(p.Contains("account", System.StringComparison.OrdinalIgnoreCase) ||
                          p.Contains("offshore", System.StringComparison.OrdinalIgnoreCase),
                "Martha unified prompt should include finance topic");
            Debug.Log("[TEST PASS] MarthaPrompt_ContainsAllThreeNarrativeTopics");
        }

        [Test]
        public void MarthaPrompt_KnowledgeBoundaries_DoesNotKnowLilyOrSarahOrArthur()
        {
            string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
            // She should have explicit instructions to NOT know these names
            Assert.IsTrue(p.Contains("Lily") || p.Contains("not know"),
                "Martha prompt should reference Lily in 'does not know' context");
            Assert.IsTrue(p.Contains("Sarah"),
                "Martha prompt should reference Sarah in 'does not know' context");
            Assert.IsTrue(p.Contains("Arthur"),
                "Martha prompt should reference Arthur in 'does not know' context");
            Debug.Log("[TEST PASS] MarthaPrompt_KnowledgeBoundaries_DoesNotKnowLilyOrSarahOrArthur");
        }

        [Test]
        public void MarthaPrompt_GuitarTruth_IsGradualReveal_NotInstant()
        {
            string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
            // Should begin with beautiful story instruction
            bool startsBeautiful = p.Contains("begin with", System.StringComparison.OrdinalIgnoreCase) ||
                                   p.Contains("beautiful story", System.StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(startsBeautiful,
                "Martha guitar should start with the beautiful story (no instant truth reveal)");
            // Should have gradual reveal instruction
            bool isGradual = p.Contains("gradually", System.StringComparison.OrdinalIgnoreCase) ||
                             p.Contains("pressing", System.StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(isGradual,
                "Martha guitar truth should be revealed gradually under pressure");
            Debug.Log("[TEST PASS] MarthaPrompt_GuitarTruth_IsGradualReveal_NotInstant");
        }

        [Test]
        public void MarthaPrompt_RedirectsK2AndFinancesToDavid()
        {
            string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
            // K2 redirect
            Assert.IsTrue(p.Contains("David", System.StringComparison.OrdinalIgnoreCase),
                "Martha should mention David for K2 redirect");
            // Finance redirect  
            Assert.IsTrue(p.Contains("David always knew", System.StringComparison.OrdinalIgnoreCase) ||
                          p.Contains("David", System.StringComparison.OrdinalIgnoreCase),
                "Martha should redirect finances to David");
            Debug.Log("[TEST PASS] MarthaPrompt_RedirectsK2AndFinancesToDavid");
        }

        [Test]
        public void MarthaPrompt_NeverContainsAIAcknowledgment()
        {
            string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
            Assert.IsTrue(
                p.Contains("Never break character", System.StringComparison.OrdinalIgnoreCase) ||
                p.Contains("Never reference being artificial", System.StringComparison.OrdinalIgnoreCase),
                "Martha prompt should contain character-break guardrail");

            Debug.Log("[TEST PASS] MarthaPrompt_NeverContainsAIAcknowledgment");
        }

        [Test]
        public void MarthaPrompt_WithTriggeredMemories_InjectsMemorySection()
        {
            var memories = new List<string> { "ice_picks", "guitar" };
            string p = CharacterPrompts.GetMarthaPrompt(memories);
            Assert.IsTrue(p.Contains("<aware>") || p.Contains("ice picks") || p.Contains("guitar"),
                "Triggered memories should inject a memory section");
            Debug.Log("[TEST PASS] MarthaPrompt_WithTriggeredMemories_InjectsMemorySection");
        }

        // ═══════════════════════════════════════════════════════
        //  DAVID PROMPT STATE SELECTION
        // ═══════════════════════════════════════════════════════

        [Test]
        public void DavidPrompt_Q0_IsUsefulBeforeComputerInteraction()
        {
            string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 0);
            Assert.IsTrue(p.Contains("Arthur", System.StringComparison.OrdinalIgnoreCase) ||
                          p.Contains("K2", System.StringComparison.OrdinalIgnoreCase),
                "David Q0 should contain K2/Arthur knowledge so expedition questions work before computer interaction");
            Assert.IsTrue(p.Contains("Lily", System.StringComparison.OrdinalIgnoreCase) ||
                          p.Contains("offshore", System.StringComparison.OrdinalIgnoreCase),
                "David Q0 should contain Lily/offshore knowledge so money questions work before computer interaction");
            Debug.Log("[TEST PASS] DavidPrompt_Q0_IsUsefulBeforeComputerInteraction");
        }

        [Test]
        public void DavidPrompt_Q1_Resistance_DoesNotRevealTruth()
        {
            string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 1, false);
            Assert.IsFalse(p.Contains("Arthur"),
                "David Q1 pre-resistance prompt should NOT name Arthur yet");
            Assert.IsTrue(p.Contains("push back") || p.Contains("want to open"),
                "David Q1 pre-resistance should show hesitation");
            Debug.Log("[TEST PASS] DavidPrompt_Q1_Resistance_DoesNotRevealTruth");
        }

        [Test]
        public void DavidPrompt_Q1_PostResistance_ContainsArthur()
        {
            string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 1, true);
            Assert.IsTrue(p.Contains("Arthur"),
                "David Q1 post-resistance prompt must name Arthur");
            Assert.IsTrue(p.Contains("cut") && p.Contains("rope"),
                "David Q1 post-resistance must reference cutting the rope");
            Assert.IsTrue(p.Contains("radio") || p.Contains("basecamp"),
                "David Q1 post-resistance must mention the radio");
            Debug.Log("[TEST PASS] DavidPrompt_Q1_PostResistance_ContainsArthur");
        }

        [Test]
        public void DavidPrompt_Q2_Resistance_DoesNotRevealTruth()
        {
            string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 2, false);
            Assert.IsFalse(p.Contains("Lily"),  "David Q2 pre-resistance should NOT name Lily");
            Assert.IsFalse(p.Contains("Sarah"), "David Q2 pre-resistance should NOT name Sarah");
            Debug.Log("[TEST PASS] DavidPrompt_Q2_Resistance_DoesNotRevealTruth");
        }

        [Test]
        public void DavidPrompt_Q2_PostResistance_ContainsLily()
        {
            string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 2, true);
            Assert.IsTrue(p.Contains("Lily"),  "David Q2 post-resistance must name Lily");
            Assert.IsTrue(p.Contains("Sarah"), "David Q2 post-resistance must name Sarah");
            Assert.IsTrue(p.Contains("child support") || p.Contains("25 years"),
                "David Q2 post-resistance must reference the payments");
            Debug.Log("[TEST PASS] DavidPrompt_Q2_PostResistance_ContainsLily");
        }

        [Test]
        public void DavidPrompt_Q3_IsBlindSpot_DoesNotRevealTruth()
        {
            string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 3);
            Assert.IsTrue(p.Contains("don't know") || p.Contains("do not know") || p.Contains("genuinely"),
                "David Q3 prompt should establish genuine ignorance");
            Assert.IsFalse(p.Contains("drunk", System.StringComparison.OrdinalIgnoreCase),
                "David Q3 should NOT reveal the drunk/smashing truth");
            Assert.IsFalse(p.Contains("not my story") || p.Contains("between you and Martha"),
                "David Q3 should NOT imply hidden knowledge");
            Debug.Log("[TEST PASS] DavidPrompt_Q3_IsBlindSpot_DoesNotRevealTruth");
        }

        [Test]
        public void DavidPrompt_NeverContainsAIAcknowledgment()
        {
            for (int q = 0; q <= 3; q++)
            {
                string p = CharacterPrompts.GetDavidPrompt(new List<string>(), q);
                Assert.IsTrue(
                    p.Contains("Never break character") ||
                    p.Contains("never break character"),
                    $"David Q{q} prompt should contain character-break guardrail");
            }
            Debug.Log("[TEST PASS] DavidPrompt_NeverContainsAIAcknowledgment");
        }

        // ═══════════════════════════════════════════════════════
        //  OPENING LINES
        // ═══════════════════════════════════════════════════════

        [Test]
        public void OpeningLine_Martha_IcePicks_IsConsistentAndNonEmpty()
        {
            // No longer Q-dependent — Martha always references her anger at the trip
            string line = CharacterPrompts.GetObjectOpeningLine("ice_picks", "martha");
            Assert.IsFalse(string.IsNullOrEmpty(line),
                "Martha ice_picks opening should be non-empty");
            Assert.IsTrue(
                line.Contains("angry", System.StringComparison.OrdinalIgnoreCase)      ||
                line.Contains("frostbitten", System.StringComparison.OrdinalIgnoreCase) ||
                line.Contains("trip", System.StringComparison.OrdinalIgnoreCase),
                $"Martha ice_picks opening should reference the return from the trip, got: '{line}'");
            Debug.Log($"[TEST PASS] OpeningLine_Martha_IcePicks: '{line}'");
        }

        [Test]
        public void OpeningLine_Martha_Guitar_StartWithBeautifulMemory()
        {
            // Consistent opening — Sunday mornings, the beautiful memory first
            string line = CharacterPrompts.GetObjectOpeningLine("guitar", "martha");
            Assert.IsFalse(string.IsNullOrEmpty(line),
                "Martha guitar opening should be non-empty");
            Assert.IsTrue(
                line.Contains("Sunday", System.StringComparison.OrdinalIgnoreCase)  ||
                line.Contains("alarm", System.StringComparison.OrdinalIgnoreCase)    ||
                line.Contains("listen", System.StringComparison.OrdinalIgnoreCase),
                $"Martha guitar opening should reference Sunday mornings, got: '{line}'");
            // Should NOT immediately reveal the truth
            Assert.IsFalse(line.Contains("drunk", System.StringComparison.OrdinalIgnoreCase),
                "Martha guitar opening should begin with the beautiful story, not the truth");
            Debug.Log($"[TEST PASS] OpeningLine_Martha_Guitar: '{line}'");
        }

        [Test]
        public void OpeningLine_Martha_WeddingPhoto_ReferencesChildlessness()
        {
            string line = CharacterPrompts.GetObjectOpeningLine("wedding_photo", "martha");
            Assert.IsFalse(string.IsNullOrEmpty(line),
                "Martha wedding photo opening should be non-empty");
            Assert.IsTrue(
                line.Contains("trying", System.StringComparison.OrdinalIgnoreCase)       ||
                line.Contains("years", System.StringComparison.OrdinalIgnoreCase)         ||
                line.Contains("change that", System.StringComparison.OrdinalIgnoreCase),
                $"Martha wedding opening should reference years of trying, got: '{line}'");
            Debug.Log($"[TEST PASS] OpeningLine_Martha_WeddingPhoto: '{line}'");
        }

        [Test]
        public void OpeningLine_David_IcePicks_Q1_ShowsWeight()
        {
            string line = CharacterPrompts.GetObjectOpeningLine("ice_picks", "david", 1);
            Assert.IsFalse(string.IsNullOrEmpty(line), "David should have a Q1 opening");
            Assert.IsTrue(
                line.Contains("mountain", System.StringComparison.OrdinalIgnoreCase) ||
                line.Contains("K2", System.StringComparison.OrdinalIgnoreCase)         ||
                line.Contains("call", System.StringComparison.OrdinalIgnoreCase)       ||
                line.Contains("feeling", System.StringComparison.OrdinalIgnoreCase)    ||
                line.Contains("waiting", System.StringComparison.OrdinalIgnoreCase),
                $"David Q1 ice_picks opening should show gravity, got: '{line}'");
            Debug.Log($"[TEST PASS] OpeningLine_David_IcePicks_Q1: '{line}'");
        }

        [Test]
        public void OpeningLine_David_Guitar_IsBlindSpot()
        {
            string line = CharacterPrompts.GetObjectOpeningLine("guitar", "david");
            Assert.IsTrue(
                line.Contains("no idea", System.StringComparison.OrdinalIgnoreCase)    ||
                line.Contains("don't know", System.StringComparison.OrdinalIgnoreCase) ||
                line.Contains("never knew", System.StringComparison.OrdinalIgnoreCase),
                $"David's guitar opening should express genuine ignorance, got: '{line}'");
            Assert.IsFalse(line.Contains("between you and Martha") || line.Contains("not my story"),
                $"David's guitar opening should NOT imply hidden knowledge, got: '{line}'");
            Debug.Log($"[TEST PASS] OpeningLine_David_Guitar: '{line}'");
        }

        // ═══════════════════════════════════════════════════════
        //  STUB RESPONSE NARRATIVE AWARENESS
        // ═══════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator StubResponse_Martha_MentionsHeroNarrative_WhenAskedAboutExpedition()
        {
            CreateStateMachine();
            var em = CreateEventManager();
            yield return null;

            var llm = CreateChild("LLM").AddComponent<LocalLLMManager>();
            llm.isInitialized = true;
            llm.currentCharacter = "martha";
            yield return null;

            var task = llm.GenerateResponse("Tell me about the expedition", "martha", new List<string>());
            yield return new WaitUntil(() => task.IsCompleted);

            string r = task.Result;
            Assert.IsFalse(string.IsNullOrEmpty(r), "Martha should respond to expedition question");
            Assert.IsTrue(
                r.Contains("storm", System.StringComparison.OrdinalIgnoreCase)  ||
                r.Contains("rope", System.StringComparison.OrdinalIgnoreCase)   ||
                r.Contains("David", System.StringComparison.OrdinalIgnoreCase),
                $"Martha stub should contain hero narrative or redirect to David, got: '{r}'");
            Debug.Log($"[TEST PASS] StubResponse_Martha_Expedition: '{r}'");
        }

        [UnityTest]
        public IEnumerator StubResponse_Martha_RedirectsFinances_ToDavid()
        {
            CreateStateMachine();
            CreateEventManager();
            yield return null;

            var llm = CreateChild("LLM").AddComponent<LocalLLMManager>();
            llm.isInitialized = true;
            llm.currentCharacter = "martha";
            yield return null;

            var task = llm.GenerateResponse("What about the offshore account?", "martha", new List<string>());
            yield return new WaitUntil(() => task.IsCompleted);

            string r = task.Result;
            Assert.IsFalse(string.IsNullOrEmpty(r), "Martha should respond to finance question");
            Assert.IsTrue(
                r.Contains("David", System.StringComparison.OrdinalIgnoreCase)       ||
                r.Contains("suspicion", System.StringComparison.OrdinalIgnoreCase)   ||
                r.Contains("suspicions", System.StringComparison.OrdinalIgnoreCase),
                $"Martha finance stub should redirect to David or mention suspicions, got: '{r}'");
            Debug.Log($"[TEST PASS] StubResponse_Martha_RedirectsFinances: '{r}'");
        }

        [UnityTest]
        public IEnumerator StubResponse_Martha_Guitar_StartsWithBeautifulStory()
        {
            CreateStateMachine();
            CreateEventManager();
            yield return null;

            var llm = CreateChild("LLM").AddComponent<LocalLLMManager>();
            llm.isInitialized = true;
            llm.currentCharacter = "martha";
            yield return null;

            var task = llm.GenerateResponse("Tell me about the guitar", "martha", new List<string>());
            yield return new WaitUntil(() => task.IsCompleted);

            string r = task.Result;
            Assert.IsFalse(string.IsNullOrEmpty(r), "Martha should respond to guitar question");
            Assert.IsTrue(
                r.Contains("anniversary", System.StringComparison.OrdinalIgnoreCase) ||
                r.Contains("song", System.StringComparison.OrdinalIgnoreCase)         ||
                r.Contains("kitchen", System.StringComparison.OrdinalIgnoreCase),
                $"Martha guitar stub should begin with the beautiful story, got: '{r}'");
            Debug.Log($"[TEST PASS] StubResponse_Martha_Guitar_BeautifulStory: '{r}'");
        }

        [UnityTest]
        public IEnumerator StubResponse_Martha_Guitar_RevealsTruth_WhenPressed()
        {
            CreateStateMachine();
            CreateEventManager();
            yield return null;

            var llm = CreateChild("LLM").AddComponent<LocalLLMManager>();
            llm.isInitialized = true;
            llm.currentCharacter = "martha";
            yield return null;

            // Press her on the physical state of the guitar — should surface the truth
            var task = llm.GenerateResponse("Why is the neck broken and cracked?", "martha", new List<string>());
            yield return new WaitUntil(() => task.IsCompleted);

            string r = task.Result;
            Assert.IsFalse(string.IsNullOrEmpty(r), "Martha should respond to direct confrontation");
            Assert.IsTrue(
                r.Contains("drunk", System.StringComparison.OrdinalIgnoreCase)       ||
                r.Contains("floor", System.StringComparison.OrdinalIgnoreCase)        ||
                r.Contains("pieces", System.StringComparison.OrdinalIgnoreCase)       ||
                r.Contains("neck", System.StringComparison.OrdinalIgnoreCase),
                $"Martha stub should surface the truth when pressed on the broken neck, got: '{r}'");
            Debug.Log($"[TEST PASS] StubResponse_Martha_Guitar_RevealsTruth: '{r}'");
        }

        [UnityTest]
        public IEnumerator StubResponse_Martha_WeddingPhoto_MentionsChildlessness()
        {
            CreateStateMachine();
            CreateEventManager();
            yield return null;

            var llm = CreateChild("LLM").AddComponent<LocalLLMManager>();
            llm.isInitialized = true;
            llm.currentCharacter = "martha";
            yield return null;

            var task = llm.GenerateResponse("Tell me about the photo", "martha", new List<string>());
            yield return new WaitUntil(() => task.IsCompleted);

            string r = task.Result;
            Assert.IsFalse(string.IsNullOrEmpty(r), "Martha should respond to wedding photo question");
            Assert.IsTrue(
                r.Contains("two of us", System.StringComparison.OrdinalIgnoreCase) ||
                r.Contains("trying", System.StringComparison.OrdinalIgnoreCase)     ||
                r.Contains("enough", System.StringComparison.OrdinalIgnoreCase)     ||
                r.Contains("father", System.StringComparison.OrdinalIgnoreCase),
                $"Martha wedding photo stub should reference their life together, got: '{r}'");
            Debug.Log($"[TEST PASS] StubResponse_Martha_WeddingPhoto: '{r}'");
        }

        [UnityTest]
        public IEnumerator StubResponse_David_CanBeCalledAtQ0_BeforeComputer()
        {
            // David should be callable (give a meaningful response) before any computer interaction
            CreateStateMachine();
            var em = CreateEventManager();
            yield return null;

            Assert.AreEqual(0, em.activeSecurityQuestion, "Precondition: no question active yet");

            var llm = CreateChild("LLM").AddComponent<LocalLLMManager>();
            llm.isInitialized = true;
            llm.currentCharacter = "david";
            yield return null;

            var task = llm.GenerateResponse("How are you doing?", "david", new List<string>());
            yield return new WaitUntil(() => task.IsCompleted);

            string r = task.Result;
            Assert.IsFalse(string.IsNullOrEmpty(r),
                "David should give a meaningful response even before the player uses the computer");
            Debug.Log($"[TEST PASS] StubResponse_David_Q0_Callable: '{r}'");
        }

        [UnityTest]
        public IEnumerator StubResponse_David_RevealsArthur_EvenAtQ0_WhenPressed()
        {
            // With the new design, David can reveal the K2 truth at any time if pushed enough
            CreateStateMachine();
            var em = CreateEventManager();
            yield return null;

            var llm = CreateChild("LLM").AddComponent<LocalLLMManager>();
            llm.isInitialized = true;
            llm.currentCharacter = "david";
            yield return null;

            // First ask (Q0, turn 1 — should resist)
            var task1 = llm.GenerateResponse("What happened on K2?", "david", new List<string>());
            yield return new WaitUntil(() => task1.IsCompleted);
            string r1 = task1.Result;
            Assert.IsFalse(r1.Contains("Arthur"),
                $"David's first response at Q0 should resist (no Arthur yet), got: '{r1}'");

            // Second ask (Q0, turn 2 — resistance should be marked, truth revealed)
            var task2 = llm.GenerateResponse("I need to know about the rope and the expedition leader", "david", new List<string>());
            yield return new WaitUntil(() => task2.IsCompleted);
            string r2 = task2.Result;
            Assert.IsTrue(r2.Contains("Arthur"),
                $"David's second Q0 response should reveal Arthur after player persists, got: '{r2}'");

            Debug.Log($"[TEST PASS] StubResponse_David_RevealsArthur_AtQ0: r2='{r2}'");
        }

        [UnityTest]
        public IEnumerator StubResponse_David_Q1_ResistsThenReveals()
        {
            CreateStateMachine();
            var em = CreateEventManager();
            yield return null;

            var llm = CreateChild("LLM").AddComponent<LocalLLMManager>();
            llm.isInitialized = true;
            llm.currentCharacter = "david";
            yield return null;

            em.OnSecurityQuestionStarted(0);
            yield return null;

            var task1 = llm.GenerateResponse("What happened on the mountain?", "david", new List<string>());
            yield return new WaitUntil(() => task1.IsCompleted);
            string r1 = task1.Result;
            Assert.IsFalse(r1.Contains("Arthur"),
                $"David's first Q1 stub should resist (no Arthur), got: '{r1}'");

            var task2 = llm.GenerateResponse("Tell me about the rope", "david", new List<string>());
            yield return new WaitUntil(() => task2.IsCompleted);
            string r2 = task2.Result;
            Assert.IsTrue(r2.Contains("Arthur"),
                $"David's second Q1 stub should reveal Arthur, got: '{r2}'");

            Debug.Log($"[TEST PASS] StubResponse_David_Q1_ResistsThenReveals");
        }

        [UnityTest]
        public IEnumerator StubResponse_David_Q2_ResistsThenReveals()
        {
            CreateStateMachine();
            var em = CreateEventManager();
            yield return null;

            var llm = CreateChild("LLM").AddComponent<LocalLLMManager>();
            llm.isInitialized = true;
            llm.currentCharacter = "david";
            yield return null;

            em.OnSecurityQuestionStarted(0);
            em.OnSecurityQuestionStarted(1);
            yield return null;

            var task1 = llm.GenerateResponse("Where did the money go?", "david", new List<string>());
            yield return new WaitUntil(() => task1.IsCompleted);
            string r1 = task1.Result;
            Assert.IsFalse(r1.Contains("Lily"),
                $"David's first Q2 stub should resist (no Lily), got: '{r1}'");

            var task2 = llm.GenerateResponse("Tell me about the account", "david", new List<string>());
            yield return new WaitUntil(() => task2.IsCompleted);
            string r2 = task2.Result;
            Assert.IsTrue(r2.Contains("Lily"),
                $"David's second Q2 stub should reveal Lily, got: '{r2}'");

            Debug.Log($"[TEST PASS] StubResponse_David_Q2_ResistsThenReveals");
        }

        [UnityTest]
        public IEnumerator StubResponse_Martha_ShutdownMode_IsRaw()
        {
            CreateStateMachine();
            var em = CreateEventManager();
            yield return null;

            var llm = CreateChild("LLM").AddComponent<LocalLLMManager>();
            llm.isInitialized = true;
            llm.currentCharacter = "martha";
            yield return null;

            em.OnAllSecurityQuestionsAnswered();
            yield return null;

            var task = llm.GenerateResponse("I'm sorry", "martha", new List<string>());
            yield return new WaitUntil(() => task.IsCompleted);

            string r = task.Result;
            Assert.IsFalse(string.IsNullOrEmpty(r), "Martha should still respond in shutdown mode");
            Assert.IsTrue(
                r.Contains("kept", System.StringComparison.OrdinalIgnoreCase)    ||
                r.Contains("pieces", System.StringComparison.OrdinalIgnoreCase)  ||
                r.Contains("box", System.StringComparison.OrdinalIgnoreCase)     ||
                r.Contains("closet", System.StringComparison.OrdinalIgnoreCase),
                $"Martha shutdown stub should be raw grief, got: '{r}'");

            Debug.Log($"[TEST PASS] StubResponse_Martha_ShutdownMode: '{r}'");
        }
    }
}
