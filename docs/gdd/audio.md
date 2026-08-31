# 4.7 — Sound

**Four sounds, and nothing else for now**: the bite, the death, the menu cursor, the menu
confirmation. Decided 2026-08-31, by the author, over a wider list. The game had **no sound at all**
up to that date — the store page says so in as many words, and it is the last thing that makes it
read as a demo rather than as a game.

The two in-game ones are the only moments where the state really changes: an apple disappears, or the
game ends. The two menu ones exist for a different reason — they are what makes the interface feel
like it answers, before a single apple has been eaten.

## Why so few

A sound on every tick, on every turn, on every refused reversal, is a sound the player stops hearing
in ninety seconds — and then the bite is no longer an event either. The refusal in particular fires
under hammering, up to eight times a second (§4.1): it already has a **visual** channel routed
through `RejectionRouting`, chosen precisely because it can repeat without becoming aggressive.
Adding a sound to it would undo that.

**Ruled out for this pass, not forever** (§7): the beaten best score and the win. Both are rare
enough to be worth a sound of their own, and the code already knows how to name the moment
(`Score.CountApple` returns the signal, `Apple.GridIsFull` decides the win). They were left out to
keep the first pass to what can be judged in one listen.

## The grain

**Soft and organic** — filtered sines, slack attacks, a mouth "plop" rather than a beep. Decided
2026-08-31. It continues the rounded, faced snake of `docs/art/cartoon.md`: this game does not look
like a Nokia game, and a square-wave beep would say it does. A retro 8-bit set was ruled out for
exactly that reason.

⚠ **Nothing here can be verified by reading.** Whether a sound is "soft" is heard, not measured; what
a script can measure — duration, attack time, spectral centroid, noise ratio — only narrows the
shortlist. Every sound in this system needs someone to listen before it ships.

## Where the sounds are produced

**A free CC0 bank, not synthesis** — decided by the author on 2026-08-31, against the recommendation
of the moment, which was to generate them by script like the fonts, the illustration and the store
cover. What the choice buys: sounds recorded by people, immediately more alive than an oscillator.
What it costs, and what must therefore be watched:

- **Opaque binaries in the repository.** A `.wav` cannot be diffed, cannot be retuned by changing a
  parameter, and cannot be regenerated if it is lost. Balancing therefore happens **outside** the
  files, in `SfxCatalog.Volume` — re-exporting a clip to make it quieter loses the original.
- **Licences to trace.** CC0 requires no attribution, but the project traces the OFL of its font and
  will trace this the same way, in `docs/CREDITS.md` and on the in-game Credits screen. A bank whose
  licence is not written down becomes, two years later, a bank nobody dares ship.

## Values and volumes

Master volume in `settings.json` (`sfxVolume`, default **0.8**): mixed to sit under the game, not
over it. ⚠ **Zero is a legitimate value** — a player who writes 0 wants silence, and the validator
therefore clamps this setting rather than falling back to the default like every duration around it.
Out of range it is clamped, not refused: someone writing 2 wants the loudest the game has.

Per-sound volumes in `SfxCatalog.Volume`, relative to the master: the cursor is at **0.45** because it
fires on every arrow press, and a menu run through quickly must not turn into a rattle.

## What makes this system silent, and how it says so

⚠ Audio fails silently by construction: a missing clip, a missing listener, a suspended browser
context and a volume at zero all produce the same nothing, and none of them raises an error. Hence
the startup audit in `SfxPlayer`, which names each cause separately, and the enum-to-table check that
turns a sound declared but unbound into a console line instead of a moment nobody notices is mute.

⚠ **The scene had no `AudioListener` at all** until 2026-08-31 — found while wiring this system. It
cost nothing only because there was no sound to lose. It is added by `SceneBuilder.BuildCamera`, and
the audit checks for it first, because its absence makes every other check moot.

The browser side is already handled: the WebGL template wakes the audio context on the first
gesture (`docs/pitfalls/audio.md`), inherited from the project template.

**Proof that a sound came out** is `SfxPlayer.OutputRms()`, not a log: a `PlayOneShot` log proves an
intention. The RMS is read downstream of the volume, the listener and the context.

## How the four clips were chosen

From Kenney's CC0 "Interface Sounds" bank, out of some 200 candidates across two banks, by
**measurement**: duration, attack time (10% to 90% of the peak), spectral centroid and spectral
flatness. Slow attack, low centroid and a tonal rather than noisy spectrum is what "soft" looks like
when a script has to see it.

⚠ **The duration window is part of the brief, not a detail.** Ranked on softness alone, the top of
"menu cursor" came back full of 1000 ms clips — perfectly soft, and useless: a cursor that long
turns a quickly-run menu into a rattle. Each role has a window, and outside it a candidate is not
ranked at all.

⚠ **The measurements narrowed the field; they did not choose.** Whether these four are *right* is
heard. They had not been listened to when they were committed.

## The music

**The menu sings, the game is silent.** A game lasts from thirty seconds to three minutes and
restarts with no transition (§2): a thirty-four second loop would run dozens of times in a row, and
the four effects would lose the silence they stand out against. Decided 2026-08-31.

One track, `Audio/Music/menu.ogg` — "Heavenly Loop" by isaiah658, CC0. Chosen against another
candidate **on a measurement, not an impression**: the other one carried 1 208 ms of digital silence
at its tail, which as a loop opens a hole of over a second on every wrap. Nothing in reading a file
reveals that; everything in listening does. The retained one measures 0 ms of padding at both ends
and a seam step of 0.003 — no click.

⚠ **The music takes the OPPOSITE import settings from the effects** (`Assets/Editor/ImportAudio.cs`,
which tests the `Music/` subfolder *before* its parent): Streaming Vorbis, not decompressed PCM.
Decompressing thirty-four seconds into memory to play one loop is the wrong trade at this length,
and it is the web build that pays.

## Silence, and why it needed a button

⚠⚠ **`settings.json` is not read on the web.** `SettingsLoader` says so itself:
`streamingAssetsPath` is a URL under WebGL, and `File.Exists` answers false without raising
anything. So `sfxVolume` and `musicVolume` have **no effect on itch.io** — the only channel this game
is published on. For four short effects that was venial. For a loop a visitor cannot silence, it is
what makes them close the tab.

Hence, decided 2026-08-31: **the M key, and a button on the menu**, mirrored in
`Audio/AudioMute.cs`. It mutes **everything**, music and effects alike — someone reaching for a mute
is silencing a tab, not balancing a mix. The choice is remembered between sessions (`PlayerPrefs`,
best effort like the best score: unreadable means "not muted", and the game starts).

The button says **"Sound: on"**, a state, never "Sound off", which half the players read as a state
and the other half as an action.

⚠ **The M key is resolved by printed character, not by physical position**
(`FindKeyOnCurrentKeyboardLayout`). `Key.M` names the QWERTY position of M, which on an AZERTY
keyboard is the key printed `,` — a player told "press M" would press M and get nothing. This is the
opposite choice from WASD, and deliberately so: a position for a gesture, a character for a mnemonic
(`docs/pitfalls/inputs.md`).

⚠ **Accepted limit: there is no mute button during a game.** A touch player who wants silence
mid-run has to go back to the menu. The margins already carry the pause button and the directional
pad, and a third control there would cost more than it gives — the choice being remembered, it is
made once. To reopen if anyone actually trips over it.

⚠ **An entry "Settings" stays ruled out** (`gdd/menu.md`), and this does not reopen it: a toggle in
a corner is not a settings screen. What was ruled out was a screen with nothing to set; the mute has
something to set, and it sets it in one tap.

## Status

Design, wiring and clips done 2026-08-31 — `Assets/Scripts/Audio/`, `SceneBuilder` (the listener),
`MenuScreen` and `SnakeGame` (the four call sites), `Assets/Resources/Audio/` (the clips).

**Proven, not assumed**: `-audiocheck` (Windows) / `?audiocheck` (web) plays one sound and reports
the RMS that actually left the mixer. Measured on 2026-08-31: `0.00000` before, peak `0.17156`
during. That is the proof the audit alone cannot give.

**Still open**: someone has to listen. And the music, of which there is nothing.
