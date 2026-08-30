using System;
using System.Collections.Generic;

namespace SnakeSnack.Rules
{
    /// <summary>The main menu entries (GDD §4.6), in display order.</summary>
    /// <remarks>
    /// ⚠ The order of this enum <b>is</b> the order on screen: <see cref="MainMenu.Entries"/> only
    /// returns a filtered subset of it. "Play" first because that is what almost every visitor to an
    /// itch page does; "Quit" last because an entry that closes the game must never sit under the
    /// cursor at the moment someone hits Enter out of reflex.
    /// </remarks>
    public enum MenuEntry
    {
        /// <summary>Starts a game: the menu fades out, the snake is laid down, the game waits for a direction.</summary>
        Play,

        /// <summary>The panel of controls and of the two rules that kill (GDD §3).</summary>
        HowToPlay,

        /// <summary>The credits panel — Nunito's SIL OFL requires attribution (docs/CREDITS.md).</summary>
        Credits,

        /// <summary>
        /// Closes the game. ⚠ <b>Absent from the web build</b>: <c>Application.Quit()</c> does
        /// nothing there, and a dead button costs the player more than no button at all (see
        /// <see cref="MainMenu.Entries"/>).
        /// </summary>
        Quit
    }

    /// <summary>
    /// The composition and navigation of the main menu — engine-free, therefore testable (GDD §4.6).
    /// </summary>
    public static class MainMenu
    {
        /// <summary>
        /// The entries actually displayed.
        /// </summary>
        /// <param name="quitAvailable">
        /// False as soon as the platform cannot close the application: that is the case on
        /// <b>WebGL</b>, where <c>Application.Quit()</c> is a no-op call. Deciding it here rather
        /// than in the UI allows both compositions to be tested without producing two builds.
        /// </param>
        public static IReadOnlyList<MenuEntry> Entries(bool quitAvailable)
        {
            var entries = new List<MenuEntry>(4) { MenuEntry.Play, MenuEntry.HowToPlay, MenuEntry.Credits };

            if (quitAvailable)
            {
                entries.Add(MenuEntry.Quit);
            }

            return entries;
        }

        /// <summary>
        /// Selected index after a directional press, with <b>wrap-around</b>.
        /// </summary>
        /// <remarks>
        /// Wrapping is not a convenience: on three or four entries it puts "Quit" one press away
        /// from "Play", and it removes the dead moment where you hammer an arrow against a silent
        /// stop. The menu has no rejection feedback (the game's own, ART §5, is reserved for
        /// directions rejected <i>in play</i>): a stop there would therefore be indistinguishable
        /// from a key that was not registered.
        ///
        /// <para>⚠ East and West move nothing and say so (<c>false</c>): the list is vertical.
        /// Accepting them would move the cursor on a key the player pressed in order to turn — a
        /// reflex that a snake game installs precisely.</para>
        /// </remarks>
        /// <returns>True if the index changed.</returns>
        public static bool Move(int index, int count, Direction direction, out int newIndex)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count), count, "A menu with no entry cannot be navigated: the composition is at fault, not the key.");
            }

            int step;
            switch (direction)
            {
                case Direction.North: step = -1; break;
                case Direction.South: step = 1; break;
                default:
                    newIndex = Clamp(index, count);
                    return false;
            }

            // C#'s modulo returns a negative remainder for a negative dividend: without the
            // "+ count", going up from the first entry would give -1 and make the display throw.
            newIndex = ((Clamp(index, count) + step) % count + count) % count;
            return newIndex != index;
        }

        /// <summary>
        /// Brings an index back within bounds.
        /// </summary>
        /// <remarks>
        /// ⚠ Genuinely useful: the composition differs between desktop and web ("Quit"), and an
        /// index remembered over four entries applied to a list of three would designate an entry
        /// that does not exist. Rather than throw, we fall back to the last one — a menu that
        /// crashes at the startup of a web build is a game that does not start.
        /// </remarks>
        public static int Clamp(int index, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "A menu with no entry has no valid index.");
            }

            if (index < 0)
            {
                return 0;
            }

            return index >= count ? count - 1 : index;
        }
    }
}
