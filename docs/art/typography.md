# Brief — 2. Typography

Taken out of `docs/ART.md` §2: the full reasoning only concerns whoever imports the font or adds a
text; `ART.md` §2 keeps only the family adopted and the reminder of the WebGL pitfall.

## 2.1 The stance

**A single family, round but restrained, in two weights only — the text must read at the size of an
itch page before it reads as a style choice.** The game is called "Snack": too geometric a design
(Arial, Roboto) would contradict the casual spirit of the title; but too round and bubbly a design
would read as a game for children, which "Snake Snack" does not announce.

## 2.2 Family adopted: Nunito (SIL OFL)

**Nunito**, two weights: **SemiBold** for secondary, permanent text, **ExtraBold** for headings and
HUD numbers. No Regular weight in the game: at these sizes and on a downscaled WebGL render, too thin
a stroke of a round typeface disappears before it can be read (a reminder of the general pitfall:
*docs/pitfalls/fonts-text.md*, and the generic warning about round display faces — a thinner stroke
than Arial at the same size).

**Why Nunito rather than a more "video game" font**: it is one of the oldest and most used Google
Fonts families in UI, hence the most likely to have **static files per weight**
(`static/Nunito-SemiBold.ttf`, `static/Nunito-ExtraBold.ttf`) next to the variable file
`Nunito[wght].ttf`.

⚠ **Those static files DO NOT EXIST — and the family is kept anyway.**
`GET https://api.github.com/repos/google/fonts/contents/ofl/nunito` (2026-08-28) returns no `static/`
folder, only `Nunito[wght].ttf` and `Nunito-Italic[wght].ttf`; `upstream_info.md` confirms
`buildStatic: false` in upstream's `config.yaml` — the static weights are not late in being published,
they are **never built**.

✅ **Ruled by the author on 2026-08-28: we INSTANTIATE, we do not change family.**
`tools/generate_fonts.py` fetches the variable file, freezes `wght=600` and `wght=800` with
`fontTools.varLib.instancer`, and writes `Assets/Resources/Fonts/Nunito-SemiBold.ttf` and
`Nunito-ExtraBold.ttf`. The pitfall in `docs/pitfalls/fonts-text.md` targets importing a **variable**
file into Unity; an extracted instance is an ordinary static `.ttf`, and Unity imports it as such
(`TrueTypeFontImporter`, `includeFontData: 1` — verified in the generated `.meta`).

**Why a versioned generator rather than two hand-dropped `.ttf` files**: six months later nobody would
know from which source, at which weight and with which version of `fonttools` they came. The script
also pins the **sha256 of upstream** — `main` moves, and a silent regeneration a year later would
produce files other than the repository's with nothing to report it.

**Licence — the name "Nunito" may be kept.** Nunito's `OFL.txt` declares **no Reserved Font Name**:
its copyright line is `Copyright 2014 The Nunito Project Authors
(https://github.com/googlefonts/nunito)`, without the `with Reserved Font Name` suffix that would
trigger clause 5 of the SIL OFL. A modified instance may therefore stay "Nunito SemiBold" / "Nunito
ExtraBold". ⚠ **This check must be redone for any other family**: most of them carry one, and renaming
then becomes mandatory. `OFL.txt` is committed next to the `.ttf` files (so it also ships in the
binary, as the licence requires) and the attribution is in `docs/CREDITS.md`.

**Glyph coverage — verified on 2026-08-28 on the INSTANTIATED file** (that is the one imported, not
upstream): `tools/generate_fonts.py` reads the `cmap` table and refuses to write if a single one of
the **125 required characters** is missing — ASCII 32-126 plus the set of accents below. Both weights
carry them all (938 glyphs in total). The check is **replayed on every regeneration**, and
`py tools/generate_fonts.py --check` replays it alone, rewriting nothing. The required set goes beyond
what the UI text uses today, so that a text added tomorrow does not force this brief to be reopened:
`à â ä ç é è ê ë î ï ô ö ù û ü À Â Ä Ç É È Ê Ë Î Ï Ô Ö Ù Û Ü`.

⚠ **And verified IN THE BROWSER, not by reasoning** (2026-08-28, `docs/TEST_REPORT.md`): web build
served by `tools/serve_web.py`, pause screen — the accent renders. The `cmap` proves the 125
characters are in the file; the browser proves the whole chain holds (instance → Unity import →
embedding → WebGL rasterisation). No system font can mask a gap at that point any more.

## 2.3 Sizes — raised by two points, 1280×720 reference

The HUD canvas (`GameHud.Build`) uses `CanvasScaler.ScaleWithScreenSize` on a 1280×720 reference
resolution: these sizes are therefore the real ones displayed in a full frame, and *shrink*
proportionally in the smaller window of an itch page — the opposite of a safety margin, hence the
raise.

| Text | Current size | Size adopted | Weight |
|---|---|---|---|
| `ControlsReminder` (the smallest, hence the most at risk) | 15 px | **18 px** | SemiBold |
| `RejectionWhilePaused` | 18 px | **20 px** | SemiBold |
| `PauseSubtitle` / `WinSubtitle` / `DeathSubtitle` | 20 px | **22 px** | SemiBold |
| `State`, `Score`, `Best` (permanent band) | 22 px | **24 px** | ExtraBold |
| `EndSummary` | 24 px | **26 px** | ExtraBold |
| `Title` (Paused / Game over / You win) | 54 px | **56 px** | ExtraBold |

**Sizes AND weights wired on 2026-08-28** in `GameHud.Build`, as they stand.
⚠ **Never a `FontStyle.Bold` on top**: the weight comes from the file. uGUI's synthetic bold would add
itself to an already bold drawing and clog Nunito's round counters, exactly what §2.4 forbids thick
outlines from doing.

⚠ `ControlsReminder` is anchored **14 px** from the bottom, not 10: the pivot being at the centre of a
24 px box, 10 px made the bottom of the box fall 2 px **below** the screen and clipped the descenders
of `g`, `p`, `q` (measured on a build, fixed 2026-08-28). It was not the text size that cut — it
already cut at 15 px — but the anchor. ⚠ **The bottom margin is still missing under the playfield**
(`docs/gdd/grid.md`): this fix keeps the text on screen, it does not settle that trade-off.

Floor adopted for any future text: **18 px** at this reference resolution — below that, itch's
downscale makes it unreadable before the typeface's weight even comes into play.

## 2.4 Technical constraints

- Font in **static TrueType** format (`.ttf`), never the variable file imported as-is.
- Every `.ttf` carries its `OFL.txt` next to it in the repository and in `docs/CREDITS.md` (SIL OFL,
  attribution required).
- Text colour always through `UiPalette.HudText` / `UiPalette.SecondaryText` (§1) — never a `Color`
  written down in `GameHud.cs` or anywhere else.
- If an `Outline` or a shadow is ever added for readability on a variable background: **≤ 1 px**. A
  thicker rim closes the counters of a round letter (`a`, `e`, `o` clog up) instead of outlining it.
  Prefer a semi-opaque plate behind the text to a thick outline if the contrast is not enough.

## 2.5 Bans

- Never a Unicode arrow character (`← → ↑ ↓`) in a `Text` component — silently lost on WebGL, already
  banned by `ART.md` §5.7 and `docs/pitfalls/fonts-text.md`. Every directional symbol is a sprite.
- Never a **variable** font file imported as though it fixed a weight: a weight is chosen at import,
  not at runtime.
- Never a text below 18 px at the 1280×720 reference resolution.
- Never a Regular weight in this game: SemiBold is the floor.
- Never a character unchecked in the `cmap` of the file actually imported — check in the browser (web
  build), not by reasoning on the desktop.
