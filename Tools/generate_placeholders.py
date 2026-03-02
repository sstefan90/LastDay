"""
Generate placeholder pixel art sprites for Last Day.
Creates simple colored PNG files for all game characters and objects.
Run: python3 generate_placeholders.py
Requires: Pillow (pip install Pillow)
"""

import os
from PIL import Image, ImageDraw

BASE = os.path.join(os.path.dirname(__file__), "..", "Assets", "Art")


def save(img, *path_parts):
    full = os.path.join(BASE, *path_parts)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    img.save(full)
    print(f"  Created: {os.path.relpath(full, os.path.join(BASE, '..', '..'))}")


def draw_character(width, height, body_color, hair_color, name_tag):
    """Draw a simple front-facing pixel character."""
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    cx = width // 2
    head_r = width // 5

    # Hair (slightly above head)
    d.ellipse([cx - head_r - 1, 4, cx + head_r + 1, 4 + head_r * 2 + 2], fill=hair_color)

    # Head (skin)
    skin = (232, 196, 168)
    d.ellipse([cx - head_r, 6, cx + head_r, 6 + head_r * 2], fill=skin)

    # Eyes
    eye_y = 6 + head_r
    d.rectangle([cx - 3, eye_y, cx - 2, eye_y + 1], fill=(50, 50, 60))
    d.rectangle([cx + 2, eye_y, cx + 3, eye_y + 1], fill=(50, 50, 60))

    # Body
    body_top = 6 + head_r * 2 + 2
    body_w = width // 3
    d.rectangle([cx - body_w, body_top, cx + body_w, height - 10], fill=body_color)

    # Legs
    leg_w = body_w // 2
    d.rectangle([cx - body_w, height - 10, cx - leg_w + 1, height - 1], fill=(60, 60, 74))
    d.rectangle([cx + leg_w - 1, height - 10, cx + body_w, height - 1], fill=(60, 60, 74))

    return img


def draw_object(width, height, bg_color, detail_color=None, shape="rect"):
    """Draw a simple object sprite."""
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    pad = 2
    if shape == "rect":
        d.rectangle([pad, pad, width - pad - 1, height - pad - 1], fill=bg_color)
        if detail_color:
            # inner border / frame
            d.rectangle([pad + 2, pad + 2, width - pad - 3, height - pad - 3],
                        outline=detail_color, width=1)
    elif shape == "circle":
        d.ellipse([pad, pad, width - pad - 1, height - pad - 1], fill=bg_color)
    elif shape == "phone":
        # handset shape
        d.rounded_rectangle([pad, pad + 4, width - pad - 1, height - pad - 5],
                            radius=3, fill=bg_color)
        d.ellipse([width // 2 - 4, height // 2 - 4, width // 2 + 4, height // 2 + 4],
                  fill=detail_color or (200, 200, 180))
    elif shape == "document":
        d.rectangle([pad, pad, width - pad - 1, height - pad - 1], fill=(245, 240, 230))
        # text lines
        for i in range(5):
            y = pad + 6 + i * 5
            d.rectangle([pad + 4, y, width - pad - 5, y + 1], fill=(80, 80, 80))
        # signature line at bottom
        d.rectangle([pad + 4, height - pad - 8, width // 2, height - pad - 7],
                    fill=(60, 60, 60))
    elif shape == "guitar":
        # simple guitar silhouette
        # neck
        d.rectangle([width // 2 - 1, pad, width // 2 + 1, height // 2 - 2], fill=bg_color)
        # body
        d.ellipse([pad + 2, height // 2 - 4, width - pad - 3, height - pad - 1], fill=bg_color)
        # sound hole
        d.ellipse([width // 2 - 3, height // 2 + 2, width // 2 + 3, height // 2 + 8],
                  fill=detail_color or (60, 40, 20))
    elif shape == "picks":
        # two crossed pick shapes
        d.line([pad + 2, height - pad - 2, width // 2, pad + 2], fill=bg_color, width=2)
        d.line([width - pad - 3, height - pad - 2, width // 2, pad + 2], fill=bg_color, width=2)
        # pick heads
        d.rectangle([pad, height - pad - 5, pad + 5, height - pad - 1], fill=detail_color or (100, 100, 110))
        d.rectangle([width - pad - 6, height - pad - 5, width - pad - 1, height - pad - 1],
                    fill=detail_color or (100, 100, 110))

    return img


def draw_glow(width, height, color):
    """Draw a soft glow version (bright, semi-transparent)."""
    img = Image.new("RGBA", (width + 8, height + 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    glow = (color[0], color[1], color[2], 80)
    d.ellipse([0, 0, width + 7, height + 7], fill=glow)
    return img


def draw_portrait(width, height, body_color, hair_color):
    """Draw a character portrait (head + shoulders)."""
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    cx, cy = width // 2, height // 2

    # Background circle
    d.ellipse([2, 2, width - 3, height - 3], fill=(40, 35, 50))

    # Shoulders
    d.ellipse([cx - 14, cy + 6, cx + 14, height - 3], fill=body_color)

    # Hair
    d.ellipse([cx - 9, cy - 14, cx + 9, cy + 2], fill=hair_color)

    # Head
    skin = (232, 196, 168)
    d.ellipse([cx - 8, cy - 12, cx + 8, cy], fill=skin)

    # Eyes
    d.rectangle([cx - 4, cy - 6, cx - 3, cy - 5], fill=(50, 50, 60))
    d.rectangle([cx + 3, cy - 6, cx + 4, cy - 5], fill=(50, 50, 60))

    return img


def draw_room_background(width, height):
    """Draw a simple room background."""
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Wall
    d.rectangle([0, 0, width - 1, height * 2 // 3], fill=(142, 120, 100))

    # Baseboard
    d.rectangle([0, height * 2 // 3 - 3, width - 1, height * 2 // 3], fill=(90, 70, 55))

    # Floor
    d.rectangle([0, height * 2 // 3 + 1, width - 1, height - 1], fill=(120, 90, 65))

    # Window
    wx, wy = width // 2 - 30, 20
    d.rectangle([wx, wy, wx + 60, wy + 50], fill=(180, 210, 235))
    d.rectangle([wx, wy, wx + 60, wy + 50], outline=(90, 70, 55), width=2)
    d.line([wx + 30, wy, wx + 30, wy + 50], fill=(90, 70, 55), width=2)
    d.line([wx, wy + 25, wx + 60, wy + 25], fill=(90, 70, 55), width=2)

    # Window light overlay (warm)
    for i in range(40):
        alpha = max(0, 30 - i)
        y = wy + 50 + i
        if y < height:
            d.line([wx - i // 2, y, wx + 60 + i // 2, y],
                   fill=(255, 240, 200, alpha))

    return img


def main():
    print("Generating placeholder sprites for Last Day...\n")

    # -- Characters --
    print("Characters:")
    robert = draw_character(32, 48, (107, 83, 68), (160, 160, 160), "Robert")
    save(robert, "Characters", "Robert", "robert_placeholder.png")

    martha = draw_character(32, 48, (122, 107, 140), (192, 192, 192), "Martha")
    save(martha, "Characters", "Martha", "martha_placeholder.png")

    # Portraits (for dialogue UI)
    print("\nPortraits:")
    robert_port = draw_portrait(48, 48, (107, 83, 68), (160, 160, 160))
    save(robert_port, "Characters", "Robert", "robert_portrait.png")

    martha_port = draw_portrait(48, 48, (122, 107, 140), (192, 192, 192))
    save(martha_port, "Characters", "Martha", "martha_portrait.png")

    # -- Objects --
    print("\nObjects:")

    wedding = draw_object(32, 32, (160, 130, 80), (200, 180, 120), "rect")
    save(wedding, "Objects", "wedding_photo.png")
    save(draw_glow(32, 32, (255, 220, 130)), "Objects", "wedding_photo_glow.png")

    guitar = draw_object(32, 32, (180, 140, 80), (60, 40, 20), "guitar")
    save(guitar, "Objects", "guitar.png")
    save(draw_glow(32, 32, (220, 180, 100)), "Objects", "guitar_glow.png")

    picks = draw_object(32, 32, (150, 155, 165), (100, 100, 110), "picks")
    save(picks, "Objects", "ice_picks.png")
    save(draw_glow(32, 32, (180, 190, 210)), "Objects", "ice_picks_glow.png")

    phone = draw_object(24, 24, (50, 50, 55), (200, 200, 180), "phone")
    save(phone, "Objects", "phone.png")
    save(draw_glow(24, 24, (200, 200, 150)), "Objects", "phone_glow.png")

    document = draw_object(32, 40, None, None, "document")
    save(document, "Objects", "document.png")
    save(draw_glow(32, 40, (255, 255, 220)), "Objects", "document_glow.png")

    # -- Environment --
    print("\nEnvironment:")
    room = draw_room_background(480, 270)
    save(room, "Environment", "room_background.png")

    # Simple furniture placeholders
    desk = draw_object(64, 32, (100, 75, 50), (80, 60, 40), "rect")
    save(desk, "Environment", "furniture_desk.png")

    bookshelf = draw_object(48, 64, (90, 70, 50), (110, 85, 60), "rect")
    save(bookshelf, "Environment", "furniture_bookshelf.png")

    chair = draw_object(32, 40, (130, 80, 50), (110, 65, 40), "rect")
    save(chair, "Environment", "furniture_chair.png")

    # -- UI --
    print("\nUI:")
    # Dialogue panel background (9-slice ready)
    panel = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    pd = ImageDraw.Draw(panel)
    pd.rounded_rectangle([0, 0, 63, 63], radius=4, fill=(30, 25, 40, 220))
    pd.rounded_rectangle([1, 1, 62, 62], radius=3, outline=(120, 100, 80), width=1)
    save(panel, "UI", "dialogue_panel.png")

    btn = Image.new("RGBA", (32, 16), (0, 0, 0, 0))
    bd = ImageDraw.Draw(btn)
    bd.rounded_rectangle([0, 0, 31, 15], radius=2, fill=(80, 65, 50))
    bd.rounded_rectangle([1, 1, 30, 14], radius=2, outline=(140, 120, 90), width=1)
    save(btn, "UI", "button_normal.png")

    btn_pressed = Image.new("RGBA", (32, 16), (0, 0, 0, 0))
    bp = ImageDraw.Draw(btn_pressed)
    bp.rounded_rectangle([0, 0, 31, 15], radius=2, fill=(60, 50, 40))
    bp.rounded_rectangle([1, 1, 30, 14], radius=2, outline=(100, 85, 65), width=1)
    save(btn_pressed, "UI", "button_pressed.png")

    inp_bg = Image.new("RGBA", (64, 16), (0, 0, 0, 0))
    ip = ImageDraw.Draw(inp_bg)
    ip.rounded_rectangle([0, 0, 63, 15], radius=2, fill=(20, 18, 30, 200))
    ip.rounded_rectangle([1, 1, 62, 14], radius=2, outline=(80, 70, 60), width=1)
    save(inp_bg, "UI", "input_field_bg.png")

    nameplate = Image.new("RGBA", (48, 12), (0, 0, 0, 0))
    np_draw = ImageDraw.Draw(nameplate)
    np_draw.rounded_rectangle([0, 0, 47, 11], radius=2, fill=(100, 80, 60, 200))
    save(nameplate, "UI", "name_plate.png")

    # -- Click indicator --
    click = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    cd = ImageDraw.Draw(click)
    cd.ellipse([2, 2, 13, 13], outline=(255, 255, 200, 180), width=1)
    cd.ellipse([5, 5, 10, 10], fill=(255, 255, 200, 120))
    save(click, "Effects", "click_indicator.png")

    print(f"\nDone! All placeholder sprites generated.")


if __name__ == "__main__":
    main()
