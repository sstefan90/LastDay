# Last Day - Development Context

## Project Overview

2D point-and-click narrative game about Robert, an elderly man with ALS deciding whether to sign papers for medical assistance in dying. One room, two NPCs (Martha - wife in room, David - friend on phone), 3-4 memory objects (wedding photo, guitar, ice picks). Built with Unity 2022.3 LTS in C#.

## Tech Stack

- **Engine**: Unity 2022.3.62f3 (2D, Personal License)
- **Language**: C# 9.0
- **AI In-Game**: LLMUnity v2.5+ + Phi-3-mini-4k-instruct-q4 (2.4GB local GGUF model)
- **Art Style**: Pixel art, 32x48 sprites, Stardew Valley aesthetic
- **Target**: macOS (M4 Apple Silicon)

## Current Status

- [x] Project scaffolding complete (folders, .gitignore, .cursorrules)
- [x] Core systems (GameManager, EventManager, GameStateMachine, FadeManager, Singleton base)
- [x] Player movement with A* pathfinding (PlayerController2D, SimplePathfinder, ClickToMoveHandler)
- [x] Character animation framework (CharacterAnimator, SubtleIdleMovement)
- [x] Interaction system (InteractableObject2D, DocumentInteraction, PhoneInteraction)
- [x] Dialogue & AI (DialogueUI typewriter, LocalLLMManager stub, CharacterPrompts, MemoryContext)
- [x] Supporting UI (DecisionUI, EndScreen, InteractionPrompt)
- [x] Audio manager with crossfade support
- [x] NPC controller (NPCController with face-player logic)
- [x] Camera controller (CameraController2D with zoom-to-target)
- [x] Placeholder sprites (25 PNGs: characters, objects, glow variants, portraits, environment, UI)
- [x] Python tooling (Tools/ with .env.example, asset_generator, music_generator, generate_placeholders)
- [x] MemoryData ScriptableObject + FallbackResponses.json
- [x] Scene setup in Unity Editor (automated via SceneSetupEditor.cs)
- [x] 5 MemoryData assets created with full story/context content
- [x] Dialogue exit (Escape key + Close button)
- [x] PlayMode integration tests (12 tests covering movement, dialogue, interaction, state machine)
- [x] Fixed Camera namespace collision (ClickToMoveHandler.cs)
- [x] LLMUnity package installed (manifest.json, asmdef refs, conditional compilation)
- [x] Phi-3-mini-4k-instruct-q4.gguf model downloaded (Assets/StreamingAssets/Models/)
- [x] LocalLLMManager rewritten with real LLMCharacter integration (Martha + David)
- [x] LLM auto-detect editor scripts (LLMUnityDefineSetup.cs, versionDefines in asmdefs)
- [x] SceneSetupEditor: "Setup LLM Components" menu item for one-click LLM wiring
- [ ] Wire LLM components in scene (run LastDay > Setup LLM Components in Unity)
- [ ] Animator controllers for Robert and Martha (walk/idle blend trees)
- [ ] Real pixel art assets (currently using generated placeholders)
- [ ] Audio clips (music, SFX)
- [ ] Polish and playtesting

## Architecture

- **Singleton managers**: GameManager, EventManager, AudioManager, FadeManager, GameStateMachine
- **Event bus**: GameEvents static class for decoupled communication
- **State machine**: Loading -> Playing <-> InDialogue/PhoneCall/Decision -> Ending
- **Interaction flow**: Click object -> Player walks to it -> OnInteract -> EventManager publishes -> DialogueUI opens -> LLM generates response

## Key Files


| File | Purpose |
|------|---------|
| GameManager.cs | Top-level orchestrator |
| GameStateMachine.cs | Game state transitions |
| EventManager.cs | Memory tracking, event publishing |
| PlayerController2D.cs | Movement with pathfinding |
| SimplePathfinder.cs | A* grid-based pathfinding |
| InteractableObject2D.cs | Base class for clickable objects |
| DocumentInteraction.cs | End-game document (sign/tear) |
| PhoneInteraction.cs | Phone ring/answer flow |
| DialogueUI.cs | Dialogue panel with typewriter |
| LocalLLMManager.cs | LLM integration (LLMUnity + stub fallback) |
| LLMUnityDefineSetup.cs | Auto-detects LLMUnity, sets LLMUNITY_AVAILABLE |
| CharacterPrompts.cs | Martha and David system prompts |
| MemoryContext.cs | Builds LLM context from memories |
| NPCController.cs | Martha face-player + idle |
| CameraController2D.cs | Zoom to object, reset |
| AudioManager.cs | Music and SFX playback |


## Known Issues

- LLM components need wiring in scene (run LastDay > Setup LLM Components in Unity Editor)
- After code changes, re-run LastDay > Setup Scene to re-wire characterLayer on ClickToMoveHandler
- Placeholder sprites are simple colored shapes (need proper pixel art)
- No Animator controllers yet (CharacterAnimator will log warnings without them)
- Input field focus may need tuning after send
- No audio clips yet (music/SFX folders empty)

## LLM Integration

- **Package**: LLMUnity (installed via git URL in manifest.json)
- **Model**: Phi-3-mini-4k-instruct-q4.gguf (2.4GB, stored at project root: Models/phi3-mini.gguf)
- **Architecture**: LLM component (model server) + 2x LLMCharacter (Martha, David)
- **Conditional**: Code uses `#if LLMUNITY_AVAILABLE` guards; falls back to stub responses without it
- **Setup**: In Unity, run LastDay > Setup LLM Components to wire the LLM + LLMCharacter GameObjects

## Memory Objects


| Object        | Memory ID     | Description                                  |
| ------------- | ------------- | -------------------------------------------- |
| Wedding Photo | wedding_photo | 47-year marriage, Robert in military uniform |
| Guitar        | guitar        | Sunday morning playing, music in the house   |
| Ice Picks     | ice_picks     | Mountain climbing adventure, 1989            |
| Phone         | phone         | David calls, old friendship                  |
| Document      | document      | Euthanasia papers, the final choice          |


