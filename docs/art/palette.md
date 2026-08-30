# Brief — 1. The palette

Taken out of `docs/ART.md` §1: the full reasoning only concerns whoever touches `UiPalette.cs` or
draws a sprite; `ART.md` §1 keeps only the decision and the hex codes.

## 1.1 The stance

**A cold, near-black base that never seeks the eye, on which only what matters in play carries a warm
colour.** The wall that kills is amber, the apple that feeds is red, the snake stays green — neither
threat nor reward, just the player — and pure white belongs only to the signal that must always win
(the rejection pictogram). Background, playfield and grid stay one blue-grey family: nothing there
draws the eye, so that the four warm colours are seen at once.

This stance changes *no* shape, *no* position, *no* size already set elsewhere (§5.4, §5.6) — it
colours a system already validated in greyscale, it does not redraw it.

## 1.2 The 12 roles — exact coverage of `UiPalette.cs`

Twelve named roles already existed, in grey. This palette **covers exactly those twelve roles, under
the same meanings** — none added, none removed. **Wired on 2026-08-28**: the values are in
`Assets/Scripts/UI/UiPalette.cs`; no caller changed, only the type's name.

| Role | Provisional grey | Colour adopted | Why this colour |
|---|---|---|---|
| `Background` | 0.07 | `#0A0E13` | Near-black slate, never pure black (§1.4): the margins must ask for nothing. |
| `Playfield` | 0.13 | `#121821` | Same family as `Background`, one notch lighter: the playfield stands out without announcing itself. |
| `GridLine` | 0.20 | `#1C2530` | Still the same cold family: helps you count, must never read as an object. |
| `PlayfieldBorder` | 0.62 | `#E3A23A` | Amber: this is the wall that kills (GDD §2), the only "alert" colour permanently on screen. |
| `SnakeBody` | 0.58 | `#4E9358` | Mid green: the snake is the player, neither danger nor goal — the game's only neutral colour. |
| `SnakeHead` | 0.94 | `#D8F5C4` | The same green pulled towards light: the cell that matters most at the tick stays the most readable. |
| `Apple` | 0.80 | `#E5473B` | Warm red, the only colour of that hue in the game: nothing else is confused with "to eat". |
| `Pictogram` | 1.00 | `#FFFFFF` | Pure white, reserved: it is the only role at that value, it must always dominate, whatever the background. |
| `HudText` | 0.86 | `#E7EDF2` | Slightly cool white: permanently readable on `Background`, never as saturated as `Pictogram`. |
| `SecondaryText` | 0.52 | `#8792A0` | Mid blue-grey: ranked below `HudText`, always in the background's cold family. |
| `PauseScrim` | black 62 % | `#000000` at 62 % | Unchanged: an achromatic scrim competes with none of the four warm colours. |
| `BuildStamp` | white 45 % | `#FFFFFF` at 45 % | Unchanged, for the same reason — and it must stay readable whatever background it actually covers. |

No role is missing, none is superfluous: the four warm colours (border, body, head, apple) each carry
a distinct piece of gameplay information; the two whites (pictogram, text) and the two cold greys
(background, playfield) stay achromatic by construction, so they never rival the first four. If a
thirteenth role ever appears (a boost, a multiplier), it will have to justify itself the same way
before entering here — not slot itself between two existing roles.

## 1.3 The contrast proof

Standard WCAG ratio: `(L_light + 0.05) / (L_dark + 0.05)`, `L` = relative luminance (standard sRGB →
linear conversion, gamma 2.4). Reference thresholds: **3:1** for a graphical object / UI component
(WCAG 1.4.11), **4.5:1** for normal-size text, **3:1** for large text (≥ 24 px or 19 px bold) — our
HUD texts all comfortably exceed one threshold or the other, as set out below.

| Pair | Ratio | Verdict | The shape that doubles the colour |
|---|---|---|---|
| Apple / SnakeHead | **3.36 : 1** | ✅ fixes the defect observed (`docs/TEST_REPORT.md`, 2026-08-27): in grey this pair was only 1.41 : 1 — it was the "neighbouring grey". | A diamond against a square, and the apple is 0.72 of a cell against a full cell for the head. |
| Apple / SnakeBody | **1.07 : 1** | ⚠ weak in luminance alone — see §1.5, a point to watch. | The same isolated diamond against a continuous chain of squares: the whole silhouette differs, not just one cell. |
| SnakeHead / SnakeBody | **3.15 : 1** | ✅ better than the original grey (2.66 : 1). | The head is also the largest cell of the articulated group — GDD §5.6: never colour alone. |
| PlayfieldBorder / Playfield | **8.06 : 1** | ✅ markedly above the original grey (6.00 : 1) — "the wall that kills" must jump out. | A continuous line closing the whole perimeter of the grid, against a flat fill. |
| HudText / Background | **16.4 : 1** | ✅ a very wide margin over the text threshold (4.5 : 1). | — (text; the shape is the typeface itself, see §2). |
| SecondaryText / Background | **6.13 : 1** | ✅ above the text threshold, including if that text ends up overlapping `Playfield` (5.64 : 1 in that case — see `docs/gdd/grid.md` on the missing bottom margin). | A lighter font weight than `HudText` (§2): the hierarchy also reads without the colour. |
| Pictogram / SnakeBody | **3.72 : 1** | ✅ the case that matters most: ART §5.6 documents that the chevron of a reversal *always* lands on the body. Better than the original grey (3.04 : 1) — a real fix, not luck. | A solid chevron barred by a perpendicular stroke: no other shape in the game has that silhouette. |
| Pictogram / Playfield | **17.8 : 1** | ✅ the case where the pictogram lands on a free cell (`RejectedQueueFull`, direction not blocked). | Same. |

## 1.4 Technical constraints

- **The project is in Gamma colour space** (`ProjectSettings.asset`: `m_ActiveColorSpace: 0`). The hex
  codes above are laid down **as-is** (`#RRGGBB` → `/255` → `Color`): no linear reconversion to do by
  hand. If the project ever moves to Linear, this page must be reopened — until then, do not "fix"
  these values pre-emptively.
  ⚠ **Verified on a Windows build on 2026-08-28**: the roles set on a uGUI `Image`/`Text` and on the
  camera background come out **pixel-exact**; those set on a `SpriteRenderer` (border, body, head,
  apple, pictogram) come out **1 to 2 units darker** on R and G (`#E3A23A` read as `#E1A13A`,
  `#4E9358` read as `#4E9158`). The discrepancy is under 1 % and moves the ratios by less than 0.15 —
  it justifies no retouch, but it explains why a measurement taken on a screenshot does not land
  exactly on the column of §1.3.
- **Never a hard-coded colour anywhere but `UiPalette.cs`.** A sprite, a shader, an `Image`/`Text`
  component references the named role, never a copied `#RRGGBB`.
- **`Background` is never pure `#000000`** (§1.5, rejected variant): a strict black crushes on low-end
  screens and makes `GridLine` invisible for part of the itch audience.
- **Contrast checked on the real background**: the ratios above compare each pair with what actually
  surrounds it in play (`Playfield`, not a neutral checkerboard), as required by `ART.md` §4.

## 1.5 What is still open

- **Apple / SnakeBody (1.07 : 1)**: the weakest pair in this palette. Red and green are the hardest
  pair of hues to tell apart with deuteranopia (the most common form of colour blindness); at almost
  identical luminance, an affected player loses much of the chromatic contrast on top of the lightness
  contrast. Shape (an isolated diamond against a chain of squares) covers that case — it is exactly
  for this kind of pair that `ART.md` §4 forbids colour alone — but I have **no verification in real
  conditions** (a colour-blindness simulator or feedback from an affected player). To be confirmed by
  `game-tester`, screenshot in hand, before considering the subject closed.
  **First indication, 2026-08-28** (`docs/TEST_REPORT.md`): a deuteranope simulation (Viénot 1999)
  applied to a screenshot of the build. Apple and body both turn to a close olive, and the border's
  amber joins them — hue separates nothing any more, only shape and size hold, and they hold. That is
  only a matrix transform on a screenshot: it replaces neither a validated simulator nor an affected
  player, and the point stays open.
- **The missing bottom margin** (`docs/gdd/grid.md`, observed 2026-08-28) stays a layout trade-off,
  not a palette subject — but it changes the real background under `SecondaryText`
  (`ControlsReminder`). The ratio holds in both cases (§1.3), so this palette does not need to wait
  for that trade-off to be settled.
