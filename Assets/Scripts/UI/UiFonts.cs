using UnityEngine;

namespace SnakeSnack.UI
{
    /// <summary>
    /// The game's two weights (<c>docs/ART.md</c> §2), loaded from <c>Resources/Fonts/</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A missing font raises nothing</b>: <c>Resources.Load</c> returns <c>null</c>, the
    /// <c>Text</c> keeps a null font and draws no pixel — no white box, no exception, an empty
    /// screen. Hence the explicit error AND the fallback to the built-in font: ugly text is seen,
    /// missing text is hunted for an hour.
    ///
    /// <para>Both <c>.ttf</c> files are <b>produced</b> by <c>tools/generate_fonts.py</c>:
    /// google/fonts only publishes Nunito as a variable file, and the script instantiates
    /// <c>wght=600</c> and <c>wght=800</c> from it. They are not re-downloaded by hand.</para>
    ///
    /// <para>This loading lives here rather than in every screen, because the fallback is a single
    /// decision: two copies would end up drifting, and the second one would forget to log.</para>
    /// </remarks>
    public static class UiFonts
    {
        /// <summary>Secondary and body text: Nunito SemiBold.</summary>
        public const string Body = "Nunito-SemiBold";

        /// <summary>Headings and numbers: Nunito ExtraBold. There is no Regular (ART §2.2).</summary>
        public const string Headings = "Nunito-ExtraBold";

        /// <summary>Loads a weight by its file name, without extension.</summary>
        public static Font Load(string name)
        {
            Font font = Resources.Load<Font>("Fonts/" + name);
            if (font != null)
            {
                return font;
            }

            Debug.LogError("Font not found: Resources/Fonts/" + name
                           + " — re-run \"py tools/generate_fonts.py\". Falling back to the built-in font.");
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
