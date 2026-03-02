"""
Last Day - Music Generation Prompt Templates
Reference prompts for Suno.ai music generation.
Run this script to print prompts to the console.
"""


PROMPTS = {
    "ambient_loop": {
        "description": "Main gameplay ambient loop (2-3 min, seamless)",
        "prompt": (
            "melancholic ambient piano, gentle sparse notes, warm nostalgic feeling, "
            "late afternoon, quiet room, soft strings pad in background, "
            "contemplative, 65 bpm, no drums, loopable"
        ),
    },
    "ending_signed": {
        "description": "Ending track for signed path (45-60 sec)",
        "prompt": (
            "peaceful piano resolution, bittersweet acceptance, soft fade, "
            "gentle finality, warm but sad, cinematic, 60 bpm"
        ),
    },
    "ending_torn": {
        "description": "Ending track for torn path (45-60 sec)",
        "prompt": (
            "hopeful ambient piano, gentle new beginning feeling, morning light, "
            "tender, quiet strength, cinematic, 68 bpm"
        ),
    },
    "memory_swell": {
        "description": "Optional emotional swell when triggering memories (15-30 sec)",
        "prompt": (
            "emotional piano solo, bittersweet, tender, saying goodbye, "
            "gentle crescendo, strings joining softly, cinematic, intimate, "
            "memories, 65 bpm"
        ),
    },
}


def print_prompts():
    print("=" * 60)
    print("LAST DAY - Suno.ai Music Prompts")
    print("=" * 60)
    for key, data in PROMPTS.items():
        print(f"\n--- {key} ---")
        print(f"Description: {data['description']}")
        print(f"Prompt:\n  {data['prompt']}")
    print("\n" + "=" * 60)
    print("Copy these prompts into Suno.ai to generate tracks.")
    print("Export as WAV, place in Assets/Audio/Music/")


if __name__ == "__main__":
    print_prompts()
