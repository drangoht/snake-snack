# Devlog — Snake Snack

The source of truth for release notes: what has **actually shipped**, newest first. Every entry is
written here before being pasted on itch — nothing is published that is not here.

Write **for the player**, not for the git history: "the blobs no longer stick to the net" and not
"fix(BlobController): clamp horizontal velocity".

## v0.5.0 — The game makes a sound (2026-08-31)

*Published on itch: <https://drangoht.itch.io/snake-snack/devlog/1647477/v050-the-game-makes-a-sound>*

**News**
- **The game has sound.** The snake bites, the snake dies, the menu answers under your finger or your
  key. Four sounds, chosen soft and round rather than beepy — this is not a Nokia game.
- **Music on the menu**, a calm loop. **The game itself stays silent**, and that is on purpose: a run
  lasts a minute or two and restarts at once, so a loop would go round and round behind it — and the
  bite would stop standing out.
- **A mute button**, top right of the menu, and the **M key** anywhere. It silences everything, and it
  remembers your choice for next time. It is right there because a game you cannot silence in one
  gesture is a game you close instead.

**Balance**
- Nothing. Same speed, same grid, same score, same apple draw — a game plays exactly as in 0.4.0, and
  your best score is kept.

**Fixes**
- Nothing was broken for you. Behind the scenes the game had no ear at all: the sound would have gone
  nowhere even once the files were in. It does now, and it is measured rather than assumed.

⚠ **Honest note**: the sounds and the music were picked on measurements — length, attack, brightness,
and whether a loop repeats without a hole — because they had to be picked before anyone had heard them
in place. If one of them grates after ten minutes, that is exactly the kind of thing worth telling me.

**Still not here**: the graphics are still shapes drawn by code, not hand-made art.

## v0.4.0 — It plays on a phone (2026-08-30)

*Published on itch **a day late**, on 2026-08-31:
<https://drangoht.itch.io/snake-snack/devlog/1647488/v040-it-plays-on-a-phone-a-day-late>. The post
says so in its first line rather than pretending otherwise, and its "still not here: sound and music"
was rewritten to point at 0.5.0 — publishing today a text claiming the game has no sound would have
been exactly the defect this page exists to prevent.*

**News**
- **The game is playable with a finger.** Hold the device in landscape: **swipe anywhere** on the
  playfield to turn, or use the **directional pad** in the right-hand margin. Both work, all the time —
  take whichever you prefer.
- **A pause button**, top left. Tap anywhere to resume; press the button again from the pause to go
  back to the menu.
- **Tap to play again** on the end screen. One press, zero waiting, exactly as the space bar.
- **The instructions no longer name keys you do not have.** On a phone the game says "swipe or use the
  pad", not "press Esc".
- **The controls cost the playfield nothing.** They sit in the margin the grid already left empty: not
  one cell had to shrink to make room.

**Balance**
- Nothing. Same speed, same grid, same score, same apple draw — a game plays exactly as in 0.3.0, and
  your best score is kept.

**Fixes**
- Nothing new; nothing was broken.

**Still not here**: sound and music.

⚠ **Honest note**: the touch controls have been tested with a simulated finger, not yet on a real
phone. If something feels wrong on yours — a key too small, a swipe that does not take — that is worth
telling me, and it is exactly the feedback this was waiting for.

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
