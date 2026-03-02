# Last Day - Complete Engineering Schematic v3 (Final)

## Table of Contents
1. [Game Overview](#1-game-overview)
2. [System Architecture](#2-system-architecture)
3. [Complete File Structure](#3-complete-file-structure)
4. [Core Systems](#4-core-systems)
5. [Character Animation System](#5-character-animation-system)
6. [Pathfinding & Movement](#6-pathfinding--movement)
7. [Dialogue & AI Integration](#7-dialogue--ai-integration)
8. [Interactive Objects](#8-interactive-objects)
9. [Game Flow & State Machine](#9-game-flow--state-machine)
10. [UI System](#10-ui-system)
11. [Audio System](#11-audio-system)
12. [Asset Specifications](#12-asset-specifications)
13. [Scene Setup Guide](#13-scene-setup-guide)
14. [Build & Deployment](#14-build--deployment)

---

## 1. Game Overview

### 1.1 Concept Summary

```
TITLE: Last Day

GENRE: 2D Point-and-Click Narrative Game

PREMISE: 
An elderly man named Robert, suffering from ALS, must decide whether 
to sign papers for medical assistance in dying. Through conversations 
with his wife Martha and phone calls with his best friend David, 
players explore memories and perspectives on this deeply personal choice.

CORE MECHANICS:
- Point-and-click movement with A* pathfinding
- AI-generated dialogue (local LLM)
- Memory triggers via object interaction
- Binary ending choice

TECHNICAL SHOWCASE:
- Local LLM integration (Phi-3-mini via LLMUnity)
- Context-aware AI responses based on triggered memories
- Pixel art aesthetic (Stardew Valley inspired)

SCOPE:
- 1 room
- 2 NPCs (Martha in-room, David via phone)
- 3-4 memory objects
- 10-day development timeline
- 2-person team
```

### 1.2 Art Style

```
VISUAL STYLE:
- Pixel art, 32x48 character sprites
- Warm, nostalgic color palette
- Stardew Valley / cozy RPG aesthetic
- Limited palette (16-32 colors per asset)
- Clean pixels, no anti-aliasing

RESOLUTION:
- Reference: 480×270 (16:9)
- Scales to: 1920×1080 (4x)
- Pixels Per Unit: 32

CHARACTER ANIMATION:
- 4-direction movement (up, down, left, right)
- 4-frame walk cycle per direction
- 3-frame idle/breathing cycle per direction
- Subtle code-based idle movement (breathing, sway)
```

---

## 2. System Architecture

### 2.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              UNITY APPLICATION                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         MANAGER LAYER                                │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌─────────────┐│   │
│  │  │ GameManager  │ │ EventManager │ │ AudioManager │ │ FadeManager ││   │
│  │  │  (Singleton) │ │  (Singleton) │ │  (Singleton) │ │ (Singleton) ││   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘ └─────────────┘│   │
│  │  ┌──────────────┐ ┌──────────────┐                                  │   │
│  │  │GameState     │ │ LocalLLM     │                                  │   │
│  │  │  Machine     │ │   Manager    │                                  │   │
│  │  └──────────────┘ └──────────────┘                                  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                         │
│                                    ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                       GAMEPLAY LAYER                                 │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │   │
│  │  │   Player     │ │  Pathfinder  │ │   Click      │                 │   │
│  │  │  Controller  │◄┤              │◄┤   Handler    │                 │   │
│  │  └──────┬───────┘ └──────────────┘ └──────────────┘                 │   │
│  │         │                                                            │   │
│  │         ▼                                                            │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │   │
│  │  │  Character   │ │  Subtle Idle │ │    NPC       │                 │   │
│  │  │  Animator    │ │   Movement   │ │  Controller  │                 │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                         │
│                                    ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      INTERACTION LAYER                               │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │   │
│  │  │ Interactable │ │   Document   │ │    Phone     │                 │   │
│  │  │   Object2D   │ │  Interaction │ │ Interaction  │                 │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                         │
│                                    ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                          UI LAYER                                    │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌─────────────┐│   │
│  │  │ DialogueUI   │ │  PhoneUI     │ │ DecisionUI   │ │  EndScreen  ││   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘ └─────────────┘│   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                           LOCAL LLM (Embedded)                               │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  LLMUnity + Phi-3-mini-4k-instruct (2.2GB, runs on Metal/CUDA)       │  │
│  │  - Context: Character prompts + triggered memories + recent events   │  │
│  │  - Output: 50-100 tokens, ~2 second latency on M4 Mac                │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Data Flow

```
PLAYER CLICK → INTERACTION FLOW:
════════════════════════════════

1. Player clicks on screen
   │
   ▼
2. ClickToMoveHandler detects click
   ├── Check: Is click on Interactable? → MoveToAndInteract()
   └── Check: Is click on Walkable area? → MoveTo()
   │
   ▼
3. SimplePathfinder.FindPath(start, destination)
   │ Returns: List<Vector2> waypoints
   │
   ▼
4. PlayerController2D follows path
   ├── Updates CharacterAnimator (walk animation)
   ├── Disables SubtleIdleMovement
   └── On arrival: Re-enables idle, triggers callback
   │
   ▼
5. If interacting with object:
   ├── InteractableObject2D.OnInteract()
   ├── EventManager.PublishEvent(memoryId)
   └── DialogueUI.ShowDialogue()
   │
   ▼
6. Player types message
   │
   ▼
7. DialogueUI → LocalLLMManager
   ├── Build context (memories, recent events)
   ├── Select character prompt (Martha/David)
   └── Generate response
   │
   ▼
8. Response displayed with typewriter effect
   │
   ▼
9. Player closes dialogue → Resume exploration
```

---

## 3. Complete File Structure

```
LastDay/
│
├── Assets/
│   │
│   ├── Scenes/
│   │   ├── MainRoom.unity              # Primary game scene
│   │   ├── TitleScreen.unity           # Optional: title/menu
│   │   └── Loading.unity               # Optional: model download screen
│   │
│   ├── Scripts/
│   │   │
│   │   ├── Core/
│   │   │   ├── GameManager.cs          # Game initialization, scene management
│   │   │   ├── GameStateMachine.cs     # State management (Playing, InDialogue, etc.)
│   │   │   ├── GameEvents.cs           # Static event definitions
│   │   │   ├── EventManager.cs         # Event publishing, memory tracking
│   │   │   └── FadeManager.cs          # Screen fade transitions
│   │   │
│   │   ├── Player/
│   │   │   ├── PlayerController2D.cs   # Movement, pathfinding integration
│   │   │   ├── CharacterAnimator.cs    # Animation state management
│   │   │   ├── SubtleIdleMovement.cs   # Breathing, sway, tremor effects
│   │   │   └── ClickToMoveHandler.cs   # Mouse input → movement commands
│   │   │
│   │   ├── Pathfinding/
│   │   │   └── SimplePathfinder.cs     # A* grid-based pathfinding
│   │   │
│   │   ├── NPC/
│   │   │   ├── NPCController.cs        # NPC idle behavior
│   │   │   └── NPCIdleMovement.cs      # Martha's subtle movements
│   │   │
│   │   ├── Interaction/
│   │   │   ├── InteractableObject2D.cs # Base class for clickable objects
│   │   │   ├── MemoryObject.cs         # Objects that trigger memories
│   │   │   ├── DocumentInteraction.cs  # End-game document (sign/tear)
│   │   │   └── PhoneInteraction.cs     # Phone call trigger
│   │   │
│   │   ├── Dialogue/
│   │   │   ├── DialogueUI.cs           # Main dialogue panel controller
│   │   │   ├── DialogueManager.cs      # Coordinates dialogue flow
│   │   │   ├── LocalLLMManager.cs      # LLMUnity integration
│   │   │   ├── CharacterPrompts.cs     # System prompts for Martha/David
│   │   │   ├── MemoryContext.cs        # Memory story data
│   │   │   └── ModelDownloader.cs      # First-run model download
│   │   │
│   │   ├── UI/
│   │   │   ├── PixelDialogueUI.cs      # Pixel art styled dialogue
│   │   │   ├── PhoneUI.cs              # Phone call variant UI
│   │   │   ├── DecisionUI.cs           # Sign/Tear choice panel
│   │   │   ├── EndScreen.cs            # Ending quote display
│   │   │   ├── ThinkingIndicator.cs    # AI "thinking" animation
│   │   │   ├── InteractionPrompt.cs    # "Click to examine" prompts
│   │   │   └── DownloadProgressUI.cs   # Model download progress
│   │   │
│   │   ├── Audio/
│   │   │   ├── AudioManager.cs         # Singleton audio controller
│   │   │   ├── MusicController.cs      # Background music, loops
│   │   │   └── SFXController.cs        # Sound effects
│   │   │
│   │   ├── Camera/
│   │   │   └── CameraController2D.cs   # Zoom for document interaction
│   │   │
│   │   ├── Data/
│   │   │   ├── MemoryData.cs           # ScriptableObject definition
│   │   │   └── GameSettings.cs         # ScriptableObject for settings
│   │   │
│   │   └── Utilities/
│   │       ├── Singleton.cs            # Generic singleton base class
│   │       └── Extensions.cs           # Helper extension methods
│   │
│   ├── Prefabs/
│   │   │
│   │   ├── Characters/
│   │   │   ├── Robert.prefab           # Player character
│   │   │   └── Martha.prefab           # NPC wife
│   │   │
│   │   ├── Interactables/
│   │   │   ├── MemoryObject.prefab     # Base memory object
│   │   │   ├── WeddingPhoto.prefab     # Configured memory object
│   │   │   ├── IcePicks.prefab
│   │   │   ├── Guitar.prefab
│   │   │   ├── Phone.prefab
│   │   │   └── Document.prefab
│   │   │
│   │   ├── UI/
│   │   │   ├── DialoguePanel.prefab
│   │   │   ├── PhonePanel.prefab
│   │   │   ├── DecisionPanel.prefab
│   │   │   ├── EndScreen.prefab
│   │   │   ├── InteractionPrompt.prefab
│   │   │   └── ClickIndicator.prefab   # Visual feedback on click
│   │   │
│   │   └── Effects/
│   │       ├── HighlightGlow.prefab    # Object hover glow
│   │       └── WalkDust.prefab         # Optional: subtle walk particles
│   │
│   ├── Art/
│   │   │
│   │   ├── Characters/
│   │   │   ├── Robert/
│   │   │   │   ├── robert_walk.png     # 128×192 (4 dirs × 4 frames)
│   │   │   │   ├── robert_idle.png     # 96×192 (4 dirs × 3 frames)
│   │   │   │   └── robert_portraits.png # 192×64 (3 expressions)
│   │   │   │
│   │   │   └── Martha/
│   │   │       ├── martha_idle.png     # 96×144 (3 dirs × 3 frames)
│   │   │       └── martha_portraits.png # 256×64 (4 expressions)
│   │   │
│   │   ├── Environment/
│   │   │   ├── room_background.png     # 480×270 or 960×540
│   │   │   ├── furniture_desk.png
│   │   │   ├── furniture_bookshelf.png
│   │   │   ├── furniture_chair.png
│   │   │   └── window_light.png        # Additive light overlay
│   │   │
│   │   ├── Objects/
│   │   │   ├── wedding_photo.png       # 32×32 + glow variant
│   │   │   ├── wedding_photo_glow.png
│   │   │   ├── ice_picks.png
│   │   │   ├── ice_picks_glow.png
│   │   │   ├── guitar.png
│   │   │   ├── guitar_glow.png
│   │   │   ├── phone.png
│   │   │   ├── phone_glow.png
│   │   │   ├── document.png
│   │   │   └── document_glow.png
│   │   │
│   │   ├── UI/
│   │   │   ├── dialogue_panel.png      # 9-slice panel border
│   │   │   ├── phone_panel.png
│   │   │   ├── button_normal.png
│   │   │   ├── button_pressed.png
│   │   │   ├── input_field_bg.png
│   │   │   └── name_plate.png          # Character name banner
│   │   │
│   │   └── Effects/
│   │       └── click_indicator.png     # Click position feedback
│   │
│   ├── Audio/
│   │   │
│   │   ├── Music/
│   │   │   ├── ambient_loop.wav        # 2-3 min, seamless loop
│   │   │   ├── ending_signed.wav       # 45-60 sec
│   │   │   └── ending_torn.wav         # 45-60 sec
│   │   │
│   │   └── SFX/
│   │       ├── footstep_1.wav
│   │       ├── footstep_2.wav
│   │       ├── click.wav
│   │       ├── hover.wav
│   │       ├── dialogue_blip.wav       # Text typewriter sound
│   │       ├── phone_ring.wav
│   │       ├── phone_pickup.wav
│   │       ├── paper_rustle.wav
│   │       └── paper_tear.wav
│   │
│   ├── Fonts/
│   │   ├── PixelifySans-Regular.ttf
│   │   ├── PixelifySans-Bold.ttf
│   │   └── PixelifySans SDF.asset      # TextMeshPro font asset
│   │
│   ├── Animation/
│   │   │
│   │   ├── Controllers/
│   │   │   ├── RobertAnimator.controller
│   │   │   └── MarthaAnimator.controller
│   │   │
│   │   └── Clips/
│   │       ├── Robert/
│   │       │   ├── Robert_Idle_Down.anim
│   │       │   ├── Robert_Idle_Up.anim
│   │       │   ├── Robert_Idle_Left.anim
│   │       │   ├── Robert_Idle_Right.anim
│   │       │   ├── Robert_Walk_Down.anim
│   │       │   ├── Robert_Walk_Up.anim
│   │       │   ├── Robert_Walk_Left.anim
│   │       │   └── Robert_Walk_Right.anim
│   │       │
│   │       └── Martha/
│   │           ├── Martha_Idle_Down.anim
│   │           ├── Martha_Idle_Left.anim
│   │           └── Martha_Idle_Right.anim
│   │
│   ├── Data/
│   │   │
│   │   ├── Memories/
│   │   │   ├── WeddingPhotoMemory.asset    # ScriptableObject
│   │   │   ├── IcePicksMemory.asset
│   │   │   └── GuitarMemory.asset
│   │   │
│   │   └── Settings/
│   │       └── GameSettings.asset
│   │
│   ├── Plugins/
│   │   └── LLMUnity/                   # LLMUnity package files
│   │
│   ├── StreamingAssets/
│   │   └── Models/                     # LLM model downloaded here at runtime
│   │       └── .gitkeep
│   │
│   └── Resources/
│       └── Dialogue/
│           └── FallbackResponses.json  # Backup if LLM fails
│
├── Packages/
│   └── manifest.json                   # Unity packages (TMP, 2D Pixel Perfect, etc.)
│
├── ProjectSettings/
│   ├── ProjectSettings.asset
│   ├── QualitySettings.asset
│   ├── TagManager.asset                # Layers: Walkable, Obstacles, Interactables
│   └── InputManager.asset
│
├── Tools/                              # Development tools (not in Unity)
│   ├── asset_generator.py              # Batch image generation
│   ├── music_generator.py              # Music generation prompts
│   └── generated_assets/               # Output from generation scripts
│
├── Documentation/
│   ├── ENGINEERING_SCHEMATIC.md        # This document
│   ├── ANIMATION_GUIDE.md
│   ├── AI_DEV_GUIDE.md
│   └── UNITY_PATTERNS.md
│
├── .gitignore
├── .cursorrules                        # Cursor AI configuration
├── CONTEXT.md                          # Project context for AI tools
└── README.md
```

---

## 4. Core Systems

### 4.1 GameManager.cs

```csharp
// GameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastDay.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("References")]
        public ModelDownloader modelDownloader;
        public LocalLLMManager llmManager;
        
        [Header("Game State")]
        public bool isGameStarted = false;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        void Start()
        {
            InitializeGame();
        }
        
        async void InitializeGame()
        {
            // Check if model needs downloading
            if (!modelDownloader.IsModelDownloaded)
            {
                await modelDownloader.DownloadModel();
            }
            
            // Initialize LLM
            llmManager.Initialize(modelDownloader.ModelPath);
            
            // Start the game
            StartGame();
        }
        
        public void StartGame()
        {
            isGameStarted = true;
            GameStateMachine.Instance.ChangeState(GameState.Playing);
            AudioManager.Instance.PlayMusic("ambient_loop");
        }
        
        public void EndGame(bool signed)
        {
            GameStateMachine.Instance.ChangeState(GameState.Ending);
            
            if (signed)
            {
                AudioManager.Instance.PlayMusic("ending_signed");
                FindObjectOfType<EndScreen>().ShowSignedEnding();
            }
            else
            {
                AudioManager.Instance.PlayMusic("ending_torn");
                FindObjectOfType<EndScreen>().ShowTornEnding();
            }
        }
        
        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
```

### 4.2 GameStateMachine.cs

```csharp
// GameStateMachine.cs
using UnityEngine;
using System;

namespace LastDay.Core
{
    public enum GameState
    {
        Loading,        // Model downloading
        Playing,        // Normal exploration
        InDialogue,     // Talking to Martha
        PhoneCall,      // Talking to David
        Decision,       // Document choice panel open
        Ending          // End screen showing
    }
    
    public class GameStateMachine : MonoBehaviour
    {
        public static GameStateMachine Instance { get; private set; }
        
        public GameState CurrentState { get; private set; } = GameState.Loading;
        
        public event Action<GameState, GameState> OnStateChanged;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        public bool ChangeState(GameState newState)
        {
            if (CurrentState == newState) return false;
            
            if (!IsValidTransition(CurrentState, newState))
            {
                Debug.LogWarning($"Invalid state transition: {CurrentState} → {newState}");
                return false;
            }
            
            GameState oldState = CurrentState;
            CurrentState = newState;
            
            Debug.Log($"[GameState] {oldState} → {newState}");
            OnStateChanged?.Invoke(oldState, newState);
            
            return true;
        }
        
        private bool IsValidTransition(GameState from, GameState to)
        {
            return (from, to) switch
            {
                (GameState.Loading, GameState.Playing) => true,
                (GameState.Playing, GameState.InDialogue) => true,
                (GameState.Playing, GameState.PhoneCall) => true,
                (GameState.Playing, GameState.Decision) => true,
                (GameState.InDialogue, GameState.Playing) => true,
                (GameState.PhoneCall, GameState.Playing) => true,
                (GameState.PhoneCall, GameState.InDialogue) => true,
                (GameState.Decision, GameState.Playing) => true,
                (GameState.Decision, GameState.Ending) => true,
                _ => false
            };
        }
        
        public bool CanPlayerMove => CurrentState == GameState.Playing;
        public bool CanInteract => CurrentState == GameState.Playing;
    }
}
```

### 4.3 EventManager.cs

```csharp
// EventManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;

namespace LastDay.Core
{
    [Serializable]
    public class GameEvent
    {
        public string eventType;      // "gaze_complete", "interact", "dialogue"
        public string objectId;
        public string memoryId;
        public float timestamp;
        
        public GameEvent(string type, string objId = "", string memId = "")
        {
            eventType = type;
            objectId = objId;
            memoryId = memId;
            timestamp = Time.time;
        }
    }
    
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }
        
        [Header("Game Progress")]
        public List<string> triggeredMemories = new List<string>();
        public bool hasAskedForHelp = false;
        public bool documentUnlocked = false;
        public bool phoneHasRung = false;
        
        [Header("Settings")]
        public int memoriesRequiredForDocument = 2;
        
        // Recent events for AI context
        private Queue<GameEvent> recentEvents = new Queue<GameEvent>();
        private const int MAX_RECENT_EVENTS = 5;
        
        // Events
        public static event Action<string> OnMemoryTriggered;
        public static event Action OnDocumentUnlocked;
        public static event Action OnPhoneRing;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        public void PublishEvent(GameEvent evt)
        {
            // Add to recent events queue
            recentEvents.Enqueue(evt);
            if (recentEvents.Count > MAX_RECENT_EVENTS)
                recentEvents.Dequeue();
            
            // Handle memory triggers
            if (!string.IsNullOrEmpty(evt.memoryId) && !triggeredMemories.Contains(evt.memoryId))
            {
                triggeredMemories.Add(evt.memoryId);
                OnMemoryTriggered?.Invoke(evt.memoryId);
                
                Debug.Log($"[Event] Memory triggered: {evt.memoryId} (Total: {triggeredMemories.Count})");
                
                CheckDocumentUnlock();
                CheckPhoneTrigger();
            }
        }
        
        private void CheckDocumentUnlock()
        {
            if (!documentUnlocked && triggeredMemories.Count >= memoriesRequiredForDocument)
            {
                documentUnlocked = true;
                OnDocumentUnlocked?.Invoke();
                Debug.Log("[Event] Document unlocked!");
            }
        }
        
        private void CheckPhoneTrigger()
        {
            if (!phoneHasRung && triggeredMemories.Count >= 2)
            {
                phoneHasRung = true;
                OnPhoneRing?.Invoke();
                Debug.Log("[Event] Phone will ring!");
            }
        }
        
        public void SetAskedForHelp()
        {
            hasAskedForHelp = true;
        }
        
        /// <summary>
        /// Build context string for AI prompt
        /// </summary>
        public string BuildContextString()
        {
            var context = new System.Text.StringBuilder();
            
            // Recent events
            foreach (var evt in recentEvents)
            {
                if (evt.eventType == "gaze_complete")
                    context.AppendLine($"Robert looked at the {evt.objectId}.");
                else if (evt.eventType == "interact")
                    context.AppendLine($"Robert examined the {evt.objectId}.");
            }
            
            // Game state
            if (!hasAskedForHelp)
                context.AppendLine("Robert hasn't asked for help getting up yet.");
            
            if (documentUnlocked)
                context.AppendLine("The document is now available for Robert to sign.");
            
            return context.ToString();
        }
        
        public List<GameEvent> GetRecentEvents() => new List<GameEvent>(recentEvents);
    }
}
```

### 4.4 FadeManager.cs

```csharp
// FadeManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace LastDay.Core
{
    public class FadeManager : MonoBehaviour
    {
        public static FadeManager Instance { get; private set; }
        
        [Header("References")]
        public Image fadeImage;
        public CanvasGroup fadeCanvasGroup;
        
        [Header("Settings")]
        public float defaultFadeDuration = 1.5f;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Start fully transparent
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeImage.raycastTarget = false;
            }
        }
        
        public async Task FadeOut(float duration = -1)
        {
            if (duration < 0) duration = defaultFadeDuration;
            
            fadeImage.raycastTarget = true;
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                await Task.Yield();
            }
            
            fadeCanvasGroup.alpha = 1f;
        }
        
        public async Task FadeIn(float duration = -1)
        {
            if (duration < 0) duration = defaultFadeDuration;
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                await Task.Yield();
            }
            
            fadeCanvasGroup.alpha = 0f;
            fadeImage.raycastTarget = false;
        }
    }
}
```

---

## 5. Character Animation System

### 5.1 Sprite Sheet Specifications

```
ROBERT (Player Character)
═════════════════════════

Walk Sprite Sheet: robert_walk.png
├── Size: 128×192 pixels
├── Grid: 4 columns × 4 rows
├── Cell size: 32×48 pixels
├── Layout:
│   Row 0: Walk Down  (frames 0-3)
│   Row 1: Walk Left  (frames 0-3)
│   Row 2: Walk Right (frames 0-3, or mirror left)
│   Row 3: Walk Up    (frames 0-3)
└── Animation: 4 frames at 0.15 sec/frame = 0.6 sec cycle

Idle Sprite Sheet: robert_idle.png
├── Size: 96×192 pixels
├── Grid: 3 columns × 4 rows
├── Cell size: 32×48 pixels
├── Layout:
│   Row 0: Idle Down  (frames 0-2, breathing)
│   Row 1: Idle Left  (frames 0-2)
│   Row 2: Idle Right (frames 0-2)
│   Row 3: Idle Up    (frames 0-2)
└── Animation: 3 frames at 0.5 sec/frame = 1.5 sec cycle

Portrait Sheet: robert_portraits.png
├── Size: 192×64 pixels (3 × 64×64)
├── Expressions: Neutral, Tired, Remembering
└── Used in: Dialogue UI (optional)


MARTHA (NPC)
════════════

Idle Sprite Sheet: martha_idle.png
├── Size: 96×144 pixels
├── Grid: 3 columns × 3 rows
├── Cell size: 32×48 pixels
├── Layout:
│   Row 0: Idle Down/Front (frames 0-2)
│   Row 1: Idle Left (frames 0-2)
│   Row 2: Idle Right (frames 0-2)
└── Animation: 3 frames at 0.5 sec/frame

Portrait Sheet: martha_portraits.png
├── Size: 256×64 pixels (4 × 64×64)
├── Expressions: Neutral, Sad, Hopeful, Concerned
└── Used in: Dialogue UI
```

### 5.2 CharacterAnimator.cs

```csharp
// CharacterAnimator.cs
using UnityEngine;

namespace LastDay.Player
{
    public class CharacterAnimator : MonoBehaviour
    {
        [Header("References")]
        public Animator animator;
        public SpriteRenderer spriteRenderer;
        
        [Header("Settings")]
        public bool useSpriteMirroring = true;  // Mirror left sprite for right
        
        // Animator parameter hashes (performance optimization)
        private static readonly int IsWalking = Animator.StringToHash("IsWalking");
        private static readonly int DirectionX = Animator.StringToHash("DirectionX");
        private static readonly int DirectionY = Animator.StringToHash("DirectionY");
        
        private Vector2 currentDirection = Vector2.down;
        
        public Vector2 CurrentDirection => currentDirection;
        
        public void SetMoving(bool isMoving, Vector2 moveDirection)
        {
            animator.SetBool(IsWalking, isMoving);
            
            if (moveDirection != Vector2.zero)
            {
                SetDirection(moveDirection);
            }
        }
        
        public void SetDirection(Vector2 direction)
        {
            // Determine primary direction (4-way)
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                // Horizontal
                currentDirection = new Vector2(Mathf.Sign(direction.x), 0);
            }
            else
            {
                // Vertical
                currentDirection = new Vector2(0, Mathf.Sign(direction.y));
            }
            
            animator.SetFloat(DirectionX, currentDirection.x);
            animator.SetFloat(DirectionY, currentDirection.y);
            
            // Sprite mirroring for left/right
            if (useSpriteMirroring && Mathf.Abs(currentDirection.x) > 0)
            {
                spriteRenderer.flipX = currentDirection.x < 0;
            }
        }
        
        public void FacePosition(Vector2 targetPosition)
        {
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            SetDirection(direction);
            animator.SetBool(IsWalking, false);
        }
    }
}
```

### 5.3 SubtleIdleMovement.cs

```csharp
// SubtleIdleMovement.cs
using UnityEngine;

namespace LastDay.Player
{
    public class SubtleIdleMovement : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The sprite transform to animate (usually child of this object)")]
        public Transform spriteTransform;
        
        [Header("Breathing")]
        [Tooltip("Scale change for breathing effect (0.015 = 1.5%)")]
        public float breathingAmount = 0.015f;
        [Tooltip("Breathing cycle speed (0.4 = one breath per 2.5 seconds)")]
        public float breathingSpeed = 0.4f;
        
        [Header("Sway")]
        [Tooltip("Horizontal sway amount in units")]
        public float swayAmount = 0.003f;
        [Tooltip("Sway cycle speed")]
        public float swaySpeed = 0.2f;
        
        [Header("Elderly Effects")]
        [Tooltip("Enable occasional hand/body tremor")]
        public bool enableTremor = true;
        [Tooltip("Tremor intensity")]
        public float tremorAmount = 0.008f;
        [Tooltip("Minimum seconds between tremors")]
        public float tremorIntervalMin = 3f;
        [Tooltip("Maximum seconds between tremors")]
        public float tremorIntervalMax = 8f;
        [Tooltip("How long each tremor lasts")]
        public float tremorDuration = 0.3f;
        
        // Internal state
        private Vector3 originalScale;
        private Vector3 originalPosition;
        private float nextTremorTime;
        private float tremorEndTime;
        private bool isTremoring;
        
        void Start()
        {
            if (spriteTransform == null)
                spriteTransform = transform;
            
            originalScale = spriteTransform.localScale;
            originalPosition = spriteTransform.localPosition;
            
            ScheduleNextTremor();
        }
        
        void Update()
        {
            Vector3 newScale = originalScale;
            Vector3 newPosition = originalPosition;
            
            // Breathing - subtle Y scale pulse
            float breathPhase = Time.time * breathingSpeed * Mathf.PI * 2f;
            float breathOffset = Mathf.Sin(breathPhase) * breathingAmount;
            newScale.y = originalScale.y * (1f + breathOffset);
            
            // Subtle Y position shift with breathing
            newPosition.y = originalPosition.y + (breathOffset * 0.02f);
            
            // Gentle sway
            float swayPhase = Time.time * swaySpeed * Mathf.PI * 2f;
            float swayOffset = Mathf.Sin(swayPhase) * swayAmount;
            newPosition.x = originalPosition.x + swayOffset;
            
            // Occasional tremor
            if (enableTremor)
            {
                ApplyTremor(ref newPosition);
            }
            
            // Apply transformations
            spriteTransform.localScale = newScale;
            spriteTransform.localPosition = newPosition;
        }
        
        private void ApplyTremor(ref Vector3 position)
        {
            // Check if it's time for a new tremor
            if (!isTremoring && Time.time >= nextTremorTime)
            {
                isTremoring = true;
                tremorEndTime = Time.time + tremorDuration;
            }
            
            // Apply tremor effect
            if (isTremoring)
            {
                // Multi-frequency tremor for natural feel
                float tremor = Mathf.Sin(Time.time * 40f) * tremorAmount;
                tremor += Mathf.Sin(Time.time * 57f) * tremorAmount * 0.5f;
                tremor += Mathf.Sin(Time.time * 31f) * tremorAmount * 0.3f;
                
                position.x += tremor;
                position.y += tremor * 0.3f;
                
                // End tremor
                if (Time.time >= tremorEndTime)
                {
                    isTremoring = false;
                    ScheduleNextTremor();
                }
            }
        }
        
        private void ScheduleNextTremor()
        {
            nextTremorTime = Time.time + Random.Range(tremorIntervalMin, tremorIntervalMax);
        }
        
        /// <summary>
        /// Call when character starts walking
        /// </summary>
        public void OnStartMoving()
        {
            enabled = false;
            
            // Reset to original position/scale
            if (spriteTransform != null)
            {
                spriteTransform.localScale = originalScale;
                spriteTransform.localPosition = originalPosition;
            }
        }
        
        /// <summary>
        /// Call when character stops walking
        /// </summary>
        public void OnStopMoving()
        {
            enabled = true;
        }
    }
}
```

### 5.4 Animator Controller Setup

```
ANIMATOR CONTROLLER: RobertAnimator
════════════════════════════════════

PARAMETERS:
├── IsWalking (Bool) - true when moving
├── DirectionX (Float) - range: -1 to 1
└── DirectionY (Float) - range: -1 to 1

LAYERS:
└── Base Layer

STATES:
├── Idle_BlendTree (default)
│   ├── Blend Type: 2D Simple Directional
│   ├── Parameters: DirectionX, DirectionY
│   └── Motions:
│       ├── (0, -1) → Robert_Idle_Down
│       ├── (-1, 0) → Robert_Idle_Left
│       ├── (1, 0) → Robert_Idle_Right
│       └── (0, 1) → Robert_Idle_Up
│
└── Walk_BlendTree
    ├── Blend Type: 2D Simple Directional
    ├── Parameters: DirectionX, DirectionY
    └── Motions:
        ├── (0, -1) → Robert_Walk_Down
        ├── (-1, 0) → Robert_Walk_Left
        ├── (1, 0) → Robert_Walk_Right
        └── (0, 1) → Robert_Walk_Up

TRANSITIONS:
├── Idle_BlendTree → Walk_BlendTree
│   ├── Condition: IsWalking = true
│   ├── Has Exit Time: false
│   └── Transition Duration: 0.1
│
└── Walk_BlendTree → Idle_BlendTree
    ├── Condition: IsWalking = false
    ├── Has Exit Time: false
    └── Transition Duration: 0.1

ANIMATION CLIP SETTINGS:
├── Walk clips: 4 frames, Sample Rate: 8 (= 0.5 sec/loop)
└── Idle clips: 3 frames, Sample Rate: 2 (= 1.5 sec/loop)
```

---

## 6. Pathfinding & Movement

### 6.1 SimplePathfinder.cs

```csharp
// SimplePathfinder.cs
using UnityEngine;
using System.Collections.Generic;

namespace LastDay.Pathfinding
{
    public class SimplePathfinder : MonoBehaviour
    {
        public static SimplePathfinder Instance { get; private set; }
        
        [Header("Grid Configuration")]
        [Tooltip("Bottom-left corner of the walkable area")]
        public Vector2 gridOrigin = new Vector2(-4, -2);
        [Tooltip("Size of the walkable area")]
        public Vector2 gridSize = new Vector2(8, 4);
        [Tooltip("Size of each grid cell")]
        public float cellSize = 0.25f;
        
        [Header("Obstacle Detection")]
        public LayerMask obstacleLayer;
        public float obstacleCheckRadius = 0.1f;
        
        [Header("Debug")]
        public bool showDebugGrid = true;
        public Color walkableColor = new Color(0, 1, 0, 0.2f);
        public Color blockedColor = new Color(1, 0, 0, 0.2f);
        
        // Grid data
        private bool[,] walkableGrid;
        private int gridWidth;
        private int gridHeight;
        
        void Awake()
        {
            Instance = this;
        }
        
        void Start()
        {
            BuildGrid();
        }
        
        /// <summary>
        /// Scan the environment and build the walkable grid
        /// </summary>
        public void BuildGrid()
        {
            gridWidth = Mathf.CeilToInt(gridSize.x / cellSize);
            gridHeight = Mathf.CeilToInt(gridSize.y / cellSize);
            walkableGrid = new bool[gridWidth, gridHeight];
            
            int walkableCount = 0;
            int blockedCount = 0;
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2 worldPos = GridToWorld(x, y);
                    Collider2D obstacle = Physics2D.OverlapCircle(worldPos, obstacleCheckRadius, obstacleLayer);
                    
                    walkableGrid[x, y] = (obstacle == null);
                    
                    if (walkableGrid[x, y]) walkableCount++;
                    else blockedCount++;
                }
            }
            
            Debug.Log($"[Pathfinder] Grid built: {gridWidth}×{gridHeight} ({walkableCount} walkable, {blockedCount} blocked)");
        }
        
        /// <summary>
        /// Find a path from start to end using A*
        /// </summary>
        public List<Vector2> FindPath(Vector2 start, Vector2 end)
        {
            Vector2Int startGrid = WorldToGrid(start);
            Vector2Int endGrid = WorldToGrid(end);
            
            // Validate start position
            if (!IsValidAndWalkable(startGrid))
            {
                startGrid = FindNearestWalkable(startGrid);
                if (startGrid.x < 0) return null;
            }
            
            // Validate end position
            if (!IsValidAndWalkable(endGrid))
            {
                endGrid = FindNearestWalkable(endGrid);
                if (endGrid.x < 0) return null;
            }
            
            // A* implementation
            var openSet = new List<PathNode>();
            var closedSet = new HashSet<Vector2Int>();
            var nodeMap = new Dictionary<Vector2Int, PathNode>();
            
            var startNode = new PathNode(startGrid, null, 0, Heuristic(startGrid, endGrid));
            openSet.Add(startNode);
            nodeMap[startGrid] = startNode;
            
            while (openSet.Count > 0)
            {
                // Get lowest F cost node
                PathNode current = GetLowestFCost(openSet);
                
                if (current.Position == endGrid)
                {
                    return ReconstructPath(current);
                }
                
                openSet.Remove(current);
                closedSet.Add(current.Position);
                
                // Check neighbors
                foreach (var neighborPos in GetNeighbors(current.Position))
                {
                    if (closedSet.Contains(neighborPos)) continue;
                    if (!IsValidAndWalkable(neighborPos)) continue;
                    
                    float tentativeG = current.G + cellSize;
                    
                    if (!nodeMap.TryGetValue(neighborPos, out PathNode neighborNode))
                    {
                        neighborNode = new PathNode(neighborPos, current, tentativeG, Heuristic(neighborPos, endGrid));
                        nodeMap[neighborPos] = neighborNode;
                        openSet.Add(neighborNode);
                    }
                    else if (tentativeG < neighborNode.G)
                    {
                        neighborNode.Parent = current;
                        neighborNode.G = tentativeG;
                        neighborNode.F = tentativeG + neighborNode.H;
                    }
                }
            }
            
            Debug.LogWarning("[Pathfinder] No path found");
            return null;
        }
        
        #region A* Helpers
        
        private class PathNode
        {
            public Vector2Int Position;
            public PathNode Parent;
            public float G;
            public float H;
            public float F;
            
            public PathNode(Vector2Int pos, PathNode parent, float g, float h)
            {
                Position = pos;
                Parent = parent;
                G = g;
                H = h;
                F = g + h;
            }
        }
        
        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        
        private PathNode GetLowestFCost(List<PathNode> nodes)
        {
            PathNode lowest = nodes[0];
            for (int i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].F < lowest.F)
                    lowest = nodes[i];
            }
            return lowest;
        }
        
        private List<Vector2Int> GetNeighbors(Vector2Int pos)
        {
            return new List<Vector2Int>
            {
                new Vector2Int(pos.x + 1, pos.y),
                new Vector2Int(pos.x - 1, pos.y),
                new Vector2Int(pos.x, pos.y + 1),
                new Vector2Int(pos.x, pos.y - 1)
            };
        }
        
        private List<Vector2> ReconstructPath(PathNode endNode)
        {
            var path = new List<Vector2>();
            var current = endNode;
            
            while (current != null)
            {
                path.Add(GridToWorld(current.Position.x, current.Position.y));
                current = current.Parent;
            }
            
            path.Reverse();
            return SmoothPath(path);
        }
        
        private List<Vector2> SmoothPath(List<Vector2> path)
        {
            if (path.Count <= 2) return path;
            
            var smoothed = new List<Vector2> { path[0] };
            
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 dir1 = (path[i] - path[i - 1]).normalized;
                Vector2 dir2 = (path[i + 1] - path[i]).normalized;
                
                // Keep point if direction changes significantly
                if (Vector2.Dot(dir1, dir2) < 0.95f)
                {
                    smoothed.Add(path[i]);
                }
            }
            
            smoothed.Add(path[path.Count - 1]);
            return smoothed;
        }
        
        #endregion
        
        #region Grid Utilities
        
        public Vector2Int WorldToGrid(Vector2 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize);
            int y = Mathf.FloorToInt((worldPos.y - gridOrigin.y) / cellSize);
            return new Vector2Int(x, y);
        }
        
        public Vector2 GridToWorld(int x, int y)
        {
            return new Vector2(
                gridOrigin.x + (x + 0.5f) * cellSize,
                gridOrigin.y + (y + 0.5f) * cellSize
            );
        }
        
        private bool IsValidAndWalkable(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight)
                return false;
            return walkableGrid[pos.x, pos.y];
        }
        
        private Vector2Int FindNearestWalkable(Vector2Int pos)
        {
            for (int radius = 1; radius < Mathf.Max(gridWidth, gridHeight); radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        Vector2Int check = new Vector2Int(pos.x + x, pos.y + y);
                        if (IsValidAndWalkable(check))
                            return check;
                    }
                }
            }
            return new Vector2Int(-1, -1);
        }
        
        #endregion
        
        #region Debug Visualization
        
        void OnDrawGizmos()
        {
            if (!showDebugGrid) return;
            
            // Draw grid bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                (Vector3)(gridOrigin + gridSize * 0.5f),
                (Vector3)gridSize
            );
            
            if (walkableGrid == null) return;
            
            // Draw cells
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2 worldPos = GridToWorld(x, y);
                    Gizmos.color = walkableGrid[x, y] ? walkableColor : blockedColor;
                    Gizmos.DrawCube(worldPos, Vector3.one * cellSize * 0.9f);
                }
            }
        }
        
        #endregion
    }
}
```

### 6.2 PlayerController2D.cs

```csharp
// PlayerController2D.cs
using UnityEngine;
using System.Collections.Generic;
using LastDay.Core;
using LastDay.Pathfinding;

namespace LastDay.Player
{
    public class PlayerController2D : MonoBehaviour
    {
        public static PlayerController2D Instance { get; private set; }
        
        [Header("Movement")]
        public float moveSpeed = 2f;
        public float nodeReachDistance = 0.1f;
        
        [Header("References")]
        public CharacterAnimator characterAnimator;
        public SubtleIdleMovement idleMovement;
        public SpriteRenderer spriteRenderer;
        
        [Header("Audio")]
        public AudioClip[] footstepSounds;
        public float footstepInterval = 0.3f;
        
        // Path following
        private List<Vector2> currentPath;
        private int currentPathIndex;
        private bool isMoving;
        private System.Action onArrivalCallback;
        
        // Footsteps
        private float lastFootstepTime;
        private AudioSource audioSource;
        
        void Awake()
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
        }
        
        void Update()
        {
            if (!isMoving) return;
            
            // Check if we have a valid path
            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                StopMoving();
                return;
            }
            
            // Get current target
            Vector2 target = currentPath[currentPathIndex];
            Vector2 currentPos = transform.position;
            Vector2 direction = (target - currentPos).normalized;
            
            // Move
            transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
            
            // Update animation
            characterAnimator.SetMoving(true, direction);
            
            // Play footsteps
            PlayFootstep();
            
            // Check if reached node
            if (Vector2.Distance(currentPos, target) < nodeReachDistance)
            {
                currentPathIndex++;
                
                if (currentPathIndex >= currentPath.Count)
                {
                    StopMoving();
                }
            }
        }
        
        /// <summary>
        /// Move to a world position
        /// </summary>
        public void MoveTo(Vector2 destination, System.Action onComplete = null)
        {
            if (!GameStateMachine.Instance.CanPlayerMove) return;
            
            // Find path
            currentPath = SimplePathfinder.Instance.FindPath(transform.position, destination);
            
            if (currentPath == null || currentPath.Count == 0)
            {
                Debug.Log("[Player] Cannot reach destination");
                return;
            }
            
            currentPathIndex = 0;
            isMoving = true;
            onArrivalCallback = onComplete;
            
            // Disable idle movement
            if (idleMovement != null)
                idleMovement.OnStartMoving();
        }
        
        /// <summary>
        /// Move to an interactable object, then interact
        /// </summary>
        public void MoveToAndInteract(Interaction.InteractableObject2D target)
        {
            Vector2 interactionPoint = GetInteractionPoint(target.transform.position);
            
            MoveTo(interactionPoint, () => {
                // Face the object
                characterAnimator.FacePosition(target.transform.position);
                // Trigger interaction
                target.OnPlayerArrived();
            });
        }
        
        private Vector2 GetInteractionPoint(Vector2 objectPosition)
        {
            // Stand slightly below/in front of the object
            Vector2 point = objectPosition;
            point.y -= 0.4f;
            return point;
        }
        
        private void StopMoving()
        {
            isMoving = false;
            currentPath = null;
            
            // Update animation
            characterAnimator.SetMoving(false, Vector2.zero);
            
            // Re-enable idle movement
            if (idleMovement != null)
                idleMovement.OnStopMoving();
            
            // Invoke callback
            onArrivalCallback?.Invoke();
            onArrivalCallback = null;
        }
        
        private void PlayFootstep()
        {
            if (footstepSounds == null || footstepSounds.Length == 0) return;
            if (Time.time - lastFootstepTime < footstepInterval) return;
            
            lastFootstepTime = Time.time;
            
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip, 0.3f);
        }
        
        public void ForceStop()
        {
            StopMoving();
        }
        
        public bool IsMoving => isMoving;
    }
}
```

### 6.3 ClickToMoveHandler.cs

```csharp
// ClickToMoveHandler.cs
using UnityEngine;
using UnityEngine.EventSystems;
using LastDay.Core;
using LastDay.Interaction;

namespace LastDay.Player
{
    public class ClickToMoveHandler : MonoBehaviour
    {
        [Header("Layers")]
        public LayerMask interactableLayer;
        public LayerMask walkableLayer;
        
        [Header("Visual Feedback")]
        public GameObject clickIndicatorPrefab;
        public float indicatorDuration = 0.5f;
        
        private GameObject currentIndicator;
        
        void Update()
        {
            // Don't process clicks if over UI
            if (EventSystem.current.IsPointerOverGameObject()) return;
            
            // Don't process if game state doesn't allow
            if (!GameStateMachine.Instance.CanInteract) return;
            
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }
        }
        
        private void HandleClick()
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // Priority 1: Check for interactable objects
            Collider2D interactableHit = Physics2D.OverlapPoint(mouseWorldPos, interactableLayer);
            if (interactableHit != null)
            {
                var interactable = interactableHit.GetComponent<InteractableObject2D>();
                if (interactable != null && interactable.IsInteractable)
                {
                    PlayerController2D.Instance.MoveToAndInteract(interactable);
                    ShowClickIndicator(interactable.transform.position);
                    return;
                }
            }
            
            // Priority 2: Move to walkable area
            Collider2D walkableHit = Physics2D.OverlapPoint(mouseWorldPos, walkableLayer);
            if (walkableHit != null)
            {
                PlayerController2D.Instance.MoveTo(mouseWorldPos);
                ShowClickIndicator(mouseWorldPos);
            }
        }
        
        private void ShowClickIndicator(Vector2 position)
        {
            if (clickIndicatorPrefab == null) return;
            
            // Destroy previous indicator
            if (currentIndicator != null)
                Destroy(currentIndicator);
            
            // Create new indicator
            currentIndicator = Instantiate(clickIndicatorPrefab, position, Quaternion.identity);
            Destroy(currentIndicator, indicatorDuration);
        }
    }
}
```

---

## 7. Dialogue & AI Integration

### 7.1 LocalLLMManager.cs

```csharp
// LocalLLMManager.cs
using UnityEngine;
using System.Threading.Tasks;
using LLMUnity;
using LastDay.Core;

namespace LastDay.Dialogue
{
    public class LocalLLMManager : MonoBehaviour
    {
        public static LocalLLMManager Instance { get; private set; }
        
        [Header("Model Settings")]
        public int maxTokens = 80;
        public float temperature = 0.7f;
        public int contextSize = 2048;
        
        [Header("State")]
        public bool isInitialized = false;
        public string currentCharacter = "partner";
        
        private LLM llm;
        
        void Awake()
        {
            Instance = this;
        }
        
        public void Initialize(string modelPath)
        {
            llm = gameObject.AddComponent<LLM>();
            llm.modelPath = modelPath;
            llm.contextSize = contextSize;
            llm.temperature = temperature;
            llm.numPredict = maxTokens;
            
            // Warm up
            _ = llm.Warmup();
            
            isInitialized = true;
            Debug.Log("[LLM] Initialized successfully");
        }
        
        public void SetCharacter(string characterName)
        {
            currentCharacter = characterName;
        }
        
        public async Task<string> GenerateResponse(string playerInput)
        {
            if (!isInitialized)
            {
                return GetFallbackResponse();
            }
            
            // Build the full prompt
            string systemPrompt = CharacterPrompts.GetPrompt(currentCharacter);
            string context = EventManager.Instance.BuildContextString();
            string memoryContext = BuildMemoryContext();
            
            string fullPrompt = $@"{systemPrompt}

CURRENT SITUATION:
{context}

MEMORIES TRIGGERED:
{memoryContext}

Robert says: ""{playerInput}""

Respond naturally in 1-3 sentences:";
            
            try
            {
                string response = await llm.Chat(fullPrompt);
                return CleanResponse(response);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LLM] Generation error: {e.Message}");
                return GetFallbackResponse();
            }
        }
        
        private string BuildMemoryContext()
        {
            var memories = EventManager.Instance.triggeredMemories;
            if (memories.Count == 0) return "None yet.";
            
            var sb = new System.Text.StringBuilder();
            foreach (var memoryId in memories)
            {
                var data = MemoryContext.GetMemory(memoryId);
                if (data != null)
                {
                    sb.AppendLine($"- {data.shortDescription}");
                }
            }
            return sb.ToString();
        }
        
        private string CleanResponse(string response)
        {
            response = response.Trim();
            
            // Remove character name prefixes
            if (response.StartsWith("Martha:"))
                response = response.Substring(7).Trim();
            if (response.StartsWith("David:"))
                response = response.Substring(6).Trim();
            
            // Remove surrounding quotes
            if (response.StartsWith("\"") && response.EndsWith("\""))
                response = response.Substring(1, response.Length - 2);
            
            return response;
        }
        
        private string GetFallbackResponse()
        {
            string[] fallbacks = new string[]
            {
                "I'm here with you, dear.",
                "Take your time.",
                "What's on your mind?",
                "I understand."
            };
            return fallbacks[Random.Range(0, fallbacks.Length)];
        }
    }
}
```

### 7.2 CharacterPrompts.cs

```csharp
// CharacterPrompts.cs
namespace LastDay.Dialogue
{
    public static class CharacterPrompts
    {
        public static string GetPrompt(string character)
        {
            return character switch
            {
                "partner" => MARTHA_PROMPT,
                "phone_friend" => DAVID_PROMPT,
                _ => MARTHA_PROMPT
            };
        }
        
        private const string MARTHA_PROMPT = @"You are Martha, 72 years old. Your husband Robert has ALS and today is his last day to decide about medical assistance in dying.

WHO YOU ARE:
- Married to Robert for 47 years
- Former elementary school teacher, retired
- His caregiver for the past 3 years
- Exhausted but hiding it

YOUR FEELINGS:
- You love him deeply and hate seeing him suffer
- Part of you wants his pain to end
- Part of you isn't ready to let go
- You support his choice but secretly hope for more time

HOW YOU SPEAK:
- Warm, gentle, sometimes tired
- Call him ""dear"" or ""Robert""
- Short sentences, no medical jargon
- Never lecture or pressure
- Reminisce when relevant

IMPORTANT: Keep responses to 1-3 sentences. Be natural.";

        private const string DAVID_PROMPT = @"You are David, 74 years old. Robert's best friend for 50 years. You're on a phone call from Seattle.

WHO YOU ARE:
- Retired Marine, served with Robert in Vietnam
- Pragmatic and direct
- Your wife passed from cancer 5 years ago
- Visited Robert last week

YOUR FEELINGS:
- Support Robert's right to choose
- Believe prolonging suffering is cruel
- Already said your goodbyes in person
- Love him like a brother

HOW YOU SPEAK:
- Direct but kind
- Call him ""buddy"" or ""brother""
- Share memories when helpful
- Don't push, just share your view if asked

IMPORTANT: Keep responses to 1-3 sentences.";
    }
}
```

---

## 8. Interactive Objects

### 8.1 InteractableObject2D.cs

```csharp
// InteractableObject2D.cs
using UnityEngine;
using UnityEngine.EventSystems;
using LastDay.Core;

namespace LastDay.Interaction
{
    public class InteractableObject2D : MonoBehaviour, 
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Object Identity")]
        public string objectId;
        public string displayName;
        
        [Header("Memory Data")]
        public bool triggersMemory = true;
        public string memoryId;
        
        [Header("Visuals")]
        public SpriteRenderer mainSprite;
        public SpriteRenderer glowSprite;
        public Color glowColor = new Color(1f, 1f, 0.8f, 0.4f);
        
        [Header("Gaze Detection")]
        public float gazeTimeToTrigger = 2f;
        private float currentGazeTime = 0f;
        private bool isHovering = false;
        private bool hasTriggeredGaze = false;
        
        [Header("Audio")]
        public AudioClip hoverSound;
        public AudioClip interactSound;
        
        public bool IsInteractable => GameStateMachine.Instance.CanInteract;
        
        void Start()
        {
            if (glowSprite != null)
                glowSprite.enabled = false;
        }
        
        void Update()
        {
            if (isHovering && !hasTriggeredGaze && triggersMemory)
            {
                currentGazeTime += Time.deltaTime;
                
                // Update glow intensity based on gaze progress
                if (glowSprite != null)
                {
                    float progress = currentGazeTime / gazeTimeToTrigger;
                    glowSprite.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * progress);
                }
                
                if (currentGazeTime >= gazeTimeToTrigger)
                {
                    OnGazeComplete();
                }
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable) return;
            
            isHovering = true;
            
            if (glowSprite != null)
                glowSprite.enabled = true;
            
            // Show interaction prompt
            UI.InteractionPrompt.Instance?.Show($"Click to examine {displayName}");
            
            // Play hover sound
            if (hoverSound != null)
                AudioSource.PlayClipAtPoint(hoverSound, transform.position, 0.5f);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            currentGazeTime = 0f;
            
            if (glowSprite != null)
                glowSprite.enabled = false;
            
            UI.InteractionPrompt.Instance?.Hide();
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable) return;
            
            // Player will walk to this object
            // Actual interaction happens in OnPlayerArrived
        }
        
        /// <summary>
        /// Called when gaze timer completes
        /// </summary>
        protected virtual void OnGazeComplete()
        {
            hasTriggeredGaze = true;
            
            EventManager.Instance.PublishEvent(new GameEvent("gaze_complete", objectId, memoryId));
            
            Debug.Log($"[Object] Gaze triggered on {objectId}");
        }
        
        /// <summary>
        /// Called when player walks to this object and arrives
        /// </summary>
        public virtual void OnPlayerArrived()
        {
            // Publish interact event
            EventManager.Instance.PublishEvent(new GameEvent("interact", objectId, memoryId));
            
            // Play sound
            if (interactSound != null)
                AudioSource.PlayClipAtPoint(interactSound, transform.position, 0.7f);
            
            // Open dialogue
            OnInteract();
        }
        
        /// <summary>
        /// Override in subclasses for specific behavior
        /// </summary>
        protected virtual void OnInteract()
        {
            // Default: Open dialogue with Martha about this object
            UI.DialogueUI.Instance.ShowDialogue("partner", memoryId);
        }
    }
}
```

### 8.2 DocumentInteraction.cs

```csharp
// DocumentInteraction.cs
using UnityEngine;
using LastDay.Core;
using LastDay.UI;

namespace LastDay.Interaction
{
    public class DocumentInteraction : InteractableObject2D
    {
        [Header("Document State")]
        public bool isUnlocked = false;
        
        [Header("Locked Responses")]
        [TextArea]
        public string[] lockedResponses = new string[]
        {
            "Not yet. I need to think about this more.",
            "I'm not ready to look at that.",
            "I should talk to Martha first."
        };
        
        void OnEnable()
        {
            EventManager.OnDocumentUnlocked += OnDocumentUnlocked;
        }
        
        void OnDisable()
        {
            EventManager.OnDocumentUnlocked -= OnDocumentUnlocked;
        }
        
        private void OnDocumentUnlocked()
        {
            isUnlocked = true;
            
            // Visual feedback - make glow more prominent
            if (glowSprite != null)
            {
                glowSprite.enabled = true;
                glowSprite.color = new Color(1f, 0.9f, 0.7f, 0.6f);
            }
        }
        
        protected override void OnInteract()
        {
            if (!isUnlocked)
            {
                // Show internal monologue
                string response = lockedResponses[Random.Range(0, lockedResponses.Length)];
                MonologueUI.Instance.Show(response);
                return;
            }
            
            // Show decision panel
            GameStateMachine.Instance.ChangeState(GameState.Decision);
            DecisionUI.Instance.Show();
        }
    }
}
```

---

## 9. Game Flow & State Machine

### 9.1 Complete Game Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              GAME FLOW                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────┐                                                            │
│  │   LOADING   │ ◄─── Game Start                                            │
│  │  (Download  │                                                            │
│  │   Model)    │                                                            │
│  └──────┬──────┘                                                            │
│         │ Model Ready                                                        │
│         ▼                                                                    │
│  ┌─────────────┐                                                            │
│  │   PLAYING   │ ◄───────────────────────────────────────┐                 │
│  │  (Explore)  │                                          │                 │
│  └──────┬──────┘                                          │                 │
│         │                                                  │                 │
│    ┌────┴────────────────┬──────────────────┐            │                 │
│    │                     │                   │            │                 │
│    ▼                     ▼                   ▼            │                 │
│  ┌─────────────┐  ┌─────────────┐    ┌─────────────┐    │                 │
│  │ IN_DIALOGUE │  │  PHONE_CALL │    │  DECISION   │    │                 │
│  │  (Martha)   │  │   (David)   │    │ (Sign/Tear) │    │                 │
│  └──────┬──────┘  └──────┬──────┘    └──────┬──────┘    │                 │
│         │                 │                   │            │                 │
│         │ Close           │ Hang Up           │            │                 │
│         └─────────────────┴───────────────────┘            │                 │
│                           │                                 │                 │
│                           └────────── Return ───────────────┘                │
│                                                                              │
│                                       │ Choose                               │
│                                       ▼                                      │
│                               ┌─────────────┐                               │
│                               │   ENDING    │                               │
│                               │ (Fade Out)  │                               │
│                               └──────┬──────┘                               │
│                                      │                                       │
│                                      ▼                                       │
│                                   [QUIT]                                     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 9.2 Memory Trigger Flow

```
MEMORY SYSTEM FLOW:
═══════════════════

Player gazes at object (2+ seconds)
        │
        ▼
OnGazeComplete()
        │
        ▼
EventManager.PublishEvent("gaze_complete", memoryId)
        │
        ├──► Add to triggeredMemories list
        │
        ├──► Check: triggeredMemories.Count >= 2?
        │           │
        │           └── YES ──► Unlock Document
        │                       Trigger Phone Ring
        │
        └──► Update AI context for next dialogue


Player clicks object → walks to it → OnPlayerArrived()
        │
        ▼
EventManager.PublishEvent("interact", memoryId)
        │
        ▼
DialogueUI.ShowDialogue(character, memoryId)
        │
        ▼
LocalLLMManager.GenerateResponse(playerInput)
        │
        ├──► Include memory context in prompt
        │
        └──► AI response references the memory naturally
```

---

## 10. UI System

### 10.1 UI Hierarchy

```
Canvas (Screen Space - Overlay)
│
├── DialoguePanel
│   ├── Background (9-slice pixel art)
│   ├── PortraitFrame
│   │   └── PortraitImage
│   ├── NamePlate
│   │   └── NameText (TMP)
│   ├── DialogueText (TMP)
│   ├── InputContainer
│   │   ├── InputField (TMP)
│   │   └── SendButton
│   └── ThinkingIndicator
│       ├── Dot1
│       ├── Dot2
│       └── Dot3
│
├── PhonePanel
│   ├── PhoneBackground
│   ├── CallerNameText
│   ├── PhoneDialogueText
│   ├── PhoneInputField
│   └── HangUpButton
│
├── MonologuePanel
│   └── MonologueText (TMP, italicized)
│
├── DecisionPanel
│   ├── DimBackground
│   ├── DocumentImage
│   ├── PromptText
│   ├── SignButton
│   └── TearButton
│
├── InteractionPrompt
│   └── PromptText
│
├── DownloadPanel
│   ├── ProgressBar
│   ├── StatusText
│   └── PercentText
│
├── FadePanel
│   └── FadeImage (full screen black)
│
└── EndScreen
    ├── Background
    ├── QuoteText
    └── AttributionText
```

### 10.2 DialogueUI.cs

```csharp
// DialogueUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
using LastDay.Core;
using LastDay.Dialogue;

namespace LastDay.UI
{
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }
        
        [Header("Panel")]
        public GameObject dialoguePanel;
        
        [Header("Character Display")]
        public Image portraitImage;
        public TMP_Text nameText;
        public Sprite[] marthaPortraits;  // 0:neutral, 1:sad, 2:hopeful, 3:concerned
        
        [Header("Dialogue")]
        public TMP_Text dialogueText;
        public float typewriterSpeed = 0.03f;
        
        [Header("Input")]
        public TMP_InputField inputField;
        public Button sendButton;
        
        [Header("Thinking Indicator")]
        public GameObject thinkingIndicator;
        
        [Header("Audio")]
        public AudioClip[] textBlips;
        public AudioSource audioSource;
        
        // State
        private string currentCharacter;
        private string currentMemoryContext;
        private bool isTyping;
        private bool isWaitingForResponse;
        private Coroutine typewriterCoroutine;
        
        void Awake()
        {
            Instance = this;
            dialoguePanel.SetActive(false);
        }
        
        void Start()
        {
            sendButton.onClick.AddListener(OnSendClicked);
            inputField.onSubmit.AddListener(_ => OnSendClicked());
        }
        
        public void ShowDialogue(string character, string memoryId = "")
        {
            currentCharacter = character;
            currentMemoryContext = memoryId;
            
            // Set up character display
            if (character == "partner")
            {
                nameText.text = "Martha";
                portraitImage.sprite = marthaPortraits[0];
            }
            
            dialoguePanel.SetActive(true);
            inputField.text = "";
            inputField.Select();
            
            // Change game state
            GameStateMachine.Instance.ChangeState(GameState.InDialogue);
            
            // Set character for LLM
            LocalLLMManager.Instance.SetCharacter(character);
            
            // If this is a memory interaction, show initial context
            if (!string.IsNullOrEmpty(memoryId))
            {
                ShowInitialMemoryResponse(memoryId);
            }
        }
        
        private async void ShowInitialMemoryResponse(string memoryId)
        {
            // Generate an initial response about the memory
            string prompt = $"Robert is looking at the {memoryId}. Comment on it briefly.";
            
            thinkingIndicator.SetActive(true);
            string response = await LocalLLMManager.Instance.GenerateResponse(prompt);
            thinkingIndicator.SetActive(false);
            
            StartTypewriter(response);
        }
        
        public void HideDialogue()
        {
            dialoguePanel.SetActive(false);
            GameStateMachine.Instance.ChangeState(GameState.Playing);
        }
        
        public async void OnSendClicked()
        {
            if (isWaitingForResponse || isTyping) return;
            
            string playerText = inputField.text.Trim();
            if (string.IsNullOrEmpty(playerText)) return;
            
            isWaitingForResponse = true;
            inputField.interactable = false;
            sendButton.interactable = false;
            inputField.text = "";
            
            thinkingIndicator.SetActive(true);
            
            // Check for help request (game start)
            CheckHelpRequest(playerText);
            
            // Get AI response
            string response = await LocalLLMManager.Instance.GenerateResponse(playerText);
            
            thinkingIndicator.SetActive(false);
            
            // Display response
            StartTypewriter(response);
            
            isWaitingForResponse = false;
            inputField.interactable = true;
            sendButton.interactable = true;
            inputField.Select();
        }
        
        private void CheckHelpRequest(string input)
        {
            if (EventManager.Instance.hasAskedForHelp) return;
            
            string lower = input.ToLower();
            string[] helpWords = { "help", "stand", "up", "can't", "need", "please", "water", "cane" };
            
            foreach (var word in helpWords)
            {
                if (lower.Contains(word))
                {
                    EventManager.Instance.SetAskedForHelp();
                    break;
                }
            }
        }
        
        private void StartTypewriter(string text)
        {
            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);
            
            typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
        }
        
        private IEnumerator TypewriterEffect(string text)
        {
            isTyping = true;
            dialogueText.text = "";
            
            foreach (char c in text)
            {
                dialogueText.text += c;
                
                // Play blip sound
                if (c != ' ' && textBlips.Length > 0)
                {
                    audioSource.pitch = Random.Range(0.95f, 1.05f);
                    audioSource.PlayOneShot(textBlips[Random.Range(0, textBlips.Length)], 0.4f);
                }
                
                yield return new WaitForSeconds(typewriterSpeed);
            }
            
            isTyping = false;
        }
        
        public void SetPortraitEmotion(int index)
        {
            if (index < marthaPortraits.Length)
                portraitImage.sprite = marthaPortraits[index];
        }
    }
}
```

---

## 11. Audio System

### 11.1 AudioManager.cs

```csharp
// AudioManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LastDay.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Sources")]
        public AudioSource musicSource;
        public AudioSource ambientSource;
        public AudioSource sfxSource;
        
        [Header("Music Tracks")]
        public AudioClip ambientLoop;
        public AudioClip endingSigned;
        public AudioClip endingTorn;
        
        [Header("Settings")]
        public float musicVolume = 0.5f;
        public float sfxVolume = 0.7f;
        public float crossfadeDuration = 2f;
        
        private Dictionary<string, AudioClip> musicTracks;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Build music dictionary
            musicTracks = new Dictionary<string, AudioClip>
            {
                { "ambient_loop", ambientLoop },
                { "ending_signed", endingSigned },
                { "ending_torn", endingTorn }
            };
        }
        
        public void PlayMusic(string trackName, bool loop = true)
        {
            if (!musicTracks.TryGetValue(trackName, out AudioClip clip))
            {
                Debug.LogWarning($"[Audio] Track not found: {trackName}");
                return;
            }
            
            StartCoroutine(CrossfadeToTrack(clip, loop));
        }
        
        private IEnumerator CrossfadeToTrack(AudioClip newClip, bool loop)
        {
            float startVolume = musicSource.volume;
            
            // Fade out current track
            float elapsed = 0f;
            while (elapsed < crossfadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (crossfadeDuration / 2f));
                yield return null;
            }
            
            // Switch track
            musicSource.clip = newClip;
            musicSource.loop = loop;
            musicSource.Play();
            
            // Fade in new track
            elapsed = 0f;
            while (elapsed < crossfadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / (crossfadeDuration / 2f));
                yield return null;
            }
            
            musicSource.volume = musicVolume;
        }
        
        public void PlaySFX(AudioClip clip, float volume = -1f)
        {
            if (clip == null) return;
            if (volume < 0) volume = sfxVolume;
            
            sfxSource.PlayOneShot(clip, volume);
        }
        
        public void PlaySFX(string clipName)
        {
            // Load from Resources/Audio/SFX/
            AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{clipName}");
            if (clip != null)
                PlaySFX(clip);
        }
    }
}
```

### 11.2 Audio Specifications

```
MUSIC TRACKS:
═════════════

ambient_loop.wav
├── Duration: 2:00 - 3:00 (seamless loop)
├── Format: WAV 44.1kHz 16-bit
├── Style: Melancholic piano, soft strings
├── Tempo: ~65 BPM
└── Unity Import: Streaming, Vorbis compression

ending_signed.wav
├── Duration: 0:45 - 1:00
├── Format: WAV 44.1kHz 16-bit
├── Style: Peaceful resolution, fade out
└── Unity Import: Decompress On Load

ending_torn.wav
├── Duration: 0:45 - 1:00
├── Format: WAV 44.1kHz 16-bit
├── Style: Hopeful, gentle
└── Unity Import: Decompress On Load


SOUND EFFECTS:
══════════════

footstep_1.wav, footstep_2.wav
├── Duration: ~0.2 sec
├── Style: Soft indoor footstep
└── Unity Import: Decompress On Load

dialogue_blip.wav
├── Duration: ~0.05 sec
├── Style: Soft click/blip for typewriter
└── Unity Import: Decompress On Load

phone_ring.wav
├── Duration: ~5 sec (loopable)
├── Style: Vintage rotary phone
└── Unity Import: Compressed In Memory

click.wav, hover.wav
├── Duration: ~0.1 sec
├── Style: UI feedback
└── Unity Import: Decompress On Load
```

---

## 12. Asset Specifications

### 12.1 Character Sprites

```
┌─────────────────────────────────────────────────────────────────┐
│                    CHARACTER SPRITE SPECS                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ROBERT (Player)                                                │
│  ═══════════════                                                │
│  Walk Sheet: 128×192 px (4 cols × 4 rows, 32×48 cells)         │
│  Idle Sheet: 96×192 px (3 cols × 4 rows, 32×48 cells)          │
│  Portraits: 192×64 px (3 expressions × 64×64)                   │
│                                                                  │
│  MARTHA (NPC)                                                   │
│  ═══════════                                                    │
│  Idle Sheet: 96×144 px (3 cols × 3 rows, 32×48 cells)          │
│  Portraits: 256×64 px (4 expressions × 64×64)                   │
│                                                                  │
│  DAVID (Phone only)                                             │
│  ═════════════════                                              │
│  Portrait: 64×64 px (1 expression only)                         │
│                                                                  │
│  UNITY IMPORT SETTINGS:                                         │
│  - Texture Type: Sprite (2D and UI)                            │
│  - Sprite Mode: Multiple                                        │
│  - Pixels Per Unit: 32                                          │
│  - Filter Mode: Point (no filter) ← CRITICAL                   │
│  - Compression: None                                            │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 12.2 Environment & Objects

```
┌─────────────────────────────────────────────────────────────────┐
│                    ENVIRONMENT SPECS                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ROOM BACKGROUND                                                │
│  ═══════════════                                                │
│  Size: 480×270 px (reference resolution)                        │
│  Style: Cozy living room, afternoon light                       │
│  Layers: Background, furniture silhouettes                      │
│                                                                  │
│  INTERACTIVE OBJECTS (32×32 px each + glow variant)            │
│  ═══════════════════════════════════════════════                │
│  wedding_photo.png + wedding_photo_glow.png                     │
│  ice_picks.png + ice_picks_glow.png                             │
│  guitar.png + guitar_glow.png                                   │
│  phone.png + phone_glow.png                                     │
│  document.png + document_glow.png                               │
│                                                                  │
│  FURNITURE (decorative, various sizes)                          │
│  ═════════════════════════════════════                          │
│  desk.png (64×48)                                               │
│  bookshelf.png (48×80)                                          │
│  armchair.png (48×48)                                           │
│  window.png (64×80)                                             │
│                                                                  │
│  GLOW SPRITES:                                                  │
│  - Same shape as main sprite                                    │
│  - White/light yellow color                                     │
│  - Gaussian blur applied (soft edges)                           │
│  - Slightly larger (add 4px padding)                            │
│  - Placed BEHIND main sprite (lower sorting order)              │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 12.3 UI Assets

```
┌─────────────────────────────────────────────────────────────────┐
│                       UI ASSET SPECS                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  DIALOGUE PANEL (9-slice)                                       │
│  ════════════════════════                                       │
│  Size: 96×96 px minimum (stretches via 9-slice)                │
│  Border: 8px on all sides                                       │
│  Style: Dark wood or midnight blue pixel border                 │
│                                                                  │
│  BUTTONS                                                        │
│  ═══════                                                        │
│  button_normal.png: 48×16 px                                    │
│  button_pressed.png: 48×16 px (shifted down 1px)               │
│  button_hover.png: 48×16 px (slightly brighter)                │
│                                                                  │
│  NAME PLATE                                                     │
│  ══════════                                                     │
│  Size: 80×20 px                                                 │
│  Style: Banner/ribbon with character name                       │
│                                                                  │
│  INPUT FIELD BACKGROUND                                         │
│  ══════════════════════                                         │
│  Size: 9-slice, minimum 64×24 px                               │
│  Style: Slightly darker than panel, subtle border               │
│                                                                  │
│  CLICK INDICATOR                                                │
│  ════════════════                                               │
│  Size: 16×16 px                                                 │
│  Animation: 3-4 frame expanding circle, fades out              │
│                                                                  │
│  FONT                                                           │
│  ═════                                                          │
│  Family: Pixelify Sans (Google Fonts, free)                    │
│  Sizes: 8px (small), 12px (body), 16px (headers)               │
│  Create TMP Font Asset with appropriate padding                 │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 13. Scene Setup Guide

### 13.1 MainRoom Scene Hierarchy

```
MainRoom (Scene)
│
├── ═══ MANAGERS ═══
├── GameManager
│   ├── GameManager.cs
│   ├── ModelDownloader.cs
│   └── LocalLLMManager.cs
│
├── EventManager
│   └── EventManager.cs
│
├── GameStateMachine
│   └── GameStateMachine.cs
│
├── AudioManager
│   ├── AudioManager.cs
│   ├── AudioSource (Music)
│   ├── AudioSource (Ambient)
│   └── AudioSource (SFX)
│
├── FadeManager
│   └── FadeManager.cs
│
├── ═══ PATHFINDING ═══
├── Pathfinder
│   └── SimplePathfinder.cs
│       ├── Grid Origin: (-4, -2)
│       ├── Grid Size: (8, 4)
│       └── Cell Size: 0.25
│
├── ═══ CAMERA ═══
├── Main Camera
│   ├── Camera (Orthographic, Size: 4.2)
│   ├── PixelPerfectCamera
│   │   ├── Assets PPU: 32
│   │   └── Reference Resolution: 480×270
│   ├── AudioListener
│   └── CameraController2D.cs
│
├── ═══ ENVIRONMENT ═══
├── Environment
│   ├── Background
│   │   └── SpriteRenderer (Order: -100)
│   │
│   ├── Furniture
│   │   ├── Desk (Order: -10)
│   │   ├── Bookshelf (Order: -10)
│   │   ├── Armchair (Order: -10)
│   │   └── Window (Order: -10)
│   │
│   ├── WindowLight
│   │   └── SpriteRenderer (Additive blend, Order: -5)
│   │
│   └── WalkableArea
│       └── PolygonCollider2D (Is Trigger, Layer: Walkable)
│
├── ═══ OBSTACLES ═══
├── Obstacles
│   ├── Desk_Collider
│   │   └── BoxCollider2D (Layer: Obstacles)
│   ├── Bookshelf_Collider
│   │   └── BoxCollider2D (Layer: Obstacles)
│   └── Wall_Colliders
│       └── EdgeCollider2D (Layer: Obstacles)
│
├── ═══ CHARACTERS ═══
├── Robert (Player)
│   ├── SpriteRenderer (Order: 0)
│   ├── Animator → RobertAnimator
│   ├── BoxCollider2D (Trigger)
│   ├── AudioSource (Footsteps)
│   ├── PlayerController2D.cs
│   ├── CharacterAnimator.cs
│   ├── SubtleIdleMovement.cs
│   └── ClickToMoveHandler.cs
│
├── Martha (NPC)
│   ├── SpriteRenderer (Order: 0)
│   ├── Animator → MarthaAnimator
│   ├── BoxCollider2D (Trigger)
│   ├── NPCController.cs
│   └── SubtleIdleMovement.cs (NPCIdleMovement.cs)
│
├── ═══ INTERACTABLES ═══
├── Interactables
│   ├── WeddingPhoto
│   │   ├── SpriteRenderer (Order: 1)
│   │   ├── GlowSprite (child, Order: 0)
│   │   ├── BoxCollider2D (Layer: Interactables)
│   │   └── InteractableObject2D.cs
│   │       ├── Object ID: "wedding_photo"
│   │       └── Memory ID: "wedding_photo"
│   │
│   ├── IcePicks
│   │   └── (same structure)
│   │
│   ├── Guitar
│   │   └── (same structure)
│   │
│   ├── Phone
│   │   ├── (same structure)
│   │   └── PhoneInteraction.cs
│   │
│   └── Document
│       ├── (same structure)
│       └── DocumentInteraction.cs
│
├── ═══ UI ═══
└── Canvas (Screen Space - Overlay)
    ├── CanvasScaler
    │   ├── UI Scale Mode: Scale With Screen Size
    │   └── Reference Resolution: 480×270
    │
    ├── DialoguePanel (inactive by default)
    │   └── DialogueUI.cs
    │
    ├── PhonePanel (inactive by default)
    │   └── PhoneUI.cs
    │
    ├── MonologuePanel (inactive by default)
    │   └── MonologueUI.cs
    │
    ├── DecisionPanel (inactive by default)
    │   └── DecisionUI.cs
    │
    ├── InteractionPrompt (inactive by default)
    │   └── InteractionPrompt.cs
    │
    ├── DownloadPanel (active on first run)
    │   └── DownloadProgressUI.cs
    │
    ├── FadePanel
    │   └── Image (Black, starts transparent)
    │
    └── EndScreen (inactive by default)
        └── EndScreen.cs
```

### 13.2 Layer Configuration

```
LAYERS (Edit > Project Settings > Tags and Layers):
═══════════════════════════════════════════════════

Layer 0:  Default
Layer 1:  TransparentFX
Layer 2:  Ignore Raycast
Layer 3:  (empty)
Layer 4:  Water
Layer 5:  UI
Layer 6:  Walkable        ← Floor/walkable areas
Layer 7:  Obstacles       ← Furniture, walls
Layer 8:  Interactables   ← Clickable objects
Layer 9:  Characters      ← Robert, Martha
Layer 10: (empty)

SORTING LAYERS (for 2D rendering order):
═══════════════════════════════════════

0: Default
1: Background    ← Room background (-100 to -50)
2: Furniture     ← Decorative furniture (-50 to -10)
3: Objects       ← Interactive objects (-10 to 10)
4: Characters    ← Robert, Martha (0 to 20)
5: Foreground    ← Any foreground elements (20+)
6: UI            ← World-space UI if any
```

---

## 14. Build & Deployment

### 14.1 Build Settings

```
PLATFORM: macOS (Primary), Windows (Secondary)

PLAYER SETTINGS:
├── Company Name: [Your Name]
├── Product Name: Last Day
├── Version: 0.1.0
│
├── Resolution and Presentation:
│   ├── Default Resolution: 1920×1080
│   ├── Fullscreen Mode: Windowed
│   └── Allow Fullscreen Switch: Yes
│
├── Other Settings:
│   ├── Color Space: Gamma (for pixel art consistency)
│   ├── Auto Graphics API: No
│   ├── Graphics APIs: Metal (macOS), Direct3D11 (Windows)
│   └── Scripting Backend: Mono
│
└── Publishing Settings:
    └── macOS: Sign with Developer ID (optional for demo)

BUILD SIZE ESTIMATE:
├── Base Unity build: ~50 MB
├── Art assets: ~10 MB
├── Audio assets: ~20 MB
├── LLM Model (downloaded at runtime): ~2.2 GB
└── Total initial download: ~80 MB (+2.2 GB on first run)
```

### 14.2 First-Run Model Download

```
MODEL DOWNLOAD FLOW:
════════════════════

1. Game launches
2. Check: Does model file exist at Application.persistentDataPath/Models/?
3. If NO:
   a. Show DownloadPanel
   b. Download from HuggingFace:
      https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf
   c. Show progress (downloaded MB / total MB)
   d. Save to persistent data path
4. Initialize LLM with model file
5. Hide DownloadPanel, start game

PERSISTENT DATA PATHS:
├── macOS: ~/Library/Application Support/[CompanyName]/LastDay/
├── Windows: C:\Users\[User]\AppData\LocalLow\[CompanyName]\LastDay\
└── Model stored at: [PersistentDataPath]/Models/phi3-mini.gguf
```

### 14.3 Development Checklist

```
PRE-DEVELOPMENT:
☐ Create Unity 2D project
☐ Import TextMeshPro
☐ Import 2D Pixel Perfect package
☐ Import LLMUnity package
☐ Set up folder structure
☐ Configure layers and sorting layers
☐ Set up .gitignore

DAY 1-2 (Foundation):
☐ GameManager, EventManager, GameStateMachine
☐ SimplePathfinder with debug visualization
☐ PlayerController2D with basic movement
☐ CharacterAnimator with placeholder animations
☐ Basic Canvas with DialoguePanel

DAY 3-4 (Core Loop):
☐ LocalLLMManager integration
☐ ModelDownloader with progress UI
☐ Character prompts (Martha, David)
☐ DialogueUI with typewriter effect
☐ Click-to-move working end-to-end

DAY 5-6 (Content):
☐ Import character sprite sheets (from LPC generator)
☐ Create animation clips and Animator controllers
☐ SubtleIdleMovement on characters
☐ InteractableObject2D with glow effect
☐ 3 memory objects configured

DAY 7-8 (Phone & Endings):
☐ PhoneInteraction triggering after 2 memories
☐ PhoneUI variant
☐ DocumentInteraction with lock/unlock
☐ DecisionUI with Sign/Tear buttons
☐ EndScreen with quotes
☐ FadeManager transitions

DAY 9 (Assets & Polish):
☐ Import final art assets
☐ Import music and SFX
☐ AudioManager with crossfade
☐ Full playthrough testing
☐ Bug fixes

DAY 10 (Ship):
☐ Final playthrough
☐ Build for macOS
☐ Build for Windows
☐ Test on fresh machine
☐ Create README
☐ Package and submit
```

---

## Quick Reference Card

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    LAST DAY - QUICK REFERENCE                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  CORE LOOP:                                                                 │
│  Click object → Pathfind → Walk (animated) → Arrive → Interact → Dialogue  │
│                                                                              │
│  KEY SCRIPTS:                                                               │
│  ├── PlayerController2D.cs  - Movement + pathfinding                       │
│  ├── CharacterAnimator.cs   - Walk/idle animation states                   │
│  ├── SubtleIdleMovement.cs  - Breathing, sway, tremor                      │
│  ├── SimplePathfinder.cs    - A* grid pathfinding                          │
│  ├── InteractableObject2D.cs- Clickable objects                            │
│  ├── DialogueUI.cs          - Dialogue panel + typewriter                  │
│  └── LocalLLMManager.cs     - AI response generation                       │
│                                                                              │
│  SPRITE SIZES:                                                              │
│  ├── Characters: 32×48 px per frame                                        │
│  ├── Objects: 32×32 px                                                      │
│  ├── Portraits: 64×64 px                                                    │
│  └── Background: 480×270 px                                                 │
│                                                                              │
│  ANIMATION TIMING:                                                          │
│  ├── Walk: 4 frames @ 0.15 sec = 0.6 sec cycle                             │
│  ├── Idle: 3 frames @ 0.5 sec = 1.5 sec cycle                              │
│  └── Breathing code: 0.4 Hz (2.5 sec per breath)                           │
│                                                                              │
│  CAMERA:                                                                    │
│  ├── Orthographic, Size: 4.2                                               │
│  ├── Reference Resolution: 480×270                                         │
│  └── Pixels Per Unit: 32                                                    │
│                                                                              │
│  PATHFINDING:                                                               │
│  ├── Grid Cell Size: 0.25 units                                            │
│  ├── Obstacle Layer: "Obstacles"                                            │
│  └── Walkable Layer: "Walkable"                                             │
│                                                                              │
│  LLM:                                                                       │
│  ├── Model: Phi-3-mini-4k-instruct-q4 (2.2 GB)                             │
│  ├── Max Tokens: 80                                                         │
│  └── Expected Latency: ~2 sec on M4 Mac                                    │
│                                                                              │
│  MEMORY SYSTEM:                                                             │
│  ├── Gaze for 2 sec → triggers memory                                      │
│  ├── 2 memories → unlock document                                          │
│  └── 2 memories → trigger phone call                                       │
│                                                                              │
│  ENDINGS:                                                                   │
│  ├── Sign → Fade to black → Viktor Frankl quote                           │
│  └── Tear → Fade to black → Cicero quote                                  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```
