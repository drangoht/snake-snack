using System;
using SnakeSnack.Core;
using SnakeSnack.Rules;
using SnakeSnack.UI;
using UnityEngine;
using UnityEngine.InputSystem;

// The rules' grid, and not UnityEngine.Grid (the tilemap component). Without this alias every
// mention of `Grid` in this file is ambiguous and nothing compiles -- a name collision that only
// shows up once the whole engine layer is written.
using Grid = SnakeSnack.Rules.Grid;

namespace SnakeSnack.Gameplay
{
    /// <summary>
    /// The game loop: reads inputs, drives the tick, and renders the state on screen.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>No rule is decided here.</b> The rate, the queue, the reversal, the start, death, score
    /// and layout all come from <c>Rules/</c>, tested without an engine. This component does only the
    /// three things a pure class cannot: read a keyboard, measure time, place objects on screen. Any
    /// rule creeping up here would become a second truth, and it is that one that would end up
    /// drifting.
    ///
    /// <para>⚠ <b>The visual-feedback deadlines use unscaled time</b> (<c>unscaledTime</c>): the
    /// "key ignored" message of the pause screen must appear and go out <i>during the pause</i>, that
    /// is, at a moment when game time may be stopped. With <c>Time.time</c>, that message would stay
    /// frozen on screen.</para>
    /// </remarks>
    public sealed class SnakeGame : MonoBehaviour
    {
        // ⚠ Physical layout: `Key` names a POSITION on a QWERTY keyboard. Key.W / Key.A / Key.S /
        // Key.D are therefore the WASD block for a QWERTY player, and the keys printed Z, Q, S, D on
        // a French AZERTY keyboard. Nothing is raised if this is wrong: the game simply answers to
        // the wrong key (see Pressed).
        private static readonly Key[] UpKeys = { Key.UpArrow, Key.W };
        private static readonly Key[] DownKeys = { Key.DownArrow, Key.S };
        private static readonly Key[] LeftKeys = { Key.LeftArrow, Key.A };
        private static readonly Key[] RightKeys = { Key.RightArrow, Key.D };

        private GameSettings _settings;
        private Grid _grid;
        private Board _board;
        private Snake _snake;
        private InputQueue _queue;
        private BoardView _view;
        private GameHud _hud;

        /// <summary>
        /// The main menu (GDD §4.6). ⚠ IT is what says whether it owns the screen
        /// (<see cref="MenuScreen.Active"/>): duplicating a "we are in the menu" boolean here would
        /// create two truths, and it is the fade-out's one that would end up drifting.
        /// </summary>
        private MenuScreen _menu;

        /// <summary>
        /// The fingers, when there are any (GDD §3, touch — reopened on 2026-08-30).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Null on a machine with no touchscreen</b>, which is the normal case on a desktop.
        /// Every touch path tests it: building it anyway would cost a poll per frame for a device
        /// that does not exist.
        /// </remarks>
        private TouchInput _touch;

        /// <summary>The drawn controls. Null whenever <see cref="_touch"/> is, or when the margin was too narrow.</summary>
        private TouchControlsView _touchControls;

        /// <summary>The current game's generator. ⚠ Nothing but the apple draws from it (§4.4).</summary>
        private RandomSource _random;

        /// <summary>
        /// The generator that makes the seeds of games, when none is fixed.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>A separate instance, and that is a rule of §4.4</b>: drawing a seed from
        /// <see cref="_random"/> would shift the apple sequence of the current game. It also avoids a
        /// subtler trap — two games restarted back to back would draw the same seed if it came
        /// straight from the clock, whose real resolution under Windows is about 15 ms. The player who
        /// presses Space twice would then replay the same apples, with nothing to explain it.
        /// </remarks>
        private RandomSource _sessionSeeds;

        /// <summary>The apple's cell. There is one <b>at every instant</b> during a game (§4.4).</summary>
        private Cell _apple;

        /// <summary>
        /// Score of the game and best score of all games (§4.5).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Built once, when the game opens</b>, and never replaced on restart: it is what carries
        /// the best score between games. Replacing it with a fresh one on every new game would re-read
        /// storage on every death — and above all would reset the best score to its written value,
        /// losing the one from a running game whose write had failed.
        /// </remarks>
        private Score _score;

        private TimedFeedback _pictogramFeedback;
        private TimedFeedback _pauseTextFeedback;
        private Direction _rejectedDirection;

        private GameState _state;
        private double _accumulatedTime;
        private double _tickDuration;

        // --- Death feedback (docs/art/juicy.md §6) ---------------------------------------
        //
        // These three values are deliberately constants rather than fields of `settings.json`: file
        // tuning does not work on WebGL — `SettingsLoader` returns the defaults there without reading
        // anything — so a settable duration would give the illusion of being able to adjust on itch
        // what in reality only adjusts on the desktop build. They will move there the day the juice
        // is tried often enough to deserve it.

        /// <summary>Delay before the end screen covers the offending cell.</summary>
        private const double HitstopDuration = 0.080;

        private const double MicroZoomDuration = 0.150;

        /// <summary>Camera size of the 1280×720 reference frame (SceneBuilder).</summary>
        private const double ReferenceCameraSize = 360.0;

        /// <summary>Dip of the micro-zoom, in half-height pixels: 6 out of 360, i.e. ≈ 1.7 %.</summary>
        private const double MicroZoomAmplitude = 6.0;

        private Camera _camera;
        private double _deathFeedbackStart = double.NegativeInfinity;
        private bool _endScreenShown = true;

        private void Awake()
        {
            // ⚠ FIRST, before anything asks whether there is a touchscreen: with `?touch` /
            // `-touch` this is what creates one. The attribute on that class did not fire on
            // the WebGL build, so the call is made here where nothing can strip it away.
            TouchSimulationBootstrap.EnableIfAsked();

            _settings = SettingsLoader.Load();

            _grid = new Grid(_settings.gridWidth, _settings.gridHeight);
            _board = new Board(_grid, Board.CellSizeFor(_grid));
            _tickDuration = Cadence.TickDurationSeconds(_settings.ticksPerSecond);

            _pictogramFeedback = new TimedFeedback(
                _settings.rejectionDisplaySeconds,
                _settings.rejectionExtensionCapSeconds,
                _settings.rejectionFadeSeconds);

            _pauseTextFeedback = new TimedFeedback(
                _settings.pauseTextSeconds,
                _settings.pauseTextSeconds,
                _settings.rejectionFadeSeconds);

            _view = gameObject.AddComponent<BoardView>();
            _view.Build(_board);

            // The slide must last exactly one tick: the view does not copy the rate, it receives it,
            // so it follows a `ticksPerSecond` retuned in settings.json.
            _view.SetTickDuration(_tickDuration);

            // ⚠ Resolved once: `Camera.main` walks the scene on every call, and the micro-zoom
            // re-reads it every frame. It can be null in tests — the zoom then simply does nothing,
            // it never has anything to decide.
            _camera = Camera.main;

            BuildTouchControls();

            _hud = gameObject.AddComponent<GameHud>();

            // The best score comes from the engine's storage; everything decided about it belongs to
            // Score (§4.5). A missing or damaged best score is zero and blocks nothing.
            _score = new Score(PersistentBest.Read());

            // The session seed comes from the clock: it is the game's only non-reproducible
            // randomness, and it only serves to keep two sessions from starting on the same apples.
            _sessionSeeds = new RandomSource((ulong)DateTime.UtcNow.Ticks);

            StartPose pose = _grid.StartingPose();
            _snake = new Snake(pose.Segments);
            _queue = new InputQueue(pose.Orientation, _settings.queueDepth);

            _menu = gameObject.AddComponent<MenuScreen>();
            _menu.Confirmed += OnMenuEntryConfirmed;

            // ⚠ No game is prepared here: `NewGame` seeds the randomness and logs the seed (§4.4).
            // Calling it at startup and again on "Play" would write two seeds in the log for a single
            // game played, and it is the first one — the one not played — that would be read in a bug
            // report.
            BackToMenu();
        }

        /// <summary>The menu takes the screen: board and HUD go dark in one go.</summary>
        private void BackToMenu()
        {
            _view.Show(false);
            _hud.Show(false);
            ShowTouchControls(false);
            _menu.Open();
        }

        /// <summary>
        /// A menu entry that commits the application has been confirmed, fade-out finished.
        /// </summary>
        /// <remarks>
        /// "How to play" and "Credits" never arrive here: they are panels, and
        /// <see cref="MenuScreen"/> handles them without leaving the menu.
        /// </remarks>
        private void OnMenuEntryConfirmed(MenuEntry entry)
        {
            if (entry == MenuEntry.Quit)
            {
                // ⚠ No effect in the editor AND on WebGL — hence the entry's absence on the web
                // (MainMenu.Entries). On the desktop, the game really does close.
                Application.Quit();
                return;
            }

            _view.Show(true);
            _hud.Show(true);
            ShowTouchControls(true);
            NewGame();
        }

        private void OnEnable()
        {
            Application.focusChanged += OnFocusChanged;
        }

        private void OnDisable()
        {
            Application.focusChanged -= OnFocusChanged;
        }

        /// <summary>
        /// ⚠ <b>A dependency of the catch-up cap</b> (GDD §4.1), not a convenience: the cap discards
        /// the accumulated backlog. Without this pause, all the time spent outside the window would be
        /// lost to the player — they would come back to a snake that has not moved, or worse, one that
        /// has.
        /// </summary>
        private void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus && _state == GameState.Running)
            {
                Pause();
            }
        }

        private void Update()
        {
            // ⚠ A touchscreen may show up AFTER Awake. Unity registers the device when the platform
            // reports it, and there is no contract saying that happens before the first frame — on a
            // browser it may well be the first contact that reveals it. Deciding once at startup and
            // never looking again would give, on exactly those devices, a game that draws itself and
            // answers nothing: the very failure this whole port exists to remove. Cheap to re-ask,
            // and it settles for good on the first frame where the answer is yes.
            if (_touch == null && TouchInput.Available)
            {
                AdoptTouch();
            }

            // ⚠ Polled every frame, the menu's included. A gesture spans several frames: skipping
            // the poll while the menu is up would leave a finger's origin stale, and the first swipe
            // of the game would be measured from wherever that finger last was.
            if (_touch != null)
            {
                _touch.Poll();

                if (_touchControls != null)
                {
                    _touchControls.SetPressed(_touch.PressedTarget);
                }
            }

            if (_menu.Active)
            {
                // ⚠ Nothing of the game runs while the menu is there, fade-out included: a direction
                // pressed on the last frames of the fade would end up queued in the input queue and
                // would start the game on its own (§4.2).
                ReadMenuInputs();
                return;
            }

            // ⚠ BEFORE reading inputs: that is what gives the hitstop the power to hold them back
            // from the very frame of impact.
            AdvanceDeathFeedback();

            ReadInputs();
            AdvanceTime();
            RefreshRejectionFeedback();
        }

        /// <summary>
        /// The menu keys. Same arrows and same WASD block as the game (GDD §3): the player has no two
        /// sets of controls to learn, and the physical layout is declared here too as <c>Key.W</c>.
        /// </summary>
        private void ReadMenuInputs()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _menu.Back();
            }

            if (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame)
            {
                _menu.Confirm();
            }

            if (Pressed(keyboard, UpKeys))
            {
                _menu.Move(Direction.North);
            }

            if (Pressed(keyboard, DownKeys))
            {
                _menu.Move(Direction.South);
            }

            if (Pressed(keyboard, LeftKeys))
            {
                _menu.Move(Direction.West);
            }

            if (Pressed(keyboard, RightKeys))
            {
                _menu.Move(Direction.East);
            }
        }

        /// <summary>
        /// Builds the touch stack, when the machine has fingers to read (GDD §3, touch).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>The frame the pad is laid out in is the VISIBLE one, not the reference one.</b> The
        /// camera fixes the vertical extent at 720 reference pixels; the width follows the panel's
        /// aspect ratio. A phone in landscape is wider than 16:9, so the margin the pad lives in is
        /// larger there than the 178 px of the reference frame — and in a window narrower than 16:9
        /// it shrinks to nothing. Laying the pad out on the constant 1280 would put it off-screen on
        /// the first and over the playfield on the second, and neither raises anything.
        /// </remarks>
        private void BuildTouchControls()
        {
            if (!TouchInput.Available || _camera == null)
            {
                return;
            }

            // ⚠ Set BEFORE the HUD and the menu are built: they read their labels once, when they
            // build their texts. Setting it afterwards would give a game that reads the fingers
            // correctly and keeps telling the player to press Esc.
            UiText.Touch = true;

            int visibleWidth = Mathf.RoundToInt(2f * _camera.orthographicSize * _camera.aspect);
            int visibleHeight = Mathf.RoundToInt(2f * _camera.orthographicSize);

            bool fits = TouchPad.Fits(_board, visibleWidth);
            TouchPad pad = fits ? new TouchPad(_board, visibleWidth, visibleHeight) : default;

            _touch = new TouchInput(_camera, pad, fits);

            if (!fits)
            {
                // Still entirely playable: swipes steer, a tap restarts. Only the pause button is
                // lost, which is why this is worth a line in the log rather than silence.
                Debug.LogWarning(
                    "Touch: the playfield leaves no room for the on-screen pad in this window — "
                    + "swipe only, and no pause button.");
                return;
            }

            _touchControls = gameObject.AddComponent<TouchControlsView>();
            _touchControls.Build(pad);
        }

        /// <summary>
        /// Takes on a touchscreen that appeared after startup: controls, labels, visibility.
        /// </summary>
        /// <remarks>
        /// ⚠ The labels have to be rewritten here, and that is the whole reason
        /// <see cref="GameHud.RefreshControlLabels"/> exists: the HUD and the menu read their text
        /// once, when they build it. Adopting the fingers without rewriting them would give a game
        /// that answers a swipe perfectly while still telling the player to press Esc.
        /// </remarks>
        private void AdoptTouch()
        {
            BuildTouchControls();

            if (_touch == null)
            {
                return;
            }

            _hud.RefreshControlLabels();
            _menu.RefreshControlLabels();
            ShowTouchControls(!_menu.Active);
        }

        /// <summary>
        /// Shows or hides the drawn controls. Harmless when there are none.
        /// </summary>
        /// <remarks>
        /// The pad follows the board and the HUD exactly: the menu is a screen in its own right
        /// (GDD §4.6) and is navigated by tapping its entries, so a directional cross laid over it
        /// would be four keys that steer nothing.
        /// </remarks>
        private void ShowTouchControls(bool visible)
        {
            if (_touchControls != null)
            {
                _touchControls.Show(visible);
            }
        }

        /// <summary>
        /// The finger's requests, mapped onto the very actions the keyboard performs (GDD §3).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>A tap never starts a game.</b> §4.1 wants the first tick triggered by a direction,
        /// so that nobody dies while reading the screen — a tap-to-start would hand the snake a
        /// heading the player never chose. On the end screen, on the other hand, a tap is exactly
        /// Space: one press, zero waiting (§2).
        ///
        /// <para>⚠ <b>The pause button carries the two meanings Esc carries</b>, and for the same
        /// reason: a paused player has to be able to leave, and a phone has no Backspace. Pressing it
        /// during a game pauses; pressing it on a screen that is already stopped — pause or end —
        /// goes back to the menu.</para>
        /// </remarks>
        private void ReadTouchInputs()
        {
            if (_touch == null)
            {
                return;
            }

            if (_touch.TakePause())
            {
                if (_state == GameState.Paused || GameOver)
                {
                    BackToMenu();
                    return;
                }

                TogglePause();
            }

            if (_touch.TakeTap())
            {
                if (GameOver)
                {
                    NewGame();
                }
                else if (_state == GameState.Paused)
                {
                    TogglePause();
                }
            }

            while (_touch.TryTakeDirection(out Direction direction))
            {
                Request(direction);
            }
        }

        private void ReadInputs()
        {
            // ⚠ Hitstop: no input is read during the 80 ms following impact, Space included
            // (docs/art/juicy.md §6). A player hammering restart just before dying would otherwise
            // set off again while the screen still holds the image of their death — they would never
            // see the cell that killed them, and the restart would look like it fired on its own.
            //
            // ⚠ The delay is BOUNDED to that duration and never extended: a block that lengthens
            // attempt after attempt ends up reading as a game that has stopped responding (§11).
            if (!_endScreenShown)
            {
                return;
            }

            // ⚠ Read BEFORE the keyboard, and above all before the null check below: a phone has no
            // keyboard at all. Leaving the touch read after that guard is what makes a mobile port
            // that builds, runs, shows the game — and answers nothing.
            ReadTouchInputs();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (GameOver)
                {
                    // GDD §2 keeps the restart at one key (Space, zero waiting): Esc is the path to
                    // the menu ONLY on the end screen, where nothing is being played any more.
                    BackToMenu();
                    return;
                }

                TogglePause();
            }

            // ⚠ Backspace, and only during the PAUSE (GDD §4.6). Three reasons for that key: Esc is
            // already the pause toggle and giving it a second meaning (long press, double press)
            // would make every game pay for a rare round trip; the M of "Menu" would be declared
            // `Key.Semicolon` on a French keyboard, a trap GDD §3 bans; and Tab is the priming key of
            // `tools/drive_game.py`, which requires a key the game ignores. From a pause screen —
            // already a screen of stopping — abandoning the game is a decision, not a reflex.
            if (keyboard.backspaceKey.wasPressedThisFrame && _state == GameState.Paused)
            {
                BackToMenu();
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame && GameOver)
            {
                NewGame();
            }

            if (Pressed(keyboard, UpKeys))
            {
                Request(Direction.North);
            }

            if (Pressed(keyboard, DownKeys))
            {
                Request(Direction.South);
            }

            if (Pressed(keyboard, LeftKeys))
            {
                Request(Direction.West);
            }

            if (Pressed(keyboard, RightKeys))
            {
                Request(Direction.East);
            }
        }

        /// <summary>
        /// ⚠ <b>Physical layout</b> (CLAUDE.md): <c>Key</c> names a <i>position</i> on a QWERTY
        /// keyboard. The keys printed Z, Q, S, D on a French keyboard are therefore declared
        /// <c>Key.W</c>, <c>Key.A</c>, <c>Key.S</c>, <c>Key.D</c> — the same physical block a QWERTY
        /// player calls WASD. Nothing is raised on a mistake: the game simply answers the wrong key.
        /// </summary>
        private static bool Pressed(Keyboard keyboard, Key[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keyboard[keys[i]].wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// End of a game, death or win: same screen, same place, same one-key restart (§4.4). The two
        /// states differ only by their label.
        /// </summary>
        private bool GameOver
        {
            get { return _state == GameState.Dead || _state == GameState.Won; }
        }

        /// <summary>A direction pressed by the player, whatever the state of the game.</summary>
        private void Request(Direction direction)
        {
            if (GameOver)
            {
                // Only Space restarts (§2): a direction must not restart by surprise.
                return;
            }

            if (_state == GameState.Waiting)
            {
                if (Startup.Decide(_grid.StartingPose().Orientation, direction) == StartDecision.RejectedReversal)
                {
                    SignalRejection(RejectionReason.Reversal, direction);
                    return;
                }

                _state = GameState.Running;
                _hud.Display(_state);
            }

            EnqueueResult result = _queue.Enqueue(direction);

            RejectionReason reason;
            if (RejectionRouting.FromEnqueue(result, out reason))
            {
                SignalRejection(reason, direction);
            }
        }

        private void AdvanceTime()
        {
            if (_state != GameState.Running)
            {
                return;
            }

            _accumulatedTime += Time.deltaTime;

            int ticks = Cadence.TickCount(
                _accumulatedTime, _tickDuration, out _accumulatedTime, _settings.catchUpCap);

            for (int i = 0; i < ticks; i++)
            {
                PlayOneTick();

                if (_state != GameState.Running)
                {
                    // Died during the burst: the remaining ticks no longer mean anything.
                    return;
                }
            }
        }

        /// <summary>
        /// One tick, in the exact order of GDD §4.4.
        /// </summary>
        /// <remarks>
        /// Steps 1 to 5 (direction, wall, bite, move, growth) belong to
        /// <see cref="Snake.Advance(Direction, Grid, Cell?, out bool)"/>; only step 6 — replacing the
        /// apple, or finding the grid full — lives here, because it touches the state of the game and
        /// the rendering.
        /// </remarks>
        private void PlayOneTick()
        {
            // ⚠ Read BEFORE the tick: `CurrentDirection` is the direction applied on the PREVIOUS
            // tick, and it is the only reference that says whether there is a turn
            // (docs/art/juicy.md §9). Re-read afterwards it would already be the new direction and no
            // turn would ever be detected — with nothing to report it.
            Direction directionBefore = _queue.CurrentDirection;

            TickResult tick = _queue.Tick();

            if (tick.ReversalRejected)
            {
                SignalRejection(RejectionReason.Reversal, tick.RejectedDirection);
            }

            // ⚠ Read BEFORE the move: after a death, `Segments[0]` is still the cell the snake tried
            // to leave, and that is the one the flash must point at.
            Cell headBefore = _snake.Segments[0];

            bool ate;
            MoveResult result = _snake.Advance(tick.AppliedDirection, _grid, _apple, out ate);

            if (result != MoveResult.Moved)
            {
                Die(result, headBefore, tick.AppliedDirection);
                return;
            }

            // ⚠ After the move, and only if the snake is alive: tilting the head of a snake that has
            // just died would spin it under the end scrim, while the game is stopped.
            _view.SignalDirection(directionBefore, tick.AppliedDirection);
            _view.DrawSnake(_snake.Segments);

            if (!ate)
            {
                return;
            }

            // The bite feedback starts BEFORE the score: it is the snake's gesture, not the number,
            // that the player is looking at right then (docs/art/juicy.md §5).
            _view.SignalBite(tick.AppliedDirection);

            // Step 6 — the score first (§4.4: "score +1, then draw the new apple"). Counted even when
            // this apple is the one that fills the grid: it has been eaten, and the win screen must
            // show the score that includes it.
            if (_score.CountApple())
            {
                // ⚠ Written HERE, on the tick the best score rises, and not at death: §4.5 wants the
                // best score to survive a tab closed mid-game. The signal returned by CountApple
                // avoids writing to storage on every apple of every game.
                PersistentBest.Write(_score.Best);
            }

            _hud.ShowScore(_score.Points, _score.Best, _score.BestBeaten);

            // The win is tested BEFORE the draw: with no free cell, the draw has no value to return
            // and would throw, on the last tick of the perfect game.
            if (Apple.GridIsFull(_grid, _snake.Length))
            {
                Win();
                return;
            }

            // ⚠ Drawn on the FINAL state of the tick (§4.4): the snake has just grown, and an apple
            // placed before that growth could land on the cell the head occupies. It is placed on the
            // very tick the old one was eaten — no frame is displayed without an apple, an empty grid
            // reading as a bug rather than as a transition.
            _apple = SnakeSnack.Rules.Apple.Draw(_grid, _snake.Segments, _random);
            _view.DrawApple(_apple);
        }

        /// <summary>Routes a rejection to its visual channel (<c>docs/ART.md</c> §5.2).</summary>
        private void SignalRejection(RejectionReason reason, Direction direction)
        {
            switch (RejectionRouting.Channel(reason))
            {
                case FeedbackChannel.Pictogram:
                    // ⚠ The direction is updated even when the notification only extends: the player
                    // must see the rejection they just pressed, not the previous one. Only the
                    // appearance animation is not restarted (ART §5.5).
                    _rejectedDirection = direction;
                    _pictogramFeedback.Notify(Time.unscaledTimeAsDouble);
                    break;

                case FeedbackChannel.PauseText:
                    _pauseTextFeedback.Notify(Time.unscaledTimeAsDouble);
                    break;

                default:
                    // Duplicate: no feedback, and the silence is a decision here (ART §5.3).
                    break;
            }
        }

        private void RefreshRejectionFeedback()
        {
            double now = Time.unscaledTimeAsDouble;

            if (_pictogramFeedback.IsVisible(now))
            {
                _view.ShowRejection(_snake.Head, _rejectedDirection, (float)_pictogramFeedback.Opacity(now));
            }
            else
            {
                _view.HideRejection();
            }

            _hud.ShowRejectionWhilePaused(_pauseTextFeedback.IsVisible(now));
        }

        private void TogglePause()
        {
            if (_state == GameState.Running)
            {
                Pause();
            }
            else if (_state == GameState.Paused)
            {
                _queue.Resume();
                _state = GameState.Running;
                _hud.Display(_state);
            }
        }

        private void Pause()
        {
            _queue.Pause();
            _state = GameState.Paused;
            _hud.Display(_state);

            // ⚠ The snake freezes on its cells: a segment finishing its slide under the scrim would
            // show a game still running, exactly what the pause claims not to be doing.
            _view.FreezeAnimations();

            // Accumulated time is discarded: resuming must not trigger an immediate tick.
            _accumulatedTime = 0.0;
        }

        /// <summary>
        /// Death, and the three pieces of feedback that make it readable (<c>docs/art/juicy.md</c> §6).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>The end screen is no longer shown here</b>, but after the hitstop, by
        /// <see cref="AdvanceDeathFeedback"/>: the scrim laid down on the very frame of impact would
        /// cover the offending cell before the eye could see it — the flash would exist in the code
        /// without ever existing for the player.
        /// </remarks>
        private void Die(MoveResult result, Cell headBefore, Direction attemptedDirection)
        {
            _queue.Die();
            _state = GameState.Dead;

            // The snake freezes: no more sliding after the last tick.
            _view.DrawSnake(_snake.Segments, false);

            // A bite has a guilty cell INSIDE the grid — the one that was bitten. A wall does not:
            // the targeted cell is outside the grid, therefore outside the playfield, and the flash
            // would show beyond the border. We then point at the cell the snake tried to leave.
            Cell offending = result == MoveResult.BitSelf
                ? Directions.Advance(headBefore, attemptedDirection)
                : headBefore;

            _view.FlashCell(offending);

            _deathFeedbackStart = Time.unscaledTimeAsDouble;
            _endScreenShown = false;
        }

        /// <summary>
        /// Runs the hitstop and the micro-zoom after a death, then lets the end screen appear.
        /// </summary>
        /// <remarks>
        /// ⚠ Everything is on <c>unscaledTime</c>, like the rejection feedback: these must run while
        /// the game is no longer running.
        ///
        /// <para>⚠ The zoom acts on <c>orthographicSize</c>, never on the camera's position
        /// (<c>juicy.md</c> §10): a lateral move would shift the cells at the precise moment the
        /// player is looking for the one that killed them.</para>
        /// </remarks>
        private void AdvanceDeathFeedback()
        {
            if (_deathFeedbackStart <= double.NegativeInfinity)
            {
                return;
            }

            double now = Time.unscaledTimeAsDouble;

            if (_camera != null)
            {
                double t = Easing.Progress(_deathFeedbackStart, MicroZoomDuration, now);
                _camera.orthographicSize = (float)(ReferenceCameraSize - (MicroZoomAmplitude * Easing.Pulse(t)));

                if (t >= 1.0)
                {
                    // Put back exactly: a camera left at 359.98 would shift the whole reference frame
                    // of GDD §4.3, with nothing to report it.
                    _camera.orthographicSize = (float)ReferenceCameraSize;
                }
            }

            if (!_endScreenShown && now - _deathFeedbackStart >= HitstopDuration)
            {
                _hud.Display(_state);
                _endScreenShown = true;
            }

            if (now - _deathFeedbackStart >= MicroZoomDuration && _endScreenShown)
            {
                _deathFeedbackStart = double.NegativeInfinity;
            }
        }

        /// <summary>
        /// Grid full (GDD §4.4). Out of human reach, written all the same.
        /// </summary>
        /// <remarks>
        /// ⚠ The apple is <b>hidden</b> here, and this is the only place in the game where it
        /// disappears: there is not one free cell left to put it on. Leaving it visible would show an
        /// apple sitting on the snake.
        /// </remarks>
        private void Win()
        {
            // Same purge as death: the game is over, no turn pressed afterwards must survive into the
            // next game.
            _queue.Die();
            _state = GameState.Won;
            _hud.Display(_state);
            _view.HideApple();
        }

        private void NewGame()
        {
            StartPose pose = _grid.StartingPose();

            _snake.Reset(pose.Segments);
            _queue.Reset(pose.Orientation);

            // The score restarts from zero, the best score survives — and "best beaten" goes back to
            // false, without which the line would stay on the end screen of the next game.
            _score.NewGame();

            _accumulatedTime = 0.0;
            _state = GameState.Waiting;

            _pictogramFeedback.Clear();
            _pauseTextFeedback.Clear();

            // ⚠ The previous death's feedback is purged HERE, without which `_endScreenShown` left
            // false would keep inputs blocked for the whole next game — a game that stops responding,
            // and whose cause would be hunted on the keyboard side.
            _deathFeedbackStart = double.NegativeInfinity;
            _endScreenShown = true;

            if (_camera != null)
            {
                _camera.orthographicSize = (float)ReferenceCameraSize;
            }

            SeedRandomness();

            // ⚠ The apple is placed BEFORE the first press (§4.4): the start is standing, the player
            // looks at the screen and picks a direction. With no apple to aim at, that choice would be
            // blind.
            _apple = SnakeSnack.Rules.Apple.Draw(_grid, _snake.Segments, _random);

            // Immediate placement: a snake sliding towards its starting position would suggest the
            // game began before the player saw it.
            _view.DrawSnake(_snake.Segments, false);

            // ⚠ The SAME direction twice: the starting snake is still but ORIENTED (§4.3), so its face
            // must already look east. No turn is signalled — the sign is zero — and nothing tilts.
            _view.SignalDirection(pose.Orientation, pose.Orientation);
            _view.DrawApple(_apple);
            _view.HideRejection();

            // ⚠ The numbers BEFORE the state: Display() composes the end summary from the last
            // numbers received. In the other order, the death screen would carry the previous game's
            // score for one frame.
            _hud.ShowScore(_score.Points, _score.Best, _score.BestBeaten);
            _hud.Display(_state);
            _hud.ShowRejectionWhilePaused(false);
        }

        /// <summary>
        /// Gives the game its apple generator (GDD §4.4, "Reproducible randomness").
        /// </summary>
        /// <remarks>
        /// Seed fixed in the tuning JSON: <b>every</b> game replays the same apple sequence — that is
        /// bench mode, not a game mode. Seed left at zero: every game gets a fresh one.
        ///
        /// <para>⚠ <b>Logged on every game, including when it comes from the session</b>: a seed not
        /// written down anywhere makes the game unreplayable, and it is precisely the remarkable game
        /// — the one worth replaying — that would be lost.</para>
        /// </remarks>
        private void SeedRandomness()
        {
            ulong seed = _settings.seed != GameSettings.ClockSeed
                ? (ulong)_settings.seed
                : _sessionSeeds.Next();

            _random = new RandomSource(seed);
            Debug.Log("[apple] seed of the game: " + seed);
        }
    }
}
