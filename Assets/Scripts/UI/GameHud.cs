using SnakeSnack.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SnakeSnack.UI
{
    /// <summary>
    /// The interface texts: state, control reminder, pause and death screens.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The HUD builds its own children at startup</b>, rather than being assembled in
    /// <c>SceneBuilder</c> with serialised references. A serialised reference to a <c>Text</c> that
    /// changed name or order comes back null <b>in the regenerated scene</b>, and the only symptom is
    /// text that does not appear — no error, no warning.
    ///
    /// <para>⚠ No Unicode arrow in these texts (<c>docs/ART.md</c> §5.7): they vanish silently on
    /// WebGL. Labels come from <see cref="UiText"/>, never written down here.</para>
    /// </remarks>
    public sealed class GameHud : MonoBehaviour
    {
        /// <summary>
        /// Margin between the screen edge and the banner numbers, in pixels of the 1280x720 frame.
        /// </summary>
        private const float SideMargin = 28f;

        /// <summary>Width reserved for a banner number ("Best 312" fits easily).</summary>
        private const float NumberWidth = 260f;

        private Text _state;
        private Text _controls;
        private Text _score;
        private Text _best;
        private Text _title;
        private Text _endSummary;
        private Text _subtitle;
        private Text _rejectionWhilePaused;
        private Image _scrim;

        /// <summary>The whole canvas, hidden in one go when the menu takes the screen (GDD §4.6).</summary>
        private GameObject _canvas;

        // The game's only two weights (ART §2.2). SemiBold carries secondary, permanent text,
        // ExtraBold the headings and numbers — there is no Regular: at these sizes, on a downscaled
        // WebGL render, a thin stroke of a round typeface disappears before it can be read.
        private Font _bodyFont;
        private Font _headingFont;

        // Last numbers received. ⚠ The end summary is composed AT DEATH from them: the end screen
        // must not depend on the order in which gameplay calls this component's two public methods.
        private int _points;
        private int _bestScore;
        private bool _bestBeaten;

        // --- Number bumps (docs/art/juicy.md §5 and §8) ----------------------------------
        //
        // ⚠ On unscaledTime: the best-score bump replays when the end screen opens, so at a moment
        // when the game is no longer running.

        private const double ScoreBumpDuration = 0.160;
        private const double BestBumpDuration = 0.220;
        private const double ScoreBumpAmount = 0.18;
        private const double BestBumpAmount = 0.30;

        private double _scoreBumpStart = double.NegativeInfinity;
        private double _bestBumpStart = double.NegativeInfinity;
        private double _summaryBumpStart = double.NegativeInfinity;

        /// <summary>
        /// True as long as the end screen stays open — it is its <b>opening</b> that bumps the
        /// summary, never a refresh of the same screen (§8: "replays <i>once</i>").
        /// </summary>
        private bool _endScreenOpen;

        private void Awake()
        {
            Build();
        }

        private void Build()
        {
            _bodyFont = UiFonts.Load(UiFonts.Body);
            _headingFont = UiFonts.Load(UiFonts.Headings);

            // Below the menu (200) and below the build stamp (1000), above the world: the HUD must
            // hide neither the menu nor the stamp that identifies the version on a screenshot.
            GameObject canvasGo = UiFactory.Canvas(transform, "HUD Canvas", 100).gameObject;
            _canvas = canvasGo;

            // ⚠ The sizes below are those of docs/ART.md §2.3, raised by two points. The CanvasScaler
            // above expresses them in pixels of the 1280x720 frame: on the smaller window of an itch
            // page they SHRINK proportionally, which is the opposite of a safety margin. Absolute
            // floor: 18 px here, below which downscaling makes text unreadable before the typeface's
            // weight even comes into play.
            _scrim = BuildScrim(canvasGo.transform);

            _state = BuildText(canvasGo.transform, "State", _headingFont, 24, TextAnchor.MiddleCenter,
                UiPalette.HudText, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 40f));

            // ⚠ 14 px from the bottom, not 10: the pivot is at the centre of a 24 px tall box, so at
            // 10 px the BOTTOM of the box fell 2 px BELOW the screen and the descenders of "g", "p",
            // "q" were cut off (BUG-002 of 2026-08-28, measured on a build). 14 px fits the whole box
            // with 2 px to spare. This is NOT the heart of the matter: there is no bottom margin under
            // the playfield at all (docs/gdd/grid.md), and that trade-off is still open.
            _controls = BuildText(canvasGo.transform, "Controls", _bodyFont, 18, TextAnchor.LowerCenter,
                UiPalette.SecondaryText, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(1100f, 24f));
            _controls.text = UiText.ControlsReminder;

            // The reminder is the one label of the HUD that names a control: it is rewritten if a
            // touchscreen turns up after the HUD was built (see RefreshControlLabels).

            // Score on the left, best score on the right, in the top banner: outside the playfield
            // (GDD §4.3), and shown AT ALL TIMES (§4.5). A goal you only discover once you have lost
            // cannot be aimed at — it is the best score read during play that turns a restart into
            // "beat 14". The rect is offset by half its width because the pivot is at the centre: the
            // text edge then falls at SideMargin from the screen edge.
            _score = BuildText(canvasGo.transform, "Score", _headingFont, 24, TextAnchor.MiddleLeft,
                UiPalette.HudText, new Vector2(0f, 1f),
                new Vector2(SideMargin + (NumberWidth / 2f), -30f), new Vector2(NumberWidth, 40f));

            // Best score in secondary text: same place, same size, but it is the current score the
            // player follows during a run. The hierarchy reads without reading either label.
            _best = BuildText(canvasGo.transform, "Best", _headingFont, 24, TextAnchor.MiddleRight,
                UiPalette.SecondaryText, new Vector2(1f, 1f),
                new Vector2(-SideMargin - (NumberWidth / 2f), -30f), new Vector2(NumberWidth, 40f));

            ShowScore(0, 0, false);

            _title = BuildText(canvasGo.transform, "Title", _headingFont, 56, TextAnchor.MiddleCenter,
                UiPalette.HudText, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(900f, 80f));

            // Between the title and the restart line: GDD §2 wants score and best "shown right there"
            // at death. They are already in the banner, but sending the eye all the way up at the
            // moment of deciding to play again is losing the player.
            _endSummary = BuildText(canvasGo.transform, "EndSummary", _headingFont, 26, TextAnchor.MiddleCenter,
                UiPalette.HudText, new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), new Vector2(900f, 34f));

            _subtitle = BuildText(canvasGo.transform, "Subtitle", _bodyFont, 22, TextAnchor.MiddleCenter,
                UiPalette.SecondaryText, new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(900f, 30f));

            // ⚠ Under the subtitle, and not in place of the summary: the two never appear together
            // (one on pause, the other at the end), but two texts fighting over the same position end
            // up overlapping the day that exclusion stops being true.
            _rejectionWhilePaused = BuildText(canvasGo.transform, "RejectionWhilePaused", _bodyFont, 20, TextAnchor.MiddleCenter,
                UiPalette.HudText, new Vector2(0.5f, 0.5f), new Vector2(0f, -95f), new Vector2(900f, 28f));
            _rejectionWhilePaused.text = UiText.RejectionWhilePaused;
            _rejectionWhilePaused.gameObject.SetActive(false);
        }

        private static Image BuildScrim(Transform parent)
        {
            Image scrim = UiFactory.Scrim(parent, "Scrim", UiPalette.PauseScrim);
            scrim.gameObject.SetActive(false);
            return scrim;
        }

        private static Text BuildText(
            Transform parent, string name, Font font, int size, TextAnchor alignment,
            Color colour, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            return UiFactory.Text(parent, name, font, size, alignment, colour, anchor, position, dimensions);
        }

        /// <summary>
        /// Shows or hides the whole HUD.
        /// </summary>
        /// <remarks>
        /// ⚠ The whole canvas, not the texts one by one: the menu (GDD §4.6) takes the entire screen,
        /// and a single forgotten text — the control reminder, the score stamp — would end up sitting
        /// on top of the game title.
        /// </remarks>
        public void Show(bool visible)
        {
            _canvas.SetActive(visible);
        }

        /// <summary>Puts the interface into the state of the game.</summary>
        public void Display(GameState state)
        {
            // ⚠ Read BEFORE the switch: it is the transition to the end screen that triggers the
            // summary bump (§8), and `End(...)` below will already have rewritten everything.
            bool endScreen = state == GameState.Dead || state == GameState.Won;
            bool endJustOpened = endScreen && !_endScreenOpen;
            _endScreenOpen = endScreen;

            switch (state)
            {
                case GameState.Waiting:
                    _state.text = UiText.StateWaiting;
                    End(false, string.Empty, string.Empty);
                    break;

                case GameState.Running:
                    _state.text = UiText.StateRunning;
                    End(false, string.Empty, string.Empty);
                    break;

                case GameState.Paused:
                    _state.text = UiText.StatePaused;
                    End(true, UiText.PauseTitle, UiText.PauseSubtitle);
                    break;

                case GameState.Won:
                    // ⚠ Written out explicitly: without it, a win falls into the `default` and the
                    // player who just filled the grid reads "GAME OVER". Nothing would report it.
                    _state.text = UiText.StateWon;
                    End(true, UiText.WinTitle, UiText.WinSubtitle, Summary());
                    break;

                default:
                    _state.text = UiText.StateDead;
                    End(true, UiText.DeathTitle, UiText.DeathSubtitle, Summary());
                    break;
            }

            if (endJustOpened && _bestBeaten)
            {
                // The game's only proud moment (§8): the "New best" line bumps once, on opening.
                // Without a beaten best score the summary is only a reminder of numbers — bumping it
                // would say "well done" to somebody who just lost at 3 points.
                _summaryBumpStart = Time.unscaledTimeAsDouble;
            }

            if (state != GameState.Paused)
            {
                // The rejection message belongs to the pause screen: letting it live past the resume
                // would show it over the running game, unrelated to any key.
                ShowRejectionWhilePaused(false);
            }
        }

        /// <summary>
        /// The two banner numbers (GDD §4.5).
        /// </summary>
        /// <param name="bestBeaten">
        /// True when the current game has passed the best score it found when it started. Used only
        /// by the end summary — during a run, two equal numbers explain themselves, since they have
        /// been seen rising together.
        /// </param>
        public void ShowScore(int points, int best, bool bestBeaten)
        {
            // ⚠ Compared BEFORE writing the fields: it is the change that triggers the bump, not the
            // value. Without this comparison, the plain refresh of a new game would make both numbers
            // jump when nothing has been won.
            bool scoreRose = points > _points;
            bool bestJustBeaten = bestBeaten && !_bestBeaten;

            _points = points;
            _bestScore = best;
            _bestBeaten = bestBeaten;

            _score.text = UiText.ScoreLine(points);
            _best.text = UiText.BestLine(best);

            if (scoreRose)
            {
                _scoreBumpStart = Time.unscaledTimeAsDouble;
            }

            if (bestJustBeaten)
            {
                _bestBumpStart = Time.unscaledTimeAsDouble;
            }
        }

        /// <summary>
        /// Makes the numbers that just went up breathe (<c>docs/art/juicy.md</c> §5, §8).
        /// </summary>
        /// <remarks>
        /// ⚠ The scale is set back to <b>exactly</b> 1 at the end of the envelope: a `Text` left at
        /// 1.002 would stay imperceptibly bigger for the rest of the session, and nobody would tie
        /// that offset back to a 160 ms animation.
        ///
        /// <para>⚠ No colour change: the bump says "this went up", it borrows neither
        /// <c>Pictogram</c> (reserved for rejection) nor <c>Apple</c> (reserved for food) —
        /// "one colour, one role" (<c>docs/art/palette.md</c> §1.2).</para>
        /// </remarks>
        private void Update()
        {
            double now = Time.unscaledTimeAsDouble;

            _scoreBumpStart = ApplyBump(_score, _scoreBumpStart, ScoreBumpDuration, ScoreBumpAmount, now);
            _bestBumpStart = ApplyBump(_best, _bestBumpStart, BestBumpDuration, BestBumpAmount, now);

            // The same bump as the banner's best score, replayed once on the "New best" line of the
            // end screen (§8): it is the same news, announced at the same rhythm.
            _summaryBumpStart = ApplyBump(_endSummary, _summaryBumpStart, BestBumpDuration, BestBumpAmount, now);
        }

        /// <summary>Returns the new envelope start: cleared once the bump is over.</summary>
        private static double ApplyBump(Text target, double start, double duration, double amount, double now)
        {
            if (start <= double.NegativeInfinity || target == null)
            {
                return start;
            }

            double t = Rules.Easing.Progress(start, duration, now);
            float factor = (float)(1.0 + (amount * Rules.Easing.Pulse(t)));
            target.transform.localScale = new Vector3(factor, factor, 1f);

            if (t < 1.0)
            {
                return start;
            }

            target.transform.localScale = Vector3.one;
            return double.NegativeInfinity;
        }

        /// <summary>The "key ignored" line of the pause screen (ART §5.4).</summary>
        public void ShowRejectionWhilePaused(bool visible)
        {
            _rejectionWhilePaused.gameObject.SetActive(visible);
        }

        /// <summary>Rewrites the labels that name a control (GDD §3, touch).</summary>
        public void RefreshControlLabels()
        {
            if (_controls != null)
            {
                _controls.text = UiText.ControlsReminder;
            }
        }

        private string Summary()
        {
            return UiText.EndSummary(_points, _bestScore, _bestBeaten);
        }

        private void End(bool visible, string title, string subtitle)
        {
            End(visible, title, subtitle, string.Empty);
        }

        private void End(bool visible, string title, string subtitle, string summary)
        {
            _scrim.gameObject.SetActive(visible);
            _title.text = title;
            _subtitle.text = subtitle;
            _endSummary.text = summary;
        }
    }
}
