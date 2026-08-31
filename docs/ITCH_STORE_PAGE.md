# itch.io page — Snake Snack

**The page's text lives here**, and it is from here that it is pasted onto itch. If the published page
is ever in another language, keep both files and **correct them together**: otherwise one of the two
lies, and nobody knows which.

✅ **The live page carries this text, since 2026-08-31.** It was pasted by writing the
`.redactor-layer` **and** the hidden `textarea`, then waiting for the "Saved" banner — the three
earlier attempts had failed for want of that. ⚠ The Controls table below goes onto the page as **two
lists** (Keyboard, then Touch): itch's editor renders a list far better than a three-column table on
a phone. Same content, different shape — do not take the difference for a drift.

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
| Turn (4 directions) | Arrows **or** WASD | Swipe anywhere, **or** the on-screen pad |
| Pause / resume | Esc | Pause button, top left — tap to resume |
| Restart after death | Space | Tap anywhere |
| Back to the menu | Esc (end screen) **or** Backspace (pause) | Pause button, from pause or end screen |
| Navigate the menu | Arrows or WASD, Enter or Space | Tap an entry · mouse: hover and click |
| Mute / unmute | **M**, anywhere | The "Sound" button, top right of the menu |

**It plays on a phone**, in landscape: swipe to turn, or use the pad in the right-hand margin. The
controls take nothing from the playfield — they live in the margin the grid already left empty.

On a French AZERTY keyboard, the WASD block is the four keys printed Z, Q, S, D — the game reads
physical positions, so it works either way, and the arrows work everywhere.

### What is in it

- A closed 21 × 15 grid, one apple at a time, one point per apple.
- A best score that survives closing the game, and that has to be beaten strictly.
- A main menu: play, how to play, credits.
- A pause, and a refused reversal that **shows** rather than swallowing the press in silence.
- A snake that **glides** from cell to cell, rounded shapes, a face that looks where it is going, and
  a death that shows the offending cell before the end screen appears.
- **Sound**: the bite, the death, and the menu answering. A calm loop on the menu — the game itself
  stays silent, so the bite keeps standing out. Everything is silenced in one key or one tap, and the
  choice is remembered.

**What is not in it yet**: the graphics are still shapes drawn by code rather than hand-made art.
This is a playable base, not a finished version.

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
- [x] **Mobile friendly** — it alone decides what itch offers a visitor on a phone. Unticked from
      2026-08-28 (rightly so then: no touch controls), **ticked on 2026-08-31**, one day after touch
      shipped. For that one day, itch hid from phone visitors a game that plays on a phone.
- [x] **Orientation**: **landscape** — set 2026-08-31. It was empty, not landscape. The playfield is
      21 × 15 and the frame is 16:9; the web template already tells a phone player to turn the device.
- [ ] **Classification** tab: genre, tags, **player count**, multiplayer mode
- [ ] **Cover 630 × 500** — the only image seen by visitors who do not open the page.
      Produced by `tools/generate_itch_cover.py`. ⚠ It carries the tagline as text: it must be
      regenerated when the page's language changes. ⚠⚠ **The English cover was regenerated on
      2026-08-30 but NEVER reached itch**: `docs/itch/cover.png` reads "It grows with every bite."
      while the page still shows the French "Il s'allonge à chaque bouchée." — checked 2026-08-31.
      The line below used to claim the upload was done; it was not. **To upload by hand.**
- [x] **Screenshots** — three, taken IN the browser on the running build, and uploaded on 2026-08-30:
      menu, a game at 4 points, and the end screen with "New best: 4". The `v0.3.0` stamp is visible on
      each.
      ⚠ They show the game's rendering, so they are **to be redone at every visible change** — the
      0.4.0 touch controls are NOT on them.
      ⚠ **The three old ones (French interface, faceless snake) are still online underneath**: deleting
      them opens a confirmation dialog that froze the browser session. To finish by hand.

⚠ **Visibility**: the page is **PUBLIC** — found so on 2026-08-30. Every document in this repository
claimed it was still in Draft, and had done since 2026-08-28. Whatever is written here is therefore
being read by visitors, not held back for review: a wrong line on this page is wrong in public.
