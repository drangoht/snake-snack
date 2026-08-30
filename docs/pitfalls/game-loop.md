# Pitfalls — Game loop and time step


**⚠ A catch-up cap that CARRIES the backlog caps nothing: it spreads it.** The game advances by ticks
accumulated in a residual time (`Cadence.TickCount`). Capping the number of ticks played per frame
without discarding the backlog gives code that *looks* correct — the cap is there, it is respected —
and a game where the eight cells of a one-second freeze go through over eight successive frames
instead of one. The symptom is not "the snake jumps", it is "the snake fast-forwards for a second
after a hitch", with no message and no error. The leftover returned must be **the sub-tick fraction
alone** (< one tick by construction): it keeps the phase without catching anything up. GDD §4.1,
ruling of 2026-08-27.

**⚠ Engine-side corollary**: this cap assumes that **losing focus pauses the game**
(`Application.focusChanged`). Without that pause, the cap costs the player all the time spent outside
the window — it discards the backlog, it does not give it back. The pure rule cannot take care of it:
it is a wiring dependency, noted in the remarks of `Cadence`.

**⚠ A guard-rail test never seen RED proves nothing.** The one locking the cap above
(`TheDiscardedBacklogDoesNotComeBackOnLaterFrames`) passes just as well on an implementation that
carries the backlog, if one only checks the first call: it is by replaying **ten frames after the
freeze** that it catches the defect. The check cost a minute — inject the regression, watch it fail,
remove it.
