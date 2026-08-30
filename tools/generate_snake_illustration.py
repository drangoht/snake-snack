"""
Generates the snake illustration shown by the main menu.
========================================================

Why a generator rather than a hand-drawn `.png`: the brief `docs/art/menu.md` wants an illustration
**made of the same material as the game** — rounded squares from the palette, laid along a spiral.
Drawn with a mouse, it would be unreplayable: six months later nobody would know which colours, which
step, which segment size were used, and a palette retouch (`docs/ART.md` §1) would leave the image
behind with nothing to report it.

⚠ **The palette is not copied here.** It is READ from `Assets/Scripts/UI/UiPalette.cs`, which remains
"the only place in the repository where a colour is written down" (CLAUDE.md). A renamed or deleted
role makes this script fail with an explicit message, instead of producing an image in the old
colours.

Usage
-----
    py tools/generate_snake_illustration.py             # writes Assets/Resources/Illustrations/
    py tools/generate_snake_illustration.py --preview   # also writes a preview on the game background

⚠ Dependency: `Pillow` (`py -m pip install Pillow`). The script says so if it is missing.

⚠ After running this script, **run a build again** (`& "tools/build.ps1"`): a file written into
`Assets/` only exists for Unity once reimported, and the batchmode build is what does that
(`docs/pitfalls/assets-import.md`). It also produces the `.meta`, to be committed with the PNG.
"""

from __future__ import annotations

import argparse
import hashlib
import math
import pathlib
import re
import sys

try:
    from PIL import Image, ImageDraw
except ImportError:  # pragma: no cover - missing dependency
    print("Pillow is required: py -m pip install Pillow", file=sys.stderr)
    raise SystemExit(2)

ROOT = pathlib.Path(__file__).resolve().parent.parent

# ⚠ `Resources/` and not `Art/`: the menu is built IN CODE (`MenuScreen.Build`), with no serialised
# reference in the scene — so it can only load the image by path,
# `Resources.Load<Sprite>("Illustrations/snake-menu")`. Placed in `Art/`, it would be invisible at
# runtime and the only symptom would be a menu with no illustration
# (docs/pitfalls/assets-import.md).
TARGET_FOLDER = ROOT / "Assets" / "Resources" / "Illustrations"
FILE_NAME = "snake-menu.png"

PALETTE_SOURCE = ROOT / "Assets" / "Scripts" / "UI" / "UiPalette.cs"

# Side of the final image, in pixels. The menu shows it about 360 px wide in the 1280×720 reference
# frame: exporting larger leaves margin for screens that scale the itch page up.
SIDE = 512

# Supersampling. Everything is drawn at `SIDE * FACTOR` then reduced with LANCZOS: that is what gives
# crisp edges on rotated squares, which Pillow cannot antialias otherwise.
FACTOR = 4

# --- Snake geometry -----------------------------------------------------------------------
# A spiral: the tail at the centre, the head coming out of the top. Values are expressed in pixels of
# the FINAL image (before supersampling), so they can be read back against the brief.

TURNS = 2.05               # number of turns of the spiral
TAIL_RADIUS = 30.0         # radius at the start (tail, at the centre)
HEAD_RADIUS = 158.0        # radius at the end (head, at the edge)
SEGMENT_STEP = 27.0        # distance between two segment centres, along the curve
SEGMENT_SIDE = 25.0        # side of a full-size body segment
TAIL_SCALE = 0.48          # scale factor of the tail segment (it tapers)
HEAD_SIDE = 40.0           # side of the head
HEAD_GRADIENT = 4          # number of segments that shade progressively towards the head colour

# The apple, placed in front of the head, along its line of sight.
APPLE_DISTANCE = 80.0      # distance from the head centre to the apple centre
APPLE_DIAGONAL = 46.0      # diagonal of the diamond (it is a square turned 45°, as in game)


def read_palette() -> dict[str, tuple[int, int, int]]:
    """
    Extracts the colour roles from `UiPalette.cs`.

    ⚠ Reads ONLY the `FromBytes(0xNN, 0xNN, 0xNN)` form. The roles written as `new Color(...)` — the
    pause scrim, the build stamp — carry transparency and are of no use to an illustration; ignoring
    them silently is deliberate here, and the caller checks in any case that the roles it needs are
    present.
    """
    if not PALETTE_SOURCE.exists():
        raise SystemExit(f"Palette not found: {PALETTE_SOURCE}")

    source = PALETTE_SOURCE.read_text(encoding="utf-8")
    pattern = re.compile(
        r"public static readonly Color (\w+) = FromBytes\("
        r"0x([0-9A-Fa-f]{2}), 0x([0-9A-Fa-f]{2}), 0x([0-9A-Fa-f]{2})\);"
    )

    palette = {
        name: (int(r, 16), int(g, 16), int(b, 16))
        for name, r, g, b in pattern.findall(source)
    }

    required = ("SnakeBody", "SnakeHead", "Apple", "Background", "Pictogram")
    missing = [name for name in required if name not in palette]
    if missing:
        raise SystemExit(
            "Roles absent from UiPalette.cs: " + ", ".join(missing)
            + " - the generator guesses no colour, fix one or the other."
        )

    return palette


def blend(a: tuple[int, int, int], b: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    """Linear interpolation between two palette colours (no new colour is invented)."""
    t = min(1.0, max(0.0, t))
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def spiral_curve(samples: int = 4000) -> list[tuple[float, float]]:
    """
    The body's centre line, finely sampled, in image coordinates (y downwards).

    The radius grows to the power 0.85 rather than linearly: with linear growth, the inner turns
    tighten to the point where segments overlap, and the spiral reads as a blob. The exponent spreads
    the first turns and lets the body show.
    """
    centre = SIDE / 2.0
    points = []
    theta_max = TURNS * 2.0 * math.pi

    for i in range(samples + 1):
        progress = i / samples
        theta = progress * theta_max
        radius = TAIL_RADIUS + (HEAD_RADIUS - TAIL_RADIUS) * (progress ** 0.85)

        # -sin in y: the spiral turns clockwise on screen, the head comes out at the top.
        points.append((centre + radius * math.cos(theta), centre - radius * math.sin(theta)))

    return points


def lay_along(curve: list[tuple[float, float]], step: float) -> list[tuple[float, float, float]]:
    """
    Returns the positions and angles of the segments, spaced by a constant distance ALONG the curve.

    ⚠ A constant spacing in the *parameter* (one point every N samples) would give segments packed at
    the centre and spread at the edge: on a spiral, arc length is not proportional to angle. It is the
    arc that is measured here.
    """
    poses = []
    travelled = 0.0
    next_at = 0.0  # the first segment is placed on the very first point: that is the tail

    for i in range(1, len(curve)):
        x0, y0 = curve[i - 1]
        x1, y1 = curve[i]
        length = math.hypot(x1 - x0, y1 - y0)
        if length <= 0.0:
            continue

        while travelled + length >= next_at:
            progress = (next_at - travelled) / length
            # Visual angle of the heading: y goes down on screen, hence the sign.
            angle = -math.degrees(math.atan2(y1 - y0, x1 - x0))
            poses.append((x0 + (x1 - x0) * progress, y0 + (y1 - y0) * progress, angle))
            next_at += step

        travelled += length

    return poses


def rounded_square(side: float, colour: tuple[int, int, int], angle: float, radius: float = 0.28) -> Image.Image:
    """A segment: a rounded square drawn upright, then rotated. The radius is a fraction of the side."""
    size = max(2, int(round(side * FACTOR)))
    tile = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(tile)
    draw.rounded_rectangle(
        (0, 0, size - 1, size - 1), radius=max(1, int(size * radius)), fill=colour + (255,)
    )
    return tile.rotate(angle, resample=Image.BICUBIC, expand=True)


def paste_centred(background: Image.Image, tile: Image.Image, x: float, y: float) -> None:
    """Pastes a tile centred on (x, y), expressed in pixels of the final image."""
    cx = int(round(x * FACTOR - tile.width / 2.0))
    cy = int(round(y * FACTOR - tile.height / 2.0))
    background.alpha_composite(tile, (cx, cy))


def draw_head(side: float, palette: dict, angle: float) -> Image.Image:
    """
    The head: a bigger rounded square, two eyes and a tongue, drawn **facing right** then rotated as
    one — the eyes therefore follow the heading with no extra arithmetic.
    """
    size = int(round(side * FACTOR))
    margin = int(size * 0.55)  # room for the tongue, which sticks out in front
    tile = Image.new("RGBA", (size + 2 * margin, size + 2 * margin), (0, 0, 0, 0))
    draw = ImageDraw.Draw(tile)

    left, top = margin, margin
    right, bottom = margin + size, margin + size

    # The tongue first: it passes UNDER the head, only its tip sticks out.
    thickness = max(2, int(size * 0.09))
    middle = top + size / 2.0
    front = right + size * 0.42
    draw.line([(right - size * 0.1, middle), (front, middle)], fill=palette["Apple"] + (255,), width=thickness)
    draw.line([(front, middle), (front + size * 0.16, middle - size * 0.16)],
              fill=palette["Apple"] + (255,), width=thickness)
    draw.line([(front, middle), (front + size * 0.16, middle + size * 0.16)],
              fill=palette["Apple"] + (255,), width=thickness)

    draw.rounded_rectangle((left, top, right, bottom), radius=int(size * 0.32),
                           fill=palette["SnakeHead"] + (255,))

    # The eyes are the blue-black of the background: the only role dark enough to stand out against
    # the light head without introducing a colour that exists nowhere else in the game.
    eye_radius = size * 0.11
    for offset in (-1, 1):
        cx = left + size * 0.66
        cy = middle + offset * size * 0.24
        draw.ellipse((cx - eye_radius, cy - eye_radius, cx + eye_radius, cy + eye_radius),
                     fill=palette["Background"] + (255,))

    return tile.rotate(angle, resample=Image.BICUBIC, expand=True)


def draw_apple(palette: dict) -> Image.Image:
    """
    The apple: a square turned 45°, exactly as in game (`BoardView.BuildApple`).

    ⚠ Shape carries the information before colour (`docs/ART.md` §4): the diamond must stay a diamond
    in the illustration, otherwise the menu announces an apple the game does not show.
    """
    side = APPLE_DIAGONAL / math.sqrt(2.0)
    diamond = rounded_square(side, palette["Apple"], 45.0, radius=0.18)

    # A tiny white highlight, top left: it gives volume without introducing a colour — pure white is
    # already a palette role (`Pictogram`).
    highlight = Image.new("RGBA", diamond.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(highlight)
    r = diamond.width * 0.065
    cx, cy = diamond.width * 0.36, diamond.height * 0.34
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=palette["Pictogram"] + (170,))
    return Image.alpha_composite(diamond, highlight)


def compose(palette: dict) -> Image.Image:
    image = Image.new("RGBA", (SIDE * FACTOR, SIDE * FACTOR), (0, 0, 0, 0))

    curve = spiral_curve()
    poses = lay_along(curve, SEGMENT_STEP)
    if len(poses) < 6:
        raise SystemExit("The spiral produced only %d segments: revisit SEGMENT_STEP." % len(poses))

    body = poses[:-1]
    head_x, head_y, head_angle = poses[-1]

    last = len(body) - 1
    for index, (x, y, angle) in enumerate(body):
        progress = index / last if last else 1.0

        # The tail tapers, and the last segments shade towards the head colour: the eye finds the head
        # before it has followed the body.
        scale = TAIL_SCALE + (1.0 - TAIL_SCALE) * (progress ** 0.6)
        nearness = max(0.0, (index - (last - HEAD_GRADIENT)) / HEAD_GRADIENT) if HEAD_GRADIENT else 0.0
        colour = blend(palette["SnakeBody"], palette["SnakeHead"], nearness * 0.55)

        paste_centred(image, rounded_square(SEGMENT_SIDE * scale, colour, angle), x, y)

    paste_centred(image, draw_head(HEAD_SIDE, palette, head_angle), head_x, head_y)

    # The apple is placed ALONG THE LINE OF SIGHT: that is what tells the game loop in one image,
    # rather than a snake and an apple simply next to each other.
    radians = math.radians(head_angle)
    apple_x = head_x + math.cos(radians) * APPLE_DISTANCE
    apple_y = head_y - math.sin(radians) * APPLE_DISTANCE
    paste_centred(image, draw_apple(palette), apple_x, apple_y)

    return reframe(image).resize((SIDE, SIDE), resample=Image.LANCZOS)


def reframe(image: Image.Image) -> Image.Image:
    """
    Recentres the drawing on a square, based on what is actually opaque.

    ⚠ This is not cosmetics: the menu places the image in a fixed rectangle, and it is the CENTRE OF
    THE FILE that lands at the centre of that rectangle. Without reframing, the slightest tweak to the
    constants above (one more turn, a further apple) shifts the illustration in the menu's layout
    without the menu having been touched — and the fix gets made in the wrong file.
    """
    box = image.getbbox()
    if box is None:
        raise SystemExit("The illustration is entirely transparent: nothing was drawn.")

    content = image.crop(box)
    side = int(max(content.width, content.height) * 1.06)  # 3 % margin on each side
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.alpha_composite(content, ((side - content.width) // 2, (side - content.height) // 2))
    return square


def main() -> int:
    parser = argparse.ArgumentParser(description="Snake illustration for the main menu.")
    parser.add_argument("--preview", action="store_true",
                        help="also writes docs/check-menu-illustration.png, the image on the real game background")
    arguments = parser.parse_args()

    palette = read_palette()
    image = compose(palette)

    TARGET_FOLDER.mkdir(parents=True, exist_ok=True)
    target = TARGET_FOLDER / FILE_NAME
    image.save(target)

    fingerprint = hashlib.sha256(target.read_bytes()).hexdigest()[:16]
    # ASCII messages: the Windows console is in cp1252 and an accented character raises
    # UnicodeEncodeError there, which would make the script fail AFTER writing its image.
    print(f"Written: {target.relative_to(ROOT)} ({SIDE}x{SIDE}, sha256 {fingerprint})")

    if arguments.preview:
        # ⚠ `docs/ART.md` §4: every sprite is validated on the REAL game background, never on a
        # checkerboard.
        background = Image.new("RGBA", image.size, palette["Background"] + (255,))
        preview = ROOT / "docs" / "check-menu-illustration.png"
        Image.alpha_composite(background, image).save(preview)
        print(f"Preview: {preview.relative_to(ROOT)}")

    print("Run a build again: Unity will import the PNG and produce its .meta, to commit with it.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
