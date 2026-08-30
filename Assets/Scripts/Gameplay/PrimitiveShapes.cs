using System.Collections.Generic;
using UnityEngine;

namespace SnakeSnack.Gameplay
{
    /// <summary>
    /// The game's sprites, all <b>generated in memory</b>: a crisp white square, and rounded squares
    /// for whatever must look alive (<c>docs/art/cartoon.md</c> §3.1).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>No asset is imported, and that is deliberate.</b> A <c>.png</c> dropped into
    /// <c>Assets/</c> only gets its GUID when the editor imports it: a batchmode build launched
    /// before that import does not see it, and the game displays with no texture <b>without raising
    /// an error</b>.
    ///
    /// <para>⚠ <b>That is why the rounding from the cartoon brief is drawn here rather than loaded
    /// from a PNG.</b> The brief proposed <c>Resources/Shapes/rounded-cell.png</c> in 9-slice,
    /// produced by a Python generator and forced to Sprite by the postprocessor. A deliberate
    /// deviation, for three reasons: the shape is trivial to describe (a rounded-rectangle SDF fits
    /// in five lines) where the menu illustration is not; 9-slice would force the whole renderer into
    /// <c>SpriteDrawMode.Sliced</c> while <c>BoardView</c> places everything by <c>localScale</c>;
    /// and above all it removes at a stroke the <c>.meta</c>, the <c>spriteBorder</c> to set at
    /// import and the batchmode build risk above. The brief explicitly left the treatment to be
    /// settled at implementation time (<c>docs/art/cartoon.md</c> §7).</para>
    ///
    /// <para>⚠ <c>pixelsPerUnit</c> is always <b>the texture's side</b>: every sprite therefore
    /// measures exactly 1 unit whatever its resolution, and <c>localScale</c> keeps being expressed
    /// in pixels of the 1280×720 reference frame — which <see cref="SnakeSnack.Rules.Board"/> assumes
    /// everywhere and the camera confirms with <c>orthographicSize = 360</c>. Without that rule,
    /// moving the square from 1 px to a 128 px shape would multiply everything drawn by 128.</para>
    ///
    /// <para>The white textures are tinted by the <c>SpriteRenderer</c> from
    /// <see cref="SnakeSnack.UI.UiPalette"/>: no colour is baked into a pixel, otherwise a palette
    /// change would no longer affect these shapes.</para>
    /// </remarks>
    public static class PrimitiveShapes
    {
        /// <summary>
        /// Side of the rounded textures, in pixels. 128 for a cell displayed at 42 px: enough margin
        /// to stay crisp if the cell size grows, small enough to be negligible in memory (64 KB per
        /// shape).
        /// </summary>
        private const int RoundedTextureSide = 128;

        private static Sprite _square;

        /// <summary>The rounded shapes already produced, keyed by their relative radius.</summary>
        private static readonly Dictionary<int, Sprite> _rounded = new Dictionary<int, Sprite>();

        /// <summary>A 1 × 1 px white square, shared by everything that must stay a crisp flat area.</summary>
        /// <remarks>
        /// ⚠ Stays in <see cref="FilterMode.Point"/>: the border and the grid lines are <b>measuring
        /// marks</b>, not characters (<c>docs/art/cartoon.md</c> §6). Smoothing them would make a
        /// 1 px line blurry, and therefore harder to count by eye.
        /// </remarks>
        public static Sprite Square()
        {
            // The null comparison goes through Unity's operator: after a domain reload or a scene
            // change the reference is "fake-null" and the sprite is recreated.
            if (_square != null)
            {
                return _square;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();

            _square = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            _square.name = "WhiteSquare";
            return _square;
        }

        /// <summary>
        /// A white square with rounded, smoothed corners. The snake's body and head, and the apple.
        /// </summary>
        /// <param name="relativeRadius">
        /// Corner radius as a fraction of the side: <c>0.28</c> for the snake — the same ratio as the
        /// menu illustration, so the character on the poster and the one in the game recognise each
        /// other as the same drawing — <c>0.18</c> for the apple.
        /// </param>
        public static Sprite RoundedSquare(float relativeRadius)
        {
            // An integer key: two calls with the same radius must return THE SAME sprite, otherwise
            // every segment would pull its own texture and the renderer would lose its material
            // batching.
            int key = Mathf.RoundToInt(relativeRadius * 1000f);

            Sprite existing;
            if (_rounded.TryGetValue(key, out existing) && existing != null)
            {
                return existing;
            }

            var sprite = BuildRounded(relativeRadius);
            _rounded[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Draws the rounded rectangle from its signed distance, and derives antialiasing from that
        /// distance.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Alpha is computed, it is not supersampled.</b> The distance to the edge gives the
        /// pixel's coverage directly: a single pass, no downsampling, and a contour regular to the
        /// pixel — where a binary render then blurred would leave visible steps on the diagonals of
        /// the corners.
        ///
        /// <para>⚠ RGB stays white <b>everywhere</b>, including where alpha is zero: a transparent
        /// black texture produces a dark fringe under bilinear filtering, because interpolation also
        /// mixes the colour channels of invisible pixels.</para>
        /// </remarks>
        private static Sprite BuildRounded(float relativeRadius)
        {
            const int side = RoundedTextureSide;
            float radius = Mathf.Clamp(relativeRadius, 0f, 0.5f) * side;
            float half = side / 2f;

            // Centre of the corner arcs: the inner square whose corners are `radius` from the edge.
            float inner = half - radius;

            var texture = new Texture2D(side, side, TextureFormat.RGBA32, false);
            var pixels = new Color32[side * side];

            for (int y = 0; y < side; y++)
            {
                for (int x = 0; x < side; x++)
                {
                    // Coordinates of the pixel CENTRE, relative to the texture centre.
                    float dx = Mathf.Abs((x + 0.5f) - half);
                    float dy = Mathf.Abs((y + 0.5f) - half);

                    // Signed distance to the rounded rectangle: negative inside, positive outside.
                    float gapX = Mathf.Max(dx - inner, 0f);
                    float gapY = Mathf.Max(dy - inner, 0f);
                    float distance = Mathf.Sqrt((gapX * gapX) + (gapY * gapY)) - radius;

                    // Pixel coverage: full at -0.5 px, empty at +0.5 px.
                    float coverage = Mathf.Clamp01(0.5f - distance);

                    pixels[(y * side) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(coverage * 255f));
                }
            }

            texture.SetPixels32(pixels);

            // Bilinear: this is what gives the smoothing once the shape is resized to the cell.
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();

            var sprite = Sprite.Create(
                texture, new Rect(0f, 0f, side, side), new Vector2(0.5f, 0.5f), side);
            sprite.name = "RoundedSquare" + Mathf.RoundToInt(relativeRadius * 100f);
            return sprite;
        }

        /// <summary>
        /// Places a coloured rectangle, child of <paramref name="parent"/>, expressed in pixels.
        /// </summary>
        public static SpriteRenderer Rectangle(Transform parent, string name, Color colour, int order)
        {
            return Place(parent, name, colour, order, Square());
        }

        /// <summary>Like <see cref="Rectangle"/>, but with rounded, smoothed corners.</summary>
        public static SpriteRenderer RoundedRectangle(
            Transform parent, string name, Color colour, int order, float relativeRadius)
        {
            return Place(parent, name, colour, order, RoundedSquare(relativeRadius));
        }

        private static SpriteRenderer Place(Transform parent, string name, Color colour, int order, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = colour;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
