# Brief — 5. Feedback for a refused input (GDD §3, §4.2)

Taken out of `docs/ART.md`: a detailed brief only concerns whoever works on THAT subject, whereas
`ART.md` is re-read before every asset. The permanent rules (palette, type, scale, contrast) stay
there.

### 5.1 The problem

Four rejection reasons exist in `Assets/Scripts/Rules/InputQueue.cs` (`EnqueueResult`):

| Reason | What it means | Expected frequency in play |
|---|---|---|
| `ReversalRejected` | The player asked for the opposite of their current direction. | Rare once the rule is learned — but possible in a panic, in a tight turn. |
| `RejectedGamePaused` | Direction pressed during the pause. | Rare, and with no time pressure: the game is frozen. |
| `RejectedQueueFull` | Two turns already waiting, the third is ignored. | Occasional, in a burst of hammering — the noise case flagged by the game designer. |
| `RejectedDuplicate` | The requested direction is already the one about to apply. | **The most frequent of the four** — the player re-presses the heading they are already following out of reflex. ⚠ The exact frequency depends on the wiring: read as `wasPressedThisFrame`, holding a key produces **only one** event, not one per frame. <!-- to observe once wired --> |

The GDD (§3) explicitly requires visible feedback for `ReversalRejected` and `RejectedGamePaused`. It
leaves `RejectedQueueFull` "to be confirmed by feel" (§4.2) and says nothing about
`RejectedDuplicate` — that is the gap this brief fills.

### 5.2 Decision: one feedback, or distinct ones?

**Distinct, in two channels — not one uniform feedback, not four independent ones.**

The deciding criterion is not the severity of the rule, it is the **risk of noise** flagged by the
game designer: feedback firing on every refused press, under hammering, stops being read as a rule and
becomes a visual defect. Two reasons are at risk of real hammering (`ReversalRejected` and
`RejectedQueueFull` happen while the game ticks, under pressure); a third (`RejectedGamePaused`)
happens outside any time pressure, the simulation being frozen; the last (`RejectedDuplicate`) is not
an event to report at all — see §5.3.

**Treatment adopted:**

| Reason | Gets feedback? | Channel |
|---|---|---|
| `ReversalRejected` | Yes | Pictogram anchored to the head (§5.4) |
| `RejectedQueueFull` | Yes, the same pictogram | Pictogram anchored to the head (§5.4) |
| `RejectedGamePaused` | Yes | Text on the already visible pause screen (§5.4) |
| `RejectedDuplicate` | **No** | — (§5.3) |

`ReversalRejected` and `RejectedQueueFull` share the **same** pictogram rather than two distinct
drawings: at 125 ms per tick, the time available does not allow teaching a nuance between "you
reversed" and "you pressed one turn too many" — what matters at that instant is that the player sees
that **their press did not count**, not exactly why. Reusing a single sign also reduces the number of
assets and makes it recognisable sooner: the player has only one shape to learn for the whole "that
did not take" class.

### 5.3 The `RejectedDuplicate` case — no feedback, and why

`RejectedDuplicate` fires when the requested direction is already the last known one (current, or last
in the queue). It is the **most common** state of a game: a player going straight and re-pressing
their direction out of reflex, or holding it, gets that reason on almost every tick where they do not
change heading. Three reasons to show nothing:

1. **It is not an error.** Unlike a reversal, the pause or a queue overflow, nothing was "missed" —
   the player's intent (keep going in that direction) is already satisfied by what is about to run.
   Reporting a rejection here would lie about the nature of the event.
2. **The confirmation already exists**: the snake carries on exactly where the player sent it. That is
   the feedback, and it is free — laying a pictogram over it adds no information.
3. **It is the highest noise risk of the four.** A frequent signal fades on its own in the player's
   reading (the ignored-alarm effect) and, worse, desensitises them to the **same** pictogram used for
   reversal — the one case where that sign must stay tied to "you made a mistake". ⚠ This argument
   rests on an **assumed** frequency, not an observed one (see §5.1); point 1 settles it alone, and it
   holds whatever the real frequency.

Decision: `RejectedDuplicate` is filtered **before** reaching the visual feedback layer, explicitly
(see the API in §5.5) — not by silent omission, so a future reader does not read it as an oversight to
fix.

### 5.4 Proposed variants

Three variants, the third ruled out immediately for the reason just set out in §5.3; it stays written
for the record.

---

**Variant A (recommended) — A barred directional pictogram anchored to the head + text on the pause
screen**

- *Pictogram* (`ReversalRejected`, `RejectedQueueFull`): a solid chevron, pointing towards the
  requested direction, barred by a diagonal stroke ("no entry" grammar). Anchored to the edge of the
  head cell, on the side of the refused direction, offset by about a quarter of a cell (~11 px) so it
  never covers the cell itself. Maximum size: half a cell (22 px), so it never spills onto the
  neighbouring cell and reads as an obstacle.
- *Pause text* (`RejectedGamePaused`): a line added to the already visible pause screen (not a new
  screen), of the "Key ignored - the game is paused" kind. No arrow, no directional symbol: ASCII
  characters only, with no risky font dependency.
- Contrast carried by the **shape** (diagonal bar + chevron), never by colour alone — applicable today
  in greyscale, without waiting for the palette.

**Variant B — The head cell's outline changing thickness**

Instead of a separate pictogram, the head cell itself gains a thicker or hatched outline during the
rejection. Advantage: no directional sprite to draw, a single asset (an outline) serves all three
active reasons. Drawback: it does not show *which* direction was refused — a player chaining attempts
does not know which one failed, only that one did. Less informative than A for a barely lower
production cost.

**Variant C (ruled out) — A single pictogram for all four reasons, `RejectedDuplicate` included**

Technically the simplest (no filter to write), but directly contradicted by §5.3: it would light the
signal on almost every tick of a player going straight, which is precisely the noise the game designer
flagged as the real trap of this task. Ruled out without reservation.

---

**Variant A adopted** (settled by the author on 2026-08-27). It separates what must be learned fast
(the pictogram, read with the eye still on the grid) from what can be read at leisure (the pause text,
with no tick constraint), and it respects the shape-before-colour constraint from its description
onwards, without waiting for the palette to exist.

### 5.5 Specification of the adopted variant

**Anti-repeat (the part that handles hammering)** — common to both channels:

The feedback is not an event replaying an animation on every notification; it is a **state** with an
expiry deadline:
- a notification shows the feedback and sets its deadline to *now + display duration*;
- a notification received while the feedback is already visible **extends** the deadline by the same
  amount, **without restarting the appearance animation** — no re-flash, no flicker under hammering;
- a continuous-extension cap keeps the feedback from becoming a fixed part of the scenery: past that
  cap it goes out once, even if it relights when the hammering continues. A signal that is always
  visible stops being read as a signal.

**Durations — by judgement, none tried in play, to be confirmed by the game tester:**

| Parameter | Proposed value | Relation to the tick (125 ms) |
|---|---|---|
| Pictogram display duration (per trigger) | 250 ms | 2 ticks |
| Continuous-extension cap (pictogram) | 500 ms | 4 ticks |
| Pause text display duration | 1.5 s after the last refused press | not tied to the tick — the simulation is frozen while paused, the 125 ms constraint does not apply |

⚠ **The rejection reason does not have a single source** — that is the trap of this API, and it
follows directly from GDD §4.2: a reversal cannot be judged on press, only at the tick, against the
direction actually applied. It therefore does **not** belong to `EnqueueResult` (which only knows
`RejectedDuplicate`, `RejectedQueueFull`, `RejectedGamePaused`) but to
`TickResult.ReversalRejected`. A UI listening only to `Enqueue()` would **never** show the reversal
rejection — the very case §3 requires to be made visible.

```csharp
// Two call sites, two moments:
//  - after InputQueue.Enqueue(), for any result other than Accepted;
//  - after InputQueue.Tick(), when TickResult.ReversalRejected is true.
public interface IRejectedInputFeedback
{
    void Notify(RejectionReason reason, Direction rejectedDirection);
}
```

`RejectionReason` is an enum **belonging to the feedback layer**, fed from both sources; its exact
shape is left to the developer, as long as it separates the three treatments below. Do not add reversal
to `EnqueueResult` to unify things: that would declare a reversal can be refused on enqueue, exactly
the mistake the North/South counter-example of §4.2 forbids.

- The implementation filters `RejectedDuplicate` first of all — no rendering, immediate return (§5.3).
- The **refused reversal** (source: the tick) and `RejectedQueueFull` (source: the enqueue) route to
  the component driving the pictogram anchored to the head (position = current head cell + refused
  direction), with the deadline logic above.
- `RejectedGamePaused` routes to the component driving the line of text on the pause screen.
- `InputQueue` stays a pure class with no engine dependency (GDD §4.2): it is up to the gameplay
  `MonoBehaviour` to call `Notify`, never up to `InputQueue` to know about the UI.

### 5.6 What the game contradicted (observed 2026-08-27)

> ⚠ **The chevron of a reversal ALWAYS lands on the snake's body.** This is not an edge case: a
> reversal aims, by definition, at the cell the snake came from — therefore the cell occupied by its
> second segment. The pictogram, anchored "to the edge of the head cell, on the side of the refused
> direction" (§5.4), therefore systematically overlays the body. In greyscale, light grey on mid grey,
> it becomes hard to read at the precise moment it must be.
>
> The brief was written without that case being visualised; the screenshot
> `docs/check-reversal-refusal.png` shows it. The pictogram **works** (position, orientation,
> deadline) — it is its contrast that fails. Three leads, unsettled: a dark outline around the
> pictogram, an offset perpendicular to the refused direction, or a reserved alert colour that the
> palette (§1) would have to provide. **To be ruled on by the art director**, and it will be easier
> once the palette is set.

> ✅ **Settled on 2026-08-28 by the palette (ART §1), without touching the shapes.** The third lead
> wins, and it costs nothing: `Pictogram` becomes pure white `#FFFFFF`, a value **reserved for that
> role alone**, and `SnakeBody` the mid green `#4E9358`. The contrast of the real case — chevron on
> the body — goes from **3.04:1 to 3.72:1**, above the WCAG 1.4.11 threshold for graphical objects
> (3:1). Neither a dark outline nor an offset: both would have changed a shape and a position already
> validated in play, for a problem that was only about colour.
> ✅ **Confirmed on a Windows build on 2026-08-28, this point is CLOSED**
> (`docs/TEST_REPORT.md`). Chevron measured at 617.5 px for a head centre at 650 px and a 44 px cell —
> it does sit on the first body segment, as announced. Solid white, no attenuation, ratio **measured
> at 3.81:1** on the real pixels (the green comes out one unit darker in a `SpriteRenderer`, which
> helps). **The dark outline is not necessary** and was not applied.
>
> ⚠ **What remains open is no longer the colour, it is the SCALE.** At half a cell
> (`Board.MaximumPictogramSize`), the barred chevron occupies **12 × 24 px** on screen: it reads as a
> white blob appearing on the snake, not as a barred chevron — the "something was refused" gets
> through, the "it is *that* direction" does not. And it only lives 250 ms. A question of shape and
> scale, to be ruled on separately; nothing has been touched.

> **The barring stroke is perpendicular to the chevron's axis, not diagonal.** A deliberate deviation
> from §5.4, decided at implementation: at 45°, the stroke falls exactly parallel to one of the
> chevron's two arms and reads as a third arm. The original "no entry" grammar is kept, only the angle
> changes.

### 5.7 What remains to be confirmed separately

- **Palette**: this brief fixes no hex code. The pictogram and the pause text must reference
  `UiPalette` (§1) once it is set; until then, build in greyscale / silhouettes.
- **Font** of the pause text: no non-ASCII glyph constraint is needed here (no arrow in the adopted
  message), so no risk of WebGL fallback — to be checked anyway once the font is chosen (§2).
- **Screen anchoring of the head cell**: this brief assumes the existence of a grid → screen conversion
  already used to draw the snake; the pictogram's component must reuse it, not recompute a new one.
- All the durations in §5.5 are starting points, not measured values.

### 5.8 Bans

- Never a Unicode arrow character (`← → ↑ ↓`) in a `Text` component — a guaranteed silent loss on
  WebGL (`docs/pitfalls/fonts-text.md`). Every directional symbol is a **sprite**.
- Never feedback for `RejectedDuplicate` (§5.3).
- Never information carried by colour alone.
- Never looping flicker (a strobe) — a single fade-in/fade-out envelope per trigger.
- Never feedback that exceeds its continuous-extension cap without going out at least once.
- Never a hard-coded colour in the pictogram's or the text's code: reference `UiPalette` as soon as it
  exists.
- Never built by clicking in the editor — the scene is an artefact regenerated by
  `SceneBuilder.Build()`; everything above is built in code.
