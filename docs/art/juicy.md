# Brief — The juice (game feedback)

Taken out of `docs/ART.md` §5. **Ruled by the author on 2026-08-28**: the art director's
recommendations are adopted as they stand (§12). No mechanic is touched — this brief changes what the
player *feels* about an event already decided by the GDD, never what the event *does*.

## 1. Diagnosis — why the game feels dry

Read in `Assets/Scripts/Gameplay/BoardView.cs` and `SnakeGame.cs`:

- **The snake teleports.** `DrawSnake` writes `localPosition` on every tick: each segment jumps from
  one cell to the next every 125 ms, with no intermediate frame. A movement never seen unfolding
  cannot be felt, however correct the rule behind it.
- **Eating shows nothing.** The new segment appears at its final size on the very tick it exists. The
  game's only positive action — the one carrying the whole loop of GDD §2 — has no visual counterpart
  beyond the number changing.
- **Dying shows nothing.** Nothing shows *where* contact happened, while the GDD rests the value of
  the restart on "death is always attributable to a decision". Today that attribution goes through the
  player's memory, never through a signal from the game.
- **The menu, though, is already juicy** (`docs/art/menu.md`): fades, a cascade, a sliding cursor. The
  know-how exists, it stops at the game's door.

## 2. Common technical principle

- **Presentation layer only.** Everything lives in `Gameplay/` or `UI/`, never in `Rules/`: the logical
  position, the tick and the collision stay unchanged to the pixel and to the millisecond.
- **Time**: what follows the game's progress (movement, bite, turn) uses `Time.deltaTime` and must stop
  with the pause. What stays visible during the pause or on death stays on
  `Time.unscaledTimeAsDouble`, like `TimedFeedback`.
- **Reuse the pool.** No `Instantiate`/`Destroy` per tick: the same segments, the same apple and the
  same `Text`s receive a position, a scale or a rotation that varies.
- **Easing utility**: a pure `Rules/Easing.cs` class, modelled on `TimedFeedback` — a
  `Progress(start, duration, now)` returning a curve with a slight overshoot, testable without an
  engine. Avoids the same formula copied into `BoardView` and `GameHud`.
- **Tuning without recompiling**: every duration and amplitude below is a candidate for
  `Assets/StreamingAssets/settings.json` (fields in `Rules/GameSettings.cs`, read by
  `Core/SettingsLoader.cs`), just like the rejection-feedback durations — none has been tried in play,
  they are all "by judgement".
  ⚠ **That tuning only holds on the desktop build**: on WebGL, `SettingsLoader` returns the defaults
  without reading anything (`streamingAssetsPath` is a URL there). Tuning the juice therefore happens
  on Windows, and whatever is frozen there ships as-is to itch.

## 3. Priorities

| # | Feedback | Why it comes first | State |
|---|---|---|---|
| P1 | Movement interpolation (§4) | The base: without it, everything else animates on a game that teleports. | shipped 0.2.0, seen |
| P1 | Death — offending cell + hitstop + micro-zoom (§6) | Directly serves "death is attributable to a decision". | shipped 0.2.0, measured |
| P1 | Bite — gulp + tail pop + score bump (§5) | The only positive action; it is what makes you want to carry on. | shipped 0.2.0, gulp measured |
| P2 | Apple appearance (§7) | Cheap, a clear gain, consistent with P1. | shipped, measured |
| P2 | Best score beaten (§8) | Rare but free; the game's only moment of pride. | shipped, measured |
| P3 | Head tilt on a turn (§9) | Pleasant, not essential — the body's trace is enough to read a turn. | shipped, measured |
| — | Ruled out (§10) | Cost or readability risk greater than the gain. | — |

**Measured** = observed on screen on the running game, the expected number written before the capture
(`docs/TEST_REPORT.md`, session of 2026-08-30). Two reservations are named there: the **pop of the new
tail segment** could not be isolated from the body's pixel count, and the **score bump** was never
sampled near its peak — its mechanism is the one, proven, of the best-score bump.

## 4. The base — movement interpolation

Every segment lerps, on every frame, from its previous cell towards its target cell, over the duration
of the current tick (`Cadence.TickDurationSeconds`, never a copied constant: if the rate is retuned,
the interpolation follows). **Linear, with no easing** — GDD §4.1 sets a constant rate, and an easing
would give the false impression of an acceleration on every cell.

- A segment that has just appeared does not interpolate from a non-existent position: it is placed on
  its cell at scale 0, and it is the pop-in of §5 that makes it grow.
- The interpolation freezes dead on death or on entering the pause — no ghost sliding.
- **WebGL cost**: one `Vector3.Lerp` per segment per frame, at worst ~300, in practice a few dozen.
  Negligible, no shader, no extra draw call.

## 5. Bite (apple eaten)

| Feedback | Duration | Amplitude | Cost |
|---|---|---|---|
| Head: squash perpendicular to the heading ("gulp") | 90 ms, ease-out | scale 1.15 / 0.85 → 1.0 | nil (transform) |
| New tail segment: pops in | 140 ms | scale 0 → 1.12 → 1.0 | nil |
| Banner score: scale bump | 160 ms | 1.0 → 1.18 → 1.0 | nil (UI) |

No new colour and no new sprite: only transforms on already pooled objects.
⚠ The longest duration (160 ms) exceeds one tick (125 ms): two bites in quick succession each start
their own envelope, without the first being cut off silently.

## 6. Death

| Feedback | Duration | Amplitude | Cost |
|---|---|---|---|
| The offending cell (wall hit or segment bitten) flashes | 220 ms, one round trip | opacity 0 → 1 → 0, colour `Pictogram` | nil |
| Hitstop before the scrim and the end text | 70–90 ms | — (a delay) | nil |
| Camera micro-zoom — an impact, not a shake | 150 ms | `orthographicSize` 360 → 354 → 360 (≈ 1.7 %) | nil |

- The white reuses `UiPalette.Pictogram`, already reserved for the signal that must dominate (ART §1.2).
- During the hitstop, **no input is read**, Space included: a player hammering restart just before
  dying must not set off again while the screen still holds the image of the impact.
- **No lateral camera movement** (§10): only the scale breathes once. A shake would move the cells at
  the precise moment the player must see which one killed them.

## 7. Apple appearance

Pop-in on appearance (new game or after a bite): 150 ms, scale 0 → 1.08 → 1.0. P2.

⚠ **The idle breathing is ruled out** (ruling of 2026-08-28): a continuous movement under 8 ticks/s
risked drawing the eye away from the head, and no in-game feedback contradicted it. To reopen only if
`game-tester` finds that the apple takes too long to locate by eye.

## 8. Best score beaten

On the tick `Score.BestBeaten` flips to true: the `Best` number in the banner makes a scale bump
(1.0 → 1.3 → 1.0 over 220 ms), **with no colour change**. The same bump replays once on the "New best"
summary when the end screen opens.

⚠ Do not borrow `Pictogram` (reserved for rejection) nor `Apple` (reserved for food): the rule "one
colour, one role" (`docs/art/palette.md` §1.2) is an achievement, not an obstacle to work around.

## 9. Turn (direction accepted)

The head tilts by ±8° in the direction of the turn, and returns to 0° over the duration of the next
tick (ease-out). Purely visual — it **does not touch** `Board.RejectionAnchor`, which keeps positioning
the chevron relative to the cell, never relative to that rotation. P3: a successful turn is already
readable from the body's trace.

## 10. What is ruled out

- **A trail/afterimage on a turn** — pool cost for a marginal gain: at 8 ticks/s the player has time to
  read a turn without help.
- **Camera shake** — contradicts "readability before style": it moves the cells at the moment their
  exact position matters most.
- **Slow motion (`Time.timeScale`) on death** — would interact with the `unscaledTime` already used by
  the pause and the rejection feedback; the display delay of §6 achieves the same effect without that
  risk.
- **Idle breathing of the apple** (§7) and **a click bump in the menu** — the menu already has its own
  settled animation language (`docs/art/menu.md`).

## 11. Bans

- Never a hard-coded colour, nor a new colour role for an effect: reuse a `UiPalette` role according to
  what it already means (§6, §8).
- Never a particle and never post-processing: everything is done with transforms and `Color.a` on
  components already present.
- Never a lateral camera shake, never a modified `Time.timeScale` (§10).
- Never `Instantiate`/`Destroy` per tick — everything lives on already pooled objects.
- Never an animation that modifies a value *read by `Rules/`* (collision, chevron anchor): presentation
  observes the state of the game, it never feeds it.
- Never feedback that flickers in a loop: one envelope per trigger, like the rejection feedback
  (`docs/art/rejection-feedback.md` §5.5).
- Never block a legitimate input for longer than the announced hitstop (§6): a delay that lengthens
  ends up reading as a game that has stopped responding.

## 12. The author's ruling, and what became of it

**2026-08-28** — the art director's recommendations adopted without modification: ship **P1 first**
with the rounding of `cartoon.md` §3.1, measure, then decide about the rest; **micro-zoom kept** (§6);
**apple breathing ruled out** (§7). Name `Rules/Easing.cs` (§2) validated.
**2026-08-30** — P2 and P3 shipped in turn, on the author's decision to finish the juice before the
assets and the sound. **This brief is closed**: what remains open is named under the table in §3.
