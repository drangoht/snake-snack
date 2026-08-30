# itch.io page — Snake Snack

**The page's text lives here**, and it is from here that it is pasted onto itch. If the published page
is ever in another language, keep both files and **correct them together**: otherwise one of the two
lies, and nobody knows which.

⚠ **This text must describe the game AS IT IS.** A page describing a feature removed two versions
earlier is the most common and the most costly defect: the visitor sees the gap and closes the tab.
Re-read it at every release that changes something visible.

---

## Title

Snake Snack

## Tagline (one line, under the title)

The classic game of Snake

## Description

**Steer a snake that grows with every bite, until its own body leaves no way through.**

No twist, no power-ups, no enemy: what stops you is your own tail. The edges of the grid kill — they
do not wrap around — so every death stays attributable to a turn, and there is always a sentence in
your head as you restart: "I should have gone right".

A game restarts in **one key with zero waiting**.

### Controls

| Action | Keyboard | Touch |
|---|---|---|
| Turn (4 directions) | Arrows **or** WASD | — not in 0.1 |
| Pause / resume | Esc | — not in 0.1 |
| Restart after death | Space | — not in 0.1 |
| Back to the menu | Esc (end screen) **or** Backspace (pause) | — not in 0.1 |
| Navigate the menu | Arrows or WASD, Enter or Space | mouse: hover and click |

⚠ **The game is played with a keyboard**: there are no touch controls, and a mouse is only enough for
the menu.

On a French AZERTY keyboard, the WASD block is the four keys printed Z, Q, S, D — the game reads
physical positions, so it works either way, and the arrows work everywhere.

### What is in it

- A closed 21 × 15 grid, one apple at a time, one point per apple.
- A best score that survives closing the game, and that has to be beaten strictly.
- A main menu: play, how to play, credits.
- A pause, and a refused reversal that **shows** rather than swallowing the press in silence.
- A snake that **glides** from cell to cell, rounded shapes, a face that looks where it is going, and
  a death that shows the offending cell before the end screen appears.

**What is not in it yet**: no sound and no music at all. This is a playable base, not a finished
version.

### Credits

- **Nunito font** — Vernon Adams, Cyreal, Jacques Le Bailly.
  `Copyright 2014 The Nunito Project Authors (https://github.com/googlefonts/nunito)`
  Under the [SIL Open Font License 1.1](https://scripts.sil.org/OFL); static instances extracted for
  this project. The full licence text is embedded in the game ("Credits" screen).
- Everything else — code, illustrations, interface — is made for this project.
- Made with Unity 6000.5.6f1 (URP 2D), from the
  [unity-game-template-with-claude](https://github.com/drangoht/unity-game-template-with-claude)
  template.

---

## Dashboard settings — ⚠ they are in NO file of the repository

To check by hand after every publish; they were wrong for several versions on an earlier project.

- [x] **Kind of project** = HTML (otherwise the web build downloads instead of playing) — set 2026-08-28
- [x] File ticked **"This file will be played in the browser"** — set 2026-08-28
- [x] **Mobile friendly** — it alone decides what itch offers a visitor on a phone.
      **Unticked**: the game has no touch controls, announcing it playable with a finger would be false.
- [ ] **Orientation** declared — moot as long as "mobile friendly" is unticked
- [ ] **Classification** tab: genre, tags, **player count**, multiplayer mode
- [x] **Cover 630 × 500** — the only image seen by visitors who do not open the page.
      Produced by `tools/generate_itch_cover.py`. ⚠ It carries the tagline as text: it must be
      regenerated when the page's language changes.
- [x] **Screenshots** — three, taken IN the browser on the itch page itself.
      ⚠ The in-game ones show the game's rendering: **to be regenerated at every visible change**,
      otherwise the page announces a snake the game no longer shows. They currently show the French
      interface and a snake with no face.

⚠ **Visibility**: the page has been in **Draft** since it was created (2026-08-28). Nothing is public
until the author clicks "Publish" themselves.
