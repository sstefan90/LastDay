# Last Day - Debugging & AI-Assisted Development Guide

## Table of Contents
1. [Foreseeable Problems & Solutions](#1-foreseeable-problems--solutions)
2. [Enhanced State Export System](#2-enhanced-state-export-system)
3. [Cursor Integration Strategies](#3-cursor-integration-strategies)
4. [Debug Console System](#4-debug-console-system)
5. [Common Bug Patterns & Fixes](#5-common-bug-patterns--fixes)

---

## 1. Foreseeable Problems & Solutions

### 1.1 LLM Integration Issues

```
PROBLEM: Model download fails or corrupts
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: HIGH (network issues, large file)

Symptoms:
- Download hangs at certain percentage
- Game crashes on LLM initialization
- "Model file corrupted" errors

Solutions:
1. Implement resume capability (track bytes downloaded)
2. Verify file hash after download (MD5/SHA256)
3. Store download progress in PlayerPrefs
4. Provide "re-download" button in settings

Code:
```csharp
// Add to ModelDownloader.cs
private async Task<bool> VerifyModelIntegrity(string path)
{
    string expectedHash = "abc123..."; // Get from HuggingFace
    using (var md5 = System.Security.Cryptography.MD5.Create())
    using (var stream = File.OpenRead(path))
    {
        byte[] hash = md5.ComputeHash(stream);
        string actualHash = BitConverter.ToString(hash).Replace("-", "");
        return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}
```

───────────────────────────────────────────────────────────────────────────────

PROBLEM: LLM responses are too slow
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: MEDIUM (depends on hardware)

Symptoms:
- 5+ second wait times
- UI feels frozen
- Players spam the send button

Solutions:
1. Show engaging "thinking" animation
2. Stream tokens as they generate (if LLMUnity supports)
3. Have fallback to smaller model (Qwen-0.5B)
4. Cache common responses
5. Pre-generate first response while player reads intro

Code:
```csharp
// Streaming tokens (if supported)
llm.OnToken += (token) => {
    dialogueText.text += token;
};
```

───────────────────────────────────────────────────────────────────────────────

PROBLEM: LLM breaks character or gives inappropriate responses
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: MEDIUM-HIGH (small models are less reliable)

Symptoms:
- Martha suddenly talks about unrelated topics
- Character gives medical advice
- Response is gibberish or cut off
- Model reveals it's an AI

Solutions:
1. Post-process responses (check length, filter keywords)
2. Have fallback responses for detected failures
3. Limit input length to reduce confusion
4. Add "retry" button for player
5. Log all responses for debugging

Code:
```csharp
// Response validation
private string ValidateResponse(string response)
{
    // Too short
    if (response.Length < 10)
        return GetFallbackResponse();
    
    // Too long (model rambling)
    if (response.Length > 500)
        response = response.Substring(0, response.LastIndexOf('.', 500)) + ".";
    
    // Contains forbidden patterns
    string[] forbidden = { "I'm an AI", "language model", "I cannot", "As an AI" };
    foreach (var pattern in forbidden)
    {
        if (response.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            return GetFallbackResponse();
    }
    
    return response;
}
```

───────────────────────────────────────────────────────────────────────────────

PROBLEM: LLMUnity package has breaking changes or bugs
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: MEDIUM (third-party package)

Solutions:
1. Pin to specific version in manifest.json
2. Have backup plan: Ollama + HTTP requests
3. Keep local copy of working package version
4. Test on fresh project before integrating

```json
// Packages/manifest.json
{
  "dependencies": {
    "com.undream.llmunity": "https://github.com/undreamai/LLMUnity.git#v1.2.3"
  }
}
```
```

### 1.2 Pathfinding Issues

```
PROBLEM: Player gets stuck or takes weird paths
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: HIGH (common in grid pathfinding)

Symptoms:
- Player walks into walls
- Path goes around obstacles unnecessarily
- Player stops mid-path
- Click on object but player walks elsewhere

Solutions:
1. Rebuild grid at runtime when needed
2. Add visual debug mode (show grid in game)
3. Use larger cell size (0.5 instead of 0.25)
4. Add "stuck detection" and auto-unstick

Code:
```csharp
// Stuck detection in PlayerController2D
private float stuckCheckInterval = 0.5f;
private Vector2 lastPosition;
private float stuckTime = 0f;

void CheckIfStuck()
{
    if (isMoving && Vector2.Distance(transform.position, lastPosition) < 0.01f)
    {
        stuckTime += stuckCheckInterval;
        if (stuckTime > 2f)
        {
            Debug.LogWarning("[Player] Stuck detected, forcing stop");
            ForceStop();
            stuckTime = 0f;
        }
    }
    else
    {
        stuckTime = 0f;
    }
    lastPosition = transform.position;
}
```

───────────────────────────────────────────────────────────────────────────────

PROBLEM: Pathfinding grid doesn't match visual obstacles
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: HIGH (common setup issue)

Symptoms:
- Player walks through furniture
- Player can't reach certain areas
- Grid shows wrong cells as blocked

Solutions:
1. Ensure obstacle colliders are on correct layer
2. Rebuild grid after scene loads
3. Make grid cell size match sprite sizes
4. Add runtime grid visualization toggle

Code:
```csharp
// Add to SimplePathfinder
[ContextMenu("Rebuild Grid Now")]
public void RebuildGridNow()
{
    BuildGrid();
    Debug.Log($"Grid rebuilt: {gridWidth}x{gridHeight}");
}

// Call after scene loads
void Start()
{
    // Wait one frame for colliders to initialize
    StartCoroutine(DelayedBuildGrid());
}

IEnumerator DelayedBuildGrid()
{
    yield return null;
    BuildGrid();
}
```
```

### 1.3 Animation Issues

```
PROBLEM: Character slides or animation doesn't match movement
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: HIGH (timing issues)

Symptoms:
- Feet slide on ground (moonwalking)
- Walk animation plays but character stationary
- Wrong direction animation plays
- Snapping between directions

Solutions:
1. Tune animation speed to match moveSpeed
2. Use root motion or sync step timing
3. Add animation events for footsteps
4. Smooth direction changes

Code:
```csharp
// Match animation to movement speed
// Walk cycle = 4 frames, each frame = 0.15 sec = 0.6 sec per cycle
// At moveSpeed = 2, that's 1.2 units per cycle
// Adjust moveSpeed or animation speed to match

// In CharacterAnimator
public void SetMovementSpeed(float speed)
{
    // Normalize animation speed to movement
    float baseSpeed = 2f;
    animator.speed = speed / baseSpeed;
}
```

───────────────────────────────────────────────────────────────────────────────

PROBLEM: Sprite sheet import is wrong
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: HIGH (easy to misconfigure)

Symptoms:
- Sprites are blurry
- Wrong frames in animation
- Sprites have artifacts/seams
- Colors look different

Solutions:
1. ALWAYS set Filter Mode: Point (no filter)
2. ALWAYS set Compression: None
3. Verify cell size matches actual sprite size
4. Check Pixels Per Unit matches your scale
5. Ensure source images have no anti-aliasing

Checklist:
☐ Texture Type: Sprite (2D and UI)
☐ Sprite Mode: Multiple
☐ Pixels Per Unit: 32 (or your chosen PPU)
☐ Filter Mode: Point (no filter) ← MOST COMMON MISTAKE
☐ Compression: None
☐ Max Size: 2048 (or larger if needed)
```

### 1.4 UI Issues

```
PROBLEM: Input field doesn't maintain focus
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: HIGH (very common)

Symptoms:
- Have to click input field every time
- Enter key doesn't work
- Input disappears

Solutions:
1. Re-select input field after each action
2. Don't deactivate/reactivate input field
3. Use inputField.ActivateInputField() not Select()

Code:
```csharp
// After sending message
inputField.text = "";
inputField.ActivateInputField();  // Better than Select()
inputField.Select();

// If still losing focus, force it
IEnumerator RefocusInput()
{
    yield return null;  // Wait one frame
    inputField.ActivateInputField();
}
```

───────────────────────────────────────────────────────────────────────────────

PROBLEM: UI doesn't scale correctly on different resolutions
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: MEDIUM

Symptoms:
- UI too big or small on different monitors
- Elements overlap
- Text gets cut off
- Buttons unclickable (wrong position)

Solutions:
1. Use Canvas Scaler correctly
2. Anchor UI elements properly
3. Test at multiple resolutions
4. Use layout groups for complex UIs

Settings:
```
Canvas Scaler:
- UI Scale Mode: Scale With Screen Size
- Reference Resolution: 480 x 270 (match game)
- Screen Match Mode: Match Width Or Height
- Match: 0.5

Anchoring:
- Dialogue panel: Bottom stretch (anchor bottom, stretch horizontal)
- Portrait: Top-left of dialogue panel
- Input field: Bottom of dialogue panel, stretch horizontal
```

───────────────────────────────────────────────────────────────────────────────

PROBLEM: Typewriter effect causes lag or visual glitches
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: MEDIUM

Symptoms:
- Stuttering during text reveal
- Characters appear in wrong order
- Rich text tags shown as text

Solutions:
1. Use TMP's built-in reveal (maxVisibleCharacters)
2. Pre-parse rich text tags
3. Don't use string concatenation in loop

Code:
```csharp
// Better typewriter using TMP's built-in feature
IEnumerator TypewriterTMP(string text)
{
    dialogueText.text = text;
    dialogueText.maxVisibleCharacters = 0;
    
    int totalChars = text.Length;
    for (int i = 0; i <= totalChars; i++)
    {
        dialogueText.maxVisibleCharacters = i;
        yield return new WaitForSeconds(typewriterSpeed);
    }
}
```
```

### 1.5 Audio Issues

```
PROBLEM: Audio doesn't loop seamlessly
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: HIGH (music loops are hard)

Symptoms:
- Audible pop/click at loop point
- Gap of silence between loops
- Music restarts from beginning audibly

Solutions:
1. Use proper loop points in audio file (Audacity)
2. Crossfade end into beginning
3. Use AudioSource's loop (not manual replay)
4. Ensure audio is uncompressed or high-quality Vorbis

Audacity workflow:
1. Open ambient_loop.wav
2. Find natural phrase ending (~1:30-2:00)
3. Select from there to end, delete
4. Effect > Crossfade Tracks (if needed)
5. Test: Effect > Repeat, listen for seam
6. Export as WAV

───────────────────────────────────────────────────────────────────────────────

PROBLEM: Multiple audio sources conflict
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: MEDIUM

Symptoms:
- Sounds cut each other off
- Wrong volume levels
- Spatial audio when you don't want it

Solutions:
1. Separate AudioSources for music, SFX, dialogue
2. Use AudioSource.PlayOneShot for short SFX
3. Set spatialBlend = 0 for 2D sounds
4. Create AudioMixer for volume control
```

### 1.6 State Management Issues

```
PROBLEM: Game state gets corrupted or stuck
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: MEDIUM

Symptoms:
- Can't close dialogue
- Player can't move after dialogue
- Phone rings multiple times
- Document stays locked despite memories

Solutions:
1. Add state machine validation (already have this)
2. Add "force reset state" debug command
3. Log all state transitions
4. Add timeout for stuck states

Code:
```csharp
// In GameStateMachine
private float stateEnterTime;
private float maxStateTime = 300f; // 5 minutes max in any state

void Update()
{
    // Detect stuck states (except Ending)
    if (CurrentState != GameState.Ending && 
        Time.time - stateEnterTime > maxStateTime)
    {
        Debug.LogWarning($"State {CurrentState} exceeded max time, forcing to Playing");
        ForceState(GameState.Playing);
    }
}

public void ForceState(GameState state)
{
    CurrentState = state;
    stateEnterTime = Time.time;
    OnStateChanged?.Invoke(CurrentState, state);
}
```

───────────────────────────────────────────────────────────────────────────────

PROBLEM: Events fire multiple times or not at all
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: MEDIUM-HIGH

Symptoms:
- Memory triggers multiple times
- Document unlocks immediately
- Phone never rings
- Dialogue opens twice

Solutions:
1. Always unsubscribe in OnDisable
2. Check for duplicate subscriptions
3. Use flags to prevent re-triggering
4. Log event firing

Code:
```csharp
// Safe subscription pattern
void OnEnable()
{
    // Unsubscribe first to prevent duplicates
    EventManager.OnMemoryTriggered -= HandleMemory;
    EventManager.OnMemoryTriggered += HandleMemory;
}

void OnDisable()
{
    EventManager.OnMemoryTriggered -= HandleMemory;
}

// With flag protection
private bool hasHandledMemory = false;

void HandleMemory(string memoryId)
{
    if (hasHandledMemory) return;
    hasHandledMemory = true;
    // ... handle memory
}
```
```

### 1.7 Build & Platform Issues

```
PROBLEM: Works in Editor but not in build
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: HIGH (very common)

Common causes:
- File paths use Application.dataPath (wrong in build)
- Resources.Load path is wrong
- Script execution order issues
- Missing scene in Build Settings
- IL2CPP stripping removes needed code

Solutions:
1. Use Application.persistentDataPath for runtime files
2. Use Application.streamingAssetsPath for read-only files
3. Check Build Settings includes all scenes
4. Test builds frequently, not just at the end
5. Check Player.log for errors

Paths:
```csharp
// EDITOR:
Application.dataPath = "[Project]/Assets"
Application.persistentDataPath = "[User]/Library/Application Support/..."
Application.streamingAssetsPath = "[Project]/Assets/StreamingAssets"

// BUILD:
Application.dataPath = "[Build]/Contents/Data" (macOS)
Application.persistentDataPath = "[User]/Library/Application Support/..."
Application.streamingAssetsPath = "[Build]/Contents/Resources/Data/StreamingAssets"

// ALWAYS use persistentDataPath for user data and downloads
string modelPath = Path.Combine(Application.persistentDataPath, "Models", "model.gguf");
```

───────────────────────────────────────────────────────────────────────────────

PROBLEM: macOS security blocks the app
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Likelihood: HIGH (for unsigned apps)

Symptoms:
- "Cannot open because developer cannot be verified"
- App immediately crashes on launch
- Network requests blocked

Solutions:
1. Right-click > Open (bypasses Gatekeeper once)
2. Sign with Developer ID (requires Apple Developer account)
3. Include instructions in README
4. Distribute via itch.io (they handle some of this)

README instructions:
```
If macOS blocks the app:
1. Right-click the app and select "Open"
2. Click "Open" in the dialog
3. The app will be allowed to run from now on
```
```

---

## 2. Enhanced State Export System

Your script is a great start! Here's an enhanced version that captures much more useful debugging info:

### 2.1 Comprehensive State Exporter

```csharp
// DebugStateExporter.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace LastDay.Debug
{
    public class DebugStateExporter : MonoBehaviour
    {
        [Header("Export Settings")]
        [SerializeField] private string fileName = "GameState.txt";
        [SerializeField] private bool includeInactiveObjects = true;
        [SerializeField] private bool includeComponentDetails = true;
        
        [Header("Root Objects (auto-finds if empty)")]
        [SerializeField] private Transform uiRoot;
        [SerializeField] private Transform gameRoot;
        
        [Header("Auto-Export")]
        [SerializeField] private bool autoExportOnError = true;
        [SerializeField] private KeyCode exportHotkey = KeyCode.F12;
        
        private string ExportPath => Path.Combine(Application.dataPath, "..", "DebugExports", fileName);
        
        void Awake()
        {
            if (autoExportOnError)
            {
                Application.logMessageReceived += OnLogMessage;
            }
        }
        
        void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessage;
        }
        
        void Update()
        {
            if (Input.GetKeyDown(exportHotkey))
            {
                ExportFullState();
            }
        }
        
        void OnLogMessage(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                ExportFullState($"_error_{System.DateTime.Now:HHmmss}");
            }
        }
        
        [ContextMenu("Export Full State")]
        public void ExportFullState(string suffix = "")
        {
            StringBuilder sb = new StringBuilder();
            
            string path = suffix != "" 
                ? ExportPath.Replace(".txt", $"{suffix}.txt") 
                : ExportPath;
            
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            
            // Header
            sb.AppendLine("╔══════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║              LAST DAY - DEBUG STATE EXPORT                       ║");
            sb.AppendLine($"║  Exported: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}                              ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            
            // Game State
            ExportGameState(sb);
            
            // Event Manager State
            ExportEventManagerState(sb);
            
            // Player State
            ExportPlayerState(sb);
            
            // LLM State
            ExportLLMState(sb);
            
            // Active Dialogues
            ExportDialogueState(sb);
            
            // UI Hierarchy
            ExportUIHierarchy(sb);
            
            // Scene Hierarchy
            ExportSceneHierarchy(sb);
            
            // Recent Logs
            ExportRecentLogs(sb);
            
            // Write to file
            File.WriteAllText(path, sb.ToString());
            UnityEngine.Debug.Log($"[DebugExport] State exported to: {path}");
        }
        
        private void ExportGameState(StringBuilder sb)
        {
            sb.AppendLine("┌──────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ GAME STATE                                                       │");
            sb.AppendLine("└──────────────────────────────────────────────────────────────────┘");
            
            var gsm = FindObjectOfType<Core.GameStateMachine>();
            if (gsm != null)
            {
                sb.AppendLine($"  Current State: {gsm.CurrentState}");
                sb.AppendLine($"  Can Player Move: {gsm.CanPlayerMove}");
                sb.AppendLine($"  Can Interact: {gsm.CanInteract}");
            }
            else
            {
                sb.AppendLine("  [GameStateMachine not found]");
            }
            
            sb.AppendLine($"  Time.time: {Time.time:F2}");
            sb.AppendLine($"  Time.timeScale: {Time.timeScale}");
            sb.AppendLine($"  Frame Count: {Time.frameCount}");
            sb.AppendLine();
        }
        
        private void ExportEventManagerState(StringBuilder sb)
        {
            sb.AppendLine("┌──────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ EVENT MANAGER STATE                                              │");
            sb.AppendLine("└──────────────────────────────────────────────────────────────────┘");
            
            var em = Core.EventManager.Instance;
            if (em != null)
            {
                sb.AppendLine($"  Has Asked For Help: {em.hasAskedForHelp}");
                sb.AppendLine($"  Document Unlocked: {em.documentUnlocked}");
                sb.AppendLine($"  Phone Has Rung: {em.phoneHasRung}");
                sb.AppendLine($"  Triggered Memories ({em.triggeredMemories.Count}):");
                foreach (var memory in em.triggeredMemories)
                {
                    sb.AppendLine($"    - {memory}");
                }
                
                sb.AppendLine($"  Recent Events:");
                foreach (var evt in em.GetRecentEvents())
                {
                    sb.AppendLine($"    - [{evt.timestamp:F1}] {evt.eventType}: {evt.objectId}");
                }
            }
            else
            {
                sb.AppendLine("  [EventManager not found]");
            }
            sb.AppendLine();
        }
        
        private void ExportPlayerState(StringBuilder sb)
        {
            sb.AppendLine("┌──────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ PLAYER STATE                                                     │");
            sb.AppendLine("└──────────────────────────────────────────────────────────────────┘");
            
            var player = Player.PlayerController2D.Instance;
            if (player != null)
            {
                sb.AppendLine($"  Position: {player.transform.position}");
                sb.AppendLine($"  Is Moving: {player.IsMoving}");
                
                var animator = player.characterAnimator;
                if (animator != null)
                {
                    sb.AppendLine($"  Facing Direction: {animator.CurrentDirection}");
                }
                
                var idle = player.idleMovement;
                if (idle != null)
                {
                    sb.AppendLine($"  Idle Movement Enabled: {idle.enabled}");
                }
            }
            else
            {
                sb.AppendLine("  [PlayerController2D not found]");
            }
            sb.AppendLine();
        }
        
        private void ExportLLMState(StringBuilder sb)
        {
            sb.AppendLine("┌──────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ LLM STATE                                                        │");
            sb.AppendLine("└──────────────────────────────────────────────────────────────────┘");
            
            var llm = Dialogue.LocalLLMManager.Instance;
            if (llm != null)
            {
                sb.AppendLine($"  Is Initialized: {llm.isInitialized}");
                sb.AppendLine($"  Current Character: {llm.currentCharacter}");
            }
            else
            {
                sb.AppendLine("  [LocalLLMManager not found]");
            }
            sb.AppendLine();
        }
        
        private void ExportDialogueState(StringBuilder sb)
        {
            sb.AppendLine("┌──────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ DIALOGUE UI STATE                                                │");
            sb.AppendLine("└──────────────────────────────────────────────────────────────────┘");
            
            var dialogueUI = UI.DialogueUI.Instance;
            if (dialogueUI != null)
            {
                sb.AppendLine($"  Panel Active: {dialogueUI.dialoguePanel.activeSelf}");
                sb.AppendLine($"  Current Text: \"{TruncateString(dialogueUI.dialogueText.text, 50)}\"");
                sb.AppendLine($"  Input Field Text: \"{dialogueUI.inputField.text}\"");
                sb.AppendLine($"  Input Field Interactable: {dialogueUI.inputField.interactable}");
                sb.AppendLine($"  Input Field Is Focused: {dialogueUI.inputField.isFocused}");
                sb.AppendLine($"  Send Button Interactable: {dialogueUI.sendButton.interactable}");
                sb.AppendLine($"  Thinking Indicator Active: {dialogueUI.thinkingIndicator.activeSelf}");
            }
            else
            {
                sb.AppendLine("  [DialogueUI not found]");
            }
            sb.AppendLine();
        }
        
        private void ExportUIHierarchy(StringBuilder sb)
        {
            sb.AppendLine("┌──────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ UI HIERARCHY                                                     │");
            sb.AppendLine("└──────────────────────────────────────────────────────────────────┘");
            
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                ParseHierarchy(canvas.transform, sb, 0, true);
            }
            else
            {
                sb.AppendLine("  [No Canvas found]");
            }
            sb.AppendLine();
        }
        
        private void ExportSceneHierarchy(StringBuilder sb)
        {
            sb.AppendLine("┌──────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ SCENE HIERARCHY (Active Objects)                                 │");
            sb.AppendLine("└──────────────────────────────────────────────────────────────────┘");
            
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                // Skip UI canvas (already exported)
                if (root.GetComponent<Canvas>() != null) continue;
                
                ParseHierarchy(root.transform, sb, 0, false);
            }
            sb.AppendLine();
        }
        
        private void ParseHierarchy(Transform current, StringBuilder sb, int depth, bool isUI)
        {
            if (!includeInactiveObjects && !current.gameObject.activeInHierarchy)
                return;
            
            string indent = new string(' ', depth * 2);
            string status = current.gameObject.activeSelf ? "●" : "○";
            
            // Build component summary
            string componentInfo = "";
            if (includeComponentDetails)
            {
                var components = current.GetComponents<Component>()
                    .Where(c => c != null && !(c is Transform))
                    .Select(c => c.GetType().Name);
                
                componentInfo = string.Join(", ", components);
                if (componentInfo.Length > 60)
                    componentInfo = componentInfo.Substring(0, 57) + "...";
            }
            
            // Special handling for UI components
            string uiState = "";
            if (isUI)
            {
                var image = current.GetComponent<Image>();
                var text = current.GetComponent<TMP_Text>();
                var inputField = current.GetComponent<TMP_InputField>();
                var button = current.GetComponent<Button>();
                
                if (inputField != null)
                {
                    uiState = $" [Input: \"{TruncateString(inputField.text, 20)}\", Focused:{inputField.isFocused}]";
                }
                else if (button != null)
                {
                    uiState = $" [Button: {(button.interactable ? "Enabled" : "Disabled")}]";
                }
                else if (text != null)
                {
                    uiState = $" [Text: \"{TruncateString(text.text, 30)}\"]";
                }
            }
            
            sb.AppendLine($"{indent}{status} {current.name}{uiState}");
            if (!string.IsNullOrEmpty(componentInfo))
            {
                sb.AppendLine($"{indent}  └─ [{componentInfo}]");
            }
            
            // Recurse children
            foreach (Transform child in current)
            {
                ParseHierarchy(child, sb, depth + 1, isUI);
            }
        }
        
        private void ExportRecentLogs(StringBuilder sb)
        {
            sb.AppendLine("┌──────────────────────────────────────────────────────────────────┐");
            sb.AppendLine("│ RECENT DEBUG LOGS                                                │");
            sb.AppendLine("└──────────────────────────────────────────────────────────────────┘");
            
            // Note: Unity doesn't expose log history directly
            // You'd need a custom log handler to capture these
            sb.AppendLine("  (Implement DebugLogCapture for full log history)");
            sb.AppendLine();
        }
        
        private string TruncateString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str)) return "";
            if (str.Length <= maxLength) return str;
            return str.Substring(0, maxLength - 3) + "...";
        }
    }
}
```

### 2.2 Log Capture System

```csharp
// DebugLogCapture.cs
using UnityEngine;
using System.Collections.Generic;

namespace LastDay.Debug
{
    public class DebugLogCapture : MonoBehaviour
    {
        public static DebugLogCapture Instance { get; private set; }
        
        [SerializeField] private int maxLogEntries = 100;
        
        private Queue<LogEntry> logHistory = new Queue<LogEntry>();
        
        public struct LogEntry
        {
            public float time;
            public LogType type;
            public string message;
            public string stackTrace;
        }
        
        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Application.logMessageReceived += CaptureLog;
        }
        
        void OnDestroy()
        {
            Application.logMessageReceived -= CaptureLog;
        }
        
        void CaptureLog(string message, string stackTrace, LogType type)
        {
            logHistory.Enqueue(new LogEntry
            {
                time = Time.time,
                type = type,
                message = message,
                stackTrace = stackTrace
            });
            
            while (logHistory.Count > maxLogEntries)
            {
                logHistory.Dequeue();
            }
        }
        
        public List<LogEntry> GetRecentLogs(int count = 20)
        {
            var logs = new List<LogEntry>(logHistory);
            logs.Reverse();
            return logs.GetRange(0, Mathf.Min(count, logs.Count));
        }
        
        public List<LogEntry> GetErrors()
        {
            var errors = new List<LogEntry>();
            foreach (var log in logHistory)
            {
                if (log.type == LogType.Error || log.type == LogType.Exception)
                {
                    errors.Add(log);
                }
            }
            return errors;
        }
    }
}
```

### 2.3 Export Format Example

Here's what the exported file looks like:

```
╔══════════════════════════════════════════════════════════════════╗
║              LAST DAY - DEBUG STATE EXPORT                       ║
║  Exported: 2024-12-15 14:32:45                                   ║
╚══════════════════════════════════════════════════════════════════╝

┌──────────────────────────────────────────────────────────────────┐
│ GAME STATE                                                       │
└──────────────────────────────────────────────────────────────────┘
  Current State: InDialogue
  Can Player Move: False
  Can Interact: False
  Time.time: 45.23
  Time.timeScale: 1
  Frame Count: 2714

┌──────────────────────────────────────────────────────────────────┐
│ EVENT MANAGER STATE                                              │
└──────────────────────────────────────────────────────────────────┘
  Has Asked For Help: True
  Document Unlocked: False
  Phone Has Rung: False
  Triggered Memories (1):
    - wedding_photo
  Recent Events:
    - [42.1] gaze_complete: wedding_photo
    - [44.5] interact: wedding_photo

┌──────────────────────────────────────────────────────────────────┐
│ PLAYER STATE                                                     │
└──────────────────────────────────────────────────────────────────┘
  Position: (1.5, -0.3, 0.0)
  Is Moving: False
  Facing Direction: (0.0, 1.0)
  Idle Movement Enabled: False

┌──────────────────────────────────────────────────────────────────┐
│ LLM STATE                                                        │
└──────────────────────────────────────────────────────────────────┘
  Is Initialized: True
  Current Character: partner

┌──────────────────────────────────────────────────────────────────┐
│ DIALOGUE UI STATE                                                │
└──────────────────────────────────────────────────────────────────┘
  Panel Active: True
  Current Text: "That photo always makes me smile. We were so youn..."
  Input Field Text: ""
  Input Field Interactable: True
  Input Field Is Focused: False  ← PROBLEM: Should be True!
  Send Button Interactable: True
  Thinking Indicator Active: False

┌──────────────────────────────────────────────────────────────────┐
│ UI HIERARCHY                                                     │
└──────────────────────────────────────────────────────────────────┘
● Canvas
  └─ [Canvas, CanvasScaler, GraphicRaycaster]
  ● DialoguePanel
    └─ [Image, DialogueUI]
    ● PortraitFrame
      └─ [Image]
      ● PortraitImage [Image: sprite=martha_neutral]
    ● NameText [Text: "Martha"]
    ● DialogueText [Text: "That photo always makes me..."]
    ● InputContainer
      ● InputField [Input: "", Focused:False]  ← HERE'S THE BUG
      ● SendButton [Button: Enabled]
    ○ ThinkingIndicator  ← Inactive (correct)
  ○ PhonePanel  ← Inactive (correct)
  ○ DecisionPanel  ← Inactive (correct)
```

---

## 3. Cursor Integration Strategies

### 3.1 Automatic State Export for Cursor

The challenge is that Cursor runs outside Unity and can't directly call Unity methods. Here are strategies:

```
STRATEGY 1: File Watcher (Recommended)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Unity side:
- Export state on every error
- Export state on hotkey (F12)
- Export state when specific events happen

Cursor side:
- Reference the exported file in your prompt
- "See GameState.txt for current state"

Workflow:
1. Bug happens in Unity
2. Press F12 (or auto-export on error)
3. File saved to /DebugExports/GameState.txt
4. In Cursor: "Input field loses focus. State: @GameState.txt"
5. Cursor sees full context

───────────────────────────────────────────────────────────────────────────────

STRATEGY 2: Continuous Export Mode
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

For debugging specific issues, export every frame (expensive):
```csharp
[Header("Continuous Export")]
[SerializeField] private bool continuousMode = false;
[SerializeField] private float exportInterval = 1f;

private float lastExportTime;

void Update()
{
    if (continuousMode && Time.time - lastExportTime > exportInterval)
    {
        ExportFullState($"_{Time.time:0000}");
        lastExportTime = Time.time;
    }
}
```

───────────────────────────────────────────────────────────────────────────────

STRATEGY 3: Clipboard Export
━━━━━━━━━━━━━━━━━━━━━━━━━━━

Copy state directly to clipboard for pasting into Cursor:
```csharp
[ContextMenu("Export to Clipboard")]
public void ExportToClipboard()
{
    StringBuilder sb = new StringBuilder();
    // ... build state string ...
    GUIUtility.systemCopyBuffer = sb.ToString();
    Debug.Log("[Debug] State copied to clipboard");
}
```

───────────────────────────────────────────────────────────────────────────────

STRATEGY 4: HTTP Debug Server
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Run a tiny HTTP server in Unity that Cursor could fetch:
```csharp
// This is more complex but enables live querying
// using UnityEngine.Networking;

// Start server on http://localhost:8080/state
// Cursor extension could auto-fetch

// Note: This is overkill for a 10-day project
```
```

### 3.2 Cursor Rules for Unity

Create a `.cursorrules` file that helps Cursor understand Unity:

```
# .cursorrules

## Unity Project Rules

### Context Awareness
- This is a Unity 2D project using C# 9.0
- All scripts are in Assets/Scripts/ organized by feature
- MonoBehaviour scripts attach to GameObjects
- Use [SerializeField] for Inspector-visible private fields

### When Debugging Unity Issues
1. Ask for the GameState.txt export first
2. Check if the issue is:
   - Code logic (can fix directly)
   - Inspector configuration (describe what to change)
   - Scene hierarchy (describe structure to create/modify)
   - Asset import settings (describe settings)

### Unity-Specific Patterns
- Singletons use Instance property and DontDestroyOnLoad
- Events use C# events with += and -= subscription
- Coroutines for time-based operations
- async/await for LLM calls (not coroutines)

### When Asked to Fix a Bug
1. Read the GameState.txt export
2. Identify which system is affected
3. Check the relevant script
4. Look for common Unity issues:
   - Null reference (component not assigned)
   - Timing (Awake vs Start vs OnEnable order)
   - State corruption (events firing wrong)
   - UI focus issues (EventSystem problems)
5. Provide specific fix with line numbers

### File References
- Main game state: GameState.txt (exported debug state)
- Architecture: Documentation/ENGINEERING_SCHEMATIC.md
- Known patterns: Documentation/UNITY_PATTERNS.md
```

### 3.3 Effective Prompt Templates for Cursor

```
TEMPLATE 1: Bug Report
━━━━━━━━━━━━━━━━━━━━━━

"Bug: [Description]

Expected: [What should happen]
Actual: [What happens]

State export:
[Paste GameState.txt contents or reference @GameState.txt]

Relevant code:
[Paste the script that's likely involved]

Please identify the issue and provide a fix."

───────────────────────────────────────────────────────────────────────────────

TEMPLATE 2: Implementation Request
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

"I need to implement [Feature].

Context:
- This is for a 2D Unity game
- Related systems: [List relevant managers/scripts]
- See architecture: @ENGINEERING_SCHEMATIC.md

Requirements:
1. [Requirement 1]
2. [Requirement 2]

Please provide the complete script with comments."

───────────────────────────────────────────────────────────────────────────────

TEMPLATE 3: Integration Help
━━━━━━━━━━━━━━━━━━━━━━━━━━━━

"I have these two scripts that need to work together:

Script A (@ScriptA.cs):
[Brief description]

Script B (@ScriptB.cs):
[Brief description]

They need to communicate when [event/condition].

Currently seeing: [Problem]

Please show how to properly connect them."
```

---

## 4. Debug Console System

### 4.1 In-Game Debug Console

```csharp
// DebugConsole.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace LastDay.Debug
{
    public class DebugConsole : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject consolePanel;
        public TMP_Text logText;
        public TMP_InputField commandInput;
        public ScrollRect scrollRect;
        
        [Header("Settings")]
        public KeyCode toggleKey = KeyCode.BackQuote; // ~ key
        public int maxLogLines = 100;
        public bool showInBuild = true;
        
        private List<string> logLines = new List<string>();
        private Dictionary<string, System.Action<string[]>> commands;
        
        void Awake()
        {
            #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            if (!showInBuild)
            {
                gameObject.SetActive(false);
                return;
            }
            #endif
            
            consolePanel.SetActive(false);
            RegisterCommands();
            Application.logMessageReceived += OnLogReceived;
        }
        
        void OnDestroy()
        {
            Application.logMessageReceived -= OnLogReceived;
        }
        
        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleConsole();
            }
        }
        
        void ToggleConsole()
        {
            consolePanel.SetActive(!consolePanel.activeSelf);
            if (consolePanel.activeSelf)
            {
                commandInput.ActivateInputField();
            }
        }
        
        void OnLogReceived(string message, string stackTrace, LogType type)
        {
            string color = type switch
            {
                LogType.Error => "#FF6666",
                LogType.Warning => "#FFFF66",
                LogType.Exception => "#FF66FF",
                _ => "#FFFFFF"
            };
            
            string prefix = type switch
            {
                LogType.Error => "[ERR]",
                LogType.Warning => "[WRN]",
                LogType.Exception => "[EXC]",
                _ => "[LOG]"
            };
            
            AddLine($"<color={color}>{prefix} {message}</color>");
        }
        
        void AddLine(string line)
        {
            logLines.Add(line);
            while (logLines.Count > maxLogLines)
            {
                logLines.RemoveAt(0);
            }
            
            logText.text = string.Join("\n", logLines);
            
            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
        
        public void OnCommandSubmit(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;
            
            AddLine($"<color=#66FF66>> {command}</color>");
            ExecuteCommand(command);
            commandInput.text = "";
            commandInput.ActivateInputField();
        }
        
        void RegisterCommands()
        {
            commands = new Dictionary<string, System.Action<string[]>>
            {
                { "help", CmdHelp },
                { "clear", CmdClear },
                { "state", CmdState },
                { "export", CmdExport },
                { "teleport", CmdTeleport },
                { "unlock", CmdUnlock },
                { "memory", CmdMemory },
                { "reset", CmdReset },
                { "llm", CmdLLM },
                { "god", CmdGod },
            };
        }
        
        void ExecuteCommand(string input)
        {
            string[] parts = input.ToLower().Split(' ');
            string cmd = parts[0];
            string[] args = parts.Length > 1 ? parts[1..] : new string[0];
            
            if (commands.TryGetValue(cmd, out var action))
            {
                try
                {
                    action(args);
                }
                catch (System.Exception e)
                {
                    AddLine($"<color=#FF6666>Error: {e.Message}</color>");
                }
            }
            else
            {
                AddLine($"<color=#FF6666>Unknown command: {cmd}</color>");
            }
        }
        
        // Commands
        void CmdHelp(string[] args)
        {
            AddLine("Available commands:");
            AddLine("  help - Show this help");
            AddLine("  clear - Clear console");
            AddLine("  state - Show game state");
            AddLine("  export - Export state to file");
            AddLine("  teleport <x> <y> - Teleport player");
            AddLine("  unlock document - Unlock document");
            AddLine("  memory <id> - Trigger memory");
            AddLine("  reset - Reset game state");
            AddLine("  llm test - Test LLM response");
            AddLine("  god - Toggle god mode (no restrictions)");
        }
        
        void CmdClear(string[] args)
        {
            logLines.Clear();
            logText.text = "";
        }
        
        void CmdState(string[] args)
        {
            var gsm = Core.GameStateMachine.Instance;
            var em = Core.EventManager.Instance;
            
            AddLine($"Game State: {gsm?.CurrentState}");
            AddLine($"Memories: {string.Join(", ", em?.triggeredMemories ?? new List<string>())}");
            AddLine($"Document Unlocked: {em?.documentUnlocked}");
            AddLine($"Phone Rung: {em?.phoneHasRung}");
        }
        
        void CmdExport(string[] args)
        {
            var exporter = FindObjectOfType<DebugStateExporter>();
            if (exporter != null)
            {
                exporter.ExportFullState();
                AddLine("State exported to file");
            }
            else
            {
                AddLine("DebugStateExporter not found");
            }
        }
        
        void CmdTeleport(string[] args)
        {
            if (args.Length < 2)
            {
                AddLine("Usage: teleport <x> <y>");
                return;
            }
            
            if (float.TryParse(args[0], out float x) && float.TryParse(args[1], out float y))
            {
                var player = Player.PlayerController2D.Instance;
                if (player != null)
                {
                    player.transform.position = new Vector3(x, y, 0);
                    AddLine($"Teleported to ({x}, {y})");
                }
            }
        }
        
        void CmdUnlock(string[] args)
        {
            if (args.Length > 0 && args[0] == "document")
            {
                Core.EventManager.Instance.documentUnlocked = true;
                AddLine("Document unlocked");
            }
        }
        
        void CmdMemory(string[] args)
        {
            if (args.Length < 1)
            {
                AddLine("Usage: memory <id>");
                AddLine("IDs: wedding_photo, ice_picks, guitar");
                return;
            }
            
            Core.EventManager.Instance.PublishEvent(new Core.GameEvent("debug_trigger", args[0], args[0]));
            AddLine($"Memory triggered: {args[0]}");
        }
        
        void CmdReset(string[] args)
        {
            var em = Core.EventManager.Instance;
            if (em != null)
            {
                em.triggeredMemories.Clear();
                em.hasAskedForHelp = false;
                em.documentUnlocked = false;
                em.phoneHasRung = false;
                AddLine("Game state reset");
            }
        }
        
        void CmdLLM(string[] args)
        {
            if (args.Length > 0 && args[0] == "test")
            {
                AddLine("Testing LLM...");
                TestLLM();
            }
        }
        
        async void TestLLM()
        {
            var llm = Dialogue.LocalLLMManager.Instance;
            if (llm != null && llm.isInitialized)
            {
                string response = await llm.GenerateResponse("Hello");
                AddLine($"LLM Response: {response}");
            }
            else
            {
                AddLine("LLM not initialized");
            }
        }
        
        void CmdGod(string[] args)
        {
            // Toggle god mode - all interactions available, document unlocked
            var em = Core.EventManager.Instance;
            em.documentUnlocked = true;
            em.hasAskedForHelp = true;
            AddLine("God mode enabled - all features unlocked");
        }
    }
}
```

---

## 5. Common Bug Patterns & Fixes

### 5.1 Quick Reference Bug Table

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ SYMPTOM                        │ LIKELY CAUSE              │ FIX           │
├────────────────────────────────┼───────────────────────────┼───────────────┤
│ Player walks through walls     │ Obstacle layer not set    │ Check Layer   │
│ Player can't reach object      │ Pathfinding grid wrong    │ Rebuild grid  │
│ Animation wrong direction      │ Animator params wrong     │ Check blend   │
│ Sprite is blurry               │ Filter Mode not Point     │ Import settings│
│ Input field loses focus        │ EventSystem issue         │ ActivateInput │
│ Dialogue opens twice           │ Double event subscription │ Unsubscribe   │
│ LLM returns gibberish          │ Context too long          │ Reduce tokens │
│ Music doesn't loop             │ Bad loop point            │ Fix in Audacity│
│ Build crashes on start         │ Missing scene in settings │ Add scene     │
│ Works in editor, not build     │ Wrong file path           │ Use persistent│
│ UI too small/big               │ Canvas Scaler wrong       │ Match reference│
│ Phone rings immediately        │ Event fires on Start      │ Add delay     │
│ Document never unlocks         │ Memory count logic wrong  │ Debug count   │
│ Character frozen               │ State machine stuck       │ Force state   │
│ Clicking does nothing          │ Layer mask wrong          │ Check layers  │
│ Sound plays wrong              │ Multiple AudioSources     │ PlayOneShot   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 5.2 First Debug Steps

When something breaks:

```
1. CHECK CONSOLE
   - Any red errors?
   - Any null references?
   - Copy exact error message

2. EXPORT STATE (F12)
   - What's the current GameState?
   - Is EventManager tracking correctly?
   - Is the right UI panel active?

3. CHECK INSPECTOR
   - Are references assigned?
   - Are layers correct?
   - Is component enabled?

4. SIMPLIFY
   - Comment out recent changes
   - Test minimal case
   - Add Debug.Log statements

5. ASK CURSOR
   - Paste error + state export
   - Describe expected vs actual
   - Reference relevant script
```

---

## Quick Reference: Debug Hotkeys

```
┌─────────────────────────────────────────────────────────────────┐
│                    DEBUG HOTKEYS                                │
├─────────────────────────────────────────────────────────────────┤
│  F12        - Export game state to file                        │
│  ~          - Toggle debug console                              │
│  F1         - Toggle pathfinding grid visualization            │
│  F2         - Toggle UI debug outlines                          │
│  F3         - Force state to Playing                            │
│  F5         - Reload current scene                              │
│  Ctrl+R     - Rebuild pathfinding grid                          │
└─────────────────────────────────────────────────────────────────┘
```
