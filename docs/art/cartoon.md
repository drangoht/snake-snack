# Brief — The cartoon (shapes, outlines, proportions in play)

Taken out of `docs/ART.md` §5. **Ruled by the author on 2026-08-28**: the art director's
recommendations are adopted as they stand (§7). This brief touches no mechanic, and reopens **neither**
the palette (`docs/art/palette.md`) **nor** the typography (`docs/art/typography.md`), both settled
with numeric proof on 2026-08-28.

## 1. Diagnosis — the cartoon already exists, but stops at the menu's door

- The whole rendering of the game (`Assets/Scripts/Gameplay/PrimitiveShapes.cs`) starts from **a single
  shared sprite**: a stretched white pixel, in `FilterMode.Point`. Playfield, grid, border, body, head,
  apple, chevron — every rectangle in the game has strictly crisp 90° edges, without the slightest
  antialiasing. It is, literally, coloured graph paper.
- Conversely, `tools/generate_snake_illustration.py` (the menu illustration, already validated) draws
  **rounded squares** (radius 0.28 of the side), supersampled ×4 then reduced with LANCZOS — hence
  smoothed —, with a face on the head (two eyes, a tongue).
- **Direct consequence for the itch page**: the menu and the cover promise a round, expressive
  character; as soon as the game starts, that character turns back into a row of mute rectangles. The
  gap between the poster and the product is the clearest symptom of "not cartoon enough".
- The typeface (Nunito, already round — see `docs/art/typography.md` §2.1, which explicitly rules out
  too geometric a design) and the palette (four warm colours on a cold base) are already in the right
  register: nothing to take up there.

## 2. Stance

**Extend to the game the material already validated for the menu** — rounded squares, smoothing, a
minimal face on the head — without touching what carries readability: no colour, no size, no cell
position changes.

## 3. What moves

### 3.1 A "rounded cell" sprite for the snake's body and head

Replaces the flat rectangle for those two elements only (not the border, not the grid — §3.4).

- **Generation**: a new script, same family as the menu illustration's generator — a transparent square
  PNG, a rounded rectangle, **radius 0.28 of the side** (the same ratio as the menu: the game's
  character and the poster's must recognise each other as the same drawing), supersampled then reduced
  with LANCZOS. Export at **128×128** — large enough to stay crisp at a 44 px cell and for a future
  zoom.
- **Import**: `Assets/Resources/Shapes/rounded-cell.png`, forced to Sprite (extending
  `Assets/Editor/ImportIllustrations.cs` to that new folder rather than writing a second one), in
  **9-slice** (`spriteBorder` ≈ 36 px, the radius in image pixels) so that `BoardView` keeps stretching
  the sprite to `CellSize - 2` without distorting the corners.
- `PrimitiveShapes` gains a `RoundedRectangle` factory, next to `Rectangle` (unchanged).
- **`FilterMode` of that sprite: Bilinear**, not Point — that is what gives the smoothing. The shared
  white square (`Square()`) stays on Point: everything that must remain a crisp flat area (border,
  grid) does not change.
- **WebGL cost**: nil to negligible. The same number of `SpriteRenderer`s as today; a 9-slice sprite
  adds a few triangles per instance, no extra draw call (already batched by shared texture).

### 3.2 The apple: slightly rounded corners

The same treatment as §3.1, radius 0.18 (the one used by the menu generator's apple) rather than
today's sharp angles. It stays a **diamond** — see §4, that is not negotiable — only its corners
soften. To be checked technically whether the 9-slice of `rounded-cell.png` turned 45° is enough, or
whether a second small dedicated PNG (`rounded-apple.png`, same script) is safer.

### 3.3 A minimal face on the head, in play

- Two dots in the `Background` colour (already a `UiPalette` role, exactly the use the menu
  illustration makes of it), placed as children of the head segment, oriented by the heading — the same
  logic as the illustration's head: the eyes stay on the side looking ahead.
- **No tongue in play** (unlike the menu): it would stick out of the cell and encroach on the next one
  every tick, flickering at 8 ticks/s — the exact opposite of the "no flicker" ban of `ART.md` §4.
- ✅ **Delivered on 2026-08-30 at the menu's exact ratio** (radius 0.11 of the cell), after a
  screenshot at real scale: the risk announced — "~4-5 px radius, below the readability threshold" —
  **did not materialise**; a 4.6 px radius makes a 9 px diameter eye, clear at 44 px. The eyes are
  **children of the head segment**, so subject to its gulp (`juicy.md` §5) and its tilt (§9) — a circle
  under a non-uniform scale stays an ellipse, with no shear.

### 3.4 What might follow, low priority

Round only the **four corners** of the playfield (not the whole length of the walls) to soften the
frame without losing the metaphor of the wall. Only to be considered once the rest is done and tested
— see also §4 on why the wall stays hard by default.

## 4. What does not move, and why

- **The 12 roles of `UiPalette.cs`**, their hex codes and their contrast ratios
  (`docs/art/palette.md`), including the apple/body colour-blindness trade-off which rests on shape
  more than on colour. Changing a hue without redoing those calculations would break that guarantee
  with nothing to report it.
- **Nunito**: already round, already "casual without being childish" — the typography brief explicitly
  rules out a more "bubbly" design (Baloo 2) for that exact reason. It is already the right register,
  nothing to take up.
- **The grid (21×15), the cell size (44 px), the rate (8 ticks/s)**: mechanics and layout, outside this
  brief's scope.
- **The shape of the rejection chevron** (`docs/art/rejection-feedback.md` §5.6): already settled and
  verified on screen (contrast measured at 3.81:1). Rounding it would reopen a measurement already
  made for a marginal cosmetic gain on a signal that must first of all be seen fast.
- **The apple's diamond silhouette**: it is the shape that distinguishes it from the snake before
  colour even comes in (`ART.md` §4) — this brief softens its corners (§3.2), it never changes its
  silhouette.
- **The playfield border (the wall)**: stays a sharp-cornered rectangle by default. A wall is not a
  character; not rounding it marks the difference between what lives (round) and what kills (hard,
  geometric) — a reading choice, not an oversight.

## 5. Technical constraints

- `Assets/Resources/Shapes/`, never `Assets/Art/`: loaded by path like the menu illustration
  (`docs/pitfalls/assets-import.md`).
- Force the import to Sprite (project in 3D mode) — extend the existing postprocessor rather than write
  a second one to maintain.
- `spriteBorder` (9-slice) set **at import**, never in a hand-edited `.meta` — it would be rewritten on
  the next reimport, the same pitfall already documented for `textureType`
  (`docs/pitfalls/assets-import.md`).
- The PNG itself stays white/transparent: the colour always comes from the `SpriteRenderer.color`
  referenced from `UiPalette`, never baked into the file — otherwise a future palette change would no
  longer affect these sprites, the exact opposite of the "one single place in the repository" rule.
- **Consequence for the itch page**: the menu (`docs/itch/capture-1-menu.png`, `cover.png`) does not
  need regenerating — its illustration does not change. On the other hand
  `docs/itch/capture-2-partie.png` and `capture-3-perdu.png` show the **current** rendering, in flat
  rectangles: to be retaken once §3.1–§3.3 are implemented, otherwise the page announces a snake the
  game no longer faithfully shows.

## 6. Bans

- Never a new colour: every sprite added dresses itself with a role that already exists in
  `UiPalette.cs`.
- Never a file loaded by `Resources.Load` placed in `Assets/Art/`.
- Never a hand-edited `.meta` to set the 9-slice.
- Never a detail (eyes, a possible tongue) sticking out of the cell's square — everything must fit in
  the `CellSize - 2` px already reserved for the segment.
- Never `FilterMode.Bilinear` on the shared white square (`PrimitiveShapes.Square()`): the border and
  the grid lines stay crisp flat areas, they are measuring marks, not characters.
- Never round the rejection chevron or change its size without going back through
  `docs/art/rejection-feedback.md` — contrast already measured, not to be reopened without something
  new.
- Never change the apple's silhouette (diamond) or a value in `UiPalette.cs` under cover of this brief.

## 7. The author's ruling (2026-08-28)

The art director's recommendations adopted without modification:

- **The rounding (§3.1) goes first**, with the P1 base of `juicy.md` — it alone takes the game off the
  graph paper, and it depends on no decision left open.
- **The in-game face (§3.3) is prototyped, not decided**: to be seen on a screenshot at real scale
  (44 px) before being kept. If it does not read, the rounding alone does the job — do not force an
  unreadable detail. → **Settled on 2026-08-30 on a screenshot: kept as-is, without shrinking the
  eyes.**
- **The playfield corners (§3.4) do not move**: the wall stays hard, by contrast with the round
  creature. A reading choice, not an oversight.
- **The itch screenshots (§5) are regenerated as soon as the rounding is delivered**:
  `capture-2-partie.png` and `capture-3-perdu.png` show the flat-rectangle rendering, and a page that
  announces a snake the game no longer shows is the costliest defect of a store page.
- **The apple's treatment (§3.2)** is left to be settled at implementation, according to what the
  9-slice really supports under rotation.
