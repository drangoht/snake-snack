# Pitfalls — Touch and mobile


**⚠ Half the mobile port lives in `index.html`, not in Unity.** [inherited] Zoom, scrolling, the back
gesture from the edge, the long press that opens a system menu, the URL bar that eats the bottom of
the screen (hence the controls that live there): Unity can do nothing about what happens **before**
it. None of these defects shows in the editor, none raises an error, and each makes the game
unplayable with a finger. The project's template handles them all — do not undo them.

**⚠ `maxTouchPoints` is the only reliable test for detecting a mobile**: the user-agent string lies
(desktop mode of a phone, an iPad declaring itself a Mac).

**⚠ Use `dvh` and not `vh`** for the canvas height: `vh` ignores the URL bar as it retracts, and the
bottom of the game ends up hidden behind it.

**⚠ Desktop Chrome provides NO `Touchscreen` at all.** [inherited] `Touchscreen.current` stays `null`
and every touch code path returns immediately. Dispatching real `TouchEvent`s in JS is useless — the
event propagates, but the engine has no device to file it under, and **no error** says so. Only a
`?touch` mode (which calls `TouchSimulation.Enable()`) makes touch testable.

**⚠ An input read that starts with `if (Keyboard.current == null) return;` kills the whole mobile
port.** Found on 2026-08-30, in our own `SnakeGame.ReadInputs`. A phone has no keyboard: that guard —
written to protect a desktop build launched without a device — made every touch path unreachable
behind it. The game builds, runs, draws itself perfectly and answers nothing. Nothing is raised,
because nothing went wrong: the code did exactly what it said. **Read the fingers before the guard,
never after it.**

**⚠ The frame the controls are laid out in is the VISIBLE one, not the reference one.** The camera
fixes the vertical extent at 720 reference pixels; the width follows the panel's aspect ratio. A phone
in landscape is *wider* than 16:9 (often 20:9), so there is more margin than the 1280 px frame
suggests — and a window narrower than 16:9 has none. Laying a control out on the constant 1280 puts it
off-screen on the first and over the playfield on the second. `2 × orthographicSize × aspect` is the
width to use.

**⚠⚠ `TouchControl.phase` is a STATE, not an EVENT — and this is the one that cost a whole diagnostic
round.** `Began` keeps being reported frame after frame for as long as no new state arrives:
**measured at six consecutive frames** for a single click. Treating it as the press itself queued six
turns for one thumb. There is no `wasPressedThisFrame` here as there is on a key: the press is the
**transition**, and detecting it needs the touch's `touchId` as well as its phase, because the slots
are a reused pool — a new finger landing in a slot whose phase still reads `Began` is otherwise
mistaken for the previous one still being held.

**⚠ A drag built on `SetCursorPos` cannot test a swipe.** It moves the cursor and puts **nothing** in
the input stream, so touch simulation sees the press at the start point and no travel at all: the
swipe is never seen, and the game looks broken while it is the harness that is. `SendInput` with
`MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE` is the only way. (The general form of this is pitfall 4 of
`/verifier-en-jeu`; it costs a second diagnostic round if you meet it here first.)

**⚠ The middle of a directional cross answers nothing, on purpose** — and a test that aims there
concludes the pad is dead. `TouchPad` deliberately returns `None` for the centre and the corners: a
diagonal is not a direction this game has. When a control test fails, check the coordinate against
`ButtonCentre` before suspecting the code.

**⚠ `Touchscreen.current` is not necessarily the touchscreen a finger is on.** A machine can carry
several: a Windows laptop with a digitizer, or a browser reporting touch capability, gives one that
never receives anything, while `?touch` simulation feeds a second. Polling only `current` reads the
idle one — the pad draws, the labels are in touch mode, nothing answers, and no error is raised.
Measured on the web build, with code that worked on Windows. **Walk `Touchscreen.all` and prefer the
device that is actually carrying a touch.**

**⚠ On a desktop, `Touchscreen.current` being non-null proves nothing about simulation.** A Windows
machine with a touch digitizer — or a browser reporting touch capability — makes the game believe it
is on a phone: the pad draws, the labels flip to "tap to play again", and no finger ever arrives. The
`?touch` / `-touch` switch has to be confirmed by its **log line**, not by the pad appearing.

**⚠ A finger that lands on an on-screen button must not also be read as a swipe.** A thumb resting on a
pad key drifts; past the swipe threshold it fires a second turn nobody asked for, and the pad reads as
having a mind of its own. One gesture, one meaning, decided when the finger lands.

**⚠ The swipe fires on travel, not on release.** Waiting for the finger to lift adds the whole length
of the gesture to the latency — at 8 ticks/s a turn decided 200 ms late is a turn taken one cell too
far. Re-arm the gesture's origin at the point the turn fired: that is what lets an L-shaped turn be
drawn in one stroke without lifting.

**⚠ Every label naming a key is a lie on a phone.** "Press Esc to pause" describes a device the player
does not have, and it is the first sentence they read. Each such label needs a touch counterpart, and
the flag choosing between them must be set **before** the HUD and the menu build their texts — they
read their labels once.
