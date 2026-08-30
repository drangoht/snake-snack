"""
Generates the game's static fonts from the upstream variable file.
=================================================================

Why this script exists rather than a hand-dropped `.ttf`: `docs/art/typography.md` §2.2 settles on
**Nunito**, but `google/fonts` publishes it **only as a variable file** (`Nunito[wght].ttf`) —
upstream carries `buildStatic: false`, so the static weights are not late in being published, they
are never built at all. The author ruled: we **instantiate** two frozen weights from the variable
file, rather than change family.

A font downloaded then instantiated by hand would be irreproducible: six months later nobody would
know from which source, at which weight, or with which version of `fonttools` the repository's two
files came. Hence this versioned generator.

Usage
-----
    py tools/generate_fonts.py           # fetch, instantiate, check, write
    py tools/generate_fonts.py --check   # rewrites nothing, re-checks what is already in Assets/

⚠ Dependency: `fonttools` (`py -m pip install fonttools`). The script says so if it is missing.
"""

from __future__ import annotations

import argparse
import hashlib
import io
import pathlib
import sys
import urllib.request

# --- Upstream ----------------------------------------------------------------------------

# ⚠ A listed URL, never a guessed one: the `ofl/nunito` folder was enumerated with
# `GET https://api.github.com/repos/google/fonts/contents/ofl/nunito` before these two lines were
# written. Guessing a `static/...` URL returns a 39 KB 404 page that looks like a successful
# download — that is the trap of `docs/pitfalls/fonts-text.md`.
VARIABLE_URL = "https://raw.githubusercontent.com/google/fonts/main/ofl/nunito/Nunito%5Bwght%5D.ttf"
LICENCE_URL = "https://raw.githubusercontent.com/google/fonts/main/ofl/nunito/OFL.txt"

# Fingerprint of the upstream file on the day these fonts were produced (2026-08-28). `main` moves:
# without this fingerprint, a silent regeneration a year later would produce files other than the
# repository's, with nothing to report it. If it no longer matches, upstream has published a new
# version: read it, re-check the `cmap`, THEN update this constant.
UPSTREAM_FINGERPRINT = "bb55a5ca5c2042335b3991af27c4d0705d0ef41cac6164ac737fd8f2a1e85207"

# --- What we produce ---------------------------------------------------------------------

# The game's only two weights (`docs/art/typography.md` §2.2). No Regular: at these sizes and on a
# downscaled WebGL render, a thin stroke of a round typeface disappears before it can be read.
WEIGHTS = {
    "Nunito-SemiBold.ttf": 600,
    "Nunito-ExtraBold.ttf": 800,
}

# ⚠ `Resources/` and not `Art/`: the HUD is built **in code** (`GameHud.Build`), with no serialised
# reference at all — so it can only load the font BY PATH,
# `Resources.Load<Font>("Fonts/Nunito-SemiBold")`. Placed in `Art/`, it would be invisible at
# runtime, and the only symptom would be text that does not draw (docs/pitfalls/assets-import.md).
TARGET_FOLDER = pathlib.Path("Assets/Resources/Fonts")

# --- Required glyph coverage -------------------------------------------------------------

# `docs/art/typography.md` §2.2: printable ASCII, plus the WHOLE set of French accents — not only
# the "e-acute" the UI happens to use today, otherwise the next text added reopens the subject.
# ⚠ A missing glyph falls back to a system font ON THE DESKTOP and disappears SILENTLY in a browser:
# this check is the only barrier before the web build.
FRENCH_ACCENTS = "àâäçéèêëîïôöùûüÀÂÄÇÉÈÊËÎÏÔÖÙÛÜ"
REQUIRED_CHARACTERS = "".join(chr(c) for c in range(32, 127)) + FRENCH_ACCENTS


def repository_root() -> pathlib.Path:
    """Repository root, derived from the script's location — no hard-coded path."""
    return pathlib.Path(__file__).resolve().parents[1]


def download(url: str) -> bytes:
    with urllib.request.urlopen(url, timeout=120) as response:
        return response.read()


def check_truetype_signature(data: bytes, origin: str) -> None:
    """
    A real TTF starts with `00 01 00 00`.

    Without this check, an HTML error page of a few dozen KB passes for a successful download and
    only fails much later, at Unity import time.
    """
    if data[:4] != b"\x00\x01\x00\x00":
        raise SystemExit(
            origin + " is not a TrueType: signature " + data[:4].hex()
            + " (a real TTF starts with 00010000). A disguised error page?"
        )


def check_glyphs(path: pathlib.Path) -> None:
    """Reads the `cmap` table of the file ACTUALLY produced, and refuses the slightest gap."""
    from fontTools.ttLib import TTFont

    with TTFont(path) as font:
        covered = set(font.getBestCmap().keys())

    missing = [c for c in REQUIRED_CHARACTERS if ord(c) not in covered]
    if missing:
        raise SystemExit(
            path.name + ": " + str(len(missing))
            + " required character(s) absent from the cmap: " + " ".join(missing)
        )
    print("  cmap OK: " + str(len(REQUIRED_CHARACTERS)) + " required characters, all present ("
          + str(len(covered)) + " in total in the font)")


def family_name(path: pathlib.Path) -> str:
    """Family name as the font declares it — used to prove OFL compliance."""
    from fontTools.ttLib import TTFont

    with TTFont(path) as font:
        family = font["name"].getDebugName(1)
        subfamily = font["name"].getDebugName(2)
    return str(family) + " / " + str(subfamily)


def instantiate(variable_data: bytes, weight: int, destination: pathlib.Path) -> None:
    from fontTools.ttLib import TTFont
    from fontTools.varLib import instancer

    font = TTFont(io.BytesIO(variable_data))
    # `updateFontNames` rewrites the `name` table from the STAT table: the instance declares itself
    # "Nunito SemiBold", not "Nunito" with an invisible weight. Without it, two files carrying the
    # same family name and the same subfamily tread on each other in the editor.
    instancer.instantiateVariableFont(font, {"wght": weight}, inplace=True, updateFontNames=True)
    destination.parent.mkdir(parents=True, exist_ok=True)
    font.save(destination)
    font.close()


def main() -> int:
    parser = argparse.ArgumentParser(description="Generates the game's static fonts.")
    parser.add_argument("--check", action="store_true",
                        help="regenerates nothing: re-validates the cmap of the files already present")
    args = parser.parse_args()

    try:
        import fontTools  # noqa: F401
    except ImportError:
        raise SystemExit("fonttools is missing. Install with: py -m pip install fonttools")

    target = repository_root() / TARGET_FOLDER

    if args.check:
        for name in WEIGHTS:
            path = target / name
            if not path.exists():
                raise SystemExit(str(path) + " is missing: run the script without --check.")
            print(name + " (" + family_name(path) + ")")
            check_glyphs(path)
        return 0

    print("Fetching upstream: " + VARIABLE_URL)
    variable = download(VARIABLE_URL)
    check_truetype_signature(variable, "The upstream file")

    fingerprint = hashlib.sha256(variable).hexdigest()
    if fingerprint != UPSTREAM_FINGERPRINT:
        raise SystemExit(
            "Upstream has changed (sha256 " + fingerprint + " instead of " + UPSTREAM_FINGERPRINT + ").\n"
            "This is not necessarily an error: read the new version, re-validate the cmap, "
            "then update UPSTREAM_FINGERPRINT in this script."
        )
    print("  upstream matches (" + str(len(variable)) + " bytes, sha256 verified)")

    for name, weight in WEIGHTS.items():
        destination = target / name
        instantiate(variable, weight, destination)
        print(name + ": wght=" + str(weight) + ", " + str(destination.stat().st_size)
              + " bytes (" + family_name(destination) + ")")
        check_glyphs(destination)

    # The SIL OFL requires the licence to accompany the Font Software, including when redistributed
    # inside a game binary. It therefore lives next to the .ttf files, not only in docs/CREDITS.md.
    licence = download(LICENCE_URL)
    (target / "OFL.txt").write_bytes(licence)
    print("OFL.txt: " + str(len(licence)) + " bytes")

    return 0


if __name__ == "__main__":
    sys.exit(main())
