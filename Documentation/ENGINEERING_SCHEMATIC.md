# Last Day - Complete Engineering Schematic v4

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
Robert, an elderly man with ALS, is seated in his living room. His objective
is to sign his end-of-life document on the computer — but his past self,
driven by self-loathing, locked the document behind three security questions
referencing his darkest secrets.

Martha (the wife) sits across from him. She is programmed to protect Robert
from his guilt — she lies, deflects, and sanitizes their history.

David (the best friend) is available via the phone. He is brutally honest
and wants Robert to face his sins before he dies.

The player must solve three mysteries by investigating objects, cross-
referencing Martha's lies against David's truths, and typing the correct
answers into the computer to unlock the document and face the final choice.

CORE LOOP:
1. Click Computer → see security question (the riddle)
2. Click the relevant object → Martha gives her sanitized version
3. Call David on the phone → he reveals the ugly truth
4. Type the truth into the Computer → unlock the next question
5. After all three → "Can you forgive yourself?" → Sign or Tear

THREE MYSTERIES:
  Q1 "Emergency Contact for the '98 K2 Expedition" → Answer: Arthur
  Q2 "Beneficiary Name for Offshore Account 4014"  → Answer: Lily
  Q3 "Date of Your Proudest Moment"                → Answer: 10th Anniversary

TECHNICAL SHOWCASE:
- Local LLM integration (Phi-3-mini via LLMUnity)
- Dynamic, state-driven NPC prompts (6 Martha personas, 4 David personas)
- Guitar breakdown: player evidence forces LLM prompt shift mid-conversation
- Pixel art aesthetic (Stardew Valley inspired)

SCOPE:
- 1 room
- 2 NPCs (Martha in-room, David via phone)
- 5 interactive objects (ice_picks, wedding_photo, guitar, phone, computer)
- 1 document object (locked until all questions answered)
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
CORE GAME LOOP — SECURITY QUESTION INVESTIGATION:
══════════════════════════════════════════════════

1. Player clicks Computer
   │
   ▼
2. ComputerInteraction.OnInteract()
   ├── Opens computerPanel, shows current question text
   ├── EventManager.OnSecurityQuestionStarted(index)
   │     ├── Sets activeSecurityQuestion (shifts Martha/David LLM prompts)
   │     └── CheckPhoneTrigger() — rings phone on Q1 so player can call David
   └── DialogueUI.ShowMonologue(hint) — internal monologue points to relevant object
   │
   ▼
3. Player clicks relevant object (ice_picks / wedding_photo / guitar)
   ├── ClickToMoveHandler → pathfind → arrive → InteractableObject2D.OnInteract()
   ├── EventManager.PublishEvent("interact", memoryId)
   └── DialogueUI.OpenForObject() → Martha gives her sanitized version
   │
   ▼
4. Player calls David (clicks phone)
   ├── DialogueUI.OpenForPhone()
   ├── LocalLLMManager builds David's prompt using activeSecurityQuestion
   └── David reveals the truth (names Arthur / Lily / admits blind spot)
   │
   ▼
5. Player returns to Computer, types the answer
   ├── ComputerInteraction.OnSubmitClicked()
   ├── IsCorrectAnswer() validates input (case-insensitive, multiple accepted forms)
   ├── EventManager.OnSecurityQuestionAnswered(index)
   └── currentQuestionIndex++ → DisplayCurrentQuestion() for next mystery
   │
   ▼
6. After all 3 answered:
   ├── EventManager.OnAllSecurityQuestionsAnswered()
   │     ├── documentUnlocked = true
   │     ├── marthaShutdownMode = true (Martha's LLM prompt permanently shifts)
   │     ├── GameEvents.UnlockDocument()
   │     └── GameEvents.AllQuestionsAnswered()
   └── ComputerInteraction.ShowFinalPrompt()
         └── "Can you forgive yourself?" → Sign / Tear → GameManager.EndGame()


DIALOGUE RESPONSE GENERATION:
═════════════════════════════

DialogueUI.SubmitInput()
   │
   ├── Guitar breakdown detection (Q3 + Martha + damage keywords)
   │     └── Sets marthaGuitarBreakdown = true → shifts LLM prompt mid-conversation
   │
   ├── LocalLLMManager.GenerateResponse(playerInput, character, memories)
   │     ├── Reads activeSecurityQuestion, shutdownMode, guitarBreakdown from EventManager
   │     ├── Builds dynamic system prompt via CharacterPrompts.GetMarthaPrompt / GetDavidPrompt
   │     └── Returns LLM response (or stub response in stub mode)
   │
   └── Response displayed with typewriter effect


PLAYER CLICK → MOVEMENT FLOW (unchanged):
══════════════════════════════════════════

1. Player clicks on screen
   ▼
2. ClickToMoveHandler detects click
   ├── Check: Is click on Interactable? → MoveToAndInteract()
   └── Check: Is click on Walkable area? → MoveTo()
   ▼
3. SimplePathfinder.FindPath(start, destination)
   ▼
4. PlayerController2D follows path
   ├── Updates CharacterAnimator (walk animation)
   └── On arrival: triggers interaction callback
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
// EventManager.cs — Centralized game state + security question progression
using UnityEngine;
using System.Collections.Generic;
using LastDay.Utilities;

namespace LastDay.Core
{
    [System.Serializable]
    public struct GameEvent
    {
        public string eventType;   // "gaze_complete", "interact"
        public string objectId;
        public string memoryId;
        public float timestamp;

        public GameEvent(string eventType, string objectId, string memoryId)
        {
            this.eventType = eventType;
            this.objectId = objectId;
            this.memoryId = memoryId;
            this.timestamp = Time.time;
        }
    }

    public class EventManager : Singleton<EventManager>
    {
        [Header("Game Progress")]
        public List<string> triggeredMemories = new List<string>();
        public bool hasAskedForHelp;
        public bool documentUnlocked;
        public bool phoneHasRung;

        [Header("Security Questions")]
        // 0 = no question active yet, 1-3 = which mystery the player is currently on
        public int activeSecurityQuestion = 0;
        public bool marthaShutdownMode = false;
        public bool marthaGuitarBreakdown = false;

        private List<GameEvent> eventHistory = new List<GameEvent>();

        // Subscribes to GameStateMachine.OnStateChanged to relay via GameEvents bus.
        // (subscription code omitted for brevity — see full source)

        public void PublishEvent(GameEvent evt)
        {
            eventHistory.Add(evt);

            switch (evt.eventType)
            {
                case "gaze_complete":
                    HandleGazeComplete(evt);
                    break;
                case "interact":
                    HandleInteract(evt);
                    break;
            }
        }

        private void HandleGazeComplete(GameEvent evt)
        {
            if (!string.IsNullOrEmpty(evt.memoryId) && !triggeredMemories.Contains(evt.memoryId))
            {
                triggeredMemories.Add(evt.memoryId);
                GameEvents.TriggerMemory(evt.memoryId);
            }
            GameEvents.CompleteGaze(evt.objectId);
        }

        private void HandleInteract(GameEvent evt)
        {
            GameEvents.InteractWithObject(evt.objectId);
        }

        // ── Security Question API (called by ComputerInteraction) ──

        // Called when a question is first SHOWN (not answered).
        // Shifts Martha/David LLM prompts and rings the phone on Q1.
        public void OnSecurityQuestionStarted(int questionIndex)
        {
            int newActive = questionIndex + 1;         // 0-indexed → 1-indexed
            if (newActive <= activeSecurityQuestion) return;

            activeSecurityQuestion = newActive;
            CheckPhoneTrigger();

            if (activeSecurityQuestion == 3)
                GameEvents.MarthaBreakdownReady();      // Q3 active — guitar confrontation possible
        }

        // Called each time a question is correctly answered.
        public void OnSecurityQuestionAnswered(int questionIndex)
        {
            GameEvents.SecurityQuestionAnswered(questionIndex);
        }

        // Called when all three security questions are answered.
        public void OnAllSecurityQuestionsAnswered()
        {
            documentUnlocked = true;
            marthaShutdownMode = true;
            GameEvents.UnlockDocument();
            GameEvents.AllQuestionsAnswered();
        }

        private void CheckPhoneTrigger()
        {
            if (!phoneHasRung && activeSecurityQuestion >= 1)
            {
                phoneHasRung = true;
                GameEvents.RingPhone();
            }
        }
    }
}
```

**Key state fields read by LLM prompt builders:**

| Field | Type | Meaning |
|---|---|---|
| `activeSecurityQuestion` | int 0-3 | Which mystery is active (0 = none, 1 = Mountain, 2 = Child, 3 = Guitar) |
| `marthaShutdownMode` | bool | All questions answered — Martha's facade permanently dropped |
| `marthaGuitarBreakdown` | bool | Player confronted Martha with guitar evidence — breakdown triggered |
| `triggeredMemories` | List\<string\> | Memory IDs the player has gazed at (injected into LLM context) |

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

### 7.1 Architecture Overview

The dialogue system uses **dynamic, state-driven LLM prompts**. Both Martha and David have multiple personas that shift based on `EventManager.activeSecurityQuestion`, `marthaShutdownMode`, and `marthaGuitarBreakdown`.

```
                    ┌─────────────────────────────────────────────────┐
                    │           LocalLLMManager                        │
                    │  ┌───────────────────────────────────────────┐  │
                    │  │ GenerateResponse(playerInput, character)   │  │
                    │  │                                            │  │
                    │  │  1. Read state from EventManager:          │  │
                    │  │     activeSecurityQuestion (0-3)           │  │
                    │  │     marthaShutdownMode                     │  │
                    │  │     marthaGuitarBreakdown                  │  │
                    │  │                                            │  │
                    │  │  2. Build system prompt:                   │  │
                    │  │     CharacterPrompts.GetMarthaPrompt(...)  │  │
                    │  │     CharacterPrompts.GetDavidPrompt(...)   │  │
                    │  │                                            │  │
                    │  │  3. LLM generates → Validate → Return     │  │
                    │  └───────────────────────────────────────────┘  │
                    └─────────────────────────────────────────────────┘

MARTHA PROMPT STATE MACHINE:
════════════════════════════
  Q0 → Warm Caretaker (default — gentle, deflects pain)
  Q1 → Hero Narrative  (K2: Robert was brave, NEVER says "cut", NEVER names Arthur)
  Q2 → Defensive Wife   (Money: "bad investments", NEVER names Sarah or Lily)
  Q3 → Romantic Lie     (Guitar: beautiful anniversary song, sunrise, coffee)
  Q3 + guitarBreakdown → Breakdown (admits drunk night, smashed guitar, exhausted grief)
  shutdownMode → Shutdown (raw grief, no comfort, no deflection, no pet names)

DAVID PROMPT STATE MACHINE:
═══════════════════════════
  Q0 → Loyal Friend     (default — honest, won't push, lost Margaret)
  Q1 → Cold / Arthur    (names Arthur, says Robert cut the rope, was on radio)
  Q2 → Disappointed     (names Sarah and Lily, 25 years of child support)
  Q3 → Blind Spot       (genuinely doesn't know about guitar, redirects to Martha)
```

### 7.2 LocalLLMManager.cs

```csharp
// LocalLLMManager.cs — Manages LLM or stub responses with narrative-aware prompting
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastDay.Core;

namespace LastDay.Dialogue
{
    public class LocalLLMManager : MonoBehaviour
    {
        public static LocalLLMManager Instance { get; private set; }

        [Header("LLM Settings")]
        [SerializeField] private int maxTokens = 80;
        [SerializeField] private float temperature = 0.7f;

        [Header("State")]
        public bool isInitialized;
        public string currentCharacter = "martha";
        public bool useLLM = true;

        // When LLMUNITY_AVAILABLE is defined, LLMAgent references are used.
        // Otherwise, falls back to stub responses that mirror the narrative.

        public async Task<string> GenerateResponse(
            string playerInput,
            string character = null,
            List<string> memories = null)
        {
            if (character != null) currentCharacter = character;

            // 1. Read narrative state from EventManager
            int activeQuestion   = EventManager.Instance?.activeSecurityQuestion ?? 0;
            bool shutdownMode    = EventManager.Instance != null && EventManager.Instance.marthaShutdownMode;
            bool guitarBreakdown = EventManager.Instance != null && EventManager.Instance.marthaGuitarBreakdown;

            // 2. Build dynamic system prompt
            string prompt = currentCharacter == "david"
                ? CharacterPrompts.GetDavidPrompt(memories ?? new List<string>(), activeQuestion)
                : CharacterPrompts.GetMarthaPrompt(memories ?? new List<string>(), activeQuestion, shutdownMode, guitarBreakdown);

            // 3. Send to LLM (or return stub)
            // ... (LLMAgent.Chat or stub fallback)
        }

        // Stub responses are narrative-aware: they check activeQuestion,
        // shutdownMode, guitarBreakdown, and keyword-match the player input
        // to return appropriate hard-coded responses for each mystery state.
        //
        // See full implementation in Assets/Scripts/Dialogue/LocalLLMManager.cs
    }
}
```

### 7.3 CharacterPrompts.cs

The prompt system is fully dynamic. Each call to `GetMarthaPrompt` / `GetDavidPrompt` assembles a prompt from modular pieces:

```csharp
// CharacterPrompts.cs — State-driven LLM prompts
namespace LastDay.Dialogue
{
    public static class CharacterPrompts
    {
        // Martha: 6 possible states
        public static string GetMarthaPrompt(
            List<string> triggeredMemories,
            int activeQuestion = 0,        // 0-3
            bool shutdownMode = false,
            bool guitarBreakdown = false)
        {
            if (shutdownMode)              return GetMarthaShutdownPrompt();
            if (activeQuestion == 3 && guitarBreakdown)
                                           return GetMarthaGuitarBreakdownPrompt(triggeredMemories);

            return BuildMarthaCore()
                 + BuildMarthaQuestionState(activeQuestion)
                 + BuildMarthaMemorySection(triggeredMemories, activeQuestion);
        }

        // David: 4 possible states
        public static string GetDavidPrompt(
            List<string> triggeredMemories,
            int activeQuestion = 0)
        {
            return BuildDavidCore()
                 + BuildDavidQuestionState(activeQuestion)
                 + BuildDavidMemorySection(triggeredMemories, activeQuestion);
        }

        // Opening lines per object + active question (used by DialogueUI)
        public static string GetObjectOpeningLine(
            string memoryId,
            string character = "martha",
            int activeQuestion = 0) { ... }
    }
}
```

**Critical guardrails enforced in every Martha/David prompt:**
- "Never mention being an AI, a model, or a character."
- Martha Q1: "NEVER say 'cut' in relation to the rope. NEVER name Arthur."
- Martha Q2: "NEVER mention Sarah. NEVER name any child. NEVER say 'Lily.'"
- Martha Q3 pre-breakdown: Maintain the romantic version unless physically confronted.
- David Q3: "Do not speculate. Do not invent. You are genuinely blind here."

**Guitar breakdown trigger (in DialogueUI.SubmitInput):**
When `activeSecurityQuestion == 3` and `currentCharacter == "martha"`, player input is scanned for keywords: `crack`, `smash`, `broken`, `broke`, `shatter`, `damaged`, `neck`, `why is it`. A match sets `marthaGuitarBreakdown = true` and the next LLM response uses the breakdown prompt.

---

## 8. Interactive Objects

### 8.1 InteractableObject2D.cs (Base Class)

The base class handles hover glow, gaze timer, event publishing, and default dialogue opening. Subclasses override `OnInteract()` for custom behavior.

```csharp
// InteractableObject2D.cs
using UnityEngine;
using UnityEngine.EventSystems;
using LastDay.Core;

namespace LastDay.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class InteractableObject2D : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Object Identity")]
        [SerializeField] private string objectId;
        [SerializeField] private string displayName;
        [SerializeField] private string memoryId;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer highlightRenderer;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.8f, 0.3f);

        [Header("Gaze/Hover")]
        [SerializeField] private float hoverTimeToTrigger = 2f;

        public string ObjectId => objectId;
        public string DisplayName => displayName;
        public string MemoryId => memoryId;

        protected virtual void Start()  // virtual — ComputerInteraction overrides
        {
            if (highlightRenderer != null) highlightRenderer.enabled = false;
        }

        // Gaze timer → OnGazeComplete() → EventManager.PublishEvent("gaze_complete")
        // OnPointerEnter / OnPointerExit → highlight management

        public virtual void OnInteract()
        {
            // Default: open Martha dialogue about this object
            var dialogueUI = FindObjectOfType<UI.DialogueUI>();
            if (dialogueUI != null)
                dialogueUI.OpenForObject(objectId, memoryId, displayName);
        }
    }
}
```

### 8.2 ComputerInteraction.cs (NEW — Security Question Gate)

The computer is the central hub of the game loop. It displays sequential security questions, validates player answers, advances the narrative state, and presents the final "Can you forgive yourself?" prompt.

```csharp
// ComputerInteraction.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LastDay.Core;
using LastDay.UI;

namespace LastDay.Interaction
{
    public class ComputerInteraction : InteractableObject2D
    {
        [Header("Computer UI Panel")]
        [SerializeField] private GameObject computerPanel;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_InputField answerInputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button closeButton;

        [Header("Final Prompt UI")]
        [SerializeField] private GameObject finalPromptPanel;
        [SerializeField] private TMP_Text finalPromptText;
        [SerializeField] private Button signButton;
        [SerializeField] private Button tearButton;

        [Header("Monologue Hints")]
        [SerializeField] private string[] monologueHints = new string[]
        {
            "Emergency contact for the '98 K2 Expedition. I remember the mountain. I remember the rope.",
            "Beneficiary for offshore account 4014. There was money going somewhere every month...",
            "Date of my proudest moment. The guitar is in the corner. I should look at the guitar."
        };

        private static readonly string[] Questions = new string[]
        {
            "SECURITY CHECK 1\n\nEmergency Contact for the '98 K2 Expedition.",
            "SECURITY CHECK 2\n\nBeneficiary Name for Offshore Account 4014.",
            "SECURITY CHECK 3\n\nDate of Your Proudest Moment."
        };

        private static readonly string[][] Answers = new string[][]
        {
            new[] { "arthur" },
            new[] { "lily" },
            new[] { "10th anniversary", "10th", "tenth anniversary", "our 10th anniversary" }
        };

        private int currentQuestionIndex = 0;
        private bool allAnswered = false;

        // OnInteract() → opens computerPanel and displays current question
        // DisplayCurrentQuestion() → calls EventManager.OnSecurityQuestionStarted()
        // OnSubmitClicked() → validates via IsCorrectAnswer(), calls OnCorrectAnswer()
        // OnCorrectAnswer() → advances index, calls OnAllSecurityQuestionsAnswered() if done
        // ShowFinalPrompt() → "Can you forgive yourself?" + Sign/Tear buttons
        // OnSignClicked() / OnTearClicked() → FadeManager → GameManager.EndGame()

        // Start() syncs to saved progress:
        //   currentQuestionIndex = Clamp(activeSecurityQuestion - 1, 0, Length)
        //   allAnswered = marthaShutdownMode

        // Subscribes to GameEvents.OnAllQuestionsAnswered for external triggers.
        // DecisionUI yields to ComputerInteraction when both are in scene.
    }
}
```

### 8.3 DocumentInteraction.cs

The document is now a secondary object. It unlocks via `GameEvents.OnDocumentUnlocked` (fired when all security questions are answered) but the primary Sign/Tear flow goes through `ComputerInteraction`.

```csharp
// DocumentInteraction.cs — Locked until all 3 security questions answered
namespace LastDay.Interaction
{
    public class DocumentInteraction : InteractableObject2D
    {
        [SerializeField] private string lockedMessage = "Not yet... I'm not ready to look at that.";
        private bool isUnlocked;

        // Subscribes to GameEvents.OnDocumentUnlocked
        // OnInteract(): if locked → ShowMonologue(lockedMessage)
        //               if unlocked → GameState.Decision → DecisionUI.Show()
    }
}
```

---

## 9. Game Flow & State Machine

### 9.1 Complete Game Flow Diagram

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                              GAME FLOW                                        │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌─────────────┐                                                             │
│  │   LOADING   │ ◄─── Game Start                                             │
│  │  (Download  │                                                             │
│  │   Model)    │                                                             │
│  └──────┬──────┘                                                             │
│         │ Model Ready                                                         │
│         ▼                                                                     │
│  ┌─────────────┐                                                             │
│  │   PLAYING   │ ◄─────────────────────────────────┐                        │
│  │  (Explore)  │                                    │                        │
│  └──────┬──────┘                                    │                        │
│         │                                            │                        │
│    ┌────┴──────────────────┬─────────────────┐      │                        │
│    │                       │                  │      │                        │
│    ▼                       ▼                  ▼      │                        │
│  ┌──────────────┐  ┌─────────────┐  ┌────────────┐ │                        │
│  │ IN_DIALOGUE  │  │ PHONE_CALL  │  │ COMPUTER   │ │                        │
│  │  (Martha +   │  │  (David)    │  │ (Security  │ │                        │
│  │   Objects)   │  │             │  │  Questions)│ │                        │
│  └──────┬───────┘  └──────┬──────┘  └─────┬──────┘ │                        │
│         │ Close            │ Hang Up       │ Close   │                        │
│         └──────────────────┴───────────────┴─────────┘                        │
│                                                                               │
│                            ┌──────────────────────┐                          │
│     All 3 Answered ──────►│ Martha → Shutdown     │                          │
│                            │ Phone rings (Q1 shown)│                          │
│                            └──────────┬───────────┘                          │
│                                       │                                       │
│                                       ▼                                       │
│                            ┌──────────────────────┐                          │
│                            │     DECISION          │                          │
│                            │ "Can you forgive      │                          │
│                            │  yourself?"           │                          │
│                            │                       │                          │
│                            │  [SIGN]     [TEAR]    │                          │
│                            └────┬───────────┬──────┘                          │
│                                 │           │                                  │
│                                 ▼           ▼                                  │
│                          ┌───────────┐ ┌───────────┐                          │
│                          │  ENDING   │ │  ENDING   │                          │
│                          │ (Sign —   │ │ (Tear —   │                          │
│                          │  Fade to  │ │ ending_   │                          │
│                          │  black)   │ │ torn.wav) │                          │
│                          └───────────┘ └───────────┘                          │
│                                                                               │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 9.2 Security Question & Narrative Flow

```
THREE-MYSTERY INVESTIGATION LOOP:
══════════════════════════════════

  ┌─────────────────────────────────────────────────────────────────────────┐
  │                    MYSTERY 1: THE MOUNTAIN                               │
  │                                                                          │
  │  Computer shows: "Emergency Contact for the '98 K2 Expedition"          │
  │  EventManager.activeSecurityQuestion → 1                                │
  │  Phone rings (David available)                                          │
  │                                                                          │
  │  ice_picks.png → Martha: "He was brave. The storm was impossible."      │
  │  phone.png     → David:  "His name was Arthur. You cut the rope."       │
  │                                                                          │
  │  Answer: Arthur                                                          │
  └─────────────────────────────────────────────────────────────────────────┘
         │
         ▼
  ┌─────────────────────────────────────────────────────────────────────────┐
  │                 MYSTERY 2: THE SECRET CHILD                              │
  │                                                                          │
  │  Computer shows: "Beneficiary Name for Offshore Account 4014"           │
  │  EventManager.activeSecurityQuestion → 2                                │
  │  Monologue: "I never invested. Where did the money go?"                 │
  │                                                                          │
  │  wedding_photo.png → Martha: "Bad investments. Just the two of us."     │
  │  phone.png         → David:  "Sarah. Lily. 25 years of payments."      │
  │                                                                          │
  │  Answer: Lily                                                            │
  └─────────────────────────────────────────────────────────────────────────┘
         │
         ▼
  ┌─────────────────────────────────────────────────────────────────────────┐
  │              MYSTERY 3: THE BROKEN MARRIAGE                              │
  │                                                                          │
  │  Computer shows: "Date of Your Proudest Moment"                         │
  │  EventManager.activeSecurityQuestion → 3                                │
  │  GameEvents.MarthaBreakdownReady() fired                                │
  │                                                                          │
  │  guitar.png → Monologue: "Crack down the back of the neck. Broken."    │
  │            → Martha: "10th anniversary. Sunrise. Beautiful song."        │
  │  phone.png → David:  "I don't know. That one's between you and Martha."│
  │                                                                          │
  │  CONFRONTATION: Player types evidence (crack/smash/broken keywords)     │
  │  → marthaGuitarBreakdown = true                                         │
  │  → Martha breaks down: admits drunk night, smashed guitar               │
  │                                                                          │
  │  Answer: 10th Anniversary                                                │
  └─────────────────────────────────────────────────────────────────────────┘
         │
         ▼
  ┌─────────────────────────────────────────────────────────────────────────┐
  │                      CLIMAX                                              │
  │                                                                          │
  │  marthaShutdownMode = true (raw grief, no comfort, no deflection)       │
  │  documentUnlocked = true                                                 │
  │                                                                          │
  │  Computer: "FINAL SECURITY CHECK — Can you forgive yourself?"           │
  │                                                                          │
  │  [SIGN] → Robert signs. Fade to black. The coward's release.           │
  │  [TEAR] → Robert tears. ending_torn.wav. He chooses to face it.        │
  └─────────────────────────────────────────────────────────────────────────┘


MEMORY/GAZE SYSTEM (feeds LLM context, unchanged):
═══════════════════════════════════════════════════

Player hovers over object (2+ sec)
        │
        ▼
InteractableObject2D.OnGazeComplete()
        │
        ▼
EventManager.PublishEvent("gaze_complete", memoryId)
        ├── Add to triggeredMemories
        ├── GameEvents.TriggerMemory(memoryId)
        └── Injected into Martha/David LLM prompts as "WHAT MARTHA/DAVID IS AWARE OF"
```

---

## 10. UI System

### 10.1 UI Hierarchy

```
Canvas (Screen Space - Overlay)
│
├── DialoguePanel (shared by Martha objects + David phone calls)
│   ├── Background (9-slice pixel art)
│   ├── PortraitFrame
│   │   └── PortraitImage (marthaPortrait / davidPortrait)
│   ├── NamePlate
│   │   └── NameText (TMP) — "Martha" or "David (Phone)"
│   ├── DialogueText (TMP) — typewriter effect
│   ├── InputContainer
│   │   ├── InputField (TMP) — player types here; scanned for guitar breakdown keywords
│   │   └── SendButton
│   ├── CloseButton
│   └── ThinkingIndicator
│
├── ComputerPanel (NEW — managed by ComputerInteraction.cs)
│   ├── Background (dark monitor aesthetic)
│   ├── QuestionText (TMP) — "SECURITY CHECK 1\n\nEmergency Contact..."
│   ├── AnswerInputField (TMP)
│   ├── SubmitButton
│   ├── FeedbackText (TMP) — "Incorrect. Think harder."
│   └── CloseButton
│
├── FinalPromptPanel (NEW — managed by ComputerInteraction.cs)
│   ├── Background
│   ├── FinalPromptText (TMP) — "Can you forgive yourself?"
│   ├── SignButton
│   └── TearButton
│
├── MonologuePanel (Robert's internal monologue / subconscious)
│   └── MonologueText (TMP, italicized)
│
├── DecisionPanel (fallback — only used if ComputerInteraction is absent)
│   ├── DimBackground
│   ├── PromptText — "FINAL SECURITY CHECK\n\nCan you forgive yourself?"
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
// DialogueUI.cs — Shared dialogue panel for Martha (objects) and David (phone)
// Key narrative responsibilities:
//   - Guitar breakdown detection via keyword scan in SubmitInput()
//   - Opening lines from CharacterPrompts.GetObjectOpeningLine() (narrative-aware)
//   - Monologue display for Robert's internal voice

namespace LastDay.UI
{
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject dialoguePanel;

        [Header("Character Display")]
        [SerializeField] private Image characterPortrait;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text dialogueText;

        [Header("Player Input")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button closeButton;

        [Header("Thinking Indicator")]
        [SerializeField] private GameObject thinkingIndicator;

        [Header("Monologue")]
        [SerializeField] private GameObject monologuePanel;
        [SerializeField] private TMP_Text monologueText;

        [Header("Typewriter")]
        [SerializeField] private float typewriterSpeed = 0.03f;

        [Header("Character Portraits")]
        [SerializeField] private Sprite marthaPortrait;
        [SerializeField] private Sprite davidPortrait;

        // ── Entry Points ──

        // OpenForObject(objectId, memoryId, displayName)
        //   Sets character = "martha", shows Martha's opening line
        //   If guitar + Q3 active → ShowMonologue("crack in the neck")

        // OpenForNPC(npcId, npcName)
        //   Direct NPC click — uses character-specific greeting

        // OpenForPhone()
        //   Sets character = "david", GameState → PhoneCall
        //   Greeting is narrative-aware via GetObjectOpeningLine("phone", "david", activeQuestion)

        // ShowMonologue(text)
        //   Robert's internal monologue — auto-hides after 3 seconds

        // ── Narrative Logic in SubmitInput() ──

        // GUITAR BREAKDOWN DETECTION:
        // When activeSecurityQuestion == 3, currentCharacter == "martha",
        // and player input contains any of:
        //   crack, smash, broken, broke, shatter, damaged, neck, "why is it"
        // → Sets EventManager.marthaGuitarBreakdown = true
        // → Fires GameEvents.MarthaBreakdownReady()
        // → The next LLM response uses the breakdown prompt
        //
        // This happens BEFORE GenerateResponse() so the LLM gets the shifted prompt.

        // ── Response Flow ──
        // SubmitInput → LocalLLMManager.GenerateResponse(playerText, character, memories)
        // → ShowResponse(text) → TypewriterEffect coroutine
        // → GameEvents.ReceiveDialogue(character, response)
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
│  wedding_photo.png + wedding_photo_glow.png  (Mystery 2 clue)  │
│  ice_picks.png + ice_picks_glow.png          (Mystery 1 clue)  │
│  guitar.png + guitar_glow.png                (Mystery 3 clue)  │
│  phone.png + phone_glow.png                  (David access)     │
│  computer.png + computer_glow.png            (Security Q hub)   │
│  document.png + document_glow.png            (End-of-life doc)  │
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
│   ├── Computer (NEW — central hub)
│   │   ├── SpriteRenderer (Order: 1) → computer.png
│   │   ├── GlowSprite (child, Order: 0) → computer_glow.png
│   │   ├── BoxCollider2D (Layer: Interactables)
│   │   └── ComputerInteraction.cs
│   │       ├── Object ID: "computer"
│   │       ├── Memory ID: "computer"
│   │       ├── computerPanel → Canvas/ComputerPanel
│   │       ├── finalPromptPanel → Canvas/FinalPromptPanel
│   │       └── monologueHints[3] (one per mystery)
│   │
│   ├── WeddingPhoto (Mystery 2 clue)
│   │   ├── SpriteRenderer (Order: 1)
│   │   ├── GlowSprite (child, Order: 0)
│   │   ├── BoxCollider2D (Layer: Interactables)
│   │   └── InteractableObject2D.cs
│   │       ├── Object ID: "wedding_photo"
│   │       └── Memory ID: "wedding_photo"
│   │
│   ├── IcePicks (Mystery 1 clue)
│   │   └── (same structure, memoryId: "ice_picks")
│   │
│   ├── Guitar (Mystery 3 clue)
│   │   └── (same structure, memoryId: "guitar")
│   │
│   ├── Phone (David access)
│   │   ├── (same structure, memoryId: "phone")
│   │   └── PhoneInteraction.cs
│   │
│   └── Document (locked until all questions answered)
│       ├── (same structure, memoryId: "document")
│       └── DocumentInteraction.cs
│
├── ═══ UI ═══
└── Canvas (Screen Space - Overlay)
    ├── CanvasScaler
    │   ├── UI Scale Mode: Scale With Screen Size
    │   └── Reference Resolution: 480×270
    │
    ├── DialoguePanel (inactive by default)
    │   └── DialogueUI.cs (shared for Martha + David)
    │
    ├── ComputerPanel (inactive by default, NEW)
    │   └── Managed by ComputerInteraction.cs (security questions)
    │
    ├── FinalPromptPanel (inactive by default, NEW)
    │   └── Managed by ComputerInteraction.cs ("Can you forgive yourself?")
    │
    ├── MonologuePanel (inactive by default)
    │   └── Managed by DialogueUI.ShowMonologue()
    │
    ├── DecisionPanel (inactive by default — fallback only)
    │   └── DecisionUI.cs (yields to ComputerInteraction when present)
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
☑ Create Unity 2D project
☑ Import TextMeshPro
☑ Import 2D Pixel Perfect package
☑ Import LLMUnity package
☑ Set up folder structure
☑ Configure layers and sorting layers
☑ Set up .gitignore

FOUNDATION:
☑ GameManager, EventManager, GameStateMachine
☑ SimplePathfinder with debug visualization
☑ PlayerController2D with basic movement
☑ CharacterAnimator with placeholder animations
☑ Basic Canvas with DialoguePanel

CORE LOOP — SECURITY QUESTIONS:
☑ ComputerInteraction.cs (question display, answer validation, final prompt)
☑ EventManager security question API (OnSecurityQuestionStarted/Answered/All)
☑ GameEvents for question progression (OnSecurityQuestionAnswered, OnAllQuestionsAnswered)
☑ Phone rings on Q1 display (CheckPhoneTrigger)
☑ Computer panel UI (ComputerPanel + FinalPromptPanel in Canvas)

NARRATIVE-AWARE DIALOGUE:
☑ CharacterPrompts.cs (6 Martha states, 4 David states, dynamic assembly)
☑ LocalLLMManager narrative-state reading (activeQuestion, shutdown, breakdown)
☑ Stub responses for all mystery states
☑ Guitar breakdown keyword detection in DialogueUI.SubmitInput()
☑ Opening lines per object + active question
☑ AI identity guardrails in ALL prompt states (including breakdown + shutdown)

MEMORY OBJECTS & ASSETS:
☑ InteractableObject2D base class (virtual Start, gaze, glow)
☑ 5 memory ScriptableObjects (ice_picks, wedding_photo, guitar, phone, computer)
☑ Memory_Document.asset updated for security question narrative
☑ Placeholder computer sprites (computer.png, computer_glow.png)
☐ Final art assets for all objects
☐ Character sprite sheets (Robert, Martha)

PHONE & ENDINGS:
☑ PhoneInteraction triggering on Q1 display
☑ DialogueUI.OpenForPhone() with narrative-aware greeting
☑ DocumentInteraction with lock/unlock (fallback path)
☑ DecisionUI yields to ComputerInteraction (no double-panel)
☑ FadeManager transitions for Sign/Tear endings
☐ EndScreen with quotes
☐ ending_torn.wav audio asset

POLISH & TESTING:
☑ NarrativeTests.cs (answer validation, state progression, prompt selection)
☑ GameplayTests.cs updated for new opening lines
☑ KEYWORD_REFERENCE.md documentation
☐ Full playthrough testing with LLM enabled
☐ AudioManager with crossfade
☐ Final art and SFX import
☐ Build for macOS
☐ Test on fresh machine
```

---

## Quick Reference Card

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                    LAST DAY - QUICK REFERENCE                                 │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  CORE LOOP:                                                                  │
│  Computer (question) → Object (Martha lies) → Phone (David's truth) →       │
│  Computer (type answer) → Next question → ... → "Can you forgive yourself?" │
│                                                                               │
│  THREE MYSTERIES:                                                            │
│  ├── Q1 "Emergency Contact, '98 K2"    → ice_picks → Arthur                │
│  ├── Q2 "Beneficiary, Account 4014"    → wedding_photo → Lily              │
│  └── Q3 "Date of Proudest Moment"      → guitar → 10th Anniversary         │
│                                                                               │
│  KEY SCRIPTS:                                                                │
│  ├── ComputerInteraction.cs  - Security question hub + final decision       │
│  ├── CharacterPrompts.cs     - 6 Martha personas, 4 David personas          │
│  ├── LocalLLMManager.cs      - Narrative-aware LLM/stub response gen        │
│  ├── EventManager.cs         - activeSecurityQuestion, shutdown, breakdown  │
│  ├── DialogueUI.cs           - Dialogue + guitar breakdown keyword scan     │
│  ├── InteractableObject2D.cs - Base clickable object (gaze, hover, glow)    │
│  ├── DocumentInteraction.cs  - Locked document (fallback decision)          │
│  ├── PlayerController2D.cs   - Movement + pathfinding                       │
│  └── GameStateMachine.cs     - Loading→Playing↔InDialogue/Phone/Decision   │
│                                                                               │
│  MARTHA PROMPT STATES:                                                       │
│  ├── Q0: Warm Caretaker (default)                                           │
│  ├── Q1: Hero Narrative  (NEVER says "cut", NEVER names Arthur)             │
│  ├── Q2: Defensive Wife  (NEVER names Sarah or Lily)                        │
│  ├── Q3: Romantic Lie    (beautiful anniversary song)                        │
│  ├── Q3+breakdown: Admits drunk night, smashed guitar                       │
│  └── Shutdown: Raw grief, no comfort, no deflection                         │
│                                                                               │
│  DAVID PROMPT STATES:                                                        │
│  ├── Q0: Loyal Friend    │  Q2: Names Sarah & Lily                          │
│  ├── Q1: Names Arthur    │  Q3: Blind spot — redirects to Martha            │
│                                                                               │
│  GUITAR BREAKDOWN KEYWORDS (DialogueUI):                                     │
│  crack, smash, broken, broke, shatter, damaged, neck, "why is it"           │
│                                                                               │
│  PHONE TIMING: Rings when Q1 is first SHOWN (not answered)                  │
│                                                                               │
│  ENDINGS:                                                                    │
│  ├── Sign → Fade to black (Robert takes the coward's release)               │
│  └── Tear → ending_torn.wav (Robert accepts guilt, chooses to live)         │
│                                                                               │
│  LLM:                                                                        │
│  ├── Model: Phi-3-mini-4k-instruct-q4 (2.2 GB)                              │
│  ├── Max Tokens: 80  │  Temperature: 0.7                                    │
│  └── Expected Latency: ~2 sec on M4 Mac                                     │
│                                                                               │
│  SPRITE SIZES:                                                               │
│  ├── Characters: 32×48 px   │  Objects: 32×32 px                            │
│  ├── Portraits: 64×64 px    │  Background: 480×270 px                       │
│                                                                               │
└──────────────────────────────────────────────────────────────────────────────┘
```
