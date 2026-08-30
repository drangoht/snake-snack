# 4.1 — The time step

The snake moves **one cell per tick**, never between two ticks: its position is always on the grid,
and the tick is the unit of everything measured later.

**Rate: 8 ticks/s, i.e. 125 ms per tick** (by judgement, to be confirmed in play; range to try 6 to
10 ticks/s <!-- to measure -->). The input window for a turn is exactly one tick. At 125 ms it is
shorter than a simple visual reaction time (200–250 ms, an accepted order of magnitude, **not measured
here**): you therefore cannot *react* to an incoming wall, you have to have decided one cell ahead.
That is the skill being aimed at — the queue (§4.2) keeps such a short window from losing presses.

**Constant rate for the whole game** (ruled by the author on 2026-08-27, against Nokia canon).
Difficulty already rises on its own: every apple lengthens the body and shrinks the free space — that
is the rule of §1, readable before you start. Speeding up on top would be a multiplier stacked over
it, and would blur the attribution of death (§2): the player would no longer know whether they planned
badly or whether the game outran their fingers. (Rejected alternative: §7.)

**Standing start** (§2): the first tick is triggered by the first **applicable** direction, not by the
scene loading, and not by any press at all. Snake facing east, body behind it: a player pressing West
sees the refusal (§3) and **nothing moves**; the game starts on the first press that is not a reversal.
The reversal rule thus teaches itself, before any danger exists, and nobody dies while the player is
reading the screen.
<!-- Author's ruling, 2026-08-27: lifts a contradiction between "accepted direction" (§4.1, the
     enqueue, which does not judge reversal) and "the refusal shows before the start" (§4.3). The
     variant "the refused press starts the game anyway" is ruled out: the game would start on its own
     from a key it has just refused. This special case lives in the engine wiring, not in InputQueue. -->

**A rate backlog is not caught up** (author's ruling, 2026-08-27). Losing window focus pauses the
game; outside that case, one frame moves the snake by **one tick at most**, and the accumulated
backlog is discarded. Without that cap, a one-second freeze (alt-tab, loading) covers eight cells at
once, **invisibly**: the death that follows is attributable to no turn, which §2 forbids. The accepted
price is a brief drift of the rate after a hitch — preferable to cells crossed out of the player's
sight.

Expected rules: `Assets/Scripts/Rules/Cadence.cs`. Tick duration is settable **without recompiling**
— it is the game value that will be retried most often.
