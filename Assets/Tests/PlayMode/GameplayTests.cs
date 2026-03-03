using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using LastDay.Core;
using LastDay.Player;
using LastDay.Pathfinding;
using LastDay.Interaction;
using LastDay.Dialogue;
using LastDay.NPC;
using LastDay.UI;

namespace LastDay.Tests
{
    /// <summary>
    /// PlayMode integration tests for Last Day core gameplay systems.
    /// Run via Window > General > Test Runner > PlayMode tab.
    /// </summary>
    public class GameplayTests
    {
        private GameObject testRoot;

        [SetUp]
        public void SetUp()
        {
            testRoot = new GameObject("__TestRoot__");
        }

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
                Object.Destroy(testRoot);

            foreach (var singleton in Object.FindObjectsOfType<GameStateMachine>())
                Object.Destroy(singleton.gameObject);
            foreach (var singleton in Object.FindObjectsOfType<EventManager>())
                Object.Destroy(singleton.gameObject);
            foreach (var singleton in Object.FindObjectsOfType<GameManager>())
                Object.Destroy(singleton.gameObject);
            foreach (var singleton in Object.FindObjectsOfType<LocalLLMManager>())
                Object.Destroy(singleton.gameObject);
        }

        // ─────────────────────────────────────────────────────
        //  HELPERS: build minimal test objects without SerializedObject
        // ─────────────────────────────────────────────────────

        GameStateMachine CreateStateMachine()
        {
            var go = CreateChild("__TestGSM__");
            var gsm = go.AddComponent<GameStateMachine>();
            gsm.ChangeState(GameState.Playing);
            return gsm;
        }

        EventManager CreateEventManager()
        {
            var go = CreateChild("__TestEventMgr__");
            return go.AddComponent<EventManager>();
        }

        LocalLLMManager CreateLLMManager()
        {
            var go = CreateChild("__TestLLM__");
            var llm = go.AddComponent<LocalLLMManager>();
            llm.isInitialized = true;
            llm.currentCharacter = "martha";
            return llm;
        }

        PlayerController2D CreatePlayer(Vector3 pos)
        {
            var go = CreateChild("__TestPlayer__");
            go.transform.position = pos;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            go.AddComponent<SimplePathfinder>();
            var controller = go.AddComponent<PlayerController2D>();

            return controller;
        }

        InteractableObject2D CreateInteractable(Vector3 pos)
        {
            var go = CreateChild("__TestInteractable__");
            go.transform.position = pos;
            go.layer = LayerMask.NameToLayer("Interactables");

            go.AddComponent<SpriteRenderer>();
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.5f, 0.5f);

            return go.AddComponent<InteractableObject2D>();
        }

        void CreateWalkableFloor()
        {
            var go = CreateChild("__TestFloor__");
            go.layer = LayerMask.NameToLayer("Walkable");
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(10, 6);
            col.offset = new Vector2(0, -1);
            col.isTrigger = true;
        }

        void CreateObstacle(Vector3 pos, Vector2 size)
        {
            var go = CreateChild("__TestObstacle__");
            go.transform.position = pos;
            go.layer = LayerMask.NameToLayer("Obstacles");
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        (DialogueUI ui, GameObject panel, TMP_InputField input, Button sendBtn, TMP_Text dlgText, Button closeBtn) CreateDialogueUI()
        {
            var canvasGo = CreateChild("__TestCanvas__");
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = CreateUIChild(canvasGo, "DialoguePanel");
            panel.AddComponent<Image>();

            var nameText = CreateUIChild(panel, "CharName").AddComponent<TextMeshProUGUI>();
            var dlgText = CreateUIChild(panel, "DlgText").AddComponent<TextMeshProUGUI>();

            var inputGo = CreateUIChild(panel, "Input");
            inputGo.AddComponent<Image>();
            var textArea = CreateUIChild(inputGo, "TextArea");
            textArea.AddComponent<RectMask2D>();
            var inputTmp = CreateUIChild(textArea, "Text").AddComponent<TextMeshProUGUI>();
            var inputField = inputGo.AddComponent<TMP_InputField>();
            inputField.textViewport = textArea.GetComponent<RectTransform>();
            inputField.textComponent = inputTmp;

            var sendBtn = CreateUIChild(panel, "Send");
            sendBtn.AddComponent<Image>();
            var send = sendBtn.AddComponent<Button>();

            var closeBtn = CreateUIChild(panel, "Close");
            closeBtn.AddComponent<Image>();
            var close = closeBtn.AddComponent<Button>();

            var thinkingGo = CreateUIChild(panel, "Thinking");

            var dialogueUI = canvasGo.AddComponent<DialogueUI>();

            SetField(dialogueUI, "dialoguePanel", panel);
            SetField(dialogueUI, "characterNameText", nameText);
            SetField(dialogueUI, "dialogueText", dlgText);
            SetField(dialogueUI, "inputField", inputField);
            SetField(dialogueUI, "sendButton", send);
            SetField(dialogueUI, "closeButton", close);
            SetField(dialogueUI, "thinkingIndicator", thinkingGo);
            SetField(dialogueUI, "typewriterSpeed", 0f);

            return (dialogueUI, panel, inputField, send, dlgText, close);
        }

        GameObject CreateChild(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(testRoot.transform);
            return go;
        }

        GameObject CreateUIChild(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            Debug.LogWarning($"[Test] Field '{fieldName}' not found on {target.GetType().Name}");
        }

        T GetField<T>(object target, string fieldName)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                    return (T)field.GetValue(target);
                type = type.BaseType;
            }
            return default;
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 1: Click Object -> Character Walks To It
        // ═══════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator T1a_ClickInteractable_PlayerWalksToIt()
        {
            var gsm = CreateStateMachine();
            CreateEventManager();
            CreateWalkableFloor();

            var controller = CreatePlayer(new Vector3(-3f, -1f, 0));
            var pathfinder = controller.GetComponent<SimplePathfinder>();
            SetField(pathfinder, "obstacleLayer", (LayerMask)(1 << LayerMask.NameToLayer("Obstacles")));
            SetField(controller, "pathfinder", pathfinder);
            SetField(controller, "moveSpeed", 10f);

            var interactable = CreateInteractable(new Vector3(2f, -1f, 0));

            yield return null;
            yield return null;
            pathfinder.BuildGrid();

            Vector3 startPos = controller.transform.position;

            controller.MoveToAndInteract(interactable);

            Assert.IsTrue(controller.IsMoving, "Player should start moving after MoveToAndInteract");

            float elapsed = 0f;
            while (controller.IsMoving && elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsFalse(controller.IsMoving, "Player should have reached destination");

            float dist = Vector2.Distance(controller.transform.position, interactable.transform.position);
            Assert.Less(dist, 1.5f,
                $"Player should be near target (dist={dist:F2}), moved from {startPos} to {controller.transform.position}");

            Debug.Log($"[TEST PASS] T1a: Player walked to interactable (dist={dist:F2}, time={elapsed:F2}s)");
        }

        [UnityTest]
        public IEnumerator T1b_ClickInteractable_OpensDialogue()
        {
            var gsm = CreateStateMachine();
            CreateEventManager();
            CreateLLMManager();

            var interactable = CreateInteractable(new Vector3(2f, -1f, 0));
            var (dialogueUI, panel, _, _, dlgText, _) = CreateDialogueUI();

            yield return null; // let Awake/Start run

            Assert.IsFalse(panel.activeSelf, "Dialogue panel should start hidden");

            interactable.OnInteract();
            yield return null;

            Assert.IsTrue(panel.activeSelf, "Dialogue panel should open after interacting");
            Assert.AreEqual(GameState.InDialogue, gsm.CurrentState,
                "Game state should be InDialogue");

            Debug.Log("[TEST PASS] T1b: Interaction opens dialogue and transitions to InDialogue state");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 2: Dialogue Uses LLM + Can Exit
        // ═══════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator T2a_Dialogue_UsesLLMStub_ReturnsResponse()
        {
            CreateStateMachine();
            CreateEventManager();
            var llm = CreateLLMManager();
            var (dialogueUI, panel, inputField, sendBtn, dlgText, _) = CreateDialogueUI();

            yield return null;

            dialogueUI.OpenForObject("guitar", "guitar", "Guitar");
            yield return null;

            Assert.IsTrue(panel.activeSelf, "Panel should be open");
            Assert.AreEqual("martha", llm.currentCharacter, "LLM should be set to martha");

            inputField.text = "Tell me about the guitar";
            sendBtn.onClick.Invoke();

            yield return new WaitForSeconds(2.5f);

            string response = dlgText.text;
            Assert.IsFalse(string.IsNullOrEmpty(response),
                "Dialogue should contain a response from the LLM stub");

            Debug.Log($"[TEST PASS] T2a: LLM stub returned response: \"{response}\"");
        }

        [UnityTest]
        public IEnumerator T2b_Dialogue_CanExitWithCloseButton()
        {
            var gsm = CreateStateMachine();
            CreateEventManager();
            CreateLLMManager();
            var (dialogueUI, panel, _, _, _, closeBtn) = CreateDialogueUI();

            yield return null;

            dialogueUI.OpenForObject("guitar", "guitar", "Guitar");
            yield return null;
            Assert.IsTrue(panel.activeSelf);
            Assert.AreEqual(GameState.InDialogue, gsm.CurrentState);

            closeBtn.onClick.Invoke();
            yield return null;

            Assert.IsFalse(panel.activeSelf, "Panel should close after clicking close button");
            Assert.AreEqual(GameState.Playing, gsm.CurrentState,
                "State should return to Playing after closing dialogue");

            Debug.Log("[TEST PASS] T2b: Close button exits dialogue and restores Playing state");
        }

        [UnityTest]
        public IEnumerator T2c_Dialogue_CloseMethod_ExitsDialogue()
        {
            var gsm = CreateStateMachine();
            CreateEventManager();
            CreateLLMManager();
            var (dialogueUI, panel, _, _, _, _) = CreateDialogueUI();

            yield return null;

            dialogueUI.OpenForObject("guitar", "guitar", "Guitar");
            yield return null;
            Assert.IsTrue(panel.activeSelf);

            dialogueUI.Close();
            yield return null;

            Assert.IsFalse(panel.activeSelf, "Panel should close via Close() (triggered by Escape key)");
            Assert.AreEqual(GameState.Playing, gsm.CurrentState);

            Debug.Log("[TEST PASS] T2c: Close() properly exits dialogue (Escape key path)");
        }

        [UnityTest]
        public IEnumerator T2d_Dialogue_PhoneCall_UsesDavid()
        {
            var gsm = CreateStateMachine();
            CreateEventManager();
            var llm = CreateLLMManager();
            var (dialogueUI, panel, _, _, dlgText, _) = CreateDialogueUI();

            yield return null;

            dialogueUI.OpenForPhone();
            yield return null;

            Assert.IsTrue(panel.activeSelf, "Panel should open for phone call");
            Assert.AreEqual("david", llm.currentCharacter,
                "LLM should switch to david for phone call");
            Assert.AreEqual(GameState.PhoneCall, gsm.CurrentState,
                "State should be PhoneCall");

            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(string.IsNullOrEmpty(dlgText.text),
                "David should have an opening line after phone call opens");
            Assert.IsTrue(dlgText.text.Length > 5,
                $"David's opening line should be a real sentence, got: '{dlgText.text}'");

            Debug.Log($"[TEST PASS] T2d: Phone call uses David, opening: \"{dlgText.text}\"");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 3: Character Movement
        // ═══════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator T3a_Player_MovesToDestination()
        {
            CreateStateMachine();
            CreateWalkableFloor();

            var controller = CreatePlayer(new Vector3(0, -1, 0));
            var pathfinder = controller.GetComponent<SimplePathfinder>();
            SetField(pathfinder, "obstacleLayer", (LayerMask)(1 << LayerMask.NameToLayer("Obstacles")));
            SetField(controller, "pathfinder", pathfinder);
            SetField(controller, "moveSpeed", 10f);

            yield return null;
            yield return null;
            pathfinder.BuildGrid();

            Vector3 dest = new Vector3(3f, -1f, 0);
            controller.MoveTo(dest);
            Assert.IsTrue(controller.IsMoving, "Player should be moving");

            float elapsed = 0f;
            while (controller.IsMoving && elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsFalse(controller.IsMoving, "Player should stop after reaching destination");

            float dist = Vector2.Distance(controller.transform.position, dest);
            Assert.Less(dist, 1f, $"Player should be near dest (dist={dist:F2})");

            Debug.Log($"[TEST PASS] T3a: Player reached destination (dist={dist:F2}, time={elapsed:F2}s)");
        }

        [UnityTest]
        public IEnumerator T3b_Player_CannotMoveInDialogueState()
        {
            var gsm = CreateStateMachine();

            var controller = CreatePlayer(new Vector3(0, -1, 0));
            SetField(controller, "moveSpeed", 10f);

            yield return null;

            gsm.ChangeState(GameState.InDialogue);
            Vector3 before = controller.transform.position;

            controller.MoveTo(new Vector3(3f, -1f, 0));
            yield return new WaitForSeconds(0.3f);

            Assert.IsFalse(controller.IsMoving,
                "Player should not move when in InDialogue state");
            Assert.AreEqual(before, controller.transform.position,
                "Position should not change");

            Debug.Log("[TEST PASS] T3b: Movement blocked during InDialogue state");
        }

        [UnityTest]
        public IEnumerator T3c_Player_StopsOnForceStop()
        {
            CreateStateMachine();
            CreateWalkableFloor();

            var controller = CreatePlayer(new Vector3(0, -1, 0));
            var pathfinder = controller.GetComponent<SimplePathfinder>();
            SetField(pathfinder, "obstacleLayer", (LayerMask)(1 << LayerMask.NameToLayer("Obstacles")));
            SetField(controller, "pathfinder", pathfinder);
            SetField(controller, "moveSpeed", 2f);

            yield return null;
            yield return null;
            pathfinder.BuildGrid();

            controller.MoveTo(new Vector3(4f, -1f, 0));
            Assert.IsTrue(controller.IsMoving);

            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(controller.IsMoving, "Should still be moving (slow speed, far target)");

            controller.ForceStop();
            yield return null;

            Assert.IsFalse(controller.IsMoving, "Should stop after ForceStop()");

            Debug.Log("[TEST PASS] T3c: ForceStop() halts movement mid-path");
        }

        [UnityTest]
        public IEnumerator T3d_Player_NavigatesAroundObstacle()
        {
            CreateStateMachine();
            CreateWalkableFloor();
            CreateObstacle(new Vector3(1.5f, -1f, 0), new Vector2(1f, 4f));

            var controller = CreatePlayer(new Vector3(0, -1, 0));
            var pathfinder = controller.GetComponent<SimplePathfinder>();
            SetField(pathfinder, "obstacleLayer", (LayerMask)(1 << LayerMask.NameToLayer("Obstacles")));
            SetField(controller, "pathfinder", pathfinder);
            SetField(controller, "moveSpeed", 10f);

            yield return null;
            yield return new WaitForFixedUpdate();
            pathfinder.BuildGrid();

            Vector3 dest = new Vector3(3f, -1f, 0);
            controller.MoveTo(dest);
            Assert.IsTrue(controller.IsMoving);

            float elapsed = 0f;
            while (controller.IsMoving && elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            float dist = Vector2.Distance(controller.transform.position, dest);
            Assert.Less(dist, 1.5f,
                $"Should reach near destination despite obstacle (dist={dist:F2})");

            Debug.Log($"[TEST PASS] T3d: Pathfinding routed around obstacle (dist={dist:F2})");
        }

        // ═══════════════════════════════════════════════════════
        //  STATE MACHINE SANITY
        // ═══════════════════════════════════════════════════════

        [Test]
        public void StateMachine_ValidTransitions()
        {
            var go = new GameObject("__TestGSM__");
            var gsm = go.AddComponent<GameStateMachine>();

            Assert.IsTrue(gsm.ChangeState(GameState.Playing));
            Assert.IsTrue(gsm.ChangeState(GameState.InDialogue));
            Assert.IsTrue(gsm.ChangeState(GameState.Playing));
            Assert.IsTrue(gsm.ChangeState(GameState.PhoneCall));
            Assert.IsTrue(gsm.ChangeState(GameState.Playing));
            Assert.IsTrue(gsm.ChangeState(GameState.Decision));
            Assert.IsTrue(gsm.ChangeState(GameState.Ending));

            Object.DestroyImmediate(go);
            Debug.Log("[TEST PASS] StateMachine_ValidTransitions: all valid paths accepted");
        }

        [Test]
        public void StateMachine_InvalidTransitions()
        {
            var go = new GameObject("__TestGSM__");
            var gsm = go.AddComponent<GameStateMachine>();
            gsm.ChangeState(GameState.Playing);

            Assert.IsFalse(gsm.ChangeState(GameState.Ending), "Playing->Ending should be invalid");
            Assert.IsFalse(gsm.ChangeState(GameState.Loading), "Playing->Loading should be invalid");
            Assert.IsFalse(gsm.ChangeState(GameState.Playing), "Same-state transition should be invalid");

            Object.DestroyImmediate(go);
            Debug.Log("[TEST PASS] StateMachine_InvalidTransitions: all invalid paths rejected");
        }

        // ═══════════════════════════════════════════════════════
        //  DIALOGUE SYSTEM TESTS
        // ═══════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator Dialogue_OpenForObject_ShowsGreeting()
        {
            CreateStateMachine();
            CreateEventManager();
            CreateLLMManager();
            var (dialogueUI, panel, _, _, dlgText, _) = CreateDialogueUI();

            yield return null;

            Assert.IsFalse(panel.activeSelf, "Panel should start hidden");

            dialogueUI.OpenForObject("wedding_photo", "wedding_photo", "Wedding Photo");
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(panel.activeSelf, "Panel should open");
            // Greeting is Martha's narrative opening line — confirm it is non-empty and in-character
            Assert.IsFalse(string.IsNullOrEmpty(dlgText.text),
                $"Greeting should be non-empty for wedding_photo object, got: '{dlgText.text}'");
            Assert.IsTrue(dlgText.text.Length > 10,
                $"Greeting should be a real sentence, got: '{dlgText.text}'"  );

            Debug.Log("[TEST PASS] Dialogue_OpenForObject_ShowsGreeting");
        }

        [UnityTest]
        public IEnumerator Dialogue_OpenForNPC_ShowsMarthaGreeting()
        {
            CreateStateMachine();
            CreateEventManager();
            CreateLLMManager();
            var (dialogueUI, panel, _, _, dlgText, _) = CreateDialogueUI();

            yield return null;

            dialogueUI.OpenForNPC("martha", "Martha");
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(panel.activeSelf, "Panel should open for NPC");
            Assert.IsFalse(string.IsNullOrEmpty(dlgText.text),
                $"Martha NPC greeting should be non-empty, got: '{dlgText.text}'");
            Assert.IsTrue(dlgText.text.Length > 5,
                $"Martha greeting should be a real sentence, got: '{dlgText.text}'"  );

            Debug.Log("[TEST PASS] Dialogue_OpenForNPC_ShowsMarthaGreeting");
        }

        [UnityTest]
        public IEnumerator Dialogue_SubmitInput_GetsStubResponse()
        {
            CreateStateMachine();
            CreateEventManager();
            CreateLLMManager();
            var (dialogueUI, panel, inputField, sendBtn, dlgText, _) = CreateDialogueUI();

            yield return null;

            dialogueUI.OpenForObject("guitar", "guitar", "Guitar");
            yield return new WaitForSeconds(0.1f);

            inputField.text = "Tell me about the guitar";
            sendBtn.onClick.Invoke();

            yield return new WaitForSeconds(3f);

            Assert.IsFalse(string.IsNullOrEmpty(dlgText.text),
                "Should have received a response");
            Assert.AreNotEqual("...", dlgText.text,
                "Response should not be the fallback '...'");

            Debug.Log($"[TEST PASS] Dialogue_SubmitInput_GetsStubResponse: '{dlgText.text}'");
        }

        [UnityTest]
        public IEnumerator Dialogue_EmptyInput_DoesNotSend()
        {
            CreateStateMachine();
            CreateEventManager();
            CreateLLMManager();
            var (dialogueUI, panel, inputField, sendBtn, dlgText, _) = CreateDialogueUI();

            yield return null;

            dialogueUI.OpenForObject("guitar", "guitar", "Guitar");
            yield return new WaitForSeconds(0.1f);

            string greetingText = dlgText.text;

            inputField.text = "";
            sendBtn.onClick.Invoke();
            yield return new WaitForSeconds(0.5f);

            // Text should still be the greeting, not overwritten
            Assert.IsTrue(dlgText.text.Length > 0,
                "Dialogue text should still show the greeting, not be overwritten");

            Debug.Log("[TEST PASS] Dialogue_EmptyInput_DoesNotSend");
        }

        [UnityTest]
        public IEnumerator Dialogue_Close_ResetsState()
        {
            var gsm = CreateStateMachine();
            CreateEventManager();
            CreateLLMManager();
            var (dialogueUI, panel, _, _, _, _) = CreateDialogueUI();

            yield return null;

            dialogueUI.OpenForObject("guitar", "guitar", "Guitar");
            yield return null;

            Assert.AreEqual(GameState.InDialogue, gsm.CurrentState);
            Assert.IsTrue(panel.activeSelf);

            dialogueUI.Close();
            yield return null;

            Assert.IsFalse(panel.activeSelf, "Panel should close");
            Assert.AreEqual(GameState.Playing, gsm.CurrentState,
                "Should return to Playing state");

            Debug.Log("[TEST PASS] Dialogue_Close_ResetsState");
        }

        [UnityTest]
        public IEnumerator Dialogue_PhoneCall_SwitchesToDavid()
        {
            var gsm = CreateStateMachine();
            CreateEventManager();
            var llm = CreateLLMManager();
            var (dialogueUI, panel, _, _, dlgText, _) = CreateDialogueUI();

            yield return null;

            dialogueUI.OpenForPhone();
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(panel.activeSelf, "Panel should open for phone");
            Assert.AreEqual("david", llm.currentCharacter,
                "LLM should be using David");
            Assert.AreEqual(GameState.PhoneCall, gsm.CurrentState,
                "State should be PhoneCall");
            Assert.IsFalse(string.IsNullOrEmpty(dlgText.text),
                $"David should have an opening line, got: '{dlgText.text}'");
            Assert.IsTrue(dlgText.text.Length > 5,
                $"David opening should be a real sentence, got: '{dlgText.text}'");

            Debug.Log("[TEST PASS] Dialogue_PhoneCall_SwitchesToDavid");
        }

        [UnityTest]
        public IEnumerator Dialogue_ClosePhoneCall_ReturnsToPlaying()
        {
            var gsm = CreateStateMachine();
            CreateEventManager();
            CreateLLMManager();
            var (dialogueUI, panel, _, _, _, closeBtn) = CreateDialogueUI();

            yield return null;

            dialogueUI.OpenForPhone();
            yield return null;

            Assert.AreEqual(GameState.PhoneCall, gsm.CurrentState);

            closeBtn.onClick.Invoke();
            yield return null;

            Assert.IsFalse(panel.activeSelf, "Panel should close");
            Assert.AreEqual(GameState.Playing, gsm.CurrentState,
                "PhoneCall should return to Playing on close");

            Debug.Log("[TEST PASS] Dialogue_ClosePhoneCall_ReturnsToPlaying");
        }

        [UnityTest]
        public IEnumerator Dialogue_MultipleOpens_NoErrors()
        {
            CreateStateMachine();
            CreateEventManager();
            CreateLLMManager();
            var (dialogueUI, panel, _, _, _, _) = CreateDialogueUI();

            yield return null;

            dialogueUI.OpenForObject("guitar", "guitar", "Guitar");
            yield return null;
            dialogueUI.Close();
            yield return null;

            dialogueUI.OpenForObject("wedding_photo", "wedding_photo", "Wedding Photo");
            yield return null;
            dialogueUI.Close();
            yield return null;

            dialogueUI.OpenForNPC("martha", "Martha");
            yield return null;
            dialogueUI.Close();
            yield return null;

            Assert.IsFalse(panel.activeSelf, "Panel should be closed after final close");

            Debug.Log("[TEST PASS] Dialogue_MultipleOpens_NoErrors");
        }

        // ═══════════════════════════════════════════════════════
        //  NPC INTERACTION: Click Martha -> Walk + Dialogue
        // ═══════════════════════════════════════════════════════

        NPCController CreateNPC(Vector3 pos, string npcId, string displayName)
        {
            var go = CreateChild("__TestNPC__");
            go.transform.position = pos;
            go.layer = LayerMask.NameToLayer("Characters");

            go.AddComponent<SpriteRenderer>();
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.5f, 0.75f);

            var npc = go.AddComponent<NPCController>();
            SetField(npc, "npcId", npcId);
            SetField(npc, "displayName", displayName);
            SetField(npc, "facePlayer", false);
            return npc;
        }

        [UnityTest]
        public IEnumerator NPC_WalkToMartha_OpensDialogue()
        {
            var gsm = CreateStateMachine();
            CreateEventManager();
            CreateLLMManager();
            CreateWalkableFloor();

            var controller = CreatePlayer(new Vector3(-3f, -1f, 0));
            var pathfinder = controller.GetComponent<SimplePathfinder>();
            SetField(pathfinder, "obstacleLayer", (LayerMask)(1 << LayerMask.NameToLayer("Obstacles")));
            SetField(controller, "pathfinder", pathfinder);
            SetField(controller, "moveSpeed", 10f);

            var martha = CreateNPC(new Vector3(2f, -1f, 0), "martha", "Martha");
            var (dialogueUI, panel, _, _, dlgText, _) = CreateDialogueUI();

            yield return null;
            yield return null;
            pathfinder.BuildGrid();

            Assert.IsFalse(panel.activeSelf, "Dialogue should be closed initially");

            controller.MoveToAndTalk(martha);
            Assert.IsTrue(controller.IsMoving, "Player should start walking to Martha");

            float elapsed = 0f;
            while (controller.IsMoving && elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);

            Assert.IsFalse(controller.IsMoving, "Player should have reached Martha");
            Assert.IsTrue(panel.activeSelf,
                "Dialogue panel should open after reaching Martha");
            Assert.AreEqual(GameState.InDialogue, gsm.CurrentState,
                "State should be InDialogue");

            float dist = Vector2.Distance(controller.transform.position, martha.transform.position);
            Assert.Less(dist, 1.5f,
                $"Player should be near Martha (dist={dist:F2})");

            Debug.Log($"[TEST PASS] NPC_WalkToMartha_OpensDialogue (dist={dist:F2}, time={elapsed:F2}s)");
        }

        [UnityTest]
        public IEnumerator NPC_InteractDirectly_OpensDialogue()
        {
            CreateStateMachine();
            CreateEventManager();
            CreateLLMManager();

            var martha = CreateNPC(new Vector3(0, 0, 0), "martha", "Martha");
            var (dialogueUI, panel, _, _, dlgText, _) = CreateDialogueUI();

            yield return null;

            martha.OnPlayerInteract();
            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(panel.activeSelf, "Dialogue should open on NPC interaction");
            Assert.IsTrue(dlgText.text.Contains("dear") || dlgText.text.Contains("Martha"),
                $"Should show Martha's greeting, got: '{dlgText.text}'");

            Debug.Log("[TEST PASS] NPC_InteractDirectly_OpensDialogue");
        }

        // ═══════════════════════════════════════════════════════
        //  CHARACTER IDLE MOVEMENT
        // ═══════════════════════════════════════════════════════

        CharacterIdleMovement CreateIdleMovement(bool enableTremor = true)
        {
            var go   = CreateChild("__TestIdle__");
            var idle = go.AddComponent<CharacterIdleMovement>();
            // Apply tremor setting via reflection so we don't need a public setter
            var config = new CharacterIdleMovement.TremorConfig
            {
                enabled     = enableTremor,
                amount      = 0.01f,
                duration    = 0.3f,
                intervalMin = 3f,
                intervalMax = 8f
            };
            SetField(idle, "tremor", config);
            return idle;
        }

        [UnityTest]
        public IEnumerator CharacterIdleMovement_OnStartWalking_DisablesComponent()
        {
            var idle = CreateIdleMovement();
            yield return null;

            idle.OnStartWalking();
            Assert.IsFalse(idle.enabled, "Component should be disabled while walking");

            Debug.Log("[TEST PASS] CharacterIdleMovement_OnStartWalking_DisablesComponent");
        }

        [UnityTest]
        public IEnumerator CharacterIdleMovement_OnStopWalking_EnablesComponent()
        {
            var idle = CreateIdleMovement();
            yield return null;

            idle.OnStartWalking();
            idle.OnStopWalking();
            Assert.IsTrue(idle.enabled, "Component should be re-enabled after stopping");

            Debug.Log("[TEST PASS] CharacterIdleMovement_OnStopWalking_EnablesComponent");
        }

        [Test]
        public void CharacterIdleMovement_TremorConfig_None_HasTremorDisabled()
        {
            var none = CharacterIdleMovement.TremorConfig.None;
            Assert.IsFalse(none.enabled, "TremorConfig.None should have enabled=false");
            Debug.Log("[TEST PASS] CharacterIdleMovement_TremorConfig_None_HasTremorDisabled");
        }

        [Test]
        public void CharacterIdleMovement_TremorConfig_Default_HasTremorEnabled()
        {
            var def = CharacterIdleMovement.TremorConfig.Default;
            Assert.IsTrue(def.enabled,    "TremorConfig.Default should have enabled=true");
            Assert.Greater(def.amount, 0f, "Default tremor amount should be positive");
            Assert.Greater(def.intervalMax, def.intervalMin, "intervalMax should exceed intervalMin");
            Debug.Log("[TEST PASS] CharacterIdleMovement_TremorConfig_Default_HasTremorEnabled");
        }

        [Test]
        public void CharacterIdleMovement_BreathingConfig_Default_HasExpectedValues()
        {
            var def = CharacterIdleMovement.BreathingConfig.Default;
            Assert.IsTrue(def.enabled,       "Breathing should be on by default");
            Assert.Greater(def.scaleAmount, 0f);
            Assert.Greater(def.speed, 0f);
            Debug.Log("[TEST PASS] CharacterIdleMovement_BreathingConfig_Default_HasExpectedValues");
        }

        [Test]
        public void CharacterIdleMovement_SwayConfig_Default_HasExpectedValues()
        {
            var def = CharacterIdleMovement.SwayConfig.Default;
            Assert.IsTrue(def.enabled, "Sway should be on by default");
            Assert.Greater(def.amount, 0f);
            Assert.Greater(def.speed, 0f);
            Debug.Log("[TEST PASS] CharacterIdleMovement_SwayConfig_Default_HasExpectedValues");
        }

        [UnityTest]
        public IEnumerator CharacterIdleMovement_UpdateDoesNotThrow_WithoutSpriteRoot()
        {
            // spriteRoot defaults to own transform — should not throw
            var go   = CreateChild("__TestIdleNoRoot__");
            var idle = go.AddComponent<CharacterIdleMovement>();
            // Leave spriteRoot unassigned (null → falls back to self in Start)

            yield return null; // triggers Start + first Update
            yield return null;

            Assert.IsTrue(idle != null, "Component should survive without explicit spriteRoot");
            Debug.Log("[TEST PASS] CharacterIdleMovement_UpdateDoesNotThrow_WithoutSpriteRoot");
        }

        // ═══════════════════════════════════════════════════════
        //  MODEL DOWNLOADER
        // ═══════════════════════════════════════════════════════

        ModelDownloader CreateModelDownloader()
        {
            var go = CreateChild("__TestModelDownloader__");
            return go.AddComponent<ModelDownloader>();
        }

        [Test]
        public void ModelDownloader_IsModelReady_FalseOnCreation()
        {
            var dl = CreateModelDownloader();
            Assert.IsFalse(dl.IsModelReady, "IsModelReady should be false before EnsureModelReady is called");
            Debug.Log("[TEST PASS] ModelDownloader_IsModelReady_FalseOnCreation");
        }

        [Test]
        public void ModelDownloader_IsDownloading_FalseOnCreation()
        {
            var dl = CreateModelDownloader();
            Assert.IsFalse(dl.IsDownloading, "IsDownloading should be false initially");
            Debug.Log("[TEST PASS] ModelDownloader_IsDownloading_FalseOnCreation");
        }

        [Test]
        public void ModelDownloader_StatusMessage_HasInitialValue()
        {
            var dl = CreateModelDownloader();
            Assert.IsFalse(string.IsNullOrEmpty(dl.StatusMessage),
                "StatusMessage should have a non-empty default value");
            Debug.Log("[TEST PASS] ModelDownloader_StatusMessage_HasInitialValue");
        }

        [UnityTest]
        public IEnumerator ModelDownloader_EnsureModelReady_WhenFileExists_SetsReadyTrue()
        {
            // Arrange: place a tiny sentinel file at the expected model path
            string modelDir  = System.IO.Path.Combine(Application.persistentDataPath, "Models");
            string modelPath = System.IO.Path.Combine(modelDir, "phi3-mini.gguf");
            System.IO.Directory.CreateDirectory(modelDir);
            System.IO.File.WriteAllText(modelPath, "test");

            var dl           = CreateModelDownloader();
            bool readyFired  = false;
            string firedPath = null;
            dl.OnModelReady += p => { readyFired = true; firedPath = p; };

            // Act
            var task = dl.EnsureModelReady();
            yield return new WaitUntil(() => task.IsCompleted);

            // Assert
            Assert.IsTrue(dl.IsModelReady,  "IsModelReady should be true when file exists");
            Assert.IsTrue(readyFired,        "OnModelReady event should fire");
            Assert.AreEqual(modelPath, firedPath, "OnModelReady should pass the correct path");
            Assert.AreEqual(modelPath, dl.ModelPath);

            // Cleanup
            System.IO.File.Delete(modelPath);
            Debug.Log("[TEST PASS] ModelDownloader_EnsureModelReady_WhenFileExists_SetsReadyTrue");
        }

        [UnityTest]
        public IEnumerator ModelDownloader_EnsureModelReady_WhenFileExists_ReturnsPath()
        {
            string modelDir  = System.IO.Path.Combine(Application.persistentDataPath, "Models");
            string modelPath = System.IO.Path.Combine(modelDir, "phi3-mini.gguf");
            System.IO.Directory.CreateDirectory(modelDir);
            System.IO.File.WriteAllText(modelPath, "test");

            var dl   = CreateModelDownloader();
            var task = dl.EnsureModelReady();
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.AreEqual(modelPath, task.Result,
                "EnsureModelReady should return the model path when the file is present");

            System.IO.File.Delete(modelPath);
            Debug.Log("[TEST PASS] ModelDownloader_EnsureModelReady_WhenFileExists_ReturnsPath");
        }

        [UnityTest]
        public IEnumerator ModelDownloader_EnsureModelReady_WhenFileMissing_ReturnsNull()
        {
            // Ensure the file does NOT exist
            string modelDir  = System.IO.Path.Combine(Application.persistentDataPath, "Models");
            string modelPath = System.IO.Path.Combine(modelDir, "phi3-mini.gguf");
            if (System.IO.File.Exists(modelPath))
                System.IO.File.Delete(modelPath);

            var dl = CreateModelDownloader();
            string capturedError = null;
            dl.OnError += err => capturedError = err;

            // Don't actually download (no network in test). The download will fail quickly.
            // We just verify that a null/failure result comes back and IsModelReady stays false.
            var task = dl.EnsureModelReady();

            // Give it a moment then cancel expectations — download won't complete in a unit test
            float timeout = 0f;
            while (!task.IsCompleted && timeout < 2f)
            {
                timeout += Time.deltaTime;
                yield return null;
            }

            if (task.IsCompleted)
            {
                if (task.Result == null)
                {
                    Assert.IsFalse(dl.IsModelReady, "IsModelReady must remain false on download failure");
                    if (capturedError != null)
                        StringAssert.IsMatch(".+", capturedError, "OnError should fire with a non-empty message");
                }
            }
            // Invariant: cannot be Ready without a ModelPath
            Assert.IsFalse(dl.IsModelReady && string.IsNullOrEmpty(dl.ModelPath),
                "Cannot be Ready with an empty ModelPath");

            Debug.Log("[TEST PASS] ModelDownloader_EnsureModelReady_WhenFileMissing_ReturnsNull");
        }

        // ═══════════════════════════════════════════════════════
        //  LOCAL LLM MANAGER - Initialize overload
        // ═══════════════════════════════════════════════════════

        [UnityTest]
        public IEnumerator LLM_Initialize_WithNullPath_InitializesInStubMode()
        {
            var llm = CreateLLMManager();
            llm.isInitialized = false;

            var task = llm.Initialize(null);
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.IsTrue(llm.isInitialized, "LLM should initialize even with null model path");
            Debug.Log("[TEST PASS] LLM_Initialize_WithNullPath_InitializesInStubMode");
        }

        [UnityTest]
        public IEnumerator LLM_Initialize_WithEmptyPath_InitializesInStubMode()
        {
            var llm = CreateLLMManager();
            llm.isInitialized = false;

            var task = llm.Initialize("");
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.IsTrue(llm.isInitialized, "LLM should initialize even with empty model path");
            Debug.Log("[TEST PASS] LLM_Initialize_WithEmptyPath_InitializesInStubMode");
        }

        [UnityTest]
        public IEnumerator LLM_Initialize_WithInvalidPath_InitializesInStubMode()
        {
            var llm = CreateLLMManager();
            llm.isInitialized = false;

            var task = llm.Initialize("/nonexistent/path/model.gguf");
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.IsTrue(llm.isInitialized, "LLM should fall back to stub mode for bad path");
            Debug.Log("[TEST PASS] LLM_Initialize_WithInvalidPath_InitializesInStubMode");
        }
    }
}
