using System.Globalization;
using SnakeSnack.Rules;

namespace SnakeSnack.UI
{
    /// <summary>
    /// Every string the game displays, in one place.
    /// </summary>
    /// <remarks>
    /// The game is <b>not localised</b>: there is no translation system, and the GDD does not ask for
    /// one. The strings live here rather than scattered through the code so that the day localisation
    /// arrives, there is <b>one</b> file to pick up and not fifteen literals spread across
    /// <c>MonoBehaviour</c>s.
    ///
    /// <para>⚠ <b>ASCII only.</b> Explicit bans from <c>docs/ART.md</c> §5.7 and
    /// <c>docs/pitfalls/fonts-text.md</c>: Unicode arrows (← → ↑ ↓) are dropped <b>silently</b> by a
    /// WebGL build, with no white box and no warning. Every directional symbol is a <b>sprite</b>,
    /// never a character. The em dash (—) is banned for the same reason: a plain hyphen replaces it
    /// everywhere.</para>
    ///
    /// <para>⚠ <b>WASD, and that is the physical layout the code binds</b>: <c>Key.W</c> /
    /// <c>Key.A</c> / <c>Key.S</c> / <c>Key.D</c> name positions on a QWERTY keyboard, so the label
    /// below is literally true for a QWERTY player. On an AZERTY keyboard those same positions are
    /// the keys printed Z, Q, S, D — the arrows are always there for them, and they are announced
    /// first.</para>
    /// </remarks>
    public static class UiText
    {
        /// <summary>
        /// True when the player has fingers rather than keys (GDD §3, touch).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Every label naming a key has a touch counterpart, and this flag is what chooses.</b>
        /// A game telling a phone player to "press Esc" describes a device they do not have: the
        /// instruction is not merely useless, it is the first sentence they read, and it says the
        /// game was not meant for them. <c>docs/pitfalls/interface.md</c> already carries the general
        /// form of the trap — a capability that does not announce its control does not exist.
        ///
        /// <para>Set once at startup by <see cref="SnakeSnack.Gameplay.SnakeGame"/>, from whether a
        /// touchscreen exists. Not read from the platform here: <c>UiText</c> holds words, and a
        /// class that decided by itself what device is in front of it could no longer be exercised
        /// from a test.</para>
        /// </remarks>
        public static bool Touch { get; set; }

        // --- Main menu (GDD §4.6) --------------------------------------------------------

        /// <summary>Game title, on the main menu. The same words as the itch page.</summary>
        public const string GameTitle = "SNAKE SNACK";

        /// <summary>
        /// The tagline under the title: the pitch of GDD §1 reduced to what reads in one second. It
        /// states the <b>consequence</b> of eating, not the control — the consequence is what makes
        /// you understand why the run always ends badly.
        /// </summary>
        public const string MenuTagline = "It grows with every bite.";

        /// <summary>Key reminder for the menu, at the foot of the screen ("invisible reads as non-existent").</summary>
        public const string MenuFooterKeyboard = "Arrows or WASD to choose   -   Enter or Space to confirm";

        /// <summary>The same, for a player who has no keyboard (GDD §3, touch).</summary>
        public const string MenuFooterTouch = "Tap an entry to choose it";

        /// <inheritdoc cref="MenuFooterKeyboard"/>
        public static string MenuFooter
        {
            get { return Touch ? MenuFooterTouch : MenuFooterKeyboard; }
        }

        /// <summary>The label of a menu entry (GDD §4.6).</summary>
        /// <remarks>
        /// ⚠ An exhaustive <c>switch</c> with a default that <b>shows</b>: an entry added to
        /// <see cref="MenuEntry"/> and forgotten here would otherwise appear as a blank line, and a
        /// blank line in a menu reads as a display defect, not as missing text.
        /// </remarks>
        public static string EntryLabel(MenuEntry entry)
        {
            switch (entry)
            {
                case MenuEntry.Play: return "Play";
                case MenuEntry.HowToPlay: return "How to play";
                case MenuEntry.Credits: return "Credits";
                case MenuEntry.Quit: return "Quit";
                default: return "(entry with no label: " + entry + ")";
            }
        }

        /// <summary>Title of the controls panel.</summary>
        public const string HowToPlayTitle = "HOW TO PLAY";

        /// <summary>
        /// The controls panel: the keys of GDD §3, then the two rules that kill.
        /// </summary>
        /// <remarks>
        /// ⚠ No Unicode arrow (§5.7): "Arrows" is spelled out. And the reversal rejection is announced
        /// <b>here</b> rather than discovered in play — a player whose press is ignored with no
        /// explanation concludes the game missed their key.
        /// </remarks>
        public const string HowToPlayBody =
            "Arrows or WASD: steer the snake\n" +
            "Esc: pause\n" +
            "Space: start a new run\n" +
            "\n" +
            "The snake moves on its own, one cell at a time.\n" +
            "Every apple makes it one segment longer and is worth one point.\n" +
            "\n" +
            "The edges kill: they do not wrap around, and biting your own body kills too.\n" +
            "An instant reversal is refused: a barred chevron says so.";

        /// <summary>Title of the credits panel.</summary>
        public const string CreditsTitle = "CREDITS";

        /// <summary>
        /// The credits shown in game.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>This text is not decorative: it is a licence obligation.</b> Nunito's SIL OFL 1.1
        /// requires attribution, and <c>docs/CREDITS.md</c> holds the reference list. Any third-party
        /// asset added to the game is added in both places, in the same commit.
        /// </remarks>
        public const string CreditsBody =
            "Snake Snack - a game by Drangoht.\n" +
            "\n" +
            "Font: Nunito, by Vernon Adams, Cyreal and Jacques Le Bailly.\n" +
            "SIL Open Font License 1.1.\n" +
            "\n" +
            "Illustration and interface made for this game.\n" +
            "Engine: Unity.";

        /// <summary>Footer of the menu panels.</summary>
        public const string PanelBackKeyboard = "Esc to go back";

        /// <summary>The same, for a player who has no keyboard (GDD §3, touch).</summary>
        public const string PanelBackTouch = "Tap to go back";

        /// <inheritdoc cref="PanelBackKeyboard"/>
        public static string PanelBack
        {
            get { return Touch ? PanelBackTouch : PanelBackKeyboard; }
        }

        // --- Game ------------------------------------------------------------------------

        /// <summary>
        /// Banner, before the first press. §4.1 starts the game on the first applicable direction:
        /// the player must know they are being waited for, otherwise they think it has frozen.
        /// </summary>
        public const string StateWaitingKeyboard = "Press a direction to start";

        /// <summary>The same, for a player who has no keyboard (GDD §3, touch).</summary>
        public const string StateWaitingTouch = "Swipe, or press a direction, to start";

        /// <inheritdoc cref="StateWaitingKeyboard"/>
        public static string StateWaiting
        {
            get { return Touch ? StateWaitingTouch : StateWaitingKeyboard; }
        }

        /// <summary>Banner, game running. Empty: nothing to say while all is well.</summary>
        public const string StateRunning = "";

        /// <summary>Banner, game paused.</summary>
        public const string StatePaused = "Paused";

        /// <summary>Banner, after death.</summary>
        public const string StateDead = "Game over";

        /// <summary>
        /// Permanent control reminder (GDD §3, and the "invisible reads as non-existent" trap of
        /// <c>docs/pitfalls/interface.md</c>: a capability that does not announce its key does not
        /// exist for the player).
        /// </summary>
        public const string ControlsReminderKeyboard = "Arrows or WASD: steer   -   Esc: pause   -   Space: restart";

        /// <summary>The same, for a player who has no keyboard (GDD §3, touch).</summary>
        public const string ControlsReminderTouch = "Swipe or use the pad to steer   -   pause button, top left";

        /// <inheritdoc cref="ControlsReminderKeyboard"/>
        public static string ControlsReminder
        {
            get { return Touch ? ControlsReminderTouch : ControlsReminderKeyboard; }
        }

        /// <summary>Title of the pause screen.</summary>
        public const string PauseTitle = "PAUSED";

        /// <summary>
        /// Subtitle of the pause screen.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Backspace is announced here, and nowhere else</b>: it is the game's only path back to
        /// the menu from a running game (GDD §4.6). A key that does not announce itself does not
        /// exist for the player (<c>docs/pitfalls/interface.md</c>) — and this one can only announce
        /// itself on the pause screen, since that is the only place it acts.
        /// </remarks>
        public const string PauseSubtitleKeyboard = "Esc to resume   -   Backspace for the menu";

        /// <summary>The same, for a player who has no keyboard (GDD §3, touch).</summary>
        public const string PauseSubtitleTouch = "Tap to resume   -   pause button for the menu";

        /// <inheritdoc cref="PauseSubtitleKeyboard"/>
        public static string PauseSubtitle
        {
            get { return Touch ? PauseSubtitleTouch : PauseSubtitleKeyboard; }
        }

        /// <summary>
        /// The rejection line of the pause screen (<c>docs/ART.md</c> §5.4, word for word).
        /// </summary>
        public const string RejectionWhilePausedKeyboard = "Key ignored - the game is paused";

        /// <summary>The same, for a player who has no keyboard (GDD §3, touch).</summary>
        public const string RejectionWhilePausedTouch = "Ignored - the game is paused";

        /// <inheritdoc cref="RejectionWhilePausedKeyboard"/>
        public static string RejectionWhilePaused
        {
            get { return Touch ? RejectionWhilePausedTouch : RejectionWhilePausedKeyboard; }
        }

        /// <summary>
        /// Death message. GDD §2 wants "score and best shown right there": they are, through the
        /// summary from <see cref="EndSummary"/> placed just under this title.
        /// </summary>
        public const string DeathTitle = "GAME OVER";

        /// <summary>One-key restart, zero waiting (GDD §2).</summary>
        public const string DeathSubtitleKeyboard = "Space to play again   -   Esc for the menu";

        /// <summary>The same, for a player who has no keyboard (GDD §3, touch).</summary>
        public const string DeathSubtitleTouch = "Tap to play again   -   pause button for the menu";

        /// <inheritdoc cref="DeathSubtitleKeyboard"/>
        public static string DeathSubtitle
        {
            get { return Touch ? DeathSubtitleTouch : DeathSubtitleKeyboard; }
        }

        /// <summary>Banner, grid filled (GDD §4.4).</summary>
        public const string StateWon = "Grid filled";

        /// <summary>
        /// Title of the win. A label <b>distinct</b> from the death one (§4.4): same screen, same
        /// place, same restart — but nothing must suggest a perfect run ended badly.
        /// </summary>
        public const string WinTitle = "YOU WIN";

        /// <summary>Subtitle of the win: the snake fills the whole grid.</summary>
        public const string WinSubtitleKeyboard = "Not one free cell left - Space to play again   -   Esc for the menu";

        /// <summary>The same, for a player who has no keyboard (GDD §3, touch).</summary>
        public const string WinSubtitleTouch = "Not one free cell left - tap to play again";

        /// <inheritdoc cref="WinSubtitleKeyboard"/>
        public static string WinSubtitle
        {
            get { return Touch ? WinSubtitleTouch : WinSubtitleKeyboard; }
        }

        /// <summary>Banner, score of the current game (GDD §4.5).</summary>
        public static string ScoreLine(int points)
        {
            return "Score " + points.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Banner, best score of every game (GDD §4.5).</summary>
        public static string BestLine(int best)
        {
            return "Best " + best.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The summary on the end screen, death or win.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>The "new best" line is not an ornament</b> (GDD §4.5): when the best score has just
        /// been beaten, score and best carry the <b>same number</b>, and two identical values side by
        /// side read as a display defect. Without that sentence, the game's only rewarding moment
        /// looks like a bug. We then show a single number: repeating it under two labels would be
        /// exactly the confusion we are trying to lift.
        /// </remarks>
        public static string EndSummary(int points, int best, bool bestBeaten)
        {
            if (bestBeaten)
            {
                return "New best: " + points.ToString(CultureInfo.InvariantCulture);
            }

            return ScoreLine(points) + "   -   " + BestLine(best);
        }
    }
}
