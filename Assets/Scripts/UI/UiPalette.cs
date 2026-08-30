using UnityEngine;

namespace SnakeSnack.UI
{
    /// <summary>
    /// ⚠ <b>THE ONLY PLACE IN THE REPOSITORY WHERE A COLOUR IS WRITTEN DOWN.</b> Twelve named roles,
    /// ruled on by <c>docs/ART.md</c> §1 (detail and contrast proofs: <c>docs/art/palette.md</c>).
    /// </summary>
    /// <remarks>
    /// <para>A sprite, a shader, an <c>Image</c> or a <c>Text</c> references the <b>role</b>, never a
    /// copied <c>#RRGGBB</c>: that is what allows the whole visual identity to be retouched without
    /// re-reading a single caller.</para>
    ///
    /// <para>⚠ <b>The project is in Gamma colour space</b> (<c>ProjectSettings.asset</c>:
    /// <c>m_ActiveColorSpace: 0</c>): bytes are laid down as-is, with no linear reconversion. If the
    /// project ever moves to Linear, these values must be reopened — not "fixed" pre-emptively.</para>
    ///
    /// <para>⚠ <b>Never information carried by colour alone</b> (ART §4): everything distinguished
    /// here is ALSO distinguished by shape or position (the head is bigger than the body, the apple
    /// is a diamond smaller than a cell, the pictogram is barred, the playfield edge is a continuous
    /// line). The weakest luminance pair, apple against body (1.07 : 1), holds by shape ALONE — see
    /// <c>docs/art/palette.md</c> §1.5.</para>
    /// </remarks>
    public static class UiPalette
    {
        /// <summary>Frame background, outside the playfield — the side margins of GDD §4.3.</summary>
        /// <remarks>
        /// Near-black slate, never pure black: a strict <c>#000000</c> crushes on low-end screens and
        /// makes <see cref="GridLine"/> invisible for part of the itch audience.
        /// </remarks>
        public static readonly Color Background = FromBytes(0x0A, 0x0E, 0x13);

        /// <summary>Playfield background: slightly lighter, so the playfield stands out.</summary>
        public static readonly Color Playfield = FromBytes(0x12, 0x18, 0x21);

        /// <summary>Grid lines: present but discreet — they help you count, not read.</summary>
        public static readonly Color GridLine = FromBytes(0x1C, 0x25, 0x30);

        /// <summary>
        /// Playfield border. <b>Amber</b>: this is the wall that kills (GDD §2), and the only "alert"
        /// colour permanently on screen. 8.06 : 1 against <see cref="Playfield"/>.
        /// </summary>
        public static readonly Color PlayfieldBorder = FromBytes(0xE3, 0xA2, 0x3A);

        /// <summary>
        /// Snake body. Mid green: the snake is the player — neither danger nor goal, so the only
        /// colour in the game that signals nothing.
        /// </summary>
        public static readonly Color SnakeBody = FromBytes(0x4E, 0x93, 0x58);

        /// <summary>
        /// Snake head — the same green pulled towards light, AND bigger: the cell that matters most
        /// at the tick stays the most readable, without the information hanging on colour alone.
        /// </summary>
        public static readonly Color SnakeHead = FromBytes(0xD8, 0xF5, 0xC4);

        /// <summary>
        /// The apple. Warm red, the only colour of that hue in the game. ⚠ It is distinguished from
        /// the snake by its <b>shape</b> (a diamond, against squares) and by its <b>size</b> before
        /// it is distinguished by colour: against <see cref="SnakeBody"/> the luminance contrast
        /// falls to 1.07 : 1, and red/green is precisely the pair deuteranopia confuses.
        /// </summary>
        public static readonly Color Apple = FromBytes(0xE5, 0x47, 0x3B);

        /// <summary>
        /// Rejection pictogram: the clearest signal on screen, it must dominate. Pure white,
        /// <b>reserved</b> — no other role reaches that value, including <see cref="HudText"/>. The
        /// chevron of a reversal always falls on the snake's body (ART §5.6), hence 3.72 : 1 against
        /// <see cref="SnakeBody"/>.
        /// </summary>
        public static readonly Color Pictogram = FromBytes(0xFF, 0xFF, 0xFF);

        /// <summary>Main HUD text: slightly cool white, never as saturated as the pictogram.</summary>
        public static readonly Color HudText = FromBytes(0xE7, 0xED, 0xF2);

        /// <summary>Secondary text (key reminder): a blue-grey from the background family, ranked below <see cref="HudText"/>.</summary>
        public static readonly Color SecondaryText = FromBytes(0x87, 0x92, 0xA0);

        /// <summary>
        /// Scrim darkening the pause screen. 62 % opaque: the grid stays readable underneath.
        /// Achromatic on purpose — a tinted scrim would compete with the game's four warm colours.
        /// </summary>
        public static readonly Color PauseScrim = new Color(0f, 0f, 0f, 0.62f);

        /// <summary>
        /// Build stamp: present for the bug report, discreet for the player. White at 45 %,
        /// achromatic so it stays readable whatever background it actually covers.
        /// </summary>
        public static readonly Color BuildStamp = new Color(1f, 1f, 1f, 0.45f);

        /// <summary>
        /// A hex code from <c>ART.md</c> §1, laid down byte by byte.
        /// </summary>
        /// <remarks>
        /// Bytes are written as <c>0xNN</c> so they can be read back against the brief as-is. Divided
        /// by 255 with no sRGB → linear conversion: the project is in Gamma (see the class remark).
        /// <c>ColorUtility.TryParseHtmlString</c> is deliberately avoided — it returns a boolean
        /// nobody tests, and a mistyped string would go through as black without raising anything.
        /// </remarks>
        private static Color FromBytes(byte red, byte green, byte blue)
        {
            return new Color(red / 255f, green / 255f, blue / 255f, 1f);
        }
    }
}
