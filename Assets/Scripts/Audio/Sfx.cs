using System;

namespace SnakeSnack.Audio
{
    /// <summary>
    /// Every sound the game can make. Adding a value here is what declares a sound to exist; the
    /// table in <see cref="SfxCatalog"/> is what gives it a file.
    /// </summary>
    public enum Sfx
    {
        /// <summary>The snake bites an apple.</summary>
        Bite,

        /// <summary>The snake hits a wall or its own body.</summary>
        Death,

        /// <summary>The menu cursor moves from one entry to the next.</summary>
        MenuMove,

        /// <summary>A menu entry is chosen.</summary>
        MenuConfirm,
    }

    /// <summary>
    /// The single place where a sound is bound to a file (<c>docs/gdd/audio.md</c>).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>An entry missing from this table is silent, and nothing says so</b>
    /// (<c>docs/pitfalls/audio.md</c>): the game plays, the moment passes, and no error is raised.
    /// Hence <see cref="FileName"/> returning <c>null</c> rather than guessing a name, and
    /// <see cref="SfxPlayer"/> auditing the whole enum at startup: a sound declared and unbound
    /// becomes a line in the console instead of a silence nobody attributes.
    ///
    /// <para>The volumes live here rather than in the files: balancing two sounds by re-exporting
    /// them loses the original each time, and the difference stops being readable in a diff.</para>
    /// </remarks>
    public static class SfxCatalog
    {
        /// <summary>Where the clips are loaded from, under <c>Assets/Resources/</c>.</summary>
        /// <remarks>
        /// ⚠ <c>Resources/</c>, never <c>Art/</c>: these clips are loaded BY PATH, and an asset
        /// written into the wrong one of the two loads as <c>null</c> without raising anything
        /// (<c>docs/pitfalls/assets-import.md</c>).
        /// </remarks>
        public const string ResourceFolder = "Audio/";

        /// <summary>All declared sounds — what the startup audit walks.</summary>
        public static Sfx[] All()
        {
            return (Sfx[])Enum.GetValues(typeof(Sfx));
        }

        /// <summary>
        /// The file name of a sound, without extension, or <c>null</c> if it has none yet.
        /// </summary>
        public static string FileName(Sfx sound)
        {
            switch (sound)
            {
                case Sfx.Bite: return "bite";
                case Sfx.Death: return "death";
                case Sfx.MenuMove: return "menu-move";
                case Sfx.MenuConfirm: return "menu-confirm";

                // ⚠ No `default` returning a guessed name: a sound added to the enum and forgotten
                // here must come out as null, so the audit can name it. Guessing would restore
                // exactly the silence this table exists to prevent.
                default: return null;
            }
        }

        /// <summary>
        /// Per-sound volume, relative to the master volume. Balances the set without touching
        /// the files.
        /// </summary>
        public static float Volume(Sfx sound)
        {
            switch (sound)
            {
                // The bite is the game's reward: it carries, without covering the rest.
                case Sfx.Bite: return 1.0f;

                // Death is heard once per game, and it is already loud in the head.
                case Sfx.Death: return 0.9f;

                // The cursor fires on every arrow press: it has to stay under the threshold of
                // annoyance, and a menu run through quickly must not turn into a rattle.
                case Sfx.MenuMove: return 0.45f;

                case Sfx.MenuConfirm: return 0.8f;

                default: return 1.0f;
            }
        }
    }
}
