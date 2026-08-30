"""
Generates the 630 x 500 cover of the itch.io page.
==================================================

Why a generator rather than a cropped screenshot: the cover is the **only image seen by visitors who
do not open the page** (`docs/ITCH_STORE_PAGE.md`), and it is read as a thumbnail, next to dozens of
others. A cropped screenshot loses either its title or its subject there — the game's menu puts the
text on the left and the snake on the right, a 16:9 ratio that no 1.26 crop keeps whole.

⚠ **The palette is not copied here**, as in `generate_snake_illustration.py`: it is READ from
`Assets/Scripts/UI/UiPalette.cs`. A cover in the old colours after a palette retouch would never be
noticed — nobody reopens an image published three versions ago.

⚠ **The illustration is not redrawn either**: we reuse the PNG already produced for the menu, so that
the thumbnail and the game's first screen show exactly the same snake. A visitor who does not find
the thumbnail's image when launching the game feels they clicked on something else.

Usage
-----
    py tools/generate_itch_cover.py           # writes docs/itch/cover.png

⚠ Dependency: `Pillow` (`py -m pip install Pillow`). The script says so if it is missing.

⚠ This image does NOT go into `Assets/`: it is never loaded by the game, only uploaded to itch.io by
hand. Putting it there would embed it in the binary for nothing.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError:
    sys.exit("Pillow missing: py -m pip install Pillow")

ROOT = Path(__file__).resolve().parent.parent

PALETTE_SOURCE = ROOT / "Assets" / "Scripts" / "UI" / "UiPalette.cs"
ILLUSTRATION_SOURCE = ROOT / "Assets" / "Resources" / "Illustrations" / "snake-menu.png"
TITLE_FONT = ROOT / "Assets" / "Resources" / "Fonts" / "Nunito-ExtraBold.ttf"
BODY_FONT = ROOT / "Assets" / "Resources" / "Fonts" / "Nunito-SemiBold.ttf"

TARGET = ROOT / "docs" / "itch" / "cover.png"

# ⚠ 630 x 500 is not a choice: it is the format itch.io expects for a cover. Any other size is resized
# by the site, and thin text comes out unreadable.
WIDTH = 630
HEIGHT = 500

# Supersampling, as for the illustration: everything is drawn large then reduced with LANCZOS.
FACTOR = 3


def read_palette() -> dict[str, tuple[int, int, int]]:
    """Extracts the colour roles from `UiPalette.cs` (same rules as the snake generator)."""
    if not PALETTE_SOURCE.exists():
        raise SystemExit(f"Palette not found: {PALETTE_SOURCE}")

    pattern = re.compile(
        r"public static readonly Color (\w+) = FromBytes\("
        r"0x([0-9A-Fa-f]{2}), 0x([0-9A-Fa-f]{2}), 0x([0-9A-Fa-f]{2})\);"
    )
    palette = {
        name: (int(r, 16), int(g, 16), int(b, 16))
        for name, r, g, b in pattern.findall(PALETTE_SOURCE.read_text(encoding="utf-8"))
    }

    required = ("Background", "HudText", "SecondaryText", "Apple")
    missing = [name for name in required if name not in palette]
    if missing:
        raise SystemExit(
            "Roles absent from UiPalette.cs: " + ", ".join(missing)
            + " - the generator guesses no colour, fix one or the other."
        )
    return palette


def compose(palette: dict[str, tuple[int, int, int]]) -> Image.Image:
    width, height = WIDTH * FACTOR, HEIGHT * FACTOR
    image = Image.new("RGB", (width, height), palette["Background"])

    # The illustration first, in the lower two thirds: it carries the eye, the title breathes above.
    # ⚠ It fits WHOLE inside the frame — a spiral clipped by the edge reads as a badly framed image,
    # not as a deliberate choice.
    if not ILLUSTRATION_SOURCE.exists():
        raise SystemExit(
            f"Illustration not found: {ILLUSTRATION_SOURCE}\n"
            "Run first: py tools/generate_snake_illustration.py"
        )
    snake = Image.open(ILLUSTRATION_SOURCE).convert("RGBA")
    side = int(height * 0.66)
    snake = snake.resize((side, side), Image.LANCZOS)
    image.paste(snake, ((width - side) // 2, height - side - int(height * 0.03)), snake)

    draw = ImageDraw.Draw(image)

    # The title, at the top, in capitals: it is the only element that must stay readable shrunk to a
    # 150 px wide thumbnail in a grid of games.
    title = ImageFont.truetype(str(TITLE_FONT), int(height * 0.135))
    _centred_text(draw, width, int(height * 0.10), "SNAKE SNACK", title, palette["HudText"])

    # The tagline: the sentence from GDD §1, not a slogan invented for the occasion.
    tagline = ImageFont.truetype(str(BODY_FONT), int(height * 0.048))
    _centred_text(
        draw, width, int(height * 0.235),
        "It grows with every bite.", tagline, palette["SecondaryText"],
    )

    return image.resize((WIDTH, HEIGHT), Image.LANCZOS)


def _centred_text(draw, width: int, y: int, text: str, font, colour) -> None:
    """Centres horizontally, `y` being the top of the text."""
    left, top, right, _ = draw.textbbox((0, 0), text, font=font)
    draw.text(((width - (right - left)) // 2 - left, y - top), text, font=font, fill=colour)


def main() -> int:
    palette = read_palette()
    for font in (TITLE_FONT, BODY_FONT):
        if not font.exists():
            raise SystemExit(
                f"Font not found: {font}\nRun first: py tools/generate_fonts.py"
            )

    TARGET.parent.mkdir(parents=True, exist_ok=True)
    compose(palette).save(TARGET)
    print(f"Cover written: {TARGET} ({WIDTH}x{HEIGHT})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
