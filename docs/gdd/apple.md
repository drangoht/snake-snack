# 4.4 — The apple

**A single apple on the grid, at every instant.** It is placed when the game is set up, therefore
**before the first press**: the start is standing (§4.1), the player looks at the screen and picks
their first direction — with nothing to aim at, that choice would be blind. It is replaced **on the
very tick** it is eaten: no frame may be displayed without an apple, an empty grid for a fraction of a
second reads as a bug, not as a transition.

**Eating is never compulsory** — no hunger, no timer, no apple that expires. That is what makes
*every* apple position legitimate: it can neither block, nor hurt, nor force a route. A player who
judges the path too risky goes round in circles and loses nothing but time. Death therefore stays
attributable to the turn that committed to it (§2), whatever cell was drawn.

**Draw: enumeration, not rejection.** The number of free cells is `Grid.CellCount − length` (the snake
occupies exactly `length` distinct cells, otherwise it would be dead). We draw `k` uniformly in
`[0, freeCount)`, then **walk the grid in a fixed order** (increasing X within increasing Y) skipping
the body cells, and stop at the `k`-th free one. A single pass, **at most 315 cells**, no allocation, a
bounded cost identical whatever the fill level.

⚠ **"Draw at random, redraw while occupied" is the trap of this system.** On a nearly full grid the
expected number of draws tends to infinity and the game **freezes without raising the slightest
error** — no exception, no log, just a frame that never comes back. The defect only appears at the end
of a long game, which is to say never during testing. Ruled out, see §7.

**No placement constraint**: no minimum distance from the head, no ban on the cell straight ahead.
Constraining would take *favourable* apples away from the player (nothing more) while changing the
number of eligible cells, and therefore making every bench harder to describe. The frequency of
"gift" apples one or two cells away is a question of feel, not of safety.
<!-- to measure: do those very close apples devalue the score in the player's eyes? Feel. -->

**Resolving a tick, in this exact order** (ambiguity here produces an off-by-one-cell bug, invisible
on reading and obvious on screen):

1. Dequeue and validate the direction (§4.2) → `target = Directions.Advance(head, direction)`.
2. `Grid.IsOutside(target)` → **death** (wall, §2).
3. `ate = (target == apple)`.
4. Body collision: `target` compared with the segments, **tail excluded if `!ate`** → death if hit.
5. Insert `target` **at the head**; drop the tail **only if `!ate`**.
6. If `ate`: score +1, then draw the new apple **on the final state of the tick**.

**The snake grows from the head, on the tick the head enters the apple's cell** — not on the next
tick, not by adding a segment behind the tail. It is the **tail that does not move** during that
single tick; the length goes from N to N+1 immediately, and always equals `3 + score`. Corollary of
step 4: outside growth, the head **may** enter the cell the tail frees on the same tick — the tail
visibly leaves on screen, and refusing that move would kill on something that looks free.
<!-- The tail exclusion at step 4 is written even though step 6 guarantees an apple never appears on
     an occupied cell: the rule must not depend on a guarantee established elsewhere. -->

**Full grid = win.** After step 6, if `length == Grid.CellCount`, there is no free cell left: the game
stops with a **win**, i.e. 312 apples on the default grid. Same screen, same place and same one-key
restart as death (§2), with a distinct label. This state is not an ornament: without it, the draw runs
on `[0, 0)` and breaks or loops. It is out of human reach <!-- to measure: real median score --> and
must be written all the same.

**Reproducible randomness.** The draw consumes an **explicit generator belonging to the game**, seeded
by an integer. The seed is settable **without recompiling**, through the same tuning file as the rate
and the grid; absent, it is derived from the clock and **logged at startup** so it stays replayable.
With the same seed and the same sequence of presses, a game replays identically — that is the
condition for the paired bench asked for in §4.1 and §4.3, not a development convenience.

**"Belonging to the game" applies to the restart too** (clarified at implementation, 2026-08-27). The
generator is re-seeded on **every** new game, not once per session:
- **seed fixed** in the tuning file → every game replays the same apple sequence. That is **bench
  mode**, not a game mode: without it, a game can only be replayed once.
- **seed absent** (value 0, used as a sentinel) → every game gets a fresh seed, **logged as well**.
  Without that, a player pressing Space would replay the same apples indefinitely, and "restart" would
  lose what makes you want to restart (§2).

Those game seeds are drawn by a **second** generator, seeded once from the clock — not by the apples'
one, which an extra draw would shift. That is the first application of the "any other need for
randomness takes a separate instance" rule below.

⚠ **The real clock resolution under Windows is about 15 ms**, not 100 ns: two games restarted back to
back would draw the same seed if it came straight from the clock. The second generator avoids that
case — which would only have shown in use, as "two games in a row have the same apples, sometimes".

⚠ **Neither `UnityEngine.Random` nor `System.Random`**: the first is shared global state and
unavailable in `Rules/`; the second's sequence **is not contractually stable** across runtimes, and a
bench whose apples change between `dotnet test`, the desktop build and the WebGL build no longer
compares anything. The generator is written in `Rules/`, its algorithm is ours.

⚠ **Nothing but the apple draws from this generator.** A visual effect drawing a number from it would
shift the whole sequence and break the pairing, with no test failing. Any other need for randomness
(cosmetic, audio) takes a separate instance.

Rules **written** (2026-08-27): `Assets/Scripts/Rules/Apple.cs` (draw by enumeration) and
`Assets/Scripts/Rules/RandomSource.cs` (seeded generator, **SplitMix64**, algorithm written down in
the repository). Resolving the tick belongs to the rule that already moves the snake
(`Assets/Scripts/Rules/Snake.cs`), not to `Apple.cs`: the apple answers "where" and "how many", never
"when". Only step 6 — replacing the apple or finding the grid full — lives in the engine wiring
(`SnakeGame.PlayOneTick`), because it touches the state of the game and the rendering.

The score of §4.5 has been **counted since 2026-08-28**: step 6 increments before drawing the new
apple, and before finding the grid full — the apple that fills the grid has been eaten, and the win
screen must show the score that includes it.
