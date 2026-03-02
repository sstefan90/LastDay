"""
Last Day - Asset Generator
Batch background removal for game sprites using rembg.
"""

import os
import sys
from pathlib import Path

from dotenv import load_dotenv

load_dotenv()

INPUT_DIR = Path("generated_assets/raw")
OUTPUT_DIR = Path("generated_assets/processed")


def remove_backgrounds(input_dir: Path = INPUT_DIR, output_dir: Path = OUTPUT_DIR):
    """Remove backgrounds from all PNG files in input_dir, save to output_dir."""
    from rembg import remove
    from PIL import Image

    input_dir.mkdir(parents=True, exist_ok=True)
    output_dir.mkdir(parents=True, exist_ok=True)

    png_files = list(input_dir.glob("*.png"))
    if not png_files:
        print(f"No PNG files found in {input_dir}")
        return

    for img_path in png_files:
        print(f"Processing: {img_path.name}")
        with Image.open(img_path) as img:
            result = remove(img)
            out_path = output_dir / img_path.name
            result.save(out_path)
            print(f"  Saved: {out_path}")

    print(f"\nDone! Processed {len(png_files)} images.")


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--help":
        print("Usage: python asset_generator.py")
        print(f"  Place raw PNGs in {INPUT_DIR}")
        print(f"  Processed PNGs output to {OUTPUT_DIR}")
    else:
        remove_backgrounds()
