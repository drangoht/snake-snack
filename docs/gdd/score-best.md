# 4.5 — Score and best score

**The score counts the apples eaten in the current game, +1 per apple, nothing else**: no time, no
speed bonus, no length. Length equals `3 + score` — displaying it as well would only add a second
number to read for the same information. A weighted score is ruled out (§7): it would introduce an
invisible time pressure and a death attributable to "too slow" rather than to a turn (§2).

**Score and best score shown at all times**, outside the playfield (the band and margins of §4.3), not
only on death. A goal you only discover once you have lost cannot be aimed at: it is the best score
read during the game that turns the restart into "beat 14". The exact placement and typography are a
brief to open in `docs/ART.md`. <!-- to ask the art director -->

**The best score is the highest score ever reached, and it rises during the game**, as soon as the
current score passes it — not on death. The score is monotonically increasing: waiting for the end
would display a best score lower than the current score, which reads as a bug, and would lose the best
score of a tab closed mid-game.

⚠ **When the best score has just been beaten, score and best are the same number** — two equal values
side by side read as a display defect. The end screen must say so explicitly (a "new best" line),
otherwise the game's only rewarding moment looks like a bug. Brief to open in `docs/ART.md`.
<!-- to ask the art director -->

**Persistence**: the best score survives closing the game, stored on the engine side under a named key
(`PlayerPrefs`, adapter in `Gameplay/` — `Rules/` stays free of engine dependencies). ⚠ On WebGL,
storage is tied to the site origin and **can disappear** (private browsing, browser purge): it is best
effort. An unreadable or missing best score restarts from zero **with no blocking error** — the game
must never refuse to start over a counter.

**Matching your best score does not beat it** (clarified at implementation, 2026-08-28). The predicate
compares the score with the best score **from before the game**, never with the current one — that has
just been raised by the score itself, so comparing them would always be false. Accepted consequence:
the player who exactly repeats their best score does see two identical numbers *without* a "new best"
line. That is not the display defect the line exists to lift: they beat nothing.

**The best score is written on the tick it rises**, not on death (clarified at implementation,
2026-08-28). It is the direct consequence of "it rises during the game": a tab closed mid-game must
keep the best score reached. The counting itself signals the ticks where the best score changes, which
avoids writing to storage on every apple of every game.

**Placement adopted, provisional** (2026-08-28): score **on the left** of the top band in main text,
best score **on the right** in secondary text, the state staying in the centre. The hierarchy reads
without reading the labels: it is the current score the player follows. On death, a **summary** is
added between the title and "Space to play again" — both numbers are already in the band, but sending
the eye all the way up at the moment of deciding to play again is losing the player. When the best
score has just been beaten, that summary shows **a single number** ("New best: 12") rather than the
same one twice under two labels. ⚠ This placement is a development decision for lack of a brief: §1 of
`docs/ART.md` (palette, type) is still empty, and everything is in grey.
<!-- to ask the art director: the brief for these two numbers is still open. -->

Rules **written** (2026-08-28): `Assets/Scripts/Rules/Score.cs` — counting, the rise of the best
score, the "best beaten" predicate, normalising a damaged best score, and the equality
`length == 3 + score` that `tests/ScoreTests.cs` checks on the real snake rather than in a comment.
Persistent reading and writing live in `Assets/Scripts/Gameplay/PersistentBest.cs` (best effort, never
blocking), the display in `Assets/Scripts/UI/GameHud.cs`.
