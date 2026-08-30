# Map — `Assets/Scripts/Rules/` (pure tested logic)

## §Rules — `Assets/Scripts/Rules/`

Pure logic, with no engine dependency, **tested**. This is where every numbered rule of the game lives.

<!-- List every class here and what it decides. One line per rule. -->

| File | What it decides | Tests |
|---|---|---|
| `Direction.cs` | The `Direction` enum (North/East/South/West) + `Directions`: opposite, **reversal**, one-cell step. ⚠ North = increasing Y | `tests/DirectionsTests.cs` |
| `Cell.cs` | Integer grid coordinate. ⚠ Exists **because `Vector2Int` comes from UnityEngine** | `GridTests` |
| `Cadence.cs` | The time step (GDD §4.1): 8 ticks/s by default, overridable; suggested range 6–10; `EffectiveRate` **ignores the length** (constant rate, §7); splits time → ticks carrying the remainder over, **capped at 1 tick per frame** (lateness thrown away, §4.1) | `tests/CadenceTests.cs` |
| `InputQueue.cs` | The input queue (GDD §4.2): FIFO of depth 2, one input per tick, **reversal validated at the tick** against the direction applied on the previous tick, overflow ignored, duplicate refused, purge on pause/death. ⚠ **The only stateful class** in `Rules/` | `tests/InputQueueTests.cs` |
| `Grid.cs` | The playfield (GDD §4.3): 21 × 15 configurable, **even dimensions refused** (an exact centre cell), starting pose (10,7)/(9,7)/(8,7) facing east, `IsOutside` = the deadly wall of §2 | `tests/GridTests.cs` |
| `Snake.cs` | The body and **the resolution of a tick** (GDD §4.4): wall → growth → self-bite → move, in that order. ⚠ The tail is excluded from the obstacles **only if not eating**; a deadly step does not move the snake and does not eat | `tests/SnakeTests.cs` |
| `Apple.cs` | Where to lay the apple (GDD §4.4): free cells, **draw by enumeration** (X increasing within Y increasing), `GridIsFull` = the win. ⚠ Answers "where" and "how many", never "when" | `tests/AppleTests.cs` |
| `RandomSource.cs` | The seeded generator of the apples — **SplitMix64 written here** (§4.4). ⚠ Neither `UnityEngine.Random` (global state) nor `System.Random` (a sequence not stable from one runtime to the next) | `tests/RandomSourceTests.cs` |
| `Board.cs` | Layout of the playfield (GDD §4.3): cell size deduced from the 1280×720 frame minus the banner, centre of each cell, anchoring of the rejection pictogram. ⚠ Unit = **pixel of the reference frame** | `tests/BoardTests.cs` |
| `Startup.cs` | The start from a standstill (§4.1): which first direction starts the game, which one is refused | `tests/StartupTests.cs` |
| `RejectionFeedback.cs` | Visual register of a refusal (ART §5): pictogram, pause text, or silence; deadlines and opacity of the feedback | `tests/RejectionFeedbackTests.cs` |
| `GameSettings.cs` | Schema of the tuning JSON + `Validate()`. See `data-outils.md` | `tests/GameSettingsTests.cs` |
| `Score.cs` | Score and best (GDD §4.5): +1 per apple, **a best that rises during the game**, the "best beaten" predicate judged against the best from BEFORE the game (equalling does not beat), normalisation of a damaged best, and `length == 3 + score` | `tests/ScoreTests.cs` |
| `Easing.cs` | The **juice curves** (`docs/art/juicy.md` §2): `Progress` clamped at both ends (a long frame would throw a segment beyond its cell), `Pulse` (there and back), `PopIn` (a pop with overshoot, **ending exactly at 1**), `Falloff`, `Gulp` (compression at constant area). ⚠ Returns a factor, decides nothing | `tests/EasingTests.cs` |
| `MainMenu.cs` | Composition of the menu entries (GDD §4.6) and navigation: "Quit" absent when the platform cannot close, wrap-around up/down, lateral directions ignored | `tests/MainMenuTests.cs` |
| `EXAMPLE_Rule.cs` | Template — to be deleted once `Rules/` is well populated. |  |

⚠ The refusal of an input (`EnqueueResult`, `TickResult.ReversalRejected`) is **returned to the caller**,
never swallowed: §3 requires the refusal to be seen on screen. The engine wiring of that feedback is
done (`SnakeGame.SignalRejection` → `BoardView.ShowRejection`), as are the pause on focus loss and the
start from a standstill.

⚠ **The game's only randomness goes through `RandomSource`**, and the game's instance serves the apple
alone — any other need takes its own (`SnakeGame._sessionSeeds`). Why this is a trap:
`docs/pitfalls/pure-logic-tests.md`.

⚠ Before adding a file here, read `docs/pitfalls/pure-logic-tests.md`: the csproj glob and the
compilation context hold two silent surprises.
