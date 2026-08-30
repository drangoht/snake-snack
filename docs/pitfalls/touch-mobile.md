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
