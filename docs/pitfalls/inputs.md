# Pitfalls — Inputs


**⚠⚠ `ProjectSettings.asset` can ship `activeInputHandler: 0` — the OLD Input Manager.** In that
mode the Input System package is disabled: **`Keyboard.current` is `null`**, every input code path
returns through its guard, and the game runs perfectly — it simply answers no key at all. No error,
no warning, nothing in the player log. Observed on 2026-08-27: the snake displayed, the HUD
displayed, and nothing moved; the first suspicion fell wrongly on key injection, then on the
pictogram rendering.

Values: `0` = old Input Manager, `1` = Input System package, `2` = both. The project requires `1`
(CLAUDE.md: "Input System, never the old Input Manager").

```powershell
Select-String "activeInputHandler" ProjectSettings\ProjectSettings.asset   # must return 1
```

⚠ Corollary of method: **a key with no effect and a key never received produce the same
screenshot**. Before concluding that a rule does not display, prove that *some* input reaches the
game — here, a valid direction that sets the snake moving.

**⚠ `KeyCode` and `Key` name a POSITION on a QWERTY keyboard**, never the printed character. On an
AZERTY keyboard, `Key.A` / `Key.D` / `Key.W` put the controls under the keys printed **Q / D / Z**.
That is the intended result, not a bug — it is also why the in-game text says "WASD", which is
literally true for a QWERTY player. Corollary: avoid `A`, `Q`, `Z`, `W`, `M` for global shortcuts —
prefer `Tab`, `R`, the digits or the arrows, whose position is common to both layouts. **This pitfall
was only discovered by injecting real keys.**

**⚠ `InputSystemUIInputModule` and not `StandaloneInputModule`.** With the Input System package
active, the old module receives nothing: the UI simply stops responding, with no error.

**⚠ The very first key after taking focus is lost**, on the Windows build as in the browser. Always
send one for nothing before measuring anything.
