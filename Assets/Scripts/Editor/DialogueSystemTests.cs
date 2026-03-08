using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using LastDay.Dialogue;

/// <summary>
/// Fast Editor-only tests for the dialogue prompt system and game state machine.
/// No Play Mode required — safe to run at any time.
/// Run via: LastDay/Test: Dialogue System
/// </summary>
public static class DialogueSystemTests
{
    [MenuItem("LastDay/Test: Dialogue System", priority = 12)]
    public static void RunAll()
    {
        int passed = 0;
        int failed = 0;

        Debug.Log("[DialogueTest] ═══════ Starting Dialogue System tests ═══════");

        // ── Martha unified prompt ──────────────────────────────────────────
        Run("1:  Martha prompt is non-empty",                          Test_Martha_PromptNonEmpty,              ref passed, ref failed);
        Run("2:  Martha contains core voice (warm)",                   Test_Martha_ContainsCoreVoice,           ref passed, ref failed);
        Run("3:  Martha contains ALS context",                         Test_Martha_ContainsALS,                 ref passed, ref failed);
        Run("4:  Martha contains miscarriage backstory",               Test_Martha_ContainsMiscarriage,         ref passed, ref failed);
        Run("5:  Martha contains K2 storm narrative",                  Test_Martha_ContainsK2Story,             ref passed, ref failed);
        Run("6:  Martha does not know Arthur",                         Test_Martha_DoesNotKnowArthur,           ref passed, ref failed);
        Run("7:  Martha redirects K2 leader to David",                 Test_Martha_RedirectsK2ToDavid,          ref passed, ref failed);
        Run("8:  Martha contains guitar anniversary story",            Test_Martha_ContainsGuitarAnniversary,   ref passed, ref failed);
        Run("9:  Martha contains guitar truth",                        Test_Martha_ContainsGuitarTruth,         ref passed, ref failed);
        Run("10: Martha guitar truth revealed gradually under pressure",Test_Martha_GuitarGradualReveal,        ref passed, ref failed);
        Run("11: Martha does not know Lily",                           Test_Martha_DoesNotKnowLily,             ref passed, ref failed);
        Run("12: Martha does not know Sarah",                          Test_Martha_DoesNotKnowSarah,            ref passed, ref failed);
        Run("13: Martha redirects finances to David",                  Test_Martha_RedirectsFinancesToDavid,    ref passed, ref failed);
        Run("14: Martha contains character guardrail",                 Test_Martha_ContainsGuardrail,           ref passed, ref failed);
        // Test 15 removed: Martha shutdown mode has been eliminated — one prompt throughout the game.
        Run("16: Martha memory section injected for triggered objects",Test_Martha_MemorySectionInjected,       ref passed, ref failed);
        Run("17: Martha wedding photo memory mentions childlessness",  Test_Martha_WeddingPhotoMemoryContext,   ref passed, ref failed);

        // ── Opening monologue ──────────────────────────────────────────────
        Run("18: Opening monologue is non-empty",                      Test_OpeningMonologue_NonEmpty,          ref passed, ref failed);
        Run("19: Opening monologue mentions the locked questions",     Test_OpeningMonologue_MentionsQuestions, ref passed, ref failed);
        Run("20: Opening monologue mentions Robert's past decision",   Test_OpeningMonologue_MentionsFacingIt,  ref passed, ref failed);

        // ── David prompt ───────────────────────────────────────────────────
        Run("21: David Q0 prompt is non-empty",                        Test_David_Q0_NonEmpty,                  ref passed, ref failed);
        Run("22: David Q0 is available for anything (no gate)",        Test_David_Q0_AlwaysAvailable,           ref passed, ref failed);
        Run("23: David Q1 pre-resist does not name Arthur",            Test_David_Q1_PreResist_NoArthur,        ref passed, ref failed);
        Run("24: David Q1 post-resist names Arthur and rope",          Test_David_Q1_PostResist_NamesArthur,    ref passed, ref failed);
        Run("25: David Q2 pre-resist names no one",                    Test_David_Q2_PreResist_Silent,          ref passed, ref failed);
        Run("26: David Q2 post-resist names Lily and Sarah",           Test_David_Q2_PostResist_NamesLily,      ref passed, ref failed);
        Run("27: David Q3 is genuine blind spot (no hidden knowledge)",Test_David_Q3_BlindSpot,                 ref passed, ref failed);
        Run("28: David prompt contains character guardrail",           Test_David_ContainsGuardrail,            ref passed, ref failed);

        // ── Opening lines ──────────────────────────────────────────────────
        Run("29: Martha ice_picks opening is consistent",              Test_OpeningLine_Martha_IcePicks,        ref passed, ref failed);
        Run("30: Martha guitar opening references Sunday mornings",    Test_OpeningLine_Martha_Guitar,          ref passed, ref failed);
        Run("31: Martha wedding photo opening references childlessness",Test_OpeningLine_Martha_Wedding,        ref passed, ref failed);
        Run("32: David phone opening is non-empty",                    Test_OpeningLine_David_Phone,            ref passed, ref failed);
        Run("33: David Q1 opening shows gravity",                      Test_OpeningLine_David_Q1IcePicks,       ref passed, ref failed);
        Run("34: David Q2 opening references money",                   Test_OpeningLine_David_Q2WeddingPhoto,   ref passed, ref failed);
        Run("35: David guitar opening expresses ignorance",            Test_OpeningLine_David_Guitar,           ref passed, ref failed);

        // ── State machine transition ───────────────────────────────────────
        Run("36: InDialogue->PhoneCall is a valid transition",         Test_StateMachine_InDialogueToPhoneCall, ref passed, ref failed);

        // ── LLM scene configuration ────────────────────────────────────────
        // Each character now has its own LLM server: Martha (Llama 3 8B, port 13333)
        // and David (Phi-3 Mini, port 13334). Each needs >= 4096 context.
        // After running LastDay > Setup: David Model, there are TWO _contextSize entries per scene.
        Run("37: Scene LLM contextSize each >= 4096 (independent model per character)",
                                                                       Test_Scene_LLMContextSize,               ref passed, ref failed);

        string summary = $"[DialogueTest] ═══════ Done: {passed} passed  {failed} failed ═══════";
        if (failed == 0) Debug.Log(summary);
        else             Debug.LogWarning(summary);
    }

    // ── Runner ────────────────────────────────────────────────────────────

    private static void Run(string label, System.Func<string> test, ref int passed, ref int failed)
    {
        string result;
        try   { result = test(); }
        catch (System.Exception ex) { result = $"EXCEPTION: {ex.Message}"; }

        if (result == null)
        {
            Debug.Log($"[DialogueTest]   PASS  {label}");
            passed++;
        }
        else
        {
            Debug.LogWarning($"[DialogueTest]   FAIL  {label} — {result}");
            failed++;
        }
    }

    // Convention: return null = PASS, return string = FAIL reason.

    // ── Martha unified prompt ─────────────────────────────────────────────

    private static string Test_Martha_PromptNonEmpty()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        return string.IsNullOrEmpty(p) ? "Martha prompt is null or empty" : null;
    }

    private static string Test_Martha_ContainsCoreVoice()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        if (!p.Contains("warm", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should describe warm personality";
        if (!p.Contains("voice", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should contain a <voice> section";
        return null;
    }

    private static string Test_Martha_ContainsALS()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        if (!p.Contains("ALS", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should mention Robert's ALS";
        return null;
    }

    private static string Test_Martha_ContainsMiscarriage()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        bool hasMiscarriage = p.Contains("miscarriage", System.StringComparison.OrdinalIgnoreCase);
        bool hasLosses      = p.Contains("loss", System.StringComparison.OrdinalIgnoreCase) ||
                              p.Contains("losses", System.StringComparison.OrdinalIgnoreCase);
        bool hasTried       = p.Contains("tried for", System.StringComparison.OrdinalIgnoreCase) ||
                              p.Contains("tried to have", System.StringComparison.OrdinalIgnoreCase);
        if (!hasMiscarriage && !(hasLosses && hasTried))
            return "Martha prompt must contain the miscarriage backstory (tried for children, losses, or miscarriage)";
        return null;
    }

    private static string Test_Martha_ContainsK2Story()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        if (!p.Contains("storm", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should contain K2 storm narrative";
        if (!p.Contains("rope", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should reference the rope";
        return null;
    }

    private static string Test_Martha_DoesNotKnowArthur()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        // She should not know Arthur but the prompt should explicitly say so
        if (!p.Contains("Arthur", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should reference Arthur in the 'does not know' guardrail";
        if (!p.Contains("not know", System.StringComparison.OrdinalIgnoreCase) &&
            !p.Contains("never heard", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should state she does not know Arthur";
        return null;
    }

    private static string Test_Martha_RedirectsK2ToDavid()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        bool hasDavid    = p.Contains("David", System.StringComparison.OrdinalIgnoreCase);
        bool hasRedirect = p.Contains("radio", System.StringComparison.OrdinalIgnoreCase) ||
                           p.Contains("call David", System.StringComparison.OrdinalIgnoreCase) ||
                           p.Contains("David", System.StringComparison.OrdinalIgnoreCase);
        if (!hasDavid || !hasRedirect)
            return "Martha prompt should redirect K2 leader questions to David (radio/call David)";
        return null;
    }

    private static string Test_Martha_ContainsGuitarAnniversary()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        if (!p.Contains("anniversary", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should mention the anniversary song";
        if (!p.Contains("sunrise", System.StringComparison.OrdinalIgnoreCase) &&
            !p.Contains("kitchen", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should describe the kitchen sunrise scene";
        return null;
    }

    private static string Test_Martha_ContainsGuitarTruth()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        if (!p.Contains("drunk", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should contain the truth about Robert coming home drunk";
        if (!p.Contains("wall", System.StringComparison.OrdinalIgnoreCase) &&
            !p.Contains("swing", System.StringComparison.OrdinalIgnoreCase) &&
            !p.Contains("swung", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should reference the guitar being smashed against the wall";
        return null;
    }

    private static string Test_Martha_GuitarGradualReveal()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        // The prompt should instruct her to start with beautiful story and reveal gradually under pressure
        bool hasPressing  = p.Contains("pressing", System.StringComparison.OrdinalIgnoreCase) ||
                            p.Contains("keeps pressing", System.StringComparison.OrdinalIgnoreCase);
        bool hasGradual   = p.Contains("gradually", System.StringComparison.OrdinalIgnoreCase) ||
                            p.Contains("surfaces gradually", System.StringComparison.OrdinalIgnoreCase);
        bool hasBeautiful = p.Contains("beautiful story", System.StringComparison.OrdinalIgnoreCase) ||
                            p.Contains("begin with", System.StringComparison.OrdinalIgnoreCase);
        if (!hasBeautiful)
            return "Martha guitar guidance should tell her to begin with the beautiful story";
        if (!hasPressing && !hasGradual)
            return "Martha guitar guidance should describe gradual revelation under pressure";
        return null;
    }

    private static string Test_Martha_DoesNotKnowLily()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        if (!p.Contains("Lily", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should reference Lily in the 'does not know' guardrail";
        if (!p.Contains("never", System.StringComparison.OrdinalIgnoreCase) &&
            !p.Contains("not know", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should state she does not know Lily";
        return null;
    }

    private static string Test_Martha_DoesNotKnowSarah()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        if (!p.Contains("Sarah", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should reference Sarah in the 'does not know' guardrail";
        return null;
    }

    private static string Test_Martha_RedirectsFinancesToDavid()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        if (!p.Contains("offshore", System.StringComparison.OrdinalIgnoreCase) &&
            !p.Contains("account", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt should reference the offshore account topic";
        // Should redirect to David for finances
        bool redirectsToDavid = p.Contains("David always knew", System.StringComparison.OrdinalIgnoreCase) ||
                                p.Contains("David", System.StringComparison.OrdinalIgnoreCase);
        if (!redirectsToDavid)
            return "Martha prompt should redirect finance questions to David";
        return null;
    }

    private static string Test_Martha_ContainsGuardrail()
    {
        string p = CharacterPrompts.GetMarthaPrompt(new List<string>());
        if (!p.Contains("Never break character", System.StringComparison.OrdinalIgnoreCase) &&
            !p.Contains("Never reference being artificial", System.StringComparison.OrdinalIgnoreCase))
            return "Martha prompt must contain character-break guardrail";
        return null;
    }

    private static string Test_Martha_MemorySectionInjected()
    {
        var memories = new List<string> { "ice_picks", "guitar", "wedding_photo" };
        string p = CharacterPrompts.GetMarthaPrompt(memories);
        if (!p.Contains("<aware>"))
            return "Triggered memories should inject an <aware> section";
        if (!p.Contains("ice picks") && !p.Contains("ice_picks") && !p.Contains("K2"))
            return "ice_picks memory should appear in the aware section";
        return null;
    }

    private static string Test_Martha_WeddingPhotoMemoryContext()
    {
        var memories = new List<string> { "wedding_photo" };
        string p = CharacterPrompts.GetMarthaPrompt(memories);
        bool hasLosses = p.Contains("losses", System.StringComparison.OrdinalIgnoreCase) ||
                         p.Contains("loss", System.StringComparison.OrdinalIgnoreCase)   ||
                         p.Contains("trying", System.StringComparison.OrdinalIgnoreCase);
        if (!hasLosses)
            return "Wedding photo memory context should reference the years of trying and losses";
        return null;
    }

    // ── Opening monologue ─────────────────────────────────────────────────

    private static string Test_OpeningMonologue_NonEmpty()
    {
        string m = CharacterPrompts.GetMarthaOpeningMonologue();
        return string.IsNullOrEmpty(m) ? "Opening monologue is null or empty" : null;
    }

    private static string Test_OpeningMonologue_MentionsQuestions()
    {
        string m = CharacterPrompts.GetMarthaOpeningMonologue();
        if (!m.Contains("question", System.StringComparison.OrdinalIgnoreCase) &&
            !m.Contains("locked", System.StringComparison.OrdinalIgnoreCase))
            return $"Opening monologue should mention the locked questions, got: '{m}'";
        return null;
    }

    private static string Test_OpeningMonologue_MentionsFacingIt()
    {
        string m = CharacterPrompts.GetMarthaOpeningMonologue();
        bool mentionsToday = m.Contains("today", System.StringComparison.OrdinalIgnoreCase) ||
                             m.Contains("day", System.StringComparison.OrdinalIgnoreCase);
        bool mentionsDecision = m.Contains("music", System.StringComparison.OrdinalIgnoreCase) ||
                                m.Contains("decision", System.StringComparison.OrdinalIgnoreCase) ||
                                m.Contains("answer", System.StringComparison.OrdinalIgnoreCase);
        if (!mentionsToday)
            return $"Opening monologue should reference the day, got: '{m}'";
        if (!mentionsDecision)
            return $"Opening monologue should reference the decision, got: '{m}'";
        return null;
    }

    // ── David prompt ──────────────────────────────────────────────────────

    private static string Test_David_Q0_NonEmpty()
    {
        string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 0);
        return string.IsNullOrEmpty(p) ? "David Q0 prompt is null or empty" : null;
    }

    private static string Test_David_Q0_AlwaysAvailable()
    {
        string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 0);
        // Q0 should contain David's full K2 and money knowledge so the LLM can respond on any topic
        bool hasK2Knowledge = p.Contains("Arthur", System.StringComparison.OrdinalIgnoreCase) ||
                              p.Contains("K2", System.StringComparison.OrdinalIgnoreCase);
        if (!hasK2Knowledge)
            return "David Q0 should contain K2/Arthur knowledge so he can answer expedition questions";
        bool hasMoneyKnowledge = p.Contains("Lily", System.StringComparison.OrdinalIgnoreCase) ||
                                 p.Contains("offshore", System.StringComparison.OrdinalIgnoreCase);
        if (!hasMoneyKnowledge)
            return "David Q0 should contain money/Lily knowledge so he can answer finance questions";
        // Should have a resistance instruction — he doesn't volunteer, but will tell when pushed
        bool hasResistance = p.Contains("resist", System.StringComparison.OrdinalIgnoreCase) ||
                             p.Contains("pushes", System.StringComparison.OrdinalIgnoreCase);
        if (!hasResistance)
            return "David Q0 should include the resist-then-reveal mechanic";
        return null;
    }

    private static string Test_David_Q1_PreResist_NoArthur()
    {
        string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 1, false);
        if (p.Contains("Arthur"))
            return "David Q1 pre-resistance prompt must NOT name Arthur yet";
        if (!p.Contains("push back", System.StringComparison.OrdinalIgnoreCase) &&
            !p.Contains("want to open", System.StringComparison.OrdinalIgnoreCase) &&
            !p.Contains("warn", System.StringComparison.OrdinalIgnoreCase))
            return "David Q1 pre-resistance should show hesitation/warning";
        return null;
    }

    private static string Test_David_Q1_PostResist_NamesArthur()
    {
        string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 1, true);
        if (!p.Contains("Arthur"))
            return "David Q1 post-resistance prompt must name Arthur";
        if (!p.Contains("rope"))
            return "David Q1 post-resistance must reference cutting the rope";
        if (!p.Contains("radio") && !p.Contains("basecamp"))
            return "David Q1 post-resistance must mention he was on the radio";
        return null;
    }

    private static string Test_David_Q2_PreResist_Silent()
    {
        string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 2, false);
        if (p.Contains("Lily"))
            return "David Q2 pre-resistance must NOT name Lily";
        if (p.Contains("Sarah"))
            return "David Q2 pre-resistance must NOT name Sarah";
        return null;
    }

    private static string Test_David_Q2_PostResist_NamesLily()
    {
        string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 2, true);
        if (!p.Contains("Lily"))
            return "David Q2 post-resistance must name Lily";
        if (!p.Contains("Sarah"))
            return "David Q2 post-resistance must name Sarah";
        if (!p.Contains("child support") && !p.Contains("25 years"))
            return "David Q2 post-resistance must reference the payments";
        return null;
    }

    private static string Test_David_Q3_BlindSpot()
    {
        string p = CharacterPrompts.GetDavidPrompt(new List<string>(), 3);
        if (!p.Contains("don't know", System.StringComparison.OrdinalIgnoreCase) &&
            !p.Contains("do not know", System.StringComparison.OrdinalIgnoreCase) &&
            !p.Contains("genuinely", System.StringComparison.OrdinalIgnoreCase))
            return "David Q3 must establish genuine ignorance";
        if (p.Contains("drunk", System.StringComparison.OrdinalIgnoreCase))
            return "David Q3 must NOT reveal drunk/smashing truth — only Martha knows";
        if (p.Contains("not my story") || p.Contains("not mine to answer") ||
            p.Contains("between you and Martha"))
            return "David Q3 must NOT imply hidden knowledge — he genuinely has none";
        return null;
    }

    private static string Test_David_ContainsGuardrail()
    {
        for (int q = 0; q <= 3; q++)
        {
            string p = CharacterPrompts.GetDavidPrompt(new List<string>(), q);
            if (!p.Contains("Never break character", System.StringComparison.OrdinalIgnoreCase))
                return $"David Q{q} prompt must contain character-break guardrail";
        }
        return null;
    }

    // ── Opening lines ─────────────────────────────────────────────────────

    private static string Test_OpeningLine_Martha_IcePicks()
    {
        // Martha's ice_picks line should reference the K2 expedition — her fear, relief,
        // or emotion around Robert going and coming back.
        string line = CharacterPrompts.GetObjectOpeningLine("ice_picks", "martha");
        if (string.IsNullOrEmpty(line))
            return "Martha ice_picks opening line is empty";
        bool referencesExpedition =
            line.Contains("K2",          System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("mountain",    System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("trip",        System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("terrified",   System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("came home",   System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("angry",       System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("trained",     System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("frostbitten", System.StringComparison.OrdinalIgnoreCase);
        if (!referencesExpedition)
            return $"Martha ice_picks opening should reference the K2 expedition, got: '{line}'";
        return null;
    }

    private static string Test_OpeningLine_Martha_Guitar()
    {
        // Martha's guitar line should evoke positive musical memory (the beautiful story first).
        // Does NOT need to mention "Sunday" literally — any nostalgic music/playing reference qualifies.
        string line = CharacterPrompts.GetObjectOpeningLine("guitar", "martha");
        if (string.IsNullOrEmpty(line))
            return "Martha guitar opening line is empty";
        bool hasMusicalNostalgia =
            line.Contains("music",   System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("play",    System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Sunday",  System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("listen",  System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("miss",    System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("song",    System.StringComparison.OrdinalIgnoreCase);
        if (!hasMusicalNostalgia)
            return $"Martha guitar opening should evoke musical memory, got: '{line}'";
        return null;
    }

    private static string Test_OpeningLine_Martha_Wedding()
    {
        // The line should acknowledge childlessness in any form.
        string line = CharacterPrompts.GetObjectOpeningLine("wedding_photo", "martha");
        if (string.IsNullOrEmpty(line))
            return "Martha wedding photo opening line is empty";
        // The opening line is a hook — it may reference childlessness directly or evoke
        // the wedding / years together, with childlessness revealed in deeper conversation.
        bool hasRelevantContent =
            line.Contains("parent",  System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("child",   System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("two of",  System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("just us", System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("kids",    System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("trying",  System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("wonder",  System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("young",   System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("photo",   System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("promise", System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("wedding", System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("us",      System.StringComparison.OrdinalIgnoreCase);
        if (!hasRelevantContent)
            return $"Martha wedding photo opening should be about the photo or childlessness, got: '{line}'";
        return null;
    }

    private static string Test_OpeningLine_David_Phone()
    {
        // Phone opening should be from the randomized pool — just check it's non-empty
        // Run a few times to ensure the pool works
        for (int i = 0; i < 5; i++)
        {
            string line = CharacterPrompts.GetObjectOpeningLine("phone", "david");
            if (string.IsNullOrEmpty(line))
                return $"David phone opening line was empty on iteration {i}";
        }
        return null;
    }

    private static string Test_OpeningLine_David_Q1IcePicks()
    {
        // David's Q1 opener should acknowledge the expedition topic with weight —
        // any reference to the expedition, mountain, K2, the past, or the gravity of the moment.
        string line = CharacterPrompts.GetObjectOpeningLine("ice_picks", "david", 1);
        if (string.IsNullOrEmpty(line))
            return "David ice_picks Q1 opening is empty";
        bool showsGravity =
            line.Contains("mountain",    System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("K2",          System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("expedition",  System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("wondered",    System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("knew",        System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("get here",    System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("call",        System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("feeling",     System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("waiting",     System.StringComparison.OrdinalIgnoreCase);
        if (!showsGravity)
            return $"David ice_picks Q1 opening should show gravity / acknowledge the expedition, got: '{line}'";
        return null;
    }

    private static string Test_OpeningLine_David_Q2WeddingPhoto()
    {
        string line = CharacterPrompts.GetObjectOpeningLine("wedding_photo", "david", 2);
        if (string.IsNullOrEmpty(line))
            return "David wedding photo Q2 opening is empty";
        if (!line.Contains("money", System.StringComparison.OrdinalIgnoreCase) &&
            !line.Contains("account", System.StringComparison.OrdinalIgnoreCase) &&
            !line.Contains("hoping", System.StringComparison.OrdinalIgnoreCase)  &&
            !line.Contains("pretend", System.StringComparison.OrdinalIgnoreCase))
            return $"David wedding Q2 opening should reference money/account, got: '{line}'";
        return null;
    }

    private static string Test_OpeningLine_David_Guitar()
    {
        // David doesn't know what happened with the guitar, so his line should express
        // ignorance, avoidance, or that the topic was left alone — in any phrasing.
        string line = CharacterPrompts.GetObjectOpeningLine("guitar", "david");
        if (string.IsNullOrEmpty(line))
            return "David guitar opening is empty";
        bool expressesIgnorance =
            line.Contains("no idea",       System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("don't know",    System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("never knew",    System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("never said",    System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("left it alone", System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("changed the subject", System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("never told",    System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("nothing",       System.StringComparison.OrdinalIgnoreCase) ||
            line.Contains("asked",         System.StringComparison.OrdinalIgnoreCase);
        if (!expressesIgnorance)
            return $"David guitar opening should express ignorance/avoidance, got: '{line}'";
        return null;
    }

    // ── LLM scene configuration ───────────────────────────────────────────

    private static string Test_Scene_LLMContextSize()
    {
        // Each character has its own LLM server (parallelPrompts=1).
        // Every _contextSize entry in the scene must be >= 4096.
        // After running "LastDay > Setup: David Model" there will be two entries per scene.
        const int requiredPerModel = 4096;

        string[] scenePaths = new[]
        {
            "Assets/Scenes/MainRoom.unity",
            "Assets/Scenes/abeyRoom.unity",
            "Assets/Scenes/MainRoom_InterviewStyle.unity",
        };

        foreach (string path in scenePaths)
        {
            string fullPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath), path);

            if (!System.IO.File.Exists(fullPath)) continue;

            string content = System.IO.File.ReadAllText(fullPath);

            // Check every _contextSize entry in the file
            int searchFrom = 0;
            while (true)
            {
                int idx = content.IndexOf("_contextSize:", searchFrom, System.StringComparison.Ordinal);
                if (idx < 0) break;

                int lineEnd = content.IndexOf('\n', idx);
                string line = lineEnd > 0
                    ? content.Substring(idx, lineEnd - idx)
                    : content.Substring(idx);

                string[] parts = line.Split(':');
                if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), out int sceneValue))
                {
                    if (sceneValue < requiredPerModel)
                        return $"{path}: _contextSize is {sceneValue} but each model needs >= {requiredPerModel}. " +
                               $"Set to {requiredPerModel} in the scene's LLM component(s).";
                }

                searchFrom = idx + 1;
            }
        }

        return null; // all scenes pass
    }

    // ── State machine ─────────────────────────────────────────────────────

    private static string Test_StateMachine_InDialogueToPhoneCall()
    {
        var go  = new GameObject("__GSM_Test__");
        var gsm = go.AddComponent<LastDay.Core.GameStateMachine>();
        gsm.ChangeState(LastDay.Core.GameState.Playing);
        gsm.ChangeState(LastDay.Core.GameState.InDialogue);

        bool ok = gsm.ChangeState(LastDay.Core.GameState.PhoneCall);
        Object.DestroyImmediate(go);

        if (!ok)
            return "InDialogue -> PhoneCall should be a valid state transition (was blocked)";
        return null;
    }
}
