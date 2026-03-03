# Last Day — Keyword & Trigger Reference

This document tracks every programmatic keyword or exact string the game uses to make decisions.
Intended audience: writers, designers, and QA. When in doubt, check the source file cited in each section.

---

## 1. Security Question Answers

**Source:** `Assets/Scripts/Interaction/ComputerInteraction.cs` — `Answers` array

Answers are compared **case-insensitively** after **trimming whitespace** from both ends.
The player only needs to type the word or phrase — no punctuation required.

| Question | Displayed Prompt | Accepted Answers |
|----------|-----------------|-----------------|
| Q1 — The Mountain | "Emergency Contact for the '98 K2 Expedition." | `arthur` |
| Q2 — The Secret Child | "Beneficiary Name for Offshore Account 4014." | `lily` |
| Q3 — The Broken Marriage | "Date of Your Proudest Moment." | `10th anniversary` · `10th` · `tenth anniversary` · `our 10th anniversary` |

**Wrong answer feedback:** "Incorrect. Think harder." (displayed in `feedbackText`). No penalty; player can retry immediately.

**Progression:** The game advances to the next question automatically after a 1.2-second pause.

---

## 2. Guitar Breakdown Detection

**Source:** `Assets/Scripts/UI/DialogueUI.cs` — `SubmitInput()` method

The guitar breakdown only triggers when **all three** conditions are true simultaneously:
1. `EventManager.activeSecurityQuestion == 3` (Q3 is the active mystery)
2. `EventManager.marthaGuitarBreakdown == false` (hasn't already broken)
3. `currentCharacter == "martha"` (player is talking to Martha, not David)

If all three are true, the game scans the player's typed input for any of these **trigger words** (case-insensitive, substring match):

| Trigger Word | Example Player Input |
|---|---|
| `crack` | "I can see a crack in the neck" |
| `smash` | "If it was a beautiful song, why is the guitar smashed?" |
| `broken` | "Why is it broken like that?" |
| `broke` | "It looks like it broke in half" |
| `shatter` | "The wood has shattered" |
| `damaged` | "The neck is clearly damaged" |
| `neck` | "There's something wrong with the neck" |
| `why is it` | "Why is it in pieces?" |

**What happens on trigger:**
- `EventManager.marthaGuitarBreakdown` is set to `true`
- `GameEvents.MarthaBreakdownReady()` is fired
- On the **same player input**, `LocalLLMManager.GenerateResponse` is called with the updated state — Martha's prompt will already be the breakdown version for that response

**Safe inputs (do NOT trigger breakdown):**
- "Tell me about the anniversary song"
- "What song did you write?"
- "Do you remember playing it?"
- "I love the guitar"
- "Did you play for her?"

---

## 3. Monologue Hints (Computer → Monologue UI)

**Source:** `Assets/Scripts/Interaction/ComputerInteraction.cs` — `monologueHints` array (Inspector-editable)

When the player opens the computer, a monologue hint fires automatically to point toward the relevant scene object.

| Question | Monologue Text Shown |
|---|---|
| Q1 | "Emergency contact for the '98 K2 Expedition. I remember the mountain. I remember the storm. I remember the rope." |
| Q2 | "Beneficiary for offshore account 4014. There was money going somewhere every month. I told myself I didn't know where." |
| Q3 | "Date of my proudest moment. The guitar is in the corner. I should look at the guitar." |

---

## 4. Guitar Monologue (Guitar Object → Monologue UI)

**Source:** `Assets/Scripts/UI/DialogueUI.cs` — `OpenForObject()`

Triggered when: player **clicks the guitar** AND `activeSecurityQuestion == 3` AND `marthaGuitarBreakdown == false`.

**Text displayed:** "There's a massive crack down the back of the neck. It's broken."

This is the visual clue that seeds the confrontation. After seeing this, the player should use a breakdown trigger word (see §2) when talking to Martha.

---

## 5. LLM Stub Response Keywords

**Source:** `Assets/Scripts/Dialogue/LocalLLMManager.cs` — `GetStubResponse()`

The stub responses (used when LLMUnity is not available) are narrative-aware. They check `EventManager.activeSecurityQuestion` and the player's input for the following keywords.

### Martha — Stub Keywords by Question State

| Question Active | Input Keywords Checked | Response Content |
|---|---|---|
| Q1 | `rope`, `expedition`, `mountain`, `k2` | Hero narrative: Robert tried to hold on, couldn't save them |
| Q2 | `money`, `account`, `investment` | Defensive: bad investments, just the two of us |
| Q3 (pre-breakdown) | `guitar`, `anniversary`, `song` | Romantic lie: 10th anniversary, sunrise, dress shirt |
| Any | `photo`, `wedding` | Wedding nostalgia: father's tie, nervous hands |
| Shutdown mode | (any input) | Raw grief: "I kept the pieces, Robert. In a box in the closet." |
| Guitar breakdown | (any input) | Truth: came home drunk, smashed guitar, "you ruined my life" |

**Fallbacks (no keyword matched):** "I'm here, love. Whatever you need to say."

### David — Stub Keywords by Question State

| Question Active | Input Keywords Checked | Response Content |
|---|---|---|
| Q1 | `rope`, `arthur`, `expedition`, `k2` | Names Arthur, confirms radio, states Robert cut the rope |
| Q2 | `money`, `account`, `investment` | Names Lily and Sarah, states 25 years of child support |
| Q3 | `guitar`, `anniversary` | Blind spot: "I don't know, buddy. That's between you and Martha." |
| Any | `help`, `advice` | Redirects: "I know you — you've never run from a hard thing. Not until now." |

**Fallbacks (no keyword matched):** "I'm here, pal. Whatever you need to say."

---

## 6. Phone Ring Timing

**Source:** `Assets/Scripts/Core/EventManager.cs` — `OnSecurityQuestionStarted()` → `CheckPhoneTrigger()`

The phone rings **once**, the **first time Q1 is displayed** to the player (when they open the computer and the first question appears on screen).

| Condition | Phone Rings? |
|---|---|
| Game starts, player hasn't opened computer yet | No |
| Player opens computer for first time (Q1 displayed) | **Yes — phone rings now** |
| Player answers Q1, Q2 is shown | No (already rung) |
| Player answers Q2, Q3 is shown | No (already rung) |

The phone will NOT ring again after this. If the player dismisses the ring and later tries to call David, `PhoneInteraction.cs` handles re-calling logic independently.

---

## 7. Martha LLM State Machine Summary

**Source:** `Assets/Scripts/Dialogue/CharacterPrompts.cs` — `GetMarthaPrompt()`

| Parameter Combination | Martha's Active Persona |
|---|---|
| `shutdownMode = true` (any other params) | **Shutdown Mode** — raw grief, no comfort |
| `activeQuestion = 3`, `guitarBreakdown = true` | **Guitar Breakdown** — admits the truth |
| `activeQuestion = 3`, `guitarBreakdown = false` | **Romantic Lie** — 10th anniversary song |
| `activeQuestion = 2` | **Defensive Wife** — bad investments, just the two of us |
| `activeQuestion = 1` | **Hero Narrative** — Robert was brave, the storm, David at basecamp |
| `activeQuestion = 0` | **Warm Caretaker** — the original gentle Martha |

**Priority order (highest to lowest):** Shutdown → Breakdown → Q3 Lie → Q2 → Q1 → Q0

---

## 8. David LLM State Machine Summary

**Source:** `Assets/Scripts/Dialogue/CharacterPrompts.cs` — `GetDavidPrompt()`

| `activeQuestion` | David's Active Persona |
|---|---|
| 0 | **Loyal Friend** — supportive, non-committal |
| 1 | **Cold / Arthur** — names Arthur, states Robert cut the rope |
| 2 | **Disappointed** — names Lily and Sarah, 25 years of silence |
| 3 | **Blind Spot** — genuinely doesn't know about the guitar |

---

## 9. Final Decision Gate

**Source:** `Assets/Scripts/Interaction/ComputerInteraction.cs` — `ShowFinalPrompt()`

After all three questions are answered, the computer displays:

> **"FINAL SECURITY CHECK**
> **Can you forgive yourself?"**

Two buttons appear:
- **Sign (Yes):** `GameManager.EndGame(signed: true)` → Fade to black
- **Tear (No):** `GameManager.EndGame(signed: false)` → `ending_torn.wav`

This prompt is **un-bypassable** — the `DecisionUI` can no longer be dismissed until one button is clicked.

---

## 10. EventManager State Fields (Inspector-Visible)

**Source:** `Assets/Scripts/Core/EventManager.cs`

| Field | Type | Meaning |
|---|---|---|
| `activeSecurityQuestion` | `int` (0–3) | 0 = none, 1 = Q1 active, 2 = Q2 active, 3 = Q3 active |
| `marthaShutdownMode` | `bool` | True after all questions answered — Martha's persona permanently shifts |
| `marthaGuitarBreakdown` | `bool` | True after player confronts Martha with the broken guitar evidence |
| `documentUnlocked` | `bool` | True after all questions answered (same event as shutdown mode) |
| `phoneHasRung` | `bool` | True after Q1 is first displayed; prevents double-ring |

All fields are public and visible in the Unity Inspector for debugging. You can set them manually in Play Mode to skip to any narrative state.
