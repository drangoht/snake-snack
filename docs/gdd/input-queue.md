# 4.2 — The input queue

Every accepted directional key is **queued in a depth-2 FIFO**. On each tick, the game dequeues
**one** input, validates it, applies it; with an empty queue it carries the current direction over.

**Validation at the tick, against the direction actually applied on the previous tick** — never at
press time, never against the last key pressed. The counter-example that forces it: snake heading
east, the player presses North then South within the same tick. Neither is a reversal of *east*;
validated on press, both go through and the next tick applies South to a snake that went north — it
bites its own neck. Validated at the tick, South is compared with the North actually applied,
recognised as a reversal, refused.

**Refusal**: the refused input is discarded (it does not block the queue) and the tick carries the
current direction over. The refusal **shows** (§3), otherwise the player reads "the game missed my
key" where the game applied a rule.

**Overflow**: queue full, the new key is **ignored** — the oldest is not overwritten. Overwriting
would silently cancel a turn already pressed: the snake would miss a turn that left the player's
fingers. The ignored press carries the **same** visible feedback as a refused reversal — the barred
chevron of `docs/ART.md` §5. At 125 ms per tick, nothing can teach the nuance between "reversal" and
"one turn too many": what must read is that the press did not count.
<!-- durations still by judgement (250 ms display, 500 ms extension): only the game tester can settle
     them by feel, no bench measures that. -->

**Why 2, neither 1 nor 3** (reasoned, to be confirmed in play). At 1, an S-bend (east then north,
pressed in under one tick) loses its second half: the player who plays *faster* than the rate is
punished. At 3, the snake executes a trajectory decided 375 ms earlier in a grid that has changed, and
death stops being attributable to the last turn read on screen (§2). 2 covers an L-shaped turn made in
one gesture, i.e. 250 ms at 8 ticks/s — **depth and rate are linked**: revisit one if the other moves.

**Purges**: the queue is emptied on entering the pause and on death. Resuming must restore the state
visible on screen, not execute a turn pressed before the pause. A direction pressed during the pause is
not queued (§3). A press identical to the last direction already queued (or to the current direction if
the queue is empty) is not queued either: it would change nothing and would use up a slot.

**A zigzag is not a duplicate** (clarified at implementation, 2026-08-27): current direction east,
queue `[North]`, the player presses East — that is **accepted**. The duplicate test compares only with
the *last* known direction, never with the current direction when the queue is not empty: refusing here
would lose the second half of a genuinely wanted East → North → East S-bend.

Expected rules: `Assets/Scripts/Rules/InputQueue.cs` — pure logic, testable without an engine. The
North/South counter-example above is the first test to write.
