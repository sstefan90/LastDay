# Last Day — Art Direction & Asset Guide

**Visual Reference**: *Interview with the Whisperer* (Deconstructeam, 2020)
**Style**: Pixel art, fixed camera, intimate seated conversation, typed dialogue

---

## 1. Scene Composition

The camera is locked. Robert and Martha sit across from each other at a desk/table in their home. The player never moves Robert — all interaction is through clicking objects placed around the room. This creates a claustrophobic, intimate feeling appropriate for the subject matter.

```
┌─────────────────────────────────────────────────────────┐
│                    ROOM BACKGROUND                       │
│                  (Sorting: Background/0)                 │
│                                                         │
│  ┌──────┐                                 ┌──────┐     │
│  │ ICE  │    ┌─────────────────────┐      │GUITAR│     │
│  │PICKS │    │   WEDDING PHOTO     │      │      │     │
│  └──────┘    │   (wall, above)     │      └──────┘     │
│              └─────────────────────┘                    │
│                                                         │
│                                                         │
│   ┌───────────────────────────────────────────────┐     │
│   │               TABLE / DESK                     │     │
│   │  ┌────────┐  ┌──────┐  ┌────────┐  ┌───────┐ │     │
│   │  │COMPUTER│  │PHONE │  │DOCUMENT│  │ LAMP  │ │     │
│   │  └────────┘  └──────┘  └────────┘  └───────┘ │     │
│   └───────────────────────────────────────────────┘     │
│                                                         │
│       ┌─────────┐             ┌─────────┐               │
│       │ MARTHA  │             │ ROBERT  │               │
│       │ (seated)│             │ (seated)│               │
│       └─────────┘             └─────────┘               │
│                                                         │
│          CHAIR                    CHAIR                  │
└─────────────────────────────────────────────────────────┘
```

### Key Differences from Current Layout
- **No pathfinding/walking** — Robert stays seated. `ClickToMoveHandler` can be simplified to only raycast for Interactables and Characters (no walkable layer needed)
- **Fixed camera** — No camera follow, no bounds. Single static orthographic view
- **Everything visible at once** — All interactable objects are in frame from the start
- **Intimacy** — The two characters and the table dominate the frame. Objects on walls and table edges are secondary

---

## 2. Sorting Layer & Physics Layer Assignment

### Sorting Layers (back to front render order)

| Sorting Layer | Order | Contents |
|---|---|---|
| **Background** | 0 | `room_background.png` — wall, floor, wallpaper, window |
| **Midground** | 0 | Table/desk, chairs, bookshelf — furniture that sits behind characters |
| **Midground** | 1 | Wall-mounted objects: wedding photo frame, ice picks mount, clock |
| **Objects** | -1 | Glow sprites (highlight underlays for all interactables) |
| **Objects** | 0 | Interactable object sprites: computer, phone, document, guitar, ice picks, wedding photo |
| **Characters** | 0 | Martha seated sprite, Robert seated sprite |
| **Characters** | 1 | Character arms/hands overlay (if using layered sprites for gestures) |
| **Foreground** | 0 | Table front edge (if table wraps around characters), lamp glow, any foreground framing |
| **UI** | (Canvas) | All UI panels — handled by Canvas sorting order 100 |

### Physics Layers

| Layer | Purpose | Used By |
|---|---|---|
| **Default** (0) | Camera, managers, environment | Non-interactive scene objects |
| **UI** (5) | Unity UI | Canvas elements |
| **Interactables** (8) | Click targets | Computer, Phone, Document, Guitar, IcePicks, WeddingPhoto |
| **Characters** (9) | Click targets for NPC dialogue | Martha (Robert is the player, not clickable) |
| **Foreground** (10) | Render-only, no physics | Foreground overlay sprites |

**Removed**: `Walkable` (6) and `Obstacles` (7) are no longer needed since Robert doesn't walk.

---

## 3. Complete Asset List

### 3.1 Environment (Background & Midground)

| Asset | File | Size (px) | Sorting Layer | Notes |
|---|---|---|---|---|
| Room background | `Art/Environment/room_background.png` | 480×270 or 960×540 | Background/0 | Wall + floor. Warm but dimming tones. Evening light from a window |
| Table/desk | `Art/Environment/furniture_desk.png` | ~320×80 | Midground/0 | Central piece. Robert and Martha sit on opposite sides |
| Table front edge | `Art/Environment/desk_front.png` | ~320×30 | Foreground/0 | Optional: overlaps character legs to sell depth |
| Chair (Martha) | `Art/Environment/chair_left.png` | ~64×80 | Midground/0 | Visible behind/beside Martha |
| Chair (Robert) | `Art/Environment/chair_right.png` | ~64×80 | Midground/0 | Visible behind/beside Robert |
| Bookshelf | `Art/Environment/furniture_bookshelf.png` | ~96×128 | Midground/0 | Background detail, left wall |
| Window | (part of background or separate) | — | Background/1 | Evening/twilight light source |
| Lamp | `Art/Environment/lamp.png` | ~32×48 | Objects/0 | On desk, provides warm light. Non-interactable |
| Lamp glow | `Art/Environment/lamp_glow.png` | ~64×64 | Foreground/0 | Additive or alpha overlay for warm light pool |

### 3.2 Interactable Objects

| Asset | File | Glow File | Sorting Layer | Position |
|---|---|---|---|---|
| Computer | `Art/Objects/computer.png` | `computer_glow.png` | Objects/0 | On desk, Robert's side |
| Phone | `Art/Objects/phone.png` | `phone_glow.png` | Objects/0 | Center of desk |
| Document | `Art/Objects/document.png` | `document_glow.png` | Objects/0 | On desk, near computer |
| Guitar | `Art/Objects/guitar.png` | `guitar_glow.png` | Objects/0 | Leaning against wall, Robert's side |
| Ice Picks | `Art/Objects/ice_picks.png` | `ice_picks_glow.png` | Objects/0 | Mounted on wall, left side |
| Wedding Photo | `Art/Objects/wedding_photo.png` | `wedding_photo_glow.png` | Objects/0 | Hanging on wall, center-left |

Each interactable needs:
- Base sprite (32×32 or appropriate)
- Glow sprite (same size, soft yellow-white, alpha ~0.3)
- `BoxCollider2D` on Interactables layer
- Filter Mode: **Point (no filter)**, Compression: **None**

### 3.3 Character Sprites

| Asset | File | Sorting Layer | Notes |
|---|---|---|---|
| Robert seated | `Art/Characters/Robert/robert_seated.png` | Characters/0 | Sprite sheet (see Section 4) |
| Robert portrait | `Art/Characters/Robert/robert_portrait.png` | (UI only) | Dialogue panel portrait |
| Martha seated | `Art/Characters/Martha/martha_seated.png` | Characters/0 | Sprite sheet (see Section 4) |
| Martha portrait | `Art/Characters/Martha/martha_portrait.png` | (UI only) | Dialogue panel portrait |

### 3.4 UI Assets

| Asset | File | Notes |
|---|---|---|
| Dialogue panel BG | `Art/UI/dialogue_panel.png` | 9-slice, dark semi-transparent |
| Computer panel BG | `Art/UI/computer_panel.png` | Terminal-style, dark green on black |
| Input field BG | `Art/UI/input_field_bg.png` | 9-slice |
| Button normal | `Art/UI/button_normal.png` | 9-slice |
| Button pressed | `Art/UI/button_pressed.png` | 9-slice |
| Name plate | `Art/UI/name_plate.png` | Behind character name in dialogue |

---

## 4. Sprite Sheet Design for Seated Characters

Since Robert and Martha are **seated and stationary**, the sprite sheets focus entirely on **emotion and idle animation** rather than locomotion. This is fundamentally different from a walk-cycle sheet.

### 4.1 Recommended Sheet Layout

**Per character: 1 sprite sheet, grid of emotion states × animation frames**

```
ROBERT SEATED SPRITE SHEET
═══════════════════════════
Sheet size: 256×320 (8 columns × 10 rows of 32×32 cells)
— or scale up to 64×64 cells for more detail (512×640)

ROWS (Emotion States):
  Row 0: NEUTRAL        — default resting state
  Row 1: THOUGHTFUL     — hand to chin, eyes down
  Row 2: PAINED         — wince, hand grips table edge
  Row 3: GUILTY         — shoulders hunched, eyes averted
  Row 4: REMEMBERING    — eyes up and to the side, slight smile
  Row 5: ANGRY          — furrowed brow, clenched jaw
  Row 6: SAD            — slumped, eyes glistening
  Row 7: RESIGNED       — exhale, shoulders drop
  Row 8: TREMOR         — ALS hand tremor (2-3 frame shake)
  Row 9: SPEAKING       — mouth movement frames

COLUMNS (Animation Frames per State):
  Col 0-2: Idle breathing loop (3 frames, subtle)
  Col 3:   Transition-in keyframe (from neutral to this emotion)
  Col 4-5: Emotion hold frames (sustain, subtle variation)
  Col 6:   Transition-out keyframe (back toward neutral)
  Col 7:   Blink frame (eyes closed variant of this row's emotion)
```

```
MARTHA SEATED SPRITE SHEET
══════════════════════════
Same structure. Her emotion rows differ:

  Row 0: WARM / CARING   — gentle smile, attentive posture
  Row 1: PROTECTIVE      — leaning forward, hand reaching out
  Row 2: LYING           — averted gaze, forced smile, fidgeting hands
  Row 3: DEFENSIVE       — arms crossed, chin raised
  Row 4: HEARTBROKEN     — hand over mouth, tears
  Row 5: BREAKDOWN       — slumped forward, shoulders shaking
  Row 6: SHUTDOWN        — hollow stare, hands in lap, motionless
  Row 7: ANGRY           — tight lips, clenched hands
  Row 8: NOSTALGIC       — soft eyes, slight tilt of head
  Row 9: SPEAKING        — mouth movement frames
```

### 4.2 Idle Animation (Breathing/Micro-movement)

Each emotion state's first 3 frames form a **breathing loop** — this plays continuously:

```
Frame 0 (Inhale):    Frame 1 (Hold):     Frame 2 (Exhale):
   Chest slightly       Normal               Chest slightly
   expanded (+1px)                           contracted (-1px)

Timing: 0.5s per frame → 1.5s full breath cycle
```

Additional code-driven micro-movements (already implemented in `CharacterIdleMovement.cs`):
- **Breathing scale**: 1.5% Y-axis pulse at 0.4 Hz
- **Gentle sway**: 0.003 units X-axis at 0.2 Hz
- **ALS tremor** (Robert only): periodic hand/arm shake, 0.01 units at 40Hz for 0.3s bursts

### 4.3 Emotion Transitions

Don't snap between emotions — use the transition frames:

```
NEUTRAL (idle loop) → Transition-in frame → EMOTION (hold loop) → Transition-out → NEUTRAL

In code (Animator Controller):
  - Each row = an Animation Clip
  - Blend Tree or state machine transitions between emotions
  - Trigger parameter per emotion: "SetGuilty", "SetPained", etc.
  - Default state: Neutral idle loop
```

### 4.4 When to Trigger Emotions

| Game Event | Robert Emotion | Martha Emotion |
|---|---|---|
| Game start / idle | Neutral | Warm/Caring |
| Viewing ice picks | Remembering → Guilty | Protective → Lying |
| Viewing guitar | Remembering → Pained | Nostalgic → Defensive |
| Viewing wedding photo | Remembering → Sad | Warm → Heartbroken |
| Computer question displayed | Thoughtful → Pained | (depends on shutdown state) |
| Correct answer entered | Resigned | Defensive → Angry |
| Martha confronted about guitar | Angry → Guilty | Breakdown |
| All questions answered | Resigned | Shutdown |
| Final prompt shown | Tremor (constant) | Shutdown |
| Talking to Martha (normal) | Neutral / Sad | Warm / Protective |
| Talking to Martha (shutdown) | Guilty | Shutdown |

### 4.5 Portrait Sprites (Dialogue Panel)

Separate from the seated sprites. These show a close-up face in the dialogue UI.

```
PORTRAIT SHEET: 128×64 (4 columns × 2 rows of 32×32)
— or 256×128 at 64×64 per cell

  Col 0: Neutral     Col 1: Happy/Warm    Col 2: Sad    Col 3: Angry
  Row 0: Eyes open
  Row 1: Eyes closed (blink)
```

The portrait swaps based on the same emotion triggers. Blink is code-driven (every 3-5 seconds, swap to Row 1 for 0.1s).

---

## 5. Pixel Art Specifications

### Canvas & Resolution

| Setting | Value |
|---|---|
| Game resolution | 480×270 (pixel art native) upscaled to 1920×1080 |
| Pixels Per Unit | 32 |
| Camera orthographic size | 4.22 (= 270 / 2 / 32) |
| Filter Mode | Point (no filter) |
| Compression | None |
| Sprite atlas | Recommended for all Objects and Characters |

### Color Palette

Stay within a **limited warm palette** for the intimate evening setting:

```
ENVIRONMENT:
  Wall:        #3D2B1F (dark wood) / #5C4033 (medium wood)
  Floor:       #4A3728 (dark) / #6B5442 (lighter boards)
  Window light: #FFE4B5 (warm amber glow)
  Shadow:      #1A1A2E (deep blue-black)

SKIN TONES (elderly):
  Base:        #E8C4A8
  Shadow:      #C9A080
  Highlight:   #F5DCC8

GRAY HAIR:
  Base:        #A0A0A0
  Shadow:      #707070
  Highlight:   #C8C8C8

ROBERT'S CLOTHES:
  Cardigan:    #6B5344 / shadow: #4A3A2F
  Shirt:       #D4C5B2
  Pants:       #3D3D4A

MARTHA'S CLOTHES:
  Blouse:      #7A6B8C / shadow: #5A4D6A
  Cardigan:    #8B7355

OBJECTS:
  Computer:    #2A2A3A (body) / #00DD55 (screen glow)
  Phone:       #1A1A1A (black) / #CC3333 (ringing indicator)
  Document:    #F0E8D8 (paper) / #2A2A2A (text)
  Guitar:      #B8860B (wood) / #DAA520 (strings)
  Ice picks:   #C0C0C0 (metal) / #8B4513 (leather grip)
  Photo frame: #DAA520 (gold trim)
```

### Recommended Tools
- **Aseprite** ($20) — best for animation, onion skinning, palette management
- **Libresprite** (free) — Aseprite fork, identical workflow
- **Piskel** (free, browser) — lighter, good for quick iteration

---

## 6. Implementation Notes

### Removing Walk System

The current `PlayerController2D`, `SimplePathfinder`, `ClickToMoveHandler`, and `CharacterAnimator` walk logic become unnecessary. Two approaches:

**Option A (Minimal change)**: Keep the scripts but disable pathfinding. `ClickToMoveHandler` already raycasts for Interactables — just remove the walkable-area fallback so clicks only register on interactable objects and Martha.

**Option B (Clean removal)**: Strip walk-related code, remove Walkable layer and floor collider, simplify Robert to a static seated sprite with `CharacterIdleMovement` only.

**Recommendation**: Option A for now (less risk), Option B as a cleanup pass later.

### Camera Setup

```
Main Camera:
  - Orthographic size: 4.22 (for 480×270 native)
  - Position: (0, 0, -10) — centered on the table scene
  - Remove CameraController2D follow behavior
  - Add Pixel Perfect Camera component (Unity 2D package)
    - Assets PPU: 32
    - Reference Resolution: 480×270
    - Upscale Render Texture: ON
```

### Animator Controller Structure

```
RobertAnimator (Animator Controller):
  Parameters:
    - EmotionIndex (int): 0=Neutral, 1=Thoughtful, 2=Pained, ...
    - IsSpeaking (bool)
    - Blink (trigger)

  States:
    Neutral_Idle (default) → loops breathing frames
    Thoughtful_Idle → loops breathing frames with thoughtful pose
    ... (one state per emotion row)
    Speaking → loops mouth frames, overlaid on current emotion

  Transitions:
    Any State → [Emotion]_Idle when EmotionIndex changes
    Any State → Speaking when IsSpeaking = true

MarthaAnimator: Same structure, different emotion set
```

---

## 7. Asset Checklist

```
PRIORITY 1 — Minimum Playable
  □ room_background.png (wall + floor + window, 480×270)
  □ furniture_desk.png (table, ~320×80)
  □ robert_seated.png (at minimum: neutral + tremor rows, 3 frames each)
  □ martha_seated.png (at minimum: warm + shutdown rows, 3 frames each)
  □ All 6 interactable object sprites (already exist as placeholders)
  □ All 6 glow sprites (already exist as placeholders)

PRIORITY 2 — Full Emotion Range
  □ robert_seated.png (complete 10-row sheet)
  □ martha_seated.png (complete 10-row sheet)
  □ robert_portrait.png (4 emotions × blink)
  □ martha_portrait.png (4 emotions × blink)
  □ chair_left.png + chair_right.png

PRIORITY 3 — Polish
  □ lamp.png + lamp_glow.png
  □ desk_front.png (foreground depth overlay)
  □ Refined interactable sprites (replace placeholders)
  □ computer_panel.png (9-slice for terminal UI)
  □ Ambient particle effects (dust motes in lamplight)
```
