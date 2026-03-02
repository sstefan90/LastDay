# Last Day — Developer Setup

## Quick start (fresh clone)

```bash
git clone <repo-url>
cd LastDay
bash setup_model.sh   # downloads the ~2.4 GB AI model
```

Then open the project in **Unity 2022.3 LTS** and let it compile.
After compilation, run **LastDay → Setup LLM Components** from the menu bar once.

---

## What lives in git vs what doesn't

| File | In git? | Why |
|------|---------|-----|
| All `Assets/Scripts/**` | ✅ | Code |
| All `Assets/Scenes/**` | ✅ | Scene data |
| `Packages/manifest.json` | ✅ | LLMUnity resolved automatically on open |
| `Models/phi3-mini.gguf` | ❌ | 2.4 GB — ignored by `.gitignore` (`*.gguf`) |
| `Models/.gitkeep` | ✅ | Keeps the `Models/` folder in the repo |
| `Library/`, `Temp/`, `obj/` | ❌ | Unity auto-generates |

The AI model is **never committed**. Every developer downloads it locally via `setup_model.sh`.

---

## Manual model download (if the script doesn't work)

1. Download from HuggingFace:  
   `https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf`  
   File: `Phi-3-mini-4k-instruct-q4.gguf`

2. Rename it to `phi3-mini.gguf` and place it at:  
   `<project root>/Models/phi3-mini.gguf`

3. Open Unity. After compilation, run **LastDay → Setup LLM Components**.

---

## First-time Unity setup (one person, once)

These are already done and committed. You **don't** need to redo them:

- LLMUnity package added to `Packages/manifest.json`
- Assembly references configured in `.asmdef` files
- `LLMUNITY_AVAILABLE` scripting define symbol auto-set

---

## Troubleshooting

**"No model file provided" in console**  
→ Model is missing. Run `bash setup_model.sh`.

**"LLM failed to start"**  
→ Run **LastDay → Setup LLM Components** to re-wire the scene.

**Unity stuck on "Importing assets" for a long time**  
→ The model file may have ended up inside `Assets/`. Move it to `Models/` (project root, next to `Assets/`).

**Dialogue falls back to scripted responses**  
→ Open the Console and look for `[LLM]` messages. If you see "stub mode", the LLM component is not wired — run **LastDay → Setup LLM Components**.

---

## Team responsibilities (from AI_DEV_GUIDE.md)

| AI / Backend | Unity / Visual |
|--------------|----------------|
| LLM integration, prompts, story content | Unity scene, UI, sprites, audio |

See `Documentation/AI_DEV_GUIDE.md` for the full breakdown.
