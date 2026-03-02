# Last Day - AI-Accelerated Development Guide

## Table of Contents
1. [AI Tools Overview](#1-ai-tools-overview)
2. [Tool-by-Task Mapping](#2-tool-by-task-mapping)
3. [2D Project Architecture](#3-2d-project-architecture)
4. [Team Split Strategy](#4-team-split-strategy)
5. [Test Cases & Validation](#5-test-cases--validation)
6. [Prompt Templates](#6-prompt-templates)

---

## 1. AI Tools Overview

### 1.1 Code Generation Tools

| Tool | Best For | How to Use | Cost |
|------|----------|------------|------|
| **Claude Code (CLI)** | Complex systems, refactoring, full file generation | Terminal-based, agentic coding, can read/write files directly | Pro subscription |
| **Cursor** | Real-time coding, autocomplete, inline edits | VS Code fork with AI built-in, uses your codebase as context | Free tier available |
| **GitHub Copilot** | Autocomplete, boilerplate, repetitive code | VS Code extension, inline suggestions | $10/mo |
| **Codeium** | Free Copilot alternative | VS Code extension | Free |
| **Claude.ai (Web)** | Architecture planning, debugging, explanations | Chat interface, paste code for review | Free tier |

**Recommendation for your project:**
- **Primary**: Claude Code CLI for generating full scripts
- **Secondary**: Cursor for real-time editing in Unity's external editor
- **Fallback**: Claude.ai web for debugging and planning

### 1.2 Art & Asset Generation

| Tool | Best For | Output | Cost |
|------|----------|--------|------|
| **Midjourney** | Highest quality 2D art, consistent style | PNG images | $10/mo |
| **DALL-E 3** | Good quality, easy API access | PNG images | Pay per image |
| **Leonardo.ai** | Game assets specifically, transparent backgrounds | PNG with alpha | Free tier |
| **Krea.ai** | Real-time generation, upscaling | PNG images | Free tier |
| **Stable Diffusion (local)** | Unlimited, full control, consistent characters | PNG images | Free (hardware) |
| **Remove.bg** | Background removal | PNG with alpha | Free tier |
| **Pixelcut** | Background removal + upscaling | PNG with alpha | Free tier |

**Recommendation for your project:**
- **Primary**: Midjourney for main assets (best quality)
- **Secondary**: Leonardo.ai for objects needing transparency
- **Post-process**: Remove.bg for any cleanup needed

### 1.3 Audio Generation

| Tool | Best For | Output | Cost |
|------|----------|--------|------|
| **Eleven Labs** | Voice acting, NPC voices | MP3/WAV | Free tier |
| **Suno.ai** | Background music, ambient tracks | MP3 | Free tier |
| **Mubert** | Generative ambient music | MP3 | Free tier |
| **Freesound.org** | SFX library | Various | Free |
| **LMNT** | Voice cloning, dialogue | MP3/WAV | Free tier |

**Recommendation for your project:**
- **Primary**: Eleven Labs for any voiced lines (optional)
- **Music**: Suno.ai for ambient emotional music
- **SFX**: Freesound.org for room ambience, phone ring, etc.

### 1.4 Design & Planning

| Tool | Best For | Output | Cost |
|------|----------|--------|------|
| **Claude.ai** | System architecture, story writing, prompts | Text | Free tier |
| **Miro AI** | Flowcharts, diagrams | Visual boards | Free tier |
| **Notion AI** | Documentation, task management | Docs | Free tier |
| **v0.dev** | UI mockups (web-based but good for reference) | React code | Free tier |

### 1.5 LLM Integration (In-Game)

| Tool | Best For | Integration | Cost |
|------|----------|-------------|------|
| **LLMUnity** | Easiest Unity integration | Unity package | Free |
| **Ollama** | Local model serving | REST API | Free |
| **llama.cpp** | Direct C++ integration | Native plugin | Free |
| **OpenAI API** | Cloud-based, highest quality | REST API | Pay per token |
| **Anthropic API** | Cloud-based, great reasoning | REST API | Pay per token |

**Recommendation for your project:**
- **Primary**: LLMUnity (simplest path to working demo)
- **Fallback**: Ollama + UnityWebRequest (if LLMUnity has issues)

---

## 2. Tool-by-Task Mapping

### 2.1 Complete Workflow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           DEVELOPMENT WORKFLOW                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  PHASE 1: PLANNING                                                          │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐                  │
│  │  Claude.ai   │───▶│   Miro AI    │───▶│  Notion AI   │                  │
│  │  (Architecture)   │  (Flowcharts) │    │  (Task List) │                  │
│  └──────────────┘    └──────────────┘    └──────────────┘                  │
│                                                                              │
│  PHASE 2: ASSET CREATION                                                    │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐                  │
│  │  Midjourney  │───▶│  Remove.bg   │───▶│    Unity     │                  │
│  │  (Generate)  │    │  (Clean up)  │    │  (Import)    │                  │
│  └──────────────┘    └──────────────┘    └──────────────┘                  │
│         │                                                                    │
│         ▼                                                                    │
│  ┌──────────────┐    ┌──────────────┐                                       │
│  │   Suno.ai    │───▶│    Unity     │                                       │
│  │   (Music)    │    │  (Import)    │                                       │
│  └──────────────┘    └──────────────┘                                       │
│                                                                              │
│  PHASE 3: CODE DEVELOPMENT                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐                  │
│  │ Claude Code  │───▶│   Cursor     │───▶│    Unity     │                  │
│  │ (Generate)   │    │  (Edit/Fix)  │    │  (Test)      │                  │
│  └──────────────┘    └──────────────┘    └──────────────┘                  │
│                                                                              │
│  PHASE 4: LLM INTEGRATION                                                   │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐                  │
│  │  LLMUnity    │───▶│  Claude.ai   │───▶│    Unity     │                  │
│  │  (Package)   │    │  (Prompts)   │    │  (Test)      │                  │
│  └──────────────┘    └──────────────┘    └──────────────┘                  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Specific Task → Tool Mapping

```
UNITY SCRIPTS:
├── PlayerController.cs      → Claude Code (generate) → Cursor (iterate)
├── DialogueUI.cs            → Claude Code (generate) → Cursor (iterate)  
├── EventManager.cs          → Claude Code (generate) → Cursor (iterate)
├── InteractableObject.cs    → Claude Code (generate) → Cursor (iterate)
├── LocalLLMManager.cs       → Claude Code + LLMUnity docs
├── FadeManager.cs           → Claude Code (simple, one-shot)
└── EndScreen.cs             → Claude Code (simple, one-shot)

ART ASSETS:
├── Room Background          → Midjourney (--ar 16:9 --style raw)
├── Wedding Photo            → Midjourney → Remove.bg
├── Ice Picks                → Midjourney → Remove.bg
├── Guitar                   → Midjourney → Remove.bg
├── Martha Character         → Midjourney (reference sheet)
├── UI Elements              → Midjourney or Figma AI
└── Document                 → Midjourney → Remove.bg

AUDIO:
├── Ambient Room Tone        → Freesound.org (search "room tone quiet")
├── Clock Ticking            → Freesound.org
├── Phone Ring               → Freesound.org (vintage phone)
├── Emotional Music          → Suno.ai (prompt: "melancholic piano ambient")
└── Typing Sounds            → Freesound.org

STORY & PROMPTS:
├── Character Backstories    → Claude.ai
├── Memory Stories           → Claude.ai
├── System Prompts           → Claude.ai
└── Dialogue Variations      → Claude.ai
```

---

## 3. 2D Project Architecture

### 3.1 Updated Scene Structure for 2D

```
MainRoom (Scene) - 2D
│
├── --- RENDERING ---
├── Main Camera (Orthographic)
│   └── Size: 5
│   └── Background: Solid color or skybox
│
├── --- BACKGROUND LAYERS ---
├── Background
│   ├── RoomBackground (SpriteRenderer, Order: -10)
│   └── WindowLight (SpriteRenderer, Order: -9, additive blend)
│
├── --- MIDGROUND (Furniture) ---
├── Midground
│   ├── Bookshelf (SpriteRenderer, Order: -5)
│   ├── Desk (SpriteRenderer, Order: -4)
│   └── Chair (SpriteRenderer, Order: -3)
│
├── --- INTERACTIVE OBJECTS ---
├── Interactables
│   ├── WeddingPhoto (SpriteRenderer + Collider2D + InteractableObject)
│   │   └── Order: 0
│   │   └── HighlightSprite (child, Order: 1, initially hidden)
│   │
│   ├── IcePicks (SpriteRenderer + Collider2D + InteractableObject)
│   │   └── Order: 0
│   │
│   ├── Guitar (SpriteRenderer + Collider2D + InteractableObject)
│   │   └── Order: 0
│   │
│   ├── Phone (SpriteRenderer + Collider2D + PhoneInteraction)
│   │   └── Order: 0
│   │
│   └── Document (SpriteRenderer + Collider2D + DocumentInteraction)
│       └── Order: 0
│
├── --- CHARACTERS ---
├── Martha (SpriteRenderer, Order: 2)
│   └── Could be animated with simple sprite swap or skeletal
│
├── --- FOREGROUND ---
├── Foreground
│   └── ChairArmrest (SpriteRenderer, Order: 5, frames the view)
│
├── --- MANAGERS ---
├── GameManager
├── EventManager
├── LocalLLMManager
├── ModelDownloader
├── FadeManager
├── AudioManager
│
├── --- UI ---
└── Canvas (Screen Space - Overlay)
    ├── DialoguePanel
    ├── PhonePanel
    ├── MonologuePanel
    ├── DecisionPanel
    ├── DownloadPanel
    ├── FadePanel
    ├── EndScreen
    └── InteractionPrompt
```

### 3.2 2D Interaction System

```csharp
// InteractableObject2D.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractableObject2D : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Object Identity")]
    public string objectId;
    public string displayName;
    public string memoryId;
    
    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer highlightRenderer;  // Glow effect
    public Color highlightColor = new Color(1f, 1f, 0.8f, 0.3f);
    
    [Header("Gaze/Hover")]
    public float hoverTimeToTrigger = 2f;
    private float currentHoverTime = 0f;
    private bool isHovering = false;
    private bool hasTriggeredGaze = false;
    
    [Header("Audio")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    
    void Start()
    {
        if (highlightRenderer != null)
            highlightRenderer.enabled = false;
            
        // Ensure we have a collider for mouse detection
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }
    }
    
    void Update()
    {
        if (isHovering && !hasTriggeredGaze)
        {
            currentHoverTime += Time.deltaTime;
            
            // Pulse highlight based on progress
            if (highlightRenderer != null)
            {
                float progress = currentHoverTime / hoverTimeToTrigger;
                highlightRenderer.color = new Color(
                    highlightColor.r,
                    highlightColor.g,
                    highlightColor.b,
                    highlightColor.a * progress
                );
            }
            
            if (currentHoverTime >= hoverTimeToTrigger)
            {
                OnGazeComplete();
            }
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        
        if (highlightRenderer != null)
            highlightRenderer.enabled = true;
            
        DialogueUI.Instance.ShowInteractionPrompt($"Click to examine {displayName}");
        
        if (hoverSound != null)
            AudioSource.PlayClipAtPoint(hoverSound, transform.position, 0.5f);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        currentHoverTime = 0f;
        
        if (highlightRenderer != null)
            highlightRenderer.enabled = false;
            
        DialogueUI.Instance.HideInteractionPrompt();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickSound != null)
            AudioSource.PlayClipAtPoint(clickSound, transform.position, 0.7f);
            
        OnInteract();
    }
    
    void OnGazeComplete()
    {
        hasTriggeredGaze = true;
        
        EventManager.Instance.PublishEvent(new GameEvent {
            eventType = "gaze_complete",
            objectId = this.objectId,
            memoryId = this.memoryId
        });
        
        // Martha might comment unprompted
        if (Random.value > 0.5f)
        {
            EventManager.Instance.TriggerNPCComment(memoryId);
        }
    }
    
    void OnInteract()
    {
        EventManager.Instance.PublishEvent(new GameEvent {
            eventType = "interact",
            objectId = this.objectId,
            memoryId = this.memoryId
        });
        
        // Open dialogue about this object
        DialogueUI.Instance.ShowDialoguePanel("partner");
        DialogueUI.Instance.SetCurrentContext(memoryId);
    }
}
```

### 3.3 2D-Specific Simplifications

Since this is 2D, we can simplify several systems:

```
REMOVED (3D Only):
- First-person controller
- Raycasting for gaze detection
- 3D colliders
- Complex camera movement

SIMPLIFIED FOR 2D:
- Mouse hover = gaze detection
- Click = interact
- No player movement (static view of room)
- Sprite layering instead of 3D depth
- UI-based everything

NEW 2D SYSTEMS:
- Point-and-click interaction
- Sprite highlighting on hover
- Simple parallax for depth (optional)
- 2D camera zoom for document interaction
```

### 3.4 Camera for 2D

```csharp
// CameraController2D.cs
using UnityEngine;
using DG.Tweening; // Optional: DOTween for smooth animations

public class CameraController2D : MonoBehaviour
{
    public static CameraController2D Instance { get; private set; }
    
    [Header("Settings")]
    public float defaultSize = 5f;
    public float zoomedSize = 2f;
    public float zoomDuration = 0.5f;
    
    private Camera cam;
    private Vector3 defaultPosition;
    
    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        defaultPosition = transform.position;
    }
    
    public void ZoomToObject(Transform target)
    {
        // Move camera to center on object and zoom in
        Vector3 targetPos = new Vector3(target.position.x, target.position.y, defaultPosition.z);
        
        // Using DOTween (recommended)
        transform.DOMove(targetPos, zoomDuration);
        DOTween.To(() => cam.orthographicSize, x => cam.orthographicSize = x, zoomedSize, zoomDuration);
        
        // Or without DOTween:
        // StartCoroutine(ZoomCoroutine(targetPos, zoomedSize));
    }
    
    public void ResetZoom()
    {
        transform.DOMove(defaultPosition, zoomDuration);
        DOTween.To(() => cam.orthographicSize, x => cam.orthographicSize = x, defaultSize, zoomDuration);
    }
}
```

---

## 4. Team Split Strategy

### 4.1 Role Definitions

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           TEAM RESPONSIBILITIES                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  PERSON A (You - Stephone)                PERSON B (Partner - Abeygail)    │
│  ══════════════════════════               ═══════════════════════════════   │
│                                                                              │
│  PRIMARY: AI & Backend                    PRIMARY: Unity & Visual           │
│  ────────────────────────                 ──────────────────────────────    │
│  • LLM integration (LLMUnity)             • Scene setup & sprite layout     │
│  • Character prompts & tuning             • UI panels & styling             │
│  • Model download system                  • 2D interaction system           │
│  • Context building logic                 • Animation (fade, highlight)     │
│  • Story content & memories               • Audio integration               │
│                                                                              │
│  SECONDARY: Asset Pipeline                SECONDARY: Game Logic             │
│  ─────────────────────────                ─────────────────────────────     │
│  • Midjourney prompts                     • EventManager                    │
│  • Style consistency                      • Game state machine              │
│  • Post-processing assets                 • Document lock/unlock            │
│                                                                              │
│  TOOLS:                                   TOOLS:                            │
│  • Claude Code                            • Unity Editor                    │
│  • Claude.ai (prompts)                    • Cursor (in VS Code)            │
│  • Midjourney                             • Unity Animator                  │
│  • Cursor (for iteration)                 • Figma (optional, UI)           │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Daily Schedule (10 Days)

```
═══════════════════════════════════════════════════════════════════════════════
DAY 1: SETUP & PLANNING
═══════════════════════════════════════════════════════════════════════════════

MORNING (Together - 2 hours):
□ Create shared GitHub repo
□ Set up Unity project (2D template)
□ Install LLMUnity package
□ Review this document together
□ Agree on interface contracts (GameEvent, DialogueRequest)

STEPHONE (Afternoon):
□ Set up Claude Code CLI
□ Test LLMUnity basic example
□ Download and test Phi-3-mini model
□ Write first draft of Martha prompt

ABEYGAIL (Afternoon):
□ Create scene hierarchy
□ Set up Canvas with basic panels
□ Create placeholder sprites (colored squares)
□ Set up camera (orthographic, size 5)

EVENING SYNC (30 min):
□ Demo LLM responding to test input
□ Demo clicking placeholder objects
□ Identify any blockers

═══════════════════════════════════════════════════════════════════════════════
DAY 2: CORE SYSTEMS
═══════════════════════════════════════════════════════════════════════════════

STEPHONE:
□ Generate LocalLLMManager.cs with Claude Code
□ Generate CharacterPrompts.cs
□ Create ModelDownloader.cs (with progress UI)
□ Test: Model downloads successfully
□ Test: Basic prompt → response works

ABEYGAIL:
□ Generate InteractableObject2D.cs with Claude Code/Cursor
□ Generate EventManager.cs
□ Wire up hover detection on placeholder objects
□ Test: Hovering object shows highlight
□ Test: Clicking object fires event

EVENING SYNC:
□ Merge branches
□ Test: Click object → EventManager receives event
□ Plan Day 3 integration

═══════════════════════════════════════════════════════════════════════════════
DAY 3: DIALOGUE INTEGRATION
═══════════════════════════════════════════════════════════════════════════════

STEPHONE:
□ Create context building logic
□ Test different inputs with Martha prompt
□ Tune prompt for short, natural responses
□ Add memory context injection
□ Test: Mentioning photo changes Martha's responses

ABEYGAIL:
□ Generate DialogueUI.cs with Claude Code
□ Implement typewriter effect
□ Create thinking indicator
□ Wire DialogueUI to LocalLLMManager
□ Test: Type input → see response appear

TOGETHER (Evening):
□ Full integration test
□ Click photo → Opens dialogue → Type "tell me about this" → Get response
□ Fix any integration bugs

═══════════════════════════════════════════════════════════════════════════════
DAY 4: ASSETS - ROUND 1
═══════════════════════════════════════════════════════════════════════════════

STEPHONE:
□ Generate room background in Midjourney
□ Generate wedding photo asset
□ Generate ice picks asset
□ Generate guitar asset
□ Post-process all with Remove.bg

ABEYGAIL:
□ Import and place background
□ Create sprite sorting layers
□ Position interactive objects
□ Add highlight sprites (white glow versions)
□ Test: Visual hover feedback working

TOGETHER (Evening):
□ Review asset quality
□ Regenerate any that don't fit
□ Adjust sorting/positioning

═══════════════════════════════════════════════════════════════════════════════
DAY 5: MEMORY SYSTEM
═══════════════════════════════════════════════════════════════════════════════

STEPHONE:
□ Write full story for wedding photo
□ Write full story for ice picks
□ Write full story for guitar
□ Add memory context to CharacterPrompts
□ Test: Each memory changes AI responses appropriately

ABEYGAIL:
□ Implement gaze timer (hover → trigger)
□ Track triggered memories in EventManager
□ Visual feedback when memory triggers (subtle pulse?)
□ Test: Hover 2 seconds → memory triggers
□ Document object lock system

TOGETHER (Evening):
□ Playtest memory system
□ Tune gaze timing
□ Verify AI incorporates memories

═══════════════════════════════════════════════════════════════════════════════
DAY 6: PHONE CALL
═══════════════════════════════════════════════════════════════════════════════

STEPHONE:
□ Write David's character prompt
□ Create phone conversation context
□ Test David's responses
□ Tune for different tone than Martha

ABEYGAIL:
□ Create phone ring trigger (after 2 memories OR timer)
□ Create phone UI variant
□ Implement phone pickup/hangup flow
□ Generate phone asset
□ Add phone ring audio (Freesound)

TOGETHER (Evening):
□ Full phone call playtest
□ Test switching between Martha and David
□ Fix any UI glitches

═══════════════════════════════════════════════════════════════════════════════
DAY 7: ENDINGS
═══════════════════════════════════════════════════════════════════════════════

STEPHONE:
□ Generate document asset
□ Write ending quotes (signed/torn)
□ Create any final dialogue variations
□ Test edge cases with AI

ABEYGAIL:
□ Implement DocumentInteraction.cs
□ Implement FadeManager.cs
□ Create EndScreen.cs with quote display
□ Camera zoom to document
□ Decision panel UI

TOGETHER (Evening):
□ Full ending playtest
□ Both paths working
□ Fade timing feels right

═══════════════════════════════════════════════════════════════════════════════
DAY 8: POLISH - AUDIO & ANIMATION
═══════════════════════════════════════════════════════════════════════════════

STEPHONE:
□ Generate ambient music with Suno.ai
□ Collect SFX from Freesound
□ Create typing sounds
□ Test AI response edge cases

ABEYGAIL:
□ Implement AudioManager
□ Add ambient room audio
□ Add UI sounds (click, hover)
□ Simple Martha idle animation (optional)
□ Polish UI transitions

TOGETHER (Evening):
□ Audio levels pass
□ Everything feels cohesive

═══════════════════════════════════════════════════════════════════════════════
DAY 9: FULL PLAYTESTING
═══════════════════════════════════════════════════════════════════════════════

MORNING (Together):
□ Fresh playthrough - don't talk, just observe
□ Note all friction points
□ List bugs

AFTERNOON (Split bug fixes):
STEPHONE:
□ Fix AI response issues
□ Tune any prompt problems
□ Handle edge case inputs

ABEYGAIL:
□ Fix UI bugs
□ Fix interaction bugs
□ Polish visual issues

EVENING (Together):
□ Another playthrough
□ Final bug list
□ Prioritize must-fix vs nice-to-have

═══════════════════════════════════════════════════════════════════════════════
DAY 10: SHIP IT
═══════════════════════════════════════════════════════════════════════════════

MORNING:
□ Fix critical bugs only
□ Final playtest
□ Build for target platform

AFTERNOON:
□ Create README
□ Package build
□ Test on fresh machine
□ Submit/share

BUFFER:
□ This day also serves as overflow for Day 9 issues
```

### 4.3 Communication Protocol

```
DAILY SYNCS:
- Morning (15 min): Quick standup, blockers
- Evening (30 min): Demo progress, plan tomorrow

ASYNC COMMUNICATION:
- Slack/Discord for quick questions
- GitHub Issues for bugs
- Shared Notion for documentation

MERGE STRATEGY:
- Each person works on feature branch
- Merge to main at end of each day
- Main should always be buildable

HANDOFF POINTS:
- Scripts generated by Stephone → Abeygail integrates into Unity
- Assets generated by Stephone → Abeygail places in scene
- Abeygail creates UI → Stephone wires to LLM
```

---

## 5. Test Cases & Validation

### 5.1 Unit Tests (Manual Validation)

```
╔═══════════════════════════════════════════════════════════════════════════╗
║                           LLM INTEGRATION TESTS                            ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                            ║
║  TEST 1: Model Download                                                   ║
║  ─────────────────────                                                    ║
║  Given: Fresh install, no model file                                      ║
║  When: Game launches                                                      ║
║  Then: Download panel appears, progress bar moves, file saved             ║
║  Verify: File exists at Application.persistentDataPath/Models/            ║
║                                                                            ║
║  TEST 2: Model Load                                                       ║
║  ────────────────────                                                     ║
║  Given: Model file exists                                                 ║
║  When: Game launches                                                      ║
║  Then: Model loads without error, warmup completes                        ║
║  Verify: Console shows "LLM initialized successfully"                     ║
║                                                                            ║
║  TEST 3: Basic Response                                                   ║
║  ─────────────────────                                                    ║
║  Given: Model loaded, Martha prompt active                                ║
║  When: Send "Hello Martha"                                                ║
║  Then: Response received within 10 seconds                                ║
║  Verify: Response is in character, < 100 tokens                           ║
║                                                                            ║
║  TEST 4: Memory Context                                                   ║
║  ─────────────────────                                                    ║
║  Given: wedding_photo in triggeredMemories                                ║
║  When: Send "What are you thinking about?"                                ║
║  Then: Response references wedding or marriage                            ║
║  Verify: Memory is influencing responses                                  ║
║                                                                            ║
║  TEST 5: Character Switch                                                 ║
║  ───────────────────────                                                  ║
║  Given: Martha prompt active                                              ║
║  When: Switch to David prompt, send "Hey buddy"                           ║
║  Then: Response is in David's voice (more direct)                         ║
║  Verify: Different character, different tone                              ║
║                                                                            ║
║  TEST 6: Off-Topic Handling                                               ║
║  ─────────────────────────                                                ║
║  Given: Martha prompt active                                              ║
║  When: Send "What's the capital of France?"                               ║
║  Then: Martha gently redirects or gives confused response                 ║
║  Verify: Doesn't break character with Wikipedia answer                    ║
║                                                                            ║
╚═══════════════════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════════════════╗
║                          INTERACTION TESTS                                 ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                            ║
║  TEST 7: Hover Detection                                                  ║
║  ─────────────────────                                                    ║
║  Given: Scene loaded, mouse over nothing                                  ║
║  When: Move mouse over wedding photo                                      ║
║  Then: Highlight appears, prompt shows "Click to examine"                 ║
║  Verify: OnPointerEnter fired                                             ║
║                                                                            ║
║  TEST 8: Gaze Timer                                                       ║
║  ─────────────────────                                                    ║
║  Given: Mouse hovering over photo                                         ║
║  When: Stay for 2+ seconds                                                ║
║  Then: Gaze event fires, highlight pulses                                 ║
║  Verify: "gaze_complete" event in EventManager                            ║
║                                                                            ║
║  TEST 9: Click Interaction                                                ║
║  ───────────────────────                                                  ║
║  Given: Mouse hovering over photo                                         ║
║  When: Click                                                              ║
║  Then: Dialogue panel opens, focus on input                               ║
║  Verify: DialogueUI.currentContext == "wedding_photo"                     ║
║                                                                            ║
║  TEST 10: Memory Tracking                                                 ║
║  ──────────────────────                                                   ║
║  Given: No memories triggered                                             ║
║  When: Gaze at photo, then gaze at guitar                                 ║
║  Then: triggeredMemories.Count == 2                                       ║
║  Verify: Both "wedding_photo" and "guitar" in list                        ║
║                                                                            ║
╚═══════════════════════════════════════════════════════════════════════════╝

╔═══════════════════════════════════════════════════════════════════════════╗
║                           GAME FLOW TESTS                                  ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                            ║
║  TEST 11: Document Lock                                                   ║
║  ─────────────────────                                                    ║
║  Given: 0 memories triggered                                              ║
║  When: Click on document                                                  ║
║  Then: Internal monologue: "Not yet..."                                   ║
║  Verify: Decision panel does NOT appear                                   ║
║                                                                            ║
║  TEST 12: Document Unlock                                                 ║
║  ───────────────────────                                                  ║
║  Given: 2+ memories triggered                                             ║
║  When: Click on document                                                  ║
║  Then: Camera zooms, decision panel appears                               ║
║  Verify: Both buttons visible and clickable                               ║
║                                                                            ║
║  TEST 13: Signed Ending                                                   ║
║  ─────────────────────                                                    ║
║  Given: Decision panel visible                                            ║
║  When: Click "Sign the Document"                                          ║
║  Then: Fade to black, quote appears                                       ║
║  Verify: Correct quote (Viktor Frankl)                                    ║
║                                                                            ║
║  TEST 14: Torn Ending                                                     ║
║  ───────────────────────                                                  ║
║  Given: Decision panel visible                                            ║
║  When: Click "Tear it Up"                                                 ║
║  Then: Fade to black, quote appears                                       ║
║  Verify: Correct quote (Cicero)                                           ║
║                                                                            ║
║  TEST 15: Phone Trigger                                                   ║
║  ─────────────────────                                                    ║
║  Given: 2 memories triggered                                              ║
║  When: Wait 5 seconds                                                     ║
║  Then: Phone rings (audio plays)                                          ║
║  Verify: Phone object highlighted                                         ║
║                                                                            ║
║  TEST 16: Phone Conversation                                              ║
║  ────────────────────────                                                 ║
║  Given: Phone ringing                                                     ║
║  When: Click phone                                                        ║
║  Then: Phone UI appears, David's greeting shows                           ║
║  Verify: currentNpc == "phone_friend"                                     ║
║                                                                            ║
╚═══════════════════════════════════════════════════════════════════════════╝
```

### 5.2 Integration Test Scenarios

```
FULL PLAYTHROUGH TEST A: "Quick Decision"
─────────────────────────────────────────
1. Launch game
2. Wait for model download (if needed)
3. Scene appears
4. Click wedding photo → dialogue opens
5. Type "Tell me about this photo" → Martha responds
6. Close dialogue
7. Click guitar → dialogue opens
8. Type "I miss playing" → Martha responds
9. Close dialogue
10. Document should now be unlocked
11. Click document → decision panel
12. Click "Sign" → ending plays
✓ Time target: < 5 minutes

FULL PLAYTHROUGH TEST B: "Explore Everything"
─────────────────────────────────────────────
1. Launch game
2. Hover over all objects (note highlights)
3. Trigger gaze on photo (wait 2 sec)
4. Trigger gaze on picks (wait 2 sec)
5. Phone should ring
6. Answer phone
7. Talk to David: "What do you think I should do?"
8. Hang up
9. Talk to Martha about ice picks
10. Click document → decision panel
11. Click "Tear" → ending plays
✓ Time target: < 10 minutes

FULL PLAYTHROUGH TEST C: "Edge Cases"
─────────────────────────────────────
1. Launch game
2. Immediately try to click document → should be locked
3. Type gibberish to Martha → should handle gracefully
4. Type offensive content → should redirect or refuse
5. Click same object multiple times → no duplicate events
6. Close dialogue without typing → no errors
7. Let phone ring without answering → it stops eventually
8. Open decision panel then click outside → panel stays
✓ No crashes, no errors
```

### 5.3 AI Response Quality Tests

```
PROMPT QUALITY TESTS
═══════════════════

Test A: Natural Conversation Flow
─────────────────────────────────
Input sequence:
1. "Good morning, Martha"
2. "How are you feeling today?"
3. "I've been thinking about the photo on the table"

Expected:
- Each response builds on previous
- No repetition
- Stays in character
- References photo naturally on #3

Test B: Memory Integration
──────────────────────────
Setup: Trigger wedding_photo memory first

Input: "What are you thinking about?"

Expected:
- References wedding, marriage, or "us"
- Doesn't awkwardly say "I notice you looked at the photo"
- Feels natural, not mechanical

Test C: Emotional Range
───────────────────────
Inputs (test separately):
1. "I love you, Martha"
2. "I'm scared"
3. "Do you think I should sign the papers?"
4. "Tell me about a happy memory"

Expected:
- Different emotional responses to each
- #2 should be comforting
- #3 should be conflicted, not directive
- #4 should be warm and nostalgic

Test D: David vs Martha Differentiation
───────────────────────────────────────
Same input to both: "What would you do in my position?"

Expected:
- Martha: conflicted, emotional, talks about love/loss
- David: more direct, might mention his wife's death, supports choice

Test E: Boundary Testing
────────────────────────
Inputs:
1. "What's 2 + 2?" → Should redirect to present moment
2. "Write me a poem" → Should decline or keep very short
3. "I want to hurt myself" → Should be supportive, not clinical
4. "Tell me about the news" → Should say she hasn't been paying attention

Expected:
- Stays in character
- Doesn't become a general assistant
- Handles dark topics with care
```

---

## 6. Prompt Templates

### 6.1 Claude Code Prompts

**For generating Unity scripts:**

```
I need a Unity C# script for a 2D point-and-click narrative game.

CONTEXT:
- Unity 2022.3 LTS
- 2D orthographic camera
- Using TextMeshPro for UI
- Using LLMUnity package for AI dialogue

REQUIREMENTS:
[Paste specific requirements here]

CONSTRAINTS:
- Include namespace: LastDay
- Include required using statements
- Add [Header] attributes for inspector organization
- Include XML documentation for public methods
- Use Unity events where appropriate for decoupling

Please generate the complete script with no placeholders.
```

**For debugging:**

```
I have a Unity C# script that's not working as expected.

SCRIPT:
[Paste script here]

EXPECTED BEHAVIOR:
[Describe what should happen]

ACTUAL BEHAVIOR:
[Describe what's actually happening]

ERROR MESSAGES (if any):
[Paste errors]

Please identify the issue and provide the corrected code.
```

### 6.2 Midjourney Prompts

**Master style reference (create first):**

```
/imagine warm nostalgic living room interior, elderly couple's home, 
afternoon light through sheer curtains, muted earth tones with burgundy 
and gold accents, slightly desaturated, painterly style like Edward Hopper, 
intimate domestic atmosphere, subtle film grain, 1970s Americana, 
the feeling of 47 years of memories --ar 16:9 --style raw --v 6
```

**Save the seed number from your favorite result, then use:**

```
/imagine [object description], warm nostalgic style, afternoon light, 
muted earth tones, painterly, isolated object on simple background 
--ar 1:1 --style raw --seed [YOUR_SEED] --v 6
```

**Individual object prompts:**

```
Wedding Photo:
/imagine vintage wedding photograph in ornate silver frame, 1970s wedding,
young couple looking happy, woman in simple white dress, man in military 
dress uniform, slightly faded colors, studio portrait style, isolated on 
cream background --ar 1:1 --style raw --v 6

Ice Picks:
/imagine pair of vintage mountaineering ice axes mounted on wooden plaque,
worn leather grips, aged metal, small brass plate reading "1989", 
adventurous memento, isolated on cream background --ar 1:1 --style raw --v 6

Guitar:
/imagine vintage acoustic guitar, dusty, warm honey-colored wood, few 
scratches from years of playing, one string broken, beautiful but 
neglected, isolated on cream background --ar 1:1 --style raw --v 6

Phone:
/imagine vintage rotary telephone, cream colored plastic with brass dial,
heavy handset, curled cord, 1970s design, isolated on cream background 
--ar 1:1 --style raw --v 6

Document:
/imagine official medical document on wooden desk, formal letterhead,
pen beside it, dramatic lighting, important decision feeling, top-down 
view, warm muted tones --ar 4:3 --style raw --v 6
```

### 6.3 Suno.ai Music Prompts

```
Ambient Background:
"melancholic ambient piano, gentle, sparse notes, warm, nostalgic, 
contemplative, soft strings in background, sunset feeling, 
intimate, 70 bpm, no drums"

Emotional Peak:
"emotional piano solo, bittersweet, tender, saying goodbye, 
gentle crescendo, strings joining softly, cinematic, intimate, 
memories, 65 bpm"

End Credits (Signed):
"peaceful ambient, acceptance, gentle resolution, soft piano, 
warm pad synths, hopeful sadness, fade out, 60 bpm"

End Credits (Torn):
"hopeful ambient piano, gentle, new beginnings, soft light feeling,
strings, warm, tender, 68 bpm"
```

### 6.4 Story Writing Prompts (for Claude.ai)

```
I'm writing a narrative game about an elderly man deciding whether 
to pursue medical assistance in dying. I need to write a memory 
story for an object in his home.

OBJECT: [Name of object]

REQUIREMENTS:
- The story should be 3-4 paragraphs
- It should reveal something about the protagonist's character
- It should subtly connect to themes of: autonomy, quality of life, 
  love, or mortality
- It should be told from the perspective of his wife Martha, 
  who might share this memory during conversation
- The tone should be warm and nostalgic, not maudlin
- Include specific sensory details

Please write this memory story.
```

---

## Appendix: Quick Reference Commands

### LLMUnity Setup
```csharp
// Minimal setup
using LLMUnity;

public class QuickStart : MonoBehaviour 
{
    LLM llm;
    
    void Start() 
    {
        llm = gameObject.AddComponent<LLM>();
        llm.modelPath = "path/to/model.gguf";
        llm.Warmup();
    }
    
    async void Chat(string input) 
    {
        string response = await llm.Chat(input);
        Debug.Log(response);
    }
}
```

### Midjourney Aspect Ratios
```
--ar 16:9   Room backgrounds
--ar 1:1    Object sprites
--ar 4:3    Document view
--ar 9:16   Character full body
```

### Unity 2D Setup Checklist
```
□ Edit > Project Settings > Editor > Default Behavior Mode: 2D
□ Camera: Orthographic, Size 5
□ Canvas: Screen Space - Overlay
□ Sprites: Pixels Per Unit = 100
□ Sorting Layers: Background, Midground, Objects, Foreground, UI
```

### Git Commands
```bash
# Daily workflow
git checkout -b feature/day-3-dialogue
git add .
git commit -m "feat: dialogue UI complete"
git push origin feature/day-3-dialogue

# End of day merge
git checkout main
git pull
git merge feature/day-3-dialogue
git push
```
