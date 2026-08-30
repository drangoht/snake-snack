# Pitfalls — Interface


**⚠ The HUD can cover a modal.** Canvas sorting order is the only arbiter: two canvases at the same
`sortingOrder` stack in hierarchy order, which is not stable when the scene is regenerated in code.
Give every canvas an explicit `sortingOrder`.

**⚠ A focus trap only shows on a gamepad.** A list whose focus can no longer leave navigates
perfectly with a mouse. Test every screen **with keyboard and gamepad**.

**⚠ Invisible reads as non-existent.** [inherited] A capability that does not announce its key does
not exist for the player: on an earlier project, a dash went a whole session unused because the
tester did not know a key existed. A passive effect with no indicator is believed inactive. That is
an ergonomics bug, not a presentation detail.


**⚠ A mouse hover selects without anyone having moved the mouse.** uGUI sends a "pointer entered"
when an element appears UNDER a still cursor — opening a screen, coming back to the menu, a window
regaining the foreground. A menu whose hover moves the selection therefore sees its selection jump to
whichever entry happens to sit under the cursor, and the next confirm key launches something other
than what the screen was showing a second earlier. Observed on 2026-08-28: the cursor sat on "Quit",
and the game closed on the first press. Countermeasure: only accept hover **after** a real pointer
movement (`MenuScreen.WatchPointer`).

**⚠ A line of text takes about 1.36 times the font size, not 1.0.** Sizing a panel by multiplying the
number of lines by `fontSize` gives a box a third too small. With `VerticalWrapMode.Overflow` (the
default), the text leaves the frame and runs over whatever follows — which reads as a rendering
defect; with `Truncate`, the last lines disappear silently. Both happened on the "How to play" panel
on 2026-08-28. Measure on a screenshot, not by arithmetic.
