# 4.3 — The grid

**21 × 15 cells, square** (315 cells, by judgement). Dimensions **odd on both axes**: an exact centre
cell exists, the condition for the snake to appear "in the centre" (§2) without a half-cell offset.

**Starting pose**: head on the centre cell `(10, 7)` zero-indexed, body at `(9, 7)` and `(8, 7)`,
**length 3** (from §2), **facing east**, at a standstill. The snake is still but **oriented**: the
reversal rule therefore applies from the very first press, and a player pressing West sees the refusal
before the game even starts — the rule teaches itself, with no tutorial.

**What 315 cells imply** (deduced, not measured): the snake occupies 1 % of the grid at the start; it
needs ~75 apples to occupy a quarter of it, the threshold at which navigation is *assumed* to become
stifling.
<!-- to measure: at what score does the player stop charging and start tracing a path? -->

**Length of a typical game** (deduced from the rate §4.1, not measured): on a W×H grid, the average
Manhattan distance between two randomly drawn cells is ≈ (W+H)/3, i.e. 12 cells here — about 1.5 s per
apple at 8 ticks/s, detours not counted. A 25-apple game fits under a minute: that is the duration the
one-key restart of §2 assumes.

**Readability** (computed): in a 1280×720 web frame with a HUD band of about 60 px, a cell is
`min(1280/21, 660/15)` = 44 px. The grid takes 924 px of width and leaves ~178 px of margin on each
side — enough to put score and best score **outside the playfield**, with no overlapping layers.

⚠ **Observed in play on 2026-08-28: there is margin only at the TOP and on the SIDES, not at the
bottom.** The 60 px band is taken entirely at the top, so the playfield touches the bottom edge of the
frame. The control reminder, anchored at the bottom, **overlaps the last row of cells**.
(The descender clipping that came with it was fixed on 2026-08-28 by raising the anchor from 10 to
14 px — it was the anchor, not the margin. The overlap itself remains whole.) The score and best score
of §4.5 do not suffer (they are in the top band), but the controls line contradicts the sentence above.
Three ways out, none settled: reserve a bottom band (the cell falls back to `min(1280/21, 600/15)` =
40 px, the grid shrinks), move the reminder into a side margin, or accept it over the playfield by
raising it a few pixels.
<!-- to settle: this is a layout trade-off, not a code bug. -->
Measured on 2026-08-28 on the Windows build: the reminder's box was anchored 10 px from the bottom and
24 px tall, so its bottom fell **2 px below the edge** — the clipping did not come from the text body
(it already existed at 15 px), but from the anchor. **Fixed**: 14 px, the box fits whole, the
descenders are complete (re-checked on a screenshot). ⚠ This fix does NOT close this trade-off: there
is still no margin under the playfield, and the text still reads over the cells.

**Bounds of the settable grid** (deduced from the starting pose, not from a design choice): width ≥ 5
and height ≥ 3 — three segments lined up from the centre column, plus one row above and one below so a
turn exists. Even dimensions are **refused at construction**: with no exact centre cell, the starting
pose of §2 makes no sense.
<!-- to validate: these minima have never been played, they only prevent an inconsistent state. -->

Expected rules: `Assets/Scripts/Rules/Grid.cs` — dimensions, centre cell, initial pose and the "cell
outside the grid" test (the lethal wall of §2). Dimensions settable **without recompiling**.
