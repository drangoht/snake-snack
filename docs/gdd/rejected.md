# What was ruled out, and why

The most useful list in the design: it keeps the same debate from being reopened ten times. Taken out
of `docs/GDD.md` (§7) because it is only consulted to reopen a decision — the summary keeps the list
of settled subjects there, enough to know whether this file needs opening.

⚠ A refuted conclusion is **kept and marked** as such rather than rewritten: the reasoning that led to
the error keeps the same detour from being taken twice.

<!-- The most useful list in the document: it keeps the same debate from being reopened ten times. -->

> **Teleporting edges (the snake comes back out of the opposite side).** Ruled out for 0.1, decided at
> design time, **not yet contradicted by a game**: a closed grid reads entirely at a glance, whereas a
> wrapping edge asks the player to simulate an invisible continuity in their head. Above all, it makes
> certain deaths unattributable ("where did it come back out?"), which the pillar of §2 forbids. To
> reopen if the first games show early mortality against the walls.

> **Snacks with distinct effects, temporary bonuses.** Ruled out at the pitch (§1). They shift the
> decision from "which way to go" to "reach the right object", and death stops being attributable to a
> turn. The game adopted is canonical Snake: what is at stake is the feel, not the addition of
> mechanics.

> **A rate that speeds up with length (Nokia Snake).** Ruled out for 0.1, **decided at design time,
> with no game played**: it is a multiplier, not a named rule — the player cannot read it before
> starting. It stacks on a difficulty that already rises on its own (§4.1), it blurs the attribution of
> death (§2: badly planned, or outrun by the rate?), and it makes the tick — the unit of measurement —
> variable, so two games incomparable on a bench. To reopen **once the paired bench is available**, not
> before: it is precisely the kind of setting a single game does not settle.

> **A depth-1 input queue (a single direction remembered).** Ruled out at design time. It loses the
> second half of any S-bend pressed in under one tick, meaning it punishes the player who plays
> *faster* than the rate, and the loss is invisible (§3). It is the usual origin of "this Snake misses
> my turns". See §4.2.

> **A 32 × 18 grid filling the 16:9 frame with no margins.** Ruled out at design time: even
> dimensions, so no exact centre cell (§4.3); 576 cells instead of 315, i.e. a typical game twice as
> long for the same repeated decision; and no margin left to put the score without overlaying the
> playfield. To reopen if the first games turn out too short or too cramped.

> **Rejection feedback: rejected variants.** The thickened cell outline (does not say *which* direction
> was refused) and a single feedback including the duplicate (noise on every tick of a player going
> straight) — detail and reasons in `docs/art/history.md`, which keeps the record of visual decisions
> as this section keeps the design ones.

> **Drawing the apple by rejection ("draw a random cell, redraw while it is occupied").** Ruled out at
> design time, **no game played**. The cost of the draw grows with the fill level and has **no bound**:
> on a nearly full grid the game freezes, with no exception and no log — a defect that only appears at
> the very end of a long game, so never during testing. Enumeration (§4.4) costs at most 315 cells,
> always. To reopen **only** if WebGL profiling shows the enumeration weighs, and then as a **bounded**
> hybrid (N rejections then fall back to enumeration), never as plain rejection.
> <!-- to measure: real cost of the walk, on WebGL -->

> **Constraining where the apple appears (minimum distance from the head, ban on the cell straight
> ahead).** Ruled out at design time. An apple can neither block nor kill, and eating is never
> compulsory (§4.4): no position makes a death unattributable, so the constraint would protect nothing.
> It would only remove *favourable* draws and change the number of eligible cells, making every bench
> heavier to describe. To reopen if the `game-tester` reports that apples offered one or two cells away
> devalue the score — that is feel, no bench settles it.

> **Several apples at once.** Ruled out at design time. A constraint is judged by what it *gives*: two
> apples do not only shorten the route, they offer a **backup target** when the first becomes
> unreachable — the player gains more than they lose. They also dilute the "which way to go" decision,
> which is the verb of §1. To reopen if the `TEST_REPORT` shows the route between two apples feels like
> dead time.

> **An apple with a limited lifetime (it disappears and reappears elsewhere).** Ruled out at design
> time. It is hostile randomness: the player commits to a corridor for a target that evaporates, and
> the death that follows is no longer attributable to their turn but to a timer they do not control
> (§2). It is also a disguised wall of patience. Not reopened for 0.1.

> **`UnityEngine.Random` or `System.Random` for the apple draw.** Ruled out at design time. The first
> is shared global state, unavailable in `Rules/`. The second **does not guarantee** the same sequence
> across runtimes: a paired bench whose apples differ between `dotnet test`, the desktop build and the
> WebGL build no longer compares anything, and the discrepancy would be attributed to the setting under
> test. To reopen if .NET publishes a sequence stability contract — not before.

> **Weighted score (speed bonus, points tied to time or length).** Ruled out at design time. It adds a
> time pressure nothing displays, and shifts the explanation of a defeat from "I should have gone
> right" (§2) to "I was too slow". Length already equals `3 + score`: it would be the same number shown
> twice. To reopen if the raw score turns out to give no reason to restart once the best score is set.

> **Gamepad and touch.** *Postponed, not ruled out* — see §3. Every device is one more path to replay
> on every build, for a web game played with a keyboard. To reopen on feedback from mobile players.
>
> ⚠ **Touch: reopened and shipped on 2026-08-30, on the author's ruling.** The conclusion above is
> kept, because its reasoning still holds — the second input path *is* a recurring cost, and it is now
> being paid. What the reasoning got wrong is the trigger it waited for: "feedback from mobile
> players" cannot arrive from a game that answers nothing on a phone. The page has been public since
> it was created; a visitor opening it from a phone saw a game draw itself perfectly and ignore every
> touch. The condition was unreachable by construction.
>
> Both paths were shipped rather than one — swipe **and** an on-screen pad — again on the author's
> ruling, against the cheaper single-path option. **Gamepad stays postponed**, on the original
> reasoning, which nothing has refuted.

⚠ When one of these conclusions is refuted by a real game, **keep it and mark it as such** rather than
rewriting it.
