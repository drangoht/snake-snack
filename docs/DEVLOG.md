# Devlog — Snake Snack

The source of truth for release notes: what has **actually shipped**, newest first. Every entry is
written here before being pasted on itch — nothing is published that is not here.

Write **for the player**, not for the git history: "the blobs no longer stick to the net" and not
"fix(BlobController): clamp horizontal velocity".

## v0.3.0 — The game speaks English (2026-08-30)

**News**
- **The whole game is now in English** — menu, controls, pause and end screens. Nothing about how it
  plays has changed.
- **The keys are announced more accurately than before**: the game reads the physical WASD block, so
  it now says "Arrows or WASD". On a French AZERTY keyboard those are the same four keys, printed
  Z, Q, S, D — and the arrows work everywhere.
- **The snake has a face.** Two eyes on the head, looking where it is going. They squash with it when
  it swallows and lean with it in a turn.
- **The apple pops in** instead of simply appearing, the head **leans into its turns**, and beating
  your best score makes the number jump on the end screen.

**Balance**
- Nothing. Speed, grid, score and apple draw are untouched: a game plays exactly as in 0.2.0. Your
  best score is kept.

**Fixes**
- Nothing new; nothing was broken.

**Still not here**: sound and music.

## v0.2.0 — The snake glides, and it has lost its right angles (2026-08-29)

**News**
- **The snake glides from cell to cell** instead of jumping. The rules have not moved a millimetre —
  it is still one cell per tick, at the same rhythm — but you can finally see it move.
- **No more graph paper**: the body, the head and the apple have rounded corners. The snake in the
  menu and the one in the game are finally the same animal.
- **Swallowing feels like something**: the head swells on the bite, the new ring appears at the tail,
  and the score jumps when it goes up.
- **You finally see what killed you**: the offending cell lights up, the game holds for a very short
  beat, and the end screen only arrives afterwards — time enough to understand.

**Balance**
- Nothing. Neither the speed, nor the grid, nor the score, nor the apple draw changed: a game plays
  exactly as in 0.1.0.

**Fixes**
- Hammering Space just before dying no longer skips the end screen. The game used to restart
  immediately, without you seeing either your score or what had stopped you.

**Still not here**: sound and music.

## v0.1.0 — First playable version (2026-08-28)

**News**
- The full game of Snake: a snake that grows with every apple, walls that kill, a score and a best
  score that survives closing the game.
- A main menu — play, how to play, credits — with keyboard as well as mouse.
- A pause, and a refused reversal that **shows** instead of being swallowed in silence.
- Playable straight in the browser.

<!-- Template for an entry, to copy at the TOP of the file:

## vX.Y.Z — <a few words of summary> (YYYY-MM-DD)

**News**
- ...

**Balance**
- ...

**Fixes**
- ...

-->
