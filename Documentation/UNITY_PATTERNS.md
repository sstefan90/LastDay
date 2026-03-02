# Last Day - Unity Patterns & FAQ Guide

## Table of Contents
1. [Answers to Your Questions](#1-answers-to-your-questions)
2. [Unity Core Concepts](#2-unity-core-concepts)
3. [Common Build Patterns](#3-common-build-patterns)
4. [2D Player Movement & Pathfinding](#4-2d-player-movement--pathfinding)
5. [Pixel Art Style Guide](#5-pixel-art-style-guide)
6. [Open Source Dialogue Systems](#6-open-source-dialogue-systems)
7. [Cursor + Unity Workflow](#7-cursor--unity-workflow)
8. [Cost & Performance Estimates](#8-cost--performance-estimates)

---

## 1. Answers to Your Questions

### Q: What is Remove.bg?

**Remove.bg** is a web service (not code you write) that automatically removes backgrounds from images.

```
Website: https://www.remove.bg
Cost: Free for low-res, ~$0.20/image for high-res via API
Alternative: https://www.photoroom.com (also free tier)

WORKFLOW:
1. Generate image in Midjourney (has background)
2. Download PNG
3. Upload to remove.bg
4. Download transparent PNG
5. Import to Unity as sprite

For batch processing, they have an API:
pip install remove-bg-api
```

**Free alternatives:**
- **Photopea.com** - Free Photoshop clone in browser, manual selection
- **GIMP** - Free desktop app, use "Select by Color" tool
- **Rembg** - Open source Python library (runs locally, unlimited free)

```bash
# Rembg (local, free, unlimited)
pip install rembg
rembg i input.png output.png
```

---

### Q: Player Movement to Objects?

For a 2D point-and-click game, you have two options:

**Option A: Simple Lerp Movement (Recommended for Demo)**
- Player sprite slides to clicked position
- No pathfinding needed if no obstacles
- ~20 lines of code

**Option B: A* Pathfinding**
- Needed if there are obstacles to navigate around
- Use free A* Pathfinding Project asset
- More complex setup

**For your demo, I recommend Option A** - since it's one room with no obstacles, you don't need real pathfinding. See Section 4 for full implementation.

---

### Q: Open Source Dialogue HUDs?

Yes! Several free options:

| Asset | Style | Link | Notes |
|-------|-------|------|-------|
| **Dialogue System for Unity** | Professional | Asset Store (free lite) | Overkill for your needs |
| **Yarn Spinner** | Node-based | github.com/YarnSpinnerTool | Great, but designed for scripted dialogue |
| **Fungus** | Visual novel | github.com/snozbot/fungus | Good UI, easy to customize |
| **Naninovel** | Visual novel | Paid, but beautiful | Too expensive for demo |
| **Ink** | Narrative | github.com/inkle/ink-unity-integration | Good for branching, less UI |

**My Recommendation:** Start with a **simple custom UI** (I'll provide the script) because:
1. Your dialogue is AI-generated, not scripted
2. You need input field, not choice buttons
3. Existing systems expect pre-written text

However, you can **borrow visual styles** from these. Fungus has nice speech bubble prefabs you could extract.

---

### Q: Cursor + Unity UI - How to Expose State?

**The Problem:** Cursor can edit code, but Unity UI is configured in the Inspector (visual), not in code.

**Solutions:**

**1. Generate UI via Code (Recommended for AI workflow)**
```csharp
// Instead of manually placing UI in Inspector, generate it:
public class UIGenerator : MonoBehaviour
{
    void Start()
    {
        CreateDialoguePanel();
    }
    
    void CreateDialoguePanel()
    {
        // All UI creation in code - Cursor can see and edit this
        GameObject panel = new GameObject("DialoguePanel");
        panel.AddComponent<CanvasRenderer>();
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);
        // ... etc
    }
}
```

**2. Export UI State to JSON**
```csharp
// Editor script to dump UI hierarchy
[MenuItem("Tools/Export UI State")]
static void ExportUIState()
{
    var canvas = FindObjectOfType<Canvas>();
    var state = SerializeHierarchy(canvas.transform);
    File.WriteAllText("ui_state.json", state);
    // Now Cursor can read ui_state.json
}
```

**3. Use UI Toolkit (Unity's newer system)**
- UI defined in UXML (XML files) and USS (CSS-like)
- These are text files Cursor CAN read and edit
- Steeper learning curve but more AI-friendly

**Practical Approach for Your Demo:**
1. Create UI manually in Unity (faster for one-time setup)
2. Keep a `UIReference.cs` script that documents all UI elements
3. When bugs occur, describe the issue to Cursor with the code context

```csharp
// UIReference.cs - Documentation file for Cursor context
/*
UI HIERARCHY:
Canvas
├── DialoguePanel (anchored bottom, height 250)
│   ├── NPCNameText (TMP, top-left, gold color)
│   ├── NPCDialogueText (TMP, below name, white)
│   ├── PlayerInputField (TMP InputField, bottom, stretches)
│   └── SendButton (right of input, 100x50)
├── PhonePanel (same structure, different colors)
└── FadePanel (full screen, black image, starts alpha 0)

KNOWN ISSUES:
- InputField loses focus after send
- Typewriter effect doesn't handle newlines
*/
```

---

### Q: How Long Should Music Be?

```
SCENE BREAKDOWN:

Ambient Loop (Main gameplay):
- Length: 2-3 minutes, seamless loop
- Generate 3 minutes in Suno, trim to loop point in Audacity
- This plays the entire time player is exploring

Emotional Swell (Optional - when triggering memories):
- Length: 30-45 seconds
- Plays over ambient when significant moment happens
- Fades back to ambient

End Screen (Signed path):
- Length: 45-60 seconds
- Plays once, can fade out

End Screen (Torn path):
- Length: 45-60 seconds
- Different tone, more hopeful

Phone Ring:
- Length: 5-10 seconds, loopable
- Classic rotary phone sound

TOTAL MUSIC TO GENERATE:
- 1 ambient loop (~3 min)
- 2 end screen tracks (~1 min each)
- Optional: 1 emotional swell (~45 sec)
= About 6 minutes of music total
```

---

### Q: M4 MacBook Pro 16GB RAM - Model Latency?

**Expected Performance:**

| Model | Size | First Token | Full Response (50 tokens) |
|-------|------|-------------|---------------------------|
| Phi-3-mini Q4 | 2.2 GB | ~500ms | ~2-3 seconds |
| Gemma-2B Q4 | 1.5 GB | ~300ms | ~1.5-2 seconds |
| Qwen2-0.5B Q4 | 400 MB | ~100ms | ~0.5-1 second |
| Llama-3.2-1B Q4 | 700 MB | ~200ms | ~1-1.5 seconds |
| Mistral-7B Q4 | 4.1 GB | ~800ms | ~4-5 seconds |

**Recommendation for M4 16GB:**
- **Primary: Phi-3-mini Q4** - Best quality/speed balance, ~2-3 sec responses
- **Fallback: Llama-3.2-1B** - Faster, still good quality
- **Avoid: Mistral-7B** - Will work but slower than ideal

**M4 Specific Notes:**
- Apple Silicon is excellent for llama.cpp (Metal acceleration)
- 16GB unified memory means model stays in RAM
- Expect consistent performance (no thermal throttling issues)

**In Practice:**
```
Player types: "Tell me about the photo"
[Thinking indicator shows for ~2 seconds]
[Response appears with typewriter effect over ~2 seconds]
Total perceived wait: ~4 seconds

This is acceptable for a narrative game - it feels like the character is "thinking"
```

---

## 2. Unity Core Concepts

### 2.1 What is a Prefab?

A **Prefab** is a reusable template for GameObjects.

```
ANALOGY:
- A Prefab is like a "class" in programming
- Instances in the scene are like "objects" created from that class
- Change the Prefab = all instances update

EXAMPLE:
You create one "InteractableObject" prefab with:
- SpriteRenderer
- BoxCollider2D  
- InteractableObject2D.cs script

Then you duplicate it for:
- WeddingPhoto (change sprite, set objectId)
- Guitar (change sprite, set objectId)
- IcePicks (change sprite, set objectId)

If you later add a "HighlightEffect" to the prefab,
ALL objects get it automatically.
```

**How to Create a Prefab:**
```
1. Create GameObject in scene with all components
2. Drag from Hierarchy → Project window (Assets folder)
3. Blue cube icon = it's now a prefab
4. Original in scene is now an "instance" (blue text)

TO EDIT PREFAB:
- Double-click prefab in Project window
- Opens Prefab Mode (isolated editing)
- Changes apply to all instances
```

**Prefab Structure for Your Game:**
```
Assets/Prefabs/
├── InteractableObject.prefab     (base template)
├── Characters/
│   └── Martha.prefab
├── UI/
│   ├── DialoguePanel.prefab
│   ├── PhonePanel.prefab
│   └── EndScreen.prefab
└── Effects/
    └── HighlightGlow.prefab
```

### 2.2 Lighting in 2D

**2D Lighting Options:**

```
OPTION A: No Dynamic Lighting (Simplest - RECOMMENDED)
──────────────────────────────────────────────────────
- Bake lighting into your sprite artwork
- Use Midjourney to generate pre-lit assets
- Add "glow" sprites as children for highlights
- Zero runtime cost

OPTION B: Unity 2D Lights (URP)
───────────────────────────────
- Requires Universal Render Pipeline (URP)
- 2D Light components: Point, Spot, Global
- Sprites need "Sprite-Lit-Default" material
- Nice for dynamic effects (lamp flicker)

OPTION C: Sprite Shaders
────────────────────────
- Custom shaders for glow/highlight
- More control, more complexity
```

**For Your Demo - Option A Implementation:**

```csharp
// Instead of dynamic lighting, use sprite tricks

public class ObjectHighlight : MonoBehaviour
{
    public SpriteRenderer mainSprite;
    public SpriteRenderer glowSprite;  // White/bright version behind main
    
    public void SetHighlight(bool on)
    {
        glowSprite.enabled = on;
    }
    
    // Pulse effect for glow
    void Update()
    {
        if (glowSprite.enabled)
        {
            float alpha = 0.3f + Mathf.Sin(Time.time * 2f) * 0.2f;
            glowSprite.color = new Color(1, 1, 0.9f, alpha);
        }
    }
}
```

**Creating Glow Sprites:**
```
1. Take your object sprite (e.g., guitar.png)
2. In Photoshop/GIMP:
   - Duplicate layer
   - Apply Gaussian Blur (10-20px)
   - Set to white or light yellow
   - Increase canvas size slightly
3. Export as guitar_glow.png
4. In Unity, place glow sprite BEHIND main sprite (lower sorting order)
```

### 2.3 Scene vs Prefab vs ScriptableObject

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    UNITY DATA ORGANIZATION                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  SCENE (.unity file)                                                    │
│  ───────────────────                                                    │
│  • A "level" or "screen" of your game                                  │
│  • Contains GameObjects arranged in space                               │
│  • You have: MainRoom.unity, maybe TitleScreen.unity                   │
│                                                                          │
│  PREFAB (.prefab file)                                                  │
│  ─────────────────────                                                  │
│  • Reusable GameObject template                                         │
│  • Can be instantiated at runtime                                       │
│  • Changes to prefab affect all instances                              │
│                                                                          │
│  SCRIPTABLEOBJECT (.asset file)                                        │
│  ──────────────────────────────                                         │
│  • Data container (no Transform, no scene presence)                    │
│  • Great for: character stats, dialogue data, settings                 │
│  • Example: MemoryData.asset containing all memory stories             │
│                                                                          │
│  SCRIPT (.cs file)                                                      │
│  ─────────────────                                                      │
│  • Behavior/logic                                                       │
│  • Attached to GameObjects                                              │
│  • Can reference Prefabs and ScriptableObjects                         │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

**ScriptableObject for Your Memory Data:**

```csharp
// MemoryData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewMemory", menuName = "LastDay/Memory Data")]
public class MemoryData : ScriptableObject
{
    public string memoryId;
    public string objectName;
    public Sprite objectSprite;
    public Sprite glowSprite;
    
    [TextArea(3, 10)]
    public string shortDescription;
    
    [TextArea(5, 20)]
    public string fullStory;
    
    [TextArea(3, 10)]
    public string marthaContext;  // Added to AI prompt when triggered
}
```

Then create: `Assets/Data/Memories/WeddingPhoto.asset`, etc.

---

## 3. Common Build Patterns

### 3.1 Singleton Pattern (Managers)

```csharp
// Used for: GameManager, EventManager, AudioManager, etc.
// Ensures only ONE instance exists and is globally accessible

public class GameManager : MonoBehaviour
{
    // Static instance
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        // If instance already exists, destroy this duplicate
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);  // Persist between scenes
    }
    
    // Now accessible anywhere:
    // GameManager.Instance.DoSomething();
}
```

### 3.2 Observer Pattern (Events)

```csharp
// Used for: Decoupling systems, UI updates, game state changes

// --- EVENT DEFINITIONS ---
public static class GameEvents
{
    // Define events
    public static event System.Action<string> OnMemoryTriggered;
    public static event System.Action OnDocumentUnlocked;
    public static event System.Action<string> OnDialogueReceived;
    
    // Methods to invoke events
    public static void TriggerMemory(string memoryId)
    {
        OnMemoryTriggered?.Invoke(memoryId);
    }
    
    public static void UnlockDocument()
    {
        OnDocumentUnlocked?.Invoke();
    }
}

// --- PUBLISHER (fires events) ---
public class InteractableObject2D : MonoBehaviour
{
    void OnGazeComplete()
    {
        GameEvents.TriggerMemory(memoryId);  // Fire event
    }
}

// --- SUBSCRIBER (listens for events) ---
public class DocumentInteraction : MonoBehaviour
{
    void OnEnable()
    {
        GameEvents.OnMemoryTriggered += HandleMemoryTriggered;
    }
    
    void OnDisable()
    {
        GameEvents.OnMemoryTriggered -= HandleMemoryTriggered;  // Always unsubscribe!
    }
    
    void HandleMemoryTriggered(string memoryId)
    {
        triggeredCount++;
        if (triggeredCount >= 2)
        {
            GameEvents.UnlockDocument();
        }
    }
}
```

### 3.3 State Machine Pattern (Game Flow)

```csharp
// Used for: Managing game states, preventing invalid transitions

public enum GameState
{
    Loading,
    Intro,
    Playing,
    InDialogue,
    PhoneCall,
    Decision,
    Ending
}

public class GameStateMachine : MonoBehaviour
{
    public static GameStateMachine Instance { get; private set; }
    
    public GameState CurrentState { get; private set; } = GameState.Loading;
    
    public event System.Action<GameState, GameState> OnStateChanged;
    
    void Awake() => Instance = this;
    
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        
        // Validate transition
        if (!IsValidTransition(CurrentState, newState))
        {
            Debug.LogWarning($"Invalid transition: {CurrentState} → {newState}");
            return;
        }
        
        GameState oldState = CurrentState;
        CurrentState = newState;
        
        OnStateChanged?.Invoke(oldState, newState);
        
        Debug.Log($"State: {oldState} → {newState}");
    }
    
    bool IsValidTransition(GameState from, GameState to)
    {
        // Define valid transitions
        return (from, to) switch
        {
            (GameState.Loading, GameState.Intro) => true,
            (GameState.Intro, GameState.Playing) => true,
            (GameState.Playing, GameState.InDialogue) => true,
            (GameState.Playing, GameState.PhoneCall) => true,
            (GameState.Playing, GameState.Decision) => true,
            (GameState.InDialogue, GameState.Playing) => true,
            (GameState.PhoneCall, GameState.Playing) => true,
            (GameState.Decision, GameState.Ending) => true,
            _ => false
        };
    }
}

// Usage in other scripts:
public class DialogueUI : MonoBehaviour
{
    public void OpenDialogue()
    {
        GameStateMachine.Instance.ChangeState(GameState.InDialogue);
        // Show panel...
    }
    
    public void CloseDialogue()
    {
        GameStateMachine.Instance.ChangeState(GameState.Playing);
        // Hide panel...
    }
}
```

### 3.4 Object Pooling (Performance)

```csharp
// Not critical for your demo, but good to know
// Used for: Particles, bullets, spawned objects

public class SimplePool : MonoBehaviour
{
    public GameObject prefab;
    public int poolSize = 10;
    
    private Queue<GameObject> pool = new Queue<GameObject>();
    
    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
    
    public GameObject Get()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(prefab);  // Fallback
    }
    
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

### 3.5 Component Architecture (Your Game)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        COMPONENT RELATIONSHIPS                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                         ┌──────────────────┐                                │
│                         │   GameManager    │                                │
│                         │   (Singleton)    │                                │
│                         └────────┬─────────┘                                │
│                                  │                                           │
│              ┌───────────────────┼───────────────────┐                      │
│              ▼                   ▼                   ▼                      │
│   ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐           │
│   │  EventManager    │ │ GameStateMachine │ │   AudioManager   │           │
│   │  (Singleton)     │ │   (Singleton)    │ │   (Singleton)    │           │
│   └────────┬─────────┘ └──────────────────┘ └──────────────────┘           │
│            │                                                                 │
│            │ Events                                                          │
│            ▼                                                                 │
│   ┌──────────────────┐         ┌──────────────────┐                         │
│   │ InteractableObj  │────────▶│   DialogueUI     │                         │
│   │ (on each object) │ click   │   (Singleton)    │                         │
│   └──────────────────┘         └────────┬─────────┘                         │
│                                         │                                    │
│                                         ▼                                    │
│                                ┌──────────────────┐                         │
│                                │  LocalLLMManager │                         │
│                                │   (Singleton)    │                         │
│                                └──────────────────┘                         │
│                                                                              │
│   FLOW:                                                                     │
│   1. Player clicks object                                                   │
│   2. InteractableObj publishes event to EventManager                       │
│   3. EventManager updates state, notifies DialogueUI                       │
│   4. DialogueUI opens panel, sends to LocalLLMManager                      │
│   5. LocalLLMManager returns response                                       │
│   6. DialogueUI displays with typewriter                                   │
│   7. GameStateMachine tracks we're "InDialogue"                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. 2D Player Movement & Pathfinding

### 4.1 Simple Movement (No Obstacles) - RECOMMENDED

```csharp
// PlayerMovement2D.cs
using UnityEngine;

public class PlayerMovement2D : MonoBehaviour
{
    public static PlayerMovement2D Instance { get; private set; }
    
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float arrivalThreshold = 0.1f;
    
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;  // Optional
    
    // State
    private Vector3 targetPosition;
    private bool isMoving = false;
    private System.Action onArrival;
    
    void Awake()
    {
        Instance = this;
        targetPosition = transform.position;
    }
    
    void Update()
    {
        if (!isMoving) return;
        
        // Move towards target
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        
        // Flip sprite based on direction
        if (direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
        
        // Set walking animation
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
        }
        
        // Check arrival
        if (Vector3.Distance(transform.position, targetPosition) < arrivalThreshold)
        {
            isMoving = false;
            
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
            }
            
            onArrival?.Invoke();
            onArrival = null;
        }
    }
    
    /// <summary>
    /// Move player to world position
    /// </summary>
    public void MoveTo(Vector3 worldPosition, System.Action callback = null)
    {
        // Don't move if in dialogue
        if (GameStateMachine.Instance.CurrentState == GameState.InDialogue)
        {
            return;
        }
        
        targetPosition = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        isMoving = true;
        onArrival = callback;
    }
    
    /// <summary>
    /// Move to object, then trigger interaction
    /// </summary>
    public void MoveToAndInteract(InteractableObject2D target)
    {
        // Calculate position slightly in front of object
        Vector3 interactionPoint = target.transform.position;
        interactionPoint.x -= 0.5f;  // Stand to the left of object
        
        MoveTo(interactionPoint, () => {
            target.OnInteract();
        });
    }
    
    public void StopMoving()
    {
        isMoving = false;
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
        }
    }
}
```

### 4.2 Click Handler for Movement

```csharp
// ClickToMove.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickToMove : MonoBehaviour
{
    public LayerMask walkableLayer;
    public LayerMask interactableLayer;
    
    void Update()
    {
        // Ignore if over UI
        if (EventSystem.current.IsPointerOverGameObject())
            return;
            
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }
    
    void HandleClick()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // First, check if clicking an interactable
        RaycastHit2D interactHit = Physics2D.Raycast(mousePos, Vector2.zero, 0f, interactableLayer);
        if (interactHit.collider != null)
        {
            var interactable = interactHit.collider.GetComponent<InteractableObject2D>();
            if (interactable != null)
            {
                PlayerMovement2D.Instance.MoveToAndInteract(interactable);
                return;
            }
        }
        
        // Otherwise, check if clicking walkable area
        RaycastHit2D walkHit = Physics2D.Raycast(mousePos, Vector2.zero, 0f, walkableLayer);
        if (walkHit.collider != null)
        {
            PlayerMovement2D.Instance.MoveTo(mousePos);
        }
    }
}
```

### 4.3 Walkable Area Setup

```
SCENE SETUP FOR WALKABLE AREA:

1. Create empty GameObject "WalkableArea"
2. Add PolygonCollider2D (or BoxCollider2D)
3. Set layer to "Walkable" (create new layer)
4. Draw the shape of where player can walk
5. Set Collider to "Is Trigger" = true

The player will only move to positions within this collider.
```

### 4.4 If You Need Pathfinding (Obstacles)

```csharp
// Using A* Pathfinding Project (free version)
// Asset Store: https://arongranberg.com/astar/

using Pathfinding;

public class PlayerPathfinding : MonoBehaviour
{
    public float speed = 3f;
    public float nextWaypointDistance = 0.5f;
    
    private Seeker seeker;
    private Path path;
    private int currentWaypoint = 0;
    
    void Start()
    {
        seeker = GetComponent<Seeker>();
    }
    
    public void MoveTo(Vector3 target)
    {
        seeker.StartPath(transform.position, target, OnPathComplete);
    }
    
    void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }
    
    void Update()
    {
        if (path == null) return;
        
        if (currentWaypoint >= path.vectorPath.Count)
        {
            path = null;
            return;
        }
        
        Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - (Vector2)transform.position).normalized;
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        
        if (Vector2.Distance(transform.position, path.vectorPath[currentWaypoint]) < nextWaypointDistance)
        {
            currentWaypoint++;
        }
    }
}
```

**For your demo:** The simple lerp movement (4.1) is sufficient. You likely won't have obstacles to navigate around in a single room.

---

## 5. Pixel Art Style Guide (Stardew Valley-like)

### 5.1 Updated Art Direction

```
OLD DIRECTION (Hopper-style):
- Painterly, realistic proportions
- Muted, desaturated
- Film grain

NEW DIRECTION (Pixel Art):
- 16x16 or 32x32 character sprites
- Limited color palette (16-32 colors)
- Chunky pixels, visible
- Warm, cozy (like Stardew Valley)
- Expressive despite simplicity
```

### 5.2 Updated Midjourney Prompts

**Characters:**

```
PIXEL ART CHARACTER PROMPT:
/imagine pixel art character sprite sheet, 32x32 pixels, elderly woman, 
gray hair in bun, kind face, simple dress and cardigan, front side back views, 
cozy warm colors, stardew valley style, retro game aesthetic, 
transparent background --ar 1:1 --v 6

PROTAGONIST (for dialogue portrait):
/imagine pixel art portrait, 48x48 pixels, elderly man, gentle expression,
gray hair, warm eyes showing both pain and wisdom, cozy sweater,
stardew valley style, limited color palette --ar 1:1 --v 6
```

**Environment:**

```
ROOM BACKGROUND:
/imagine pixel art interior scene, cozy living room, afternoon sunlight 
through window, warm wooden floors, comfortable furniture, personal touches 
like photos and books, 16-bit style, stardew valley aesthetic, 
320x180 resolution upscaled, game background --ar 16:9 --v 6
```

**Objects:**

```
INTERACTIVE OBJECTS:
/imagine pixel art game item sprite, vintage wedding photograph in silver frame,
32x32 pixels, warm colors, slight glow effect, transparent background,
stardew valley item style --ar 1:1 --v 6

/imagine pixel art game item sprite, old acoustic guitar leaning against wall,
32x32 pixels, warm wood tones, transparent background,
stardew valley item style --ar 1:1 --v 6

/imagine pixel art game item sprite, ice climbing picks mounted on plaque,
32x32 pixels, adventure memento, transparent background,
stardew valley item style --ar 1:1 --v 6
```

### 5.3 Alternative: Pixel Art Tools

Since you want pixel art, you might skip Midjourney for characters and use:

| Tool | Best For | Cost |
|------|----------|------|
| **Aseprite** | Industry standard pixel art | $20 one-time |
| **Piskel** | Free browser-based | Free |
| **Pixelorama** | Free Aseprite alternative | Free |
| **Lospec** | Color palettes | Free |
| **PixelMe** | Convert photos to pixel art | Free tier |
| **Pixel It** | Similar to PixelMe | Free |

**AI + Pixel Art Workflow:**

```
OPTION A: Generate then Pixelate
1. Generate realistic image in Midjourney
2. Use PixelMe or Pixel It to convert
3. Clean up in Aseprite

OPTION B: Prompt for Pixel Art directly
1. Add "pixel art, 32x32, limited palette" to prompts
2. Results are inconsistent but sometimes great
3. Clean up in Aseprite

OPTION C: Find Existing Assets
1. https://itch.io/game-assets/tag-pixel-art (many free)
2. Look for "cozy" or "rpg" asset packs
3. Modify colors to match your palette
```

### 5.4 Recommended Pixel Art Asset Packs (Free)

```
ITCH.IO FREE PACKS:

Modern Interiors:
https://limezu.itch.io/moderninteriors
- Free version has living room furniture
- Very Stardew-like

Cozy Farm Asset Pack:
https://shubibubi.itch.io/cozy-farm
- Warm colors, indoor items

Pixel UI Pack:
https://kenney-assets.itch.io/pixel-ui-pack
- Free dialogue boxes, buttons

Character Base:
https://sanderfrenken.github.io/Universal-LPC-Spritesheet-Character-Generator/
- Free character generator
- Customize appearance, export sprites
```

### 5.5 Unity Import Settings for Pixel Art

```
SPRITE IMPORT SETTINGS:

1. Select sprite in Project window
2. In Inspector:
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: Single (or Multiple for spritesheets)
   - Pixels Per Unit: 16 (for 16x16) or 32 (for 32x32)
   - Filter Mode: Point (no filter) ← CRITICAL for crisp pixels
   - Compression: None
   
3. In Edit > Project Settings > Quality:
   - Anti-aliasing: Disabled
   
4. Camera settings:
   - Size: Calculate for your pixel density
   - For 32x32 sprites at 1080p: Size ≈ 5-8
```

---

## 6. Open Source Dialogue Systems

### 6.1 Comparison for Your Needs

```
YOUR REQUIREMENTS:
✓ Text input from player (not choice buttons)
✓ AI-generated responses (not scripted)
✓ Portrait display
✓ Typewriter effect
✓ Pixel art aesthetic

VERDICT: Build custom, but borrow UI design from existing systems
```

### 6.2 Simple Custom Dialogue System

```csharp
// PixelDialogueUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PixelDialogueUI : MonoBehaviour
{
    public static PixelDialogueUI Instance { get; private set; }
    
    [Header("Panel")]
    public GameObject dialoguePanel;
    public Image panelBackground;  // 9-slice pixel art border
    
    [Header("Character Display")]
    public Image characterPortrait;
    public TMP_Text characterNameText;
    public Sprite[] characterPortraits;  // 0: Martha neutral, 1: Martha happy, etc.
    
    [Header("Dialogue Display")]
    public TMP_Text dialogueText;
    public float textSpeed = 0.05f;
    
    [Header("Player Input")]
    public TMP_InputField playerInput;
    public Image sendButtonImage;
    
    [Header("Pixel Art UI Sprites")]
    public Sprite buttonNormal;
    public Sprite buttonPressed;
    
    [Header("Audio")]
    public AudioClip[] textBlips;  // Pixel art games use blip sounds
    public AudioSource audioSource;
    
    private bool isTyping = false;
    
    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }
    
    public void ShowDialogue(string characterName, Sprite portrait)
    {
        dialoguePanel.SetActive(true);
        characterNameText.text = characterName;
        characterPortrait.sprite = portrait;
        playerInput.text = "";
        playerInput.Select();
        
        GameStateMachine.Instance.ChangeState(GameState.InDialogue);
    }
    
    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
        GameStateMachine.Instance.ChangeState(GameState.Playing);
    }
    
    public IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        
        foreach (char c in text)
        {
            dialogueText.text += c;
            
            // Play random blip for each character (pixel art style)
            if (c != ' ' && textBlips.Length > 0)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(textBlips[Random.Range(0, textBlips.Length)]);
            }
            
            yield return new WaitForSeconds(textSpeed);
        }
        
        isTyping = false;
    }
    
    public void SetCharacterEmotion(int emotionIndex)
    {
        if (emotionIndex < characterPortraits.Length)
        {
            characterPortrait.sprite = characterPortraits[emotionIndex];
        }
    }
    
    // Skip to end of text if clicking during typewriter
    public void OnDialogueAreaClick()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            // Show full text immediately
            isTyping = false;
        }
    }
}
```

### 6.3 Pixel Art UI Layout

```
┌─────────────────────────────────────────────────────────────────┐
│ ┌─────────┐                                                     │
│ │         │  ┌─────────────────────────────────────────────────┐│
│ │ MARTHA  │  │ "I remember when you used to play that         ││
│ │ [image] │  │  guitar every Sunday morning..."               ││
│ │         │  │                                                 ││
│ └─────────┘  └─────────────────────────────────────────────────┘│
│              ┌─────────────────────────────────┐ ┌─────────────┐│
│              │ Type your response...           │ │   SEND      ││
│              └─────────────────────────────────┘ └─────────────┘│
└─────────────────────────────────────────────────────────────────┘

PIXEL ART DETAILS:
- Panel: 9-slice sprite with pixel border (brown wood or dark blue)
- Portrait: 48x48 or 64x64 pixel character
- Nameplate: Simple banner/ribbon sprite
- Buttons: Pixel art with pressed state
- Text: Pixel font (see below)
```

### 6.4 Pixel Fonts

```
FREE PIXEL FONTS:

1. "Press Start 2P" - Classic arcade
   https://fonts.google.com/specimen/Press+Start+2P

2. "VT323" - Terminal style
   https://fonts.google.com/specimen/VT323

3. "Pixelify Sans" - Modern readable pixel
   https://fonts.google.com/specimen/Pixelify+Sans

4. "DotGothic16" - Clean Japanese pixel
   https://fonts.google.com/specimen/DotGothic16

INSTALL IN UNITY:
1. Download .ttf file
2. Import to Assets/Fonts/
3. Create TextMeshPro font asset:
   Window > TextMeshPro > Font Asset Creator
4. Assign to TMP_Text components
```

---

## 7. Cursor + Unity Workflow

### 7.1 Setting Up Cursor with Unity

```
PROJECT STRUCTURE FOR CURSOR:

LastDay/
├── Assets/
│   └── Scripts/        ← Cursor edits these
├── .cursorrules        ← Custom rules for Cursor
├── .cursorignore       ← Ignore Unity meta files
└── CONTEXT.md          ← Project context for AI
```

**.cursorrules:**
```
# Unity C# Project Rules

## Code Style
- Use Unity's MonoBehaviour patterns
- Prefer [SerializeField] over public fields
- Use #region for organization
- Include XML documentation for public methods

## Unity Specifics
- Always null-check GetComponent results
- Use TryGetComponent when appropriate
- Avoid Find() in Update loops
- Use events over direct references

## Project Context
- This is a 2D point-and-click narrative game
- Using LLMUnity for AI dialogue
- Pixel art aesthetic (Stardew Valley style)
- Target platform: macOS (M4)

## File Locations
- Scripts: Assets/Scripts/
- Prefabs: Assets/Prefabs/
- Scenes: Assets/Scenes/
```

**.cursorignore:**
```
# Ignore Unity generated files
*.meta
*.unity
*.prefab
*.asset
*.mat
*.physicMaterial
Library/
Temp/
Logs/
obj/
Build/
```

**CONTEXT.md:**
```markdown
# Last Day - Development Context

## Project Overview
2D narrative game about an elderly man deciding on euthanasia.
One room, two NPCs (Martha wife, David phone friend), 3-4 memory objects.

## Current Focus
[Update daily with what you're working on]

## Known Issues
[List bugs for Cursor to help with]

## Architecture
- Singleton managers (GameManager, EventManager, DialogueUI)
- Event-based communication (GameEvents static class)
- State machine for game flow (GameStateMachine)

## Key Files
- DialogueUI.cs - Main UI controller
- LocalLLMManager.cs - AI integration
- InteractableObject2D.cs - Clickable objects
- PlayerMovement2D.cs - Player navigation
```

### 7.2 Cursor Workflow for Unity Bugs

**When you have a Unity-specific bug:**

```
1. GATHER CONTEXT
   - Copy the relevant script(s)
   - Describe the Unity setup (Inspector values, hierarchy)
   - Include error messages from Console

2. CREATE A CURSOR PROMPT
   "I'm working on a Unity 2D game. Here's my DialogueUI script:
   [paste script]
   
   Unity Setup:
   - DialoguePanel is a child of Canvas
   - PlayerInputField is TMP_InputField, anchored bottom
   - SendButton is UI Button, has OnClick → DialogueUI.OnSendClicked
   
   Problem:
   After sending a message, the input field loses focus and I can't type again.
   
   Error (if any):
   [paste error]"

3. ITERATE
   - Apply Cursor's fix
   - Test in Unity
   - If still broken, describe what changed
```

### 7.3 UI Debugging Without Cursor Access

Since Cursor can't see Unity's visual state, maintain a debug script:

```csharp
// UIDebugger.cs - Helps diagnose UI issues
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDebugger : MonoBehaviour
{
    [Header("References to Debug")]
    public TMP_InputField inputField;
    public Button sendButton;
    public GameObject dialoguePanel;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            PrintUIState();
        }
    }
    
    void PrintUIState()
    {
        Debug.Log("=== UI DEBUG STATE ===");
        Debug.Log($"Panel Active: {dialoguePanel.activeSelf}");
        Debug.Log($"InputField Interactable: {inputField.interactable}");
        Debug.Log($"InputField IsFocused: {inputField.isFocused}");
        Debug.Log($"InputField Text: '{inputField.text}'");
        Debug.Log($"Button Interactable: {sendButton.interactable}");
        Debug.Log($"EventSystem Selected: {UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.name}");
        Debug.Log("======================");
    }
    
    // Call this to dump state to a file Cursor can read
    [ContextMenu("Export UI State to File")]
    void ExportToFile()
    {
        string state = $@"
UI State Export - {System.DateTime.Now}

Hierarchy:
{GetHierarchy(dialoguePanel.transform, 0)}

States:
- dialoguePanel.activeSelf: {dialoguePanel.activeSelf}
- inputField.interactable: {inputField.interactable}
- inputField.isFocused: {inputField.isFocused}
- sendButton.interactable: {sendButton.interactable}
";
        System.IO.File.WriteAllText("ui_state_debug.txt", state);
        Debug.Log("Exported to ui_state_debug.txt");
    }
    
    string GetHierarchy(Transform t, int depth)
    {
        string indent = new string(' ', depth * 2);
        string result = $"{indent}- {t.name} (active: {t.gameObject.activeSelf})\n";
        
        foreach (Transform child in t)
        {
            result += GetHierarchy(child, depth + 1);
        }
        
        return result;
    }
}
```

---

## 8. Cost & Performance Estimates

### 8.1 AI Tool Costs (10-Day Sprint)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          AI TOOL COST ESTIMATE                               │
├────────────────────────┬──────────────┬─────────────┬───────────────────────┤
│ Tool                   │ Usage        │ Cost/Unit   │ Estimated Total       │
├────────────────────────┼──────────────┼─────────────┼───────────────────────┤
│ ASSET GENERATION                                                             │
├────────────────────────┼──────────────┼─────────────┼───────────────────────┤
│ Midjourney             │ ~100 images  │ $10/mo      │ $10.00 (Basic plan)   │
│ Remove.bg              │ ~20 images   │ Free tier   │ $0.00                 │
│ Leonardo.ai            │ ~30 images   │ Free tier   │ $0.00                 │
│                        │              │             │                       │
├────────────────────────┼──────────────┼─────────────┼───────────────────────┤
│ AUDIO                                                                        │
├────────────────────────┼──────────────┼─────────────┼───────────────────────┤
│ Suno.ai                │ ~10 tracks   │ Free tier   │ $0.00                 │
│ ElevenLabs             │ (optional)   │ Free tier   │ $0.00                 │
│ Freesound.org          │ ~10 SFX      │ Free        │ $0.00                 │
│                        │              │             │                       │
├────────────────────────┼──────────────┼─────────────┼───────────────────────┤
│ CODE ASSISTANCE                                                              │
├────────────────────────┼──────────────┼─────────────┼───────────────────────┤
│ Claude.ai (Web)        │ Daily use    │ Free tier   │ $0.00                 │
│ Claude Pro             │ (optional)   │ $20/mo      │ $20.00 (if needed)    │
│ Cursor                 │ Daily use    │ Free tier   │ $0.00                 │
│ Cursor Pro             │ (optional)   │ $20/mo      │ $20.00 (if needed)    │
│ GitHub Copilot         │ (if used)    │ $10/mo      │ $10.00                │
│                        │              │             │                       │
├────────────────────────┼──────────────┼─────────────┼───────────────────────┤
│ LLM (IN-GAME)                                                                │
├────────────────────────┼──────────────┼─────────────┼───────────────────────┤
│ LLMUnity               │ Package      │ Free        │ $0.00                 │
│ Phi-3-mini model       │ Download     │ Free        │ $0.00                 │
│ Ollama                 │ (optional)   │ Free        │ $0.00                 │
│                        │              │             │                       │
├────────────────────────┼──────────────┼─────────────┼───────────────────────┤
│ UNITY                                                                        │
├────────────────────────┼──────────────┼─────────────┼───────────────────────┤
│ Unity Personal         │ Full project │ Free        │ $0.00                 │
│ DOTween (animation)    │ Package      │ Free        │ $0.00                 │
│                        │              │             │                       │
├────────────────────────┴──────────────┴─────────────┼───────────────────────┤
│                                                     │                       │
│ MINIMUM BUDGET (all free tiers)                     │ $10.00                │
│ RECOMMENDED BUDGET (Midjourney + Claude Pro)        │ $30.00                │
│ COMFORTABLE BUDGET (add Cursor Pro)                 │ $50.00                │
│                                                     │                       │
└─────────────────────────────────────────────────────┴───────────────────────┘
```

### 8.2 What You Get with Free Tiers

```
MIDJOURNEY (Required - $10):
- 200 images/month on Basic
- Enough for all assets with iteration

CLAUDE.AI FREE:
- ~30-50 messages/day
- Good for planning, debugging, prompts
- May need Pro if heavy usage ($20)

CURSOR FREE:
- 2000 completions/month
- Should be enough for 10-day sprint
- Pro if you're doing a lot of AI edits ($20)

SUNO.AI FREE:
- 50 credits/day (5-10 songs)
- More than enough for your demo

REMOVE.BG FREE:
- 1 free/month at full quality
- Low-res previews unlimited
- Use Rembg locally instead (free unlimited)

LEONARDO.AI FREE:
- 150 tokens/day
- ~30 images/day
- Good backup for Midjourney
```

### 8.3 Model Performance on M4 16GB

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   M4 MacBook Pro 16GB - LLM PERFORMANCE                      │
├─────────────────────────┬──────────┬────────────┬───────────┬───────────────┤
│ Model                   │ Size     │ Tokens/sec │ 50 tokens │ Quality       │
├─────────────────────────┼──────────┼────────────┼───────────┼───────────────┤
│ Qwen2.5-0.5B-Instruct   │ 400 MB   │ ~80 t/s    │ 0.6 sec   │ Basic         │
│ Llama-3.2-1B-Instruct   │ 700 MB   │ ~60 t/s    │ 0.8 sec   │ Good          │
│ Gemma-2-2B-Instruct     │ 1.5 GB   │ ~40 t/s    │ 1.3 sec   │ Good+         │
│ Phi-3-mini-Instruct     │ 2.2 GB   │ ~30 t/s    │ 1.7 sec   │ Great         │ ← RECOMMENDED
│ Llama-3.2-3B-Instruct   │ 2.0 GB   │ ~25 t/s    │ 2.0 sec   │ Great         │
│ Mistral-7B-Instruct     │ 4.1 GB   │ ~15 t/s    │ 3.3 sec   │ Excellent     │
├─────────────────────────┴──────────┴────────────┴───────────┴───────────────┤
│                                                                              │
│ RECOMMENDATION FOR YOUR GAME:                                               │
│                                                                              │
│ Primary: Phi-3-mini-4k-instruct-q4_K_M.gguf (2.2 GB)                        │
│ - Best balance of quality and speed                                        │
│ - ~1.5-2 seconds for typical response                                      │
│ - Follows character prompts well                                           │
│ - Good at staying in character                                             │
│                                                                              │
│ Fallback: Llama-3.2-1B-Instruct-q4_K_M.gguf (700 MB)                       │
│ - Faster if Phi-3 feels too slow                                           │
│ - Less nuanced but still good                                              │
│                                                                              │
│ Download from: https://huggingface.co/models?search=gguf                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 8.4 Optimizing LLM Performance

```csharp
// LLMPerformanceSettings.cs
public class LLMPerformanceSettings
{
    // CONTEXT SIZE
    // Lower = faster, but less memory of conversation
    public int contextSize = 2048;  // Default 4096, reduce for speed
    
    // MAX TOKENS
    // Limit response length for speed
    public int maxTokens = 80;  // Enough for 2-3 sentences
    
    // TEMPERATURE
    // Lower = faster (less sampling), but less creative
    public float temperature = 0.7f;  // Good balance
    
    // THREADS
    // M4 has 10 cores, use most of them
    public int threads = 8;
    
    // GPU LAYERS
    // Metal acceleration on M4
    public int gpuLayers = 32;  // All layers on GPU
}
```

### 8.5 Music Length Recommendations

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           MUSIC TRACK LENGTHS                                │
├─────────────────────────┬─────────────┬─────────────────────────────────────┤
│ Track                   │ Length      │ Notes                               │
├─────────────────────────┼─────────────┼─────────────────────────────────────┤
│ Main Ambient Loop       │ 2:00-3:00   │ Seamless loop, plays throughout    │
│                         │             │ Use Audacity to find loop point    │
│                         │             │                                     │
│ Ending - Signed         │ 0:45-1:00   │ Plays once during fade/quote       │
│                         │             │ Melancholic, peaceful resolution   │
│                         │             │                                     │
│ Ending - Torn           │ 0:45-1:00   │ Plays once during fade/quote       │
│                         │             │ Hopeful, tender                    │
│                         │             │                                     │
│ Phone Ring              │ 0:05-0:10   │ Loopable vintage phone sound       │
│                         │             │ Get from Freesound, not Suno       │
│                         │             │                                     │
│ Memory Trigger          │ 0:15-0:30   │ Optional: soft swell when          │
│ (optional)              │             │ player triggers a memory           │
│                         │             │                                     │
├─────────────────────────┴─────────────┴─────────────────────────────────────┤
│ TOTAL MUSIC NEEDED: ~5-6 minutes of generated music                         │
│ SUNO FREE TIER: More than enough (50 credits/day = ~10 songs)              │
└─────────────────────────────────────────────────────────────────────────────┘

SUNO PROMPTS:

Main Ambient:
"melancholic ambient piano, gentle sparse notes, warm nostalgic feeling,
late afternoon, quiet room, soft strings pad in background, 
contemplative, 65 bpm, no drums, loopable"

Ending Signed:
"peaceful piano resolution, bittersweet acceptance, soft fade,
gentle finality, warm but sad, cinematic, 60 bpm"

Ending Torn:
"hopeful ambient piano, gentle new beginning feeling, morning light,
tender, quiet strength, cinematic, 68 bpm"
```

---

## Quick Reference: Complete Tech Stack

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         FINAL TECH STACK SUMMARY                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ENGINE:           Unity 2022.3 LTS (2D, Personal License)                  │
│  LANGUAGE:         C# 9.0                                                   │
│  IDE:              Cursor (primary) + VS Code (fallback)                    │
│                                                                              │
│  AI CODE:          Claude Code CLI, Claude.ai, Cursor AI                    │
│  AI ART:           Midjourney ($10), Remove.bg/Rembg (free)                │
│  AI AUDIO:         Suno.ai (free), Freesound.org (free)                    │
│  AI IN-GAME:       LLMUnity + Phi-3-mini (2.2GB local model)               │
│                                                                              │
│  ART STYLE:        Pixel art, 32x32 sprites, Stardew Valley-like           │
│  RESOLUTION:       Target 1920x1080, pixel-perfect rendering               │
│  FONT:             Pixelify Sans or Press Start 2P                         │
│                                                                              │
│  MOVEMENT:         Simple lerp (no pathfinding needed)                     │
│  DIALOGUE:         Custom UI with TMP_InputField                           │
│  STATE:            GameStateMachine (enum-based)                           │
│  EVENTS:           Static GameEvents class                                  │
│                                                                              │
│  TARGET PLATFORM:  macOS (M4), Windows secondary                           │
│  ESTIMATED SIZE:   ~500MB (mostly the LLM model)                           │
│                                                                              │
│  BUDGET:           $10-30 (Midjourney + optional Claude Pro)               │
│  TIMELINE:         10 days, 2-person team                                  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```
