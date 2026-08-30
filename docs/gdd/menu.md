# 4.6 — The main menu

**The game opens on a menu, and the menu never comes between a death and the next game.** Author's
ruling, 2026-08-28.

## What the menu must do, and what it must not undo

§2 says "Space: a new game at once, **with no menu and no intermediate screen**". That sentence is
about the **restart after death**, not about the game's first screen: it stays true. What would have
protected it badly, and was **ruled out**: an end screen turned into a small "Play again / Menu" menu.
It would have put a choice — hence a hesitation — exactly where §2 wants zero waiting.

| Situation | Key | What happens |
|---|---|---|
| Game launch | — | the menu, animated, selection on "Play" |
| Menu | Arrows or WASD (up/down) | moves the selection, with **wrap-around** |
| Menu | Enter or Space | confirms |
| Menu, panel open | Esc, Enter, Space, or a click | closes the panel |
| Death or win screen | Space | **immediate game**, unchanged |
| Death or win screen | Esc | back to the menu |
| Game running | Esc | pause, unchanged |
| Pause screen | Backspace | back to the menu, the game is abandoned |

**Going back to the menu from a game goes through the pause**, on **Backspace** (author's ruling,
2026-08-28). The pause screen is already a screen of stopping: abandoning the game there is a
decision, not a reflex, and nothing is put in the way of the running loop.

⚠ **Why not another key.** Esc is the pause toggle: giving it a second meaning (long press, double
press) would make every game pay for the convenience of a rare round trip. The "M" of Menu is declared
`Key.Semicolon` on a French keyboard — the trap §3 bans. And Tab is the priming key of
`tools/drive_game.py`, which requires a key the game ignores.

⚠ **No confirmation is asked for**, and that is deliberate: a game of Snake lasts under a minute
(§4.3), the score is already in the band, and the best score is written **on the tick it rises**
(§4.5) — so there is nothing to lose but a running game, whose abandonment has just been asked for
explicitly from a stopped screen.

## The entries

Four, in this order: **Play**, **How to play**, **Credits**, **Quit**.

- **Play** first: it is what almost every visitor of an itch page does, and the selection rests there
  on **every** opening of the menu, including on the way back from a game.
- **How to play** exists because the HUD's key reminder is packed into one line at the bottom of the
  screen: the panel can state the two rules that kill (the edges, the body) and the reversal refusal.
  A press ignored with no explanation reads as a missed key (§3).
- **Credits** is not decoration: Nunito's SIL OFL 1.1 **requires** attribution (`docs/CREDITS.md`),
  and a licence text that only lives in the repository does not discharge the obligation towards a
  player who will never see the repository.
- **Quit** is **absent from the web build**: `Application.Quit()` does nothing there. A dead button
  costs more than a missing entry — the player clicks, nothing happens, and they doubt the rest.

The **wrap-around** of the navigation (from the last entry back to the first) is there because the
menu has no rejection feedback: the one from §3 is reserved for directions refused *in play*. A silent
stop there would be indistinguishable from a key that was not received.

⚠ **Sideways directions move nothing.** A Snake player presses the left and right arrows out of
reflex; accepting them would jump the selection at the moment they are simply trying to turn.

## The mouse

§3 decides "gamepad and touch: not in 0.1". The **mouse** is not part of that batch: the visitor of an
itch page has their hand on it, and a menu that ignores clicks reads as a broken game before it has
started. Hover **moves the selection** (it does not draw a competing second highlight), the click
confirms.

⚠ **Hover only takes over once the mouse has actually moved.** The menu opens under a still cursor —
at launch, on the way back from a game, when the window regains the foreground — and the interface
system then sends a "pointer entered" for a mouse nobody touched. Without that lock, the selection
jumps to whichever entry happens to sit under the cursor, and the player pressing Enter while thinking
they are starting a game **quits the game**. Observed while driving the build on 2026-08-28.

## What was ruled out

- **A navigable end screen** ("Play again / Menu") — see above, it contradicts §2.
- **A "Settings" entry**: the tuning lives in a JSON not editable in game, and §7 has already ruled
  out a variable rate. The entry would have nothing to set.
- **The best score shown on the menu**: it already is, permanently, during the game (§4.5), and the
  menu is not where you decide to beat a score — the death screen is.

Expected rules: `Assets/Scripts/Rules/MainMenu.cs` — composition of the entries and navigation, tested
without an engine. The screen itself: `Assets/Scripts/UI/MenuScreen.cs`.
