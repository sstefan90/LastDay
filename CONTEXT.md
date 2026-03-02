# Last Day - Development Context

## Project Overview

2D point-and-click narrative game about Robert, an elderly man with ALS deciding
whether to sign papers for medical assistance in dying. One room, two NPCs
(Martha — wife in room, David — friend on phone), 3–4 memory objects.
Built with Unity 2022.3 LTS in C#.

## Tech Stack

- **Engine**: Unity 2022.3.62f3 (2D, Personal License)
- **Language**: C# 9.0
- **AI In-Game**: LLMUnity v2.5+ + Phi-3-mini-4k-instruct-q4 (2.4 GB local GGUF)
- **Art Style**: Pixel art, 32×48 sprites, Stardew Valley aesthetic
- **Target**: macOS (M4 Apple Silicon)

---

## Current Status

### Core Systems ✅
- [x] Project scaffolding (folders, .gitignore, .cursorrules, SETUP.md)
- [x] GameManager, EventManager, GameStateMachine, FadeManager, Singleton base
- [x] Audio manager with crossfade (AudioManager — no clips yet)
- [x] GameEvents static event bus for decoupled communication

### Player & Movement ✅
- [x] A\* pathfinding (SimplePathfinder — grid-based, obstacle-aware)
- [x] PlayerController2D (MoveTo, MoveToAndInteract, MoveToAndTalk)
- [x] ClickToMoveHandler (sole owner of click → interaction; interactable / NPC / walkable priority)
- [x] CharacterAnimator (4-direction, IsWalking/DirectionX/DirectionY; null-safe — no crash without controller)
- [x] CharacterIdleMovement — modular: BreathingConfig / SwayConfig / TremorConfig structs,
      each independently toggled. Robert: all on. Martha (NPC): tremor disabled.

### Interaction ✅
- [x] InteractableObject2D — hover highlight + gaze timer; **IPointerClickHandler removed**
      (ClickToMoveHandler is the sole click handler; fixes double-open flash bug)
- [x] DocumentInteraction (sign / tear ending)
- [x] PhoneInteraction (phone ring trigger)
- [x] NPCController (face-player logic, click → walk-up → dialogue)

### Dialogue & AI ✅
- [x] DialogueUI — open for object / NPC / phone call; story-aware opening lines per object
- [x] LocalLLMManager — LLMUnity LLMAgent integration + stub fallback;
      Initialize(modelPath) overload accepts path from ModelDownloader
- [x] CharacterPrompts — rewritten: explicit output-format rules (no [Robert]: lines),
      narrative memory sections (not raw data blocks), natural off-topic drift,
      per-object opening lines (GetObjectOpeningLine)
- [x] MemoryContext — GetTriggeredMemoryIds(); BuildMemoryContext() removed
      (was leaking [MEMORY CONTEXT] header into LLM responses)
- [x] LLMUnity package installed (manifest.json + asmdef versionDefines + LLMUnityDefineSetup.cs)
- [x] LLMAgent (replaces deprecated LLMCharacter)
- [x] ValidateResponse strips [Martha]:, [Robert]:, [MEMORY CONTEXT] artifacts from LLM output
- [x] ModelDownloader — auto-downloads Phi-3-mini on first run to persistentDataPath/Models/;
      exposes OnProgress / OnModelReady / OnError events; GameManager calls EnsureModelReady()
      before LLM init

### UI ✅
- [x] DecisionUI (sign / tear choice)
- [x] EndScreen (quote display)
- [x] InteractionPrompt ("Click to examine X")

### Editor Tooling ✅
- [x] SceneSetupEditor — full programmatic scene build (LastDay > Setup Scene)
- [x] SceneSetupEditor — Setup LLM Components (wires LLM + LLMAgent GameObjects, sets model path)
- [x] LLMUnityDefineSetup — auto-adds/removes LLMUNITY_AVAILABLE scripting define

### Tests ✅
- [x] 40+ PlayMode integration tests covering:
      movement, pathfinding, dialogue (open/close/submit/phone), state machine,
      NPC interaction, CharacterIdleMovement presets, ModelDownloader events,
      LLMManager Initialize overloads

### Partner Onboarding ✅
- [x] SETUP.md — full onboarding guide
- [x] setup_model.sh — one-command model download (`bash setup_model.sh`)
- [x] Models/.gitkeep — folder tracked in git; *.gguf never committed

---

## Outstanding

| Area | What's needed |
|------|---------------|
| **Animator controllers** | RobertAnimator + MarthaAnimator with walk/idle blend trees |
| **Pixel art assets** | All 25 sprites are placeholder colored shapes |
| **Audio clips** | ambient_loop, ending_signed, ending_torn, footsteps, phone_ring, etc. |
| **Download progress UI** | DownloadProgressUI.cs — in-game progress bar for ModelDownloader |
| **Thinking indicator** | Spinner/dots while LLM is generating a response |
| **Phone auto-ring** | EventManager fires OnPhoneRing after 2 memories — PhoneInteraction wiring unverified |
| **Document lock** | DocumentInteraction checks EventManager.documentUnlocked — scene wiring unverified |
| **NPCIdleMovement** | NPCController has idleMovement field but Martha doesn't call OnStartWalking (she doesn't move) |

---

## Architecture

```
Input (mouse click)
  └── ClickToMoveHandler           (sole click → game logic owner)
        ├── Physics2D hit: Interactable → PlayerController2D.MoveToAndInteract()
        ├── Physics2D hit: Character   → PlayerController2D.MoveToAndTalk()
        └── Physics2D hit: Walkable    → PlayerController2D.MoveTo()

PlayerController2D
  ├── SimplePathfinder (A*)
  ├── CharacterAnimator (direction + walk state)
  └── CharacterIdleMovement (breathing / sway / tremor — disabled while moving)

On arrival:
  InteractableObject2D.OnInteract()
    └── EventManager.PublishEvent(memoryId)  → tracks memories, unlocks document, triggers phone
    └── DialogueUI.OpenForObject()           → shows story-aware opening line

DialogueUI.SubmitInput(text)
  └── LocalLLMManager.GenerateResponse()
        ├── #if LLMUNITY_AVAILABLE → LLMAgent.Chat() → ValidateResponse() → strips artifacts
        └── fallback → GetStubResponse()

GameManager startup:
  ModelDownloader.EnsureModelReady()  → download if missing
  LocalLLMManager.Initialize(path)   → warm up LLM

State machine: Loading → Playing ↔ InDialogue / PhoneCall / Decision → Ending
```

---

## Key Files

| File | Purpose |
|------|---------|
| `GameManager.cs` | Startup: model download → LLM init → StartGame |
| `GameStateMachine.cs` | State transitions + CanPlayerMove / CanInteract guards |
| `EventManager.cs` | Memory tracking, document unlock, phone ring trigger |
| `PlayerController2D.cs` | Path following + MoveToAndInteract / MoveToAndTalk |
| `SimplePathfinder.cs` | A\* grid pathfinding with obstacle layer |
| `ClickToMoveHandler.cs` | Mouse click → player action (sole click handler) |
| `CharacterIdleMovement.cs` | Modular idle animation; BreathingConfig / SwayConfig / TremorConfig |
| `InteractableObject2D.cs` | Hover highlight + gaze timer; OnInteract() opens dialogue |
| `DocumentInteraction.cs` | End-game sign / tear choice |
| `PhoneInteraction.cs` | Phone ring + David conversation |
| `NPCController.cs` | Martha face-player, OnPlayerInteract() opens dialogue |
| `DialogueUI.cs` | Dialogue panel; OpenForObject / OpenForNPC / OpenForPhone |
| `LocalLLMManager.cs` | LLMUnity LLMAgent + stub fallback; Initialize(modelPath) |
| `ModelDownloader.cs` | First-run download of Phi-3-mini to persistentDataPath |
| `CharacterPrompts.cs` | Martha + David system prompts; GetObjectOpeningLine() |
| `MemoryContext.cs` | MemoryData lookup; memory IDs passed to CharacterPrompts |
| `SceneSetupEditor.cs` | Programmatic scene + LLM component wiring |

---

## LLM Integration

- **Package**: LLMUnity (git URL in manifest.json → `ai.undream.llm`)
- **Assembly**: `undream.llmunity.Runtime` referenced in both .asmdef files
- **Model**: `Phi-3-mini-4k-instruct-q4.gguf` — stored at `<project root>/Models/` (dev)
  or `Application.persistentDataPath/Models/` (runtime via ModelDownloader)
- **Characters**: `LLMAgent` × 2 (Martha, David) — shared LLM component
- **Guard**: `#if LLMUNITY_AVAILABLE` — auto-set by `LLMUnityDefineSetup.cs`
- **Setup**: run `LastDay → Setup LLM Components` once after opening in Unity

## Memory Objects

| Object | Memory ID | Story |
|--------|-----------|-------|
| Wedding Photo | `wedding_photo` | 47-year marriage, Robert in military uniform |
| Guitar | `guitar` | Sunday morning playing, the house alive with music |
| Ice Picks | `ice_picks` | Mount Washington climb, February 1989 |
| Phone | `phone` | David calls more than usual; he never leaves a message |
| Document | `document` | The papers. Martha found them three weeks ago and said nothing. |

## Known Issues

- No Animator controllers — CharacterAnimator is null-safe (no crash), but no walk/idle animation plays
- No audio clips — AudioManager is wired but has nothing to play
- Placeholder sprites only — all art is generated colored shapes
- `LastDay → Setup LLM Components` must be run once after fresh clone + model download
