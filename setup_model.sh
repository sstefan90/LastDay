#!/usr/bin/env bash
# ────────────────────────────────────────────────────────────
#  Last Day — LLM model setup
#  Run once after cloning: bash setup_model.sh
# ────────────────────────────────────────────────────────────

set -e

DEST="$(dirname "$0")/Models/phi3-mini.gguf"
URL="https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf"

if [ -f "$DEST" ]; then
  echo "✓ Model already present at Models/phi3-mini.gguf"
  exit 0
fi

echo "Downloading Phi-3-mini (~2.4 GB) — this takes a few minutes..."
curl -L --progress-bar -o "$DEST" "$URL"
echo ""
echo "✓ Done. Model saved to Models/phi3-mini.gguf"
echo ""
echo "Next: Open the project in Unity, then run:"
echo "  LastDay > Setup LLM Components"
