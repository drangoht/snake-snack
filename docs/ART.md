# ART — Art direction for Snake Snack

> This document takes in every art-direction decision of the project. §1 (palette), §2 (typography)
> and §5 (feedback for a refused input) are settled. §3 (sprite grid and scale) stays **empty and
> structured**, so that what follows files itself there without redoing the outline every time — do
> not fill it in pre-emptively.

## 1. Palette

**Stance**: a cold, near-black base (background, playfield, grid) on which only four warm colours
carry gameplay information — the wall in amber, the apple in red, the snake in green, and a pure white
reserved for the signal that must always dominate (the rejection pictogram). Full reasoning, numeric
contrast ratios and rejected variants: [`art/palette.md`](art/palette.md).

Lives in `Assets/Scripts/UI/UiPalette.cs`. **Never a hard-coded colour anywhere else** in the code or
in the generators.

| Role | Colour |
|---|---|
| `Background` | `#0A0E13` |
| `Playfield` | `#121821` |
| `GridLine` | `#1C2530` |
| `PlayfieldBorder` | `#E3A23A` |
| `SnakeBody` | `#4E9358` |
| `SnakeHead` | `#D8F5C4` |
| `Apple` | `#E5473B` |
| `Pictogram` | `#FFFFFF` |
| `HudText` | `#E7EDF2` |
| `SecondaryText` | `#8792A0` |
| `PauseScrim` | `#000000` at 62 % |
| `BuildStamp` | `#FFFFFF` at 45 % |

⚠ The project is in **Gamma** colour space (`ProjectSettings.asset`): these hex codes are laid down
as-is (`/255` → `Color`), with no linear reconversion to do by hand.

## 2. Typography

⚠ **Nunito exists only as a VARIABLE file on `google/fonts`** (no `static/`, upstream in
`buildStatic: false`). Author's decision, 2026-08-28: **we instantiate it** rather than change family.
`tools/generate_fonts.py` freezes `wght=600` and `wght=800` with `fontTools.varLib.instancer` and
writes `Assets/Resources/Fonts/` — the repository's two `.ttf` files are not re-downloaded by hand.
Nunito declares **no Reserved Font Name**, so the name is kept (`docs/CREDITS.md`).

**Family adopted: Nunito** (SIL OFL), two weights only — **SemiBold** for secondary text,
**ExtraBold** for headings and HUD numbers. Sizes raised by two points relative to the code's current
ones (floor: 18 px at the 1280×720 reference resolution), strokes lightened — a round display face has
a thinner stroke than Arial at the same size. Full reasoning, sizes per text, licence and the
procedure for obtaining the static `.ttf`: [`art/typography.md`](art/typography.md).

⚠ A reminder of the pitfall already paid for (`docs/pitfalls/fonts-text.md`): Unity's fallback for
missing glyphs exists ONLY on the desktop — a WebGL browser silently loses any character absent from
the font (arrows ← → ↑ ↓ at the top of the list). Write only characters the font contains, and draw
the symbols as sprites. Check the `cmap` table of the file actually imported before trusting anything,
and check in the browser, not by reasoning.

## 3. Sprite grid and scale

<!-- To be defined as the briefs come (cell grid, outline thickness, text sizes). What §5 already sets
     as a hard constraint, reusable for the rest: game cell = 44 px, playfield 924×660 in a 1280×720
     frame, HUD band ~60 px, side margins ~178 px. -->

## 4. Contrast and accessibility — permanent rules

- Never information carried by colour alone: always doubled by a difference of shape, position or
  text.
- Never periodic looping flicker over a large area of the screen. An opacity variation triggered once
  (fade in/out) is allowed; a strobe is not.
- Every sprite is validated on the **real game background**, never on a neutral checkerboard.

## 5. Briefs — one subject, one file

> ⚠ **The `§5.x` numbering is preserved inside the brief's file.** The code and the tests refer to
> "ART §5.4", "ART §5.7" in some sixty places: those references stay correct, they name the matching
> subsection of `art/rejection-feedback.md`. Do not renumber.

<!-- ⚠ INDEX. A detailed brief goes into docs/art/<subject>.md: it only concerns whoever works on THAT
     subject, whereas this file is re-read before every asset. One line here. -->

| Brief | File | Status |
|---|---|---|
| The palette (§1) | [`art/palette.md`](art/palette.md) | settled 2026-08-28 |
| The typography (§2) | [`art/typography.md`](art/typography.md) | settled 2026-08-28 |
| The main menu and its illustration (GDD §4.6) | [`art/menu.md`](art/menu.md) | settled 2026-08-28 |
| Feedback for a refused input (GDD §3, §4.2) | [`art/rejection-feedback.md`](art/rejection-feedback.md) | settled; the 2026-08-27 objection (chevron contrast) is lifted by §1, to be reconfirmed in play |
| The juice — game feedback (movement, bite, death, apple, best score) | [`art/juicy.md`](art/juicy.md) | ruled 2026-08-28; delivered in full 2026-08-30 |
| The cartoon — shapes and outlines in play | [`art/cartoon.md`](art/cartoon.md) | ruled 2026-08-28; rounding and face delivered 2026-08-30 |

## 6. Decision history

Visual decisions already settled and rejected variants: [`art/history.md`](art/history.md). Do not
reopen a subject without something new.
