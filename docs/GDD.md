# GDD — Snake Snack

> **How to fill this document in**: the **`/write-the-gdd`** skill — it runs the interview section by
> section, in the order decisions get made, and works through the complete example of a small game.

**The source of truth for design.** Every gameplay decision is recorded here *immediately*, with what
justifies it. The code says *how*; this document says **why**.

⚠ **This file stays a summary: ~150 lines, ceiling.** The detail of a system goes into
`docs/gdd/<system>.md` (§4), the rejected decisions into `docs/gdd/rejected.md` (§7). It is re-read by
every agent before every task: whatever is added here gets paid for on every later task, including the
ones that have nothing to do with it.

> When a conclusion is refuted, **keep it and mark it as such** rather than rewriting it: the
> reasoning that led to the error is worth as much as the correction, and it is what stops the same
> detour being taken twice.

## 1. Pitch

**You steer a snake that grows with every bite, until its own body leaves no way through.**

The player's verb: *steer*. What opposes it: *its own tail* — not an enemy, not randomness. Canonical
Snake, with no twist: the game is already whole, what is at stake is the quality of the feel, not the
addition of mechanics. "Canonical" applies to the *mechanics*, not to the *settings*: the accelerating
rate of Nokia Snake is not inherited (§4.1, and §7 for the reason).

## 2. The game loop

```
appears at the centre, three segments at a standstill  ->  point the head; the snake moves on its
   own, one cell per tick
   ->  apple swallowed: +1 segment, +1 point, a new apple appears elsewhere
   ->  the free space shrinks with every bite
   ->  the head touches the body or a wall: death, score and best score shown right there
   ->  Space: a new game at once, with no menu and no intermediate screen
```

⚠ **"No menu" applies to the restart, not to launching the game**: since 2026-08-28 the game opens on
a main menu (§4.6), and Esc goes back there **from the end screen only**. Space still restarts in one
key with zero waiting — that sentence is the one protecting the loop.

**The edges kill, they do not teleport.** A closed grid reads at a glance from the first second, and
every death stays attributable to a turn — never to a snake that "vanished somewhere". (Teleporting
was considered then ruled out: see §7.)

**What makes you want to restart**: restarting costs one key and zero waiting, and the previous game
left a sentence in your head — "I should have gone right". That sentence is what restarts the game,
not the button. It only exists if death is always attributable to a player's decision: hence the
absence of hostile randomness anywhere in the game.

## 3. Controls

| Action | Keyboard | Gamepad | Touch |
|---|---|---|---|
| Turn (4 directions) | Arrows **or** WASD | — not in 0.1 | Swipe **or** the on-screen pad |
| Pause / resume | Esc | — not in 0.1 | Pause button / tap to resume |
| Restart after death | Space | — not in 0.1 | Tap anywhere |
| Back to the menu | Esc (end screen) **or** Backspace (pause) | — not in 0.1 | Pause button, from pause or end screen |
| Navigate the menu (§4.6) | Arrows or WASD, Enter or Space | — not in 0.1 | tap an entry · mouse: hover and click |

Gamepad stays **decided empty**, not forgotten: every extra device is a path to replay on every
build.

**Touch was decided empty too, and was reopened on 2026-08-30 by the author** (see §7): the game is
published on a page anyone can open from a phone, and what a phone player got was a game that drew
itself perfectly and answered nothing. Both paths are provided, **on the author's ruling** — swipe
anywhere on the playfield, and a visible pad — with the cost stated plainly: two input paths to
replay at every build, which is the very cost that had the feature postponed.

⚠ **The controls take nothing from the playfield.** A 21 × 15 grid at 44 px is 924 px wide inside the
1280 px frame: the pad lives in the 178 px margin that rounding already left over, and the pause
button in the one opposite (`Rules/TouchPad.cs`). Shrinking the grid to make room would have made
every cell smaller for the player whose screen is already the smallest.

⚠ **A tap never starts a game.** §4.1 wants the first tick triggered by a direction, so nobody dies
while reading the screen — a tap-to-start would hand the snake a heading the player never chose. On
the end screen a tap *is* Space: one press, zero waiting (§2).

⚠ **Declaration on the code side**: `Key.W`, `Key.A`, `Key.S`, `Key.D` name POSITIONS on a QWERTY
keyboard — the WASD block. On a French AZERTY keyboard the same positions are the keys printed Z, Q,
S, D. Writing `Key.Z` for the key printed Z actually targets W — no error is raised, the game simply
answers the wrong key.

⚠ **Two inputs are refused, and the refusal must show**:
- the **instant reversal** (the snake would bite its own neck);
- any direction pressed during the pause.

Invisible reads as non-existent: a press ignored with no on-screen feedback is read as a press the
game *missed*. **The form of the feedback is settled → `docs/art/rejection-feedback.md`** (author's
ruling, 2026-08-27): a barred chevron, pointing towards the refused direction, anchored to the edge of
the head cell; a line of text on the pause screen for a direction pressed while paused. The refusal is
a **state with a deadline**, never a replayed animation: hammering the key extends the display without
making it flicker.

⚠ **A duplicate gets no feedback** — repeating the direction already being followed is not an error,
and the snake carrying straight on is already the confirmation. The filtering is **explicit** in the
code, so it does not read as an oversight to fix.

⚠ **A capability must announce its key inside the game** (HUD, description, acquisition screen).

## 4. Systems

<!-- ⚠ THIS SECTION IS AN INDEX. One system = one docs/gdd/<system>.md file, one line here.
     What is written in a system file is only re-read by whoever touches THAT system. -->

| System | File | In one sentence |
|---|---|---|
| **4.1** The time step | [`gdd/time-step.md`](gdd/time-step.md) | The snake moves one cell per tick, never between two — the unit of every later measurement. |
| **4.2** The input queue | [`gdd/input-queue.md`](gdd/input-queue.md) | A depth-2 FIFO: one input dequeued, validated and applied per tick. |
| **4.3** The grid | [`gdd/grid.md`](gdd/grid.md) | 21 × 15 square cells, odd on both axes so an exact centre cell exists. |
| **4.4** The apple | [`gdd/apple.md`](gdd/apple.md) | A single apple at every instant, placed before the first press so the start has a target. |
| **4.5** Score and best score | [`gdd/score-best.md`](gdd/score-best.md) | +1 per apple, nothing else; the best score survives closing and is beaten strictly. |
| **4.6** The main menu | [`gdd/menu.md`](gdd/menu.md) | The game opens on a menu, but nothing comes between a death and the next game. |

## 5. Progression and difficulty

<!--
Acquired rules, to be respected unless there is a new reason:
- A step of difficulty adds a NAMED RULE, not a multiplier. The player must be able to read it before
  starting and understand why they lost.
- Before adding a constraint, check what it GIVES the player: a constraint that also hands out its own
  antidote hardens nothing.
- An optional lever is not a rule: a rule applies to every game.
- Never a wall of patience on a key confrontation: more dangerous beats longer.
-->

## 6. What has been measured

<!--
Point at docs/TEST_REPORT.md, and record the CONCLUSION here, not the raw data.

⚠ A single game settles nothing: the variance between two games can reach a factor of 2.4 before the
setting under test even acts. A balancing verdict is taken on a paired bench, with the sign test.
-->

## 7. What was ruled out, and why

<!-- INDEX. The full reasoning for each decision is in docs/gdd/rejected.md: it is only opened to
     reopen a debate, not on every task. One line here per settled subject. -->

Detail and reasons: [`gdd/rejected.md`](gdd/rejected.md). Subjects already settled — **do not reopen
them without something new**:

- Teleporting edges (the snake comes back out of the opposite side)
- Snacks with distinct effects, temporary bonuses
- A rate that speeds up with length (Nokia Snake)
- A depth-1 input queue (a single direction remembered)
- A 32 × 18 grid filling the 16:9 frame with no margins
- Rejection feedback: rejected variants
- Drawing the apple by rejection ("draw a random cell, redraw while it is occupied")
- Constraining where the apple appears (minimum distance from the head, ban on the cell straight ahead)
- Several apples at once
- An apple with a limited lifetime (it disappears and reappears elsewhere)
- `UnityEngine.Random` or `System.Random` for the apple draw
- Weighted score (speed bonus, points tied to time or length)
- Menu: a navigable end screen, a "Settings" entry, the best score shown on the menu (detail in [`gdd/menu.md`](gdd/menu.md))
- Gamepad — and touch, until the author reopened it on 2026-08-30 (shipped: swipe + on-screen pad)
