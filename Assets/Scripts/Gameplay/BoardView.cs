using System.Collections.Generic;
using SnakeSnack.Rules;
using SnakeSnack.UI;
using UnityEngine;

namespace SnakeSnack.Gameplay
{
    /// <summary>
    /// Draws the playfield, the snake and the rejection feedback. Decides nothing.
    /// </summary>
    /// <remarks>
    /// Every position comes from <see cref="Board"/> (GDD §4.3): this class redoes no layout
    /// arithmetic, it places rectangles where the rule says. That is what allows the grid to move to
    /// 25 × 17 without touching the renderer.
    ///
    /// <para>⚠ Segments are a <b>reused pool</b>, never destroyed nor recreated every tick: at
    /// 8 ticks/s, creating and destroying <c>GameObject</c>s would produce regular garbage
    /// collection, visible in WebGL as micro-stutters — and a stutter shifts the reading of a
    /// turn.</para>
    /// </remarks>
    public sealed class BoardView : MonoBehaviour
    {
        /// <summary>Corner radius of the snake, as a fraction of the side (<c>docs/art/cartoon.md</c> §3.1).</summary>
        private const float SegmentRadius = 0.28f;

        /// <summary>Corner radius of the apple — tighter, so it stays a clear diamond (§3.2).</summary>
        private const float AppleRadius = 0.18f;

        private const double GulpDuration = 0.090;
        private const double PopDuration = 0.140;
        private const double DeathFlashDuration = 0.220;
        private const double ApplePopDuration = 0.150;
        private const double GulpAmplitude = 0.15;
        private const double PopOvershoot = 0.12;
        private const double ApplePopOvershoot = 0.08;

        /// <summary>Head tilt on a turn, in degrees (<c>docs/art/juicy.md</c> §9).</summary>
        private const float TurnAngle = 8f;

        // The head's face (docs/art/cartoon.md §3.3), at the proportions of the menu illustration
        // (`tools/generate_snake_illustration.py`, `draw_head`): the same ratios, so the character on
        // the poster and the one in play are recognisable as the same one. Expressed as a fraction of
        // the cell side, the head being drawn looking EAST.
        private const float EyeForward = 0.16f;
        private const float EyeSpread = 0.24f;
        private const float EyeRadius = 0.11f;

        private readonly List<SpriteRenderer> _segments = new List<SpriteRenderer>();

        /// <summary>Where each rendered segment starts from, and where it is going (juicy §4).</summary>
        private readonly List<Vector3> _from = new List<Vector3>();
        private readonly List<Vector3> _to = new List<Vector3>();

        private Board _board;

        /// <summary>Container of everything this view draws — see <see cref="Show"/>.</summary>
        private Transform _root;

        private Transform _segmentRoot;
        private Transform _chevron;
        private SpriteRenderer[] _chevronBars;
        private SpriteRenderer _apple;
        private SpriteRenderer _deathFlash;

        private int _visibleSegments;
        private double _tickDuration = Cadence.DefaultTickDurationSeconds;

        /// <summary>
        /// ⚠ A flag, not a null test on <c>_board</c>: <see cref="Board"/> is a <b>value type</b>, so
        /// it is "zero" and never "nothing". <c>Update</c> runs as soon as the component exists, that
        /// is, before <see cref="Build"/> — without this guard the first frame would read an empty
        /// board.
        /// </summary>
        private bool _built;

        /// <summary>Envelopes under way. <c>double.NegativeInfinity</c> = off.</summary>
        private double _slideStart = double.NegativeInfinity;
        private double _gulpStart = double.NegativeInfinity;
        private double _popStart = double.NegativeInfinity;
        private double _applePopStart = double.NegativeInfinity;
        private double _flashStart = double.NegativeInfinity;
        private double _turnStart = double.NegativeInfinity;

        private int _popIndex = -1;
        private int _turnSign;
        private Direction _gulpDirection = Direction.East;

        /// <summary>The two eyes, carried by a pivot that turns with the heading (cartoon §3.3).</summary>
        private Transform _face;

        /// <summary>Side of the apple diamond at rest — the scale its pop returns to (§7).</summary>
        private float _appleSide;

        /// <summary>
        /// Tick duration, so the slide lasts exactly the time of one cell.
        /// </summary>
        /// <remarks>
        /// ⚠ Received from <see cref="SnakeGame"/> rather than copied: the rate is settable
        /// (<c>settings.json</c>), and an interpolation frozen at 125 ms on a game retuned to
        /// 6 ticks/s would show the snake arrive then wait — a stuttering movement no error would
        /// report.
        /// </remarks>
        public void SetTickDuration(double tickDurationSeconds)
        {
            _tickDuration = tickDurationSeconds;
        }

        /// <summary>Builds the playfield. To be called once, before any drawing.</summary>
        public void Build(Board board)
        {
            _board = board;

            // ⚠ All the board rendering is parented to THIS container rather than to the component
            // itself: it is what gets switched off in one go when the menu takes the screen
            // (GDD §4.6). Disabling the component's GameObject would also stop SnakeGame and the HUD,
            // which live on it.
            var container = new GameObject("Board");
            container.transform.SetParent(transform, false);
            _root = container.transform;

            BuildPlayfield();
            BuildGridLines();
            BuildBorder();

            BuildApple();

            var root = new GameObject("Segments");
            root.transform.SetParent(_root, false);
            _segmentRoot = root.transform;

            BuildDeathFlash();
            BuildChevron();

            _built = true;
        }

        /// <summary>
        /// The cell that killed, highlighted for one round trip (<c>juicy.md</c> §6).
        /// </summary>
        /// <remarks>
        /// ⚠ A CRISP square, not rounded: this is a signal, not a creature. It reuses
        /// <see cref="UiPalette.Pictogram"/>, already reserved for whatever must dominate — no colour
        /// role is added for an effect (<c>juicy.md</c> §11).
        /// </remarks>
        private void BuildDeathFlash()
        {
            _deathFlash = PrimitiveShapes.Rectangle(_root, "DeathFlash", UiPalette.Pictogram, 20);

            double side = _board.CellSize - 2.0;
            _deathFlash.transform.localScale = new Vector3((float)side, (float)side, 1f);
            _deathFlash.gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows or hides the whole board (playfield, grid, snake, apple, chevron).
        /// </summary>
        /// <remarks>
        /// ⚠ Hide rather than destroy: the segment pool, the apple and the chevron are built once for
        /// the whole session. A round trip to the menu that destroyed and rebuilt everything would
        /// produce a garbage collection exactly as the game starts.
        /// </remarks>
        public void Show(bool visible)
        {
            _root.gameObject.SetActive(visible);
        }

        /// <summary>
        /// The apple: a square turned 45°, therefore a <b>diamond</b>.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Shape carries the information, not colour</b> (<c>docs/ART.md</c> §4, and §5.6 which
        /// requires building in greyscale until the palette is settled): the snake is made of solid
        /// squares that almost fill their cell, the apple is a smaller, centred diamond. A player who
        /// cannot tell two neighbouring greys apart still finds it. The day the palette arrives,
        /// readability therefore does not rest on it.
        ///
        /// <para>The diamond's diagonal is 0.72 cell; the side of the square to rotate is that
        /// diagonal divided by root two. Setting 0.72 as the side directly would give a diamond
        /// overflowing its cell and touching its neighbours.</para>
        /// </remarks>
        private void BuildApple()
        {
            // ⚠ Softened corners, unchanged silhouette (cartoon §3.2): the diamond stays what
            // distinguishes the apple from the snake before colour even comes in, including for a
            // colour-blind player.
            _apple = PrimitiveShapes.RoundedRectangle(_root, "Apple", UiPalette.Apple, 5, AppleRadius);

            _appleSide = (float)(_board.CellSize * 0.72 / Mathf.Sqrt(2f));
            _apple.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            SetAppleScale(1.0);
            _apple.gameObject.SetActive(false);
        }

        /// <summary>
        /// Places the apple on its cell (GDD §4.4).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>No frame may be displayed without an apple</b> (§4.4): it is replaced on the very tick
        /// it is eaten. An empty grid, even for a fraction of a second, reads as a bug and not as a
        /// transition — hence this method never hides, it moves.
        ///
        /// <para>⚠ <b>The pop-in of §7 starts from scale zero, and still does not contradict §4.4
        /// above</b>: the rise is ease-out, so the apple already reaches nearly a third of its size on
        /// the first frame and its full size in 150 ms. It is never absent from the grid — it arrives
        /// there. A linear rise, by contrast, would have left it invisible long enough to be looked
        /// for.</para>
        /// </remarks>
        public void DrawApple(Cell appleCell)
        {
            BoardPoint centre = _board.CellCentre(appleCell);

            // ⚠ Only the position is written here: the 45° rotation was set at build time, and
            // rewriting it with `Place` would erase it — the diamond would become a square again,
            // indistinguishable from a snake segment. Only the scale moves, and uniformly.
            _apple.transform.localPosition = new Vector3((float)centre.X, (float)centre.Y, 0f);
            _apple.gameObject.SetActive(true);

            _applePopStart = Time.timeAsDouble;
            SetAppleScale(0.0);
        }

        /// <summary>Scale of the diamond, as a fraction of its resting size.</summary>
        private void SetAppleScale(double factor)
        {
            float side = (float)(_appleSide * factor);
            _apple.transform.localScale = new Vector3(side, side, 1f);
        }

        /// <summary>Takes the apple off screen — only on a win, when there is none left.</summary>
        public void HideApple()
        {
            _apple.gameObject.SetActive(false);
        }

        /// <summary>Places a rectangle, in pixels of the reference frame.</summary>
        private static void Place(SpriteRenderer renderer, double centreX, double centreY, double width, double height)
        {
            renderer.transform.localPosition = new Vector3((float)centreX, (float)centreY, 0f);
            renderer.transform.localScale = new Vector3((float)width, (float)height, 1f);
        }

        private void BuildPlayfield()
        {
            var background = PrimitiveShapes.Rectangle(_root, "Playfield", UiPalette.Playfield, -100);
            Place(background, 0.0, _board.PlayfieldVerticalOffset, _board.PlayfieldWidth, _board.PlayfieldHeight);
        }

        /// <summary>
        /// The grid lines. They are not decorative: with no landmark the player cannot count the
        /// cells separating them from a wall, and death stops being anticipatable (§2).
        /// </summary>
        private void BuildGridLines()
        {
            var root = new GameObject("Lines");
            root.transform.SetParent(_root, false);

            double left = -_board.PlayfieldWidth / 2.0;
            double bottom = (-_board.PlayfieldHeight / 2.0) + _board.PlayfieldVerticalOffset;

            for (int x = 1; x < _board.Grid.Width; x++)
            {
                var line = PrimitiveShapes.Rectangle(root.transform, "LineV" + x, UiPalette.GridLine, -90);
                Place(line, left + (x * _board.CellSize), _board.PlayfieldVerticalOffset, 1.0, _board.PlayfieldHeight);
            }

            for (int y = 1; y < _board.Grid.Height; y++)
            {
                var line = PrimitiveShapes.Rectangle(root.transform, "LineH" + y, UiPalette.GridLine, -90);
                Place(line, 0.0, bottom + (y * _board.CellSize), _board.PlayfieldWidth, 1.0);
            }
        }

        /// <summary>
        /// The playfield border. ⚠ It carries a rule, not an ornament: the edges <b>kill</b> (§2). A
        /// playfield whose limit cannot be seen produces inexplicable deaths.
        /// </summary>
        private void BuildBorder()
        {
            var root = new GameObject("Border");
            root.transform.SetParent(_root, false);

            double centreY = _board.PlayfieldVerticalOffset;
            double halfWidth = _board.PlayfieldWidth / 2.0;
            double halfHeight = _board.PlayfieldHeight / 2.0;
            const double thickness = 3.0;

            var top = PrimitiveShapes.Rectangle(root.transform, "Top", UiPalette.PlayfieldBorder, -80);
            Place(top, 0.0, centreY + halfHeight, _board.PlayfieldWidth + (2 * thickness), thickness);

            var bottom = PrimitiveShapes.Rectangle(root.transform, "Bottom", UiPalette.PlayfieldBorder, -80);
            Place(bottom, 0.0, centreY - halfHeight, _board.PlayfieldWidth + (2 * thickness), thickness);

            var left = PrimitiveShapes.Rectangle(root.transform, "Left", UiPalette.PlayfieldBorder, -80);
            Place(left, -halfWidth, centreY, thickness, _board.PlayfieldHeight);

            var right = PrimitiveShapes.Rectangle(root.transform, "Right", UiPalette.PlayfieldBorder, -80);
            Place(right, halfWidth, centreY, thickness, _board.PlayfieldHeight);
        }

        /// <summary>
        /// The barred chevron of <c>docs/ART.md</c> §5.4, drawn pointing <b>north</b>; the rejected
        /// direction is then only a rotation.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>The bar is perpendicular to the chevron's axis, not diagonal.</b> The brief says
        /// "barred with a diagonal stroke" — but at 45° that stroke falls exactly parallel to one of
        /// the two arms and reads as a third arm, not as a bar. A deliberate deviation, reported in
        /// <c>docs/ART.md</c> §5.4.
        /// </remarks>
        private void BuildChevron()
        {
            var go = new GameObject("RejectionChevron");
            go.transform.SetParent(_root, false);
            _chevron = go.transform;

            double size = _board.MaximumPictogramSize;
            float thickness = Mathf.Max(2f, (float)(size * 0.20));
            float arm = (float)(size * 0.62);

            var left = PrimitiveShapes.Rectangle(_chevron, "LeftArm", UiPalette.Pictogram, 50);
            left.transform.localPosition = new Vector3((float)(-size * 0.20), (float)(-size * 0.08), 0f);
            left.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            left.transform.localScale = new Vector3(arm, thickness, 1f);

            var right = PrimitiveShapes.Rectangle(_chevron, "RightArm", UiPalette.Pictogram, 50);
            right.transform.localPosition = new Vector3((float)(size * 0.20), (float)(-size * 0.08), 0f);
            right.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            right.transform.localScale = new Vector3(arm, thickness, 1f);

            var bar = PrimitiveShapes.Rectangle(_chevron, "Bar", UiPalette.Pictogram, 51);
            bar.transform.localPosition = new Vector3(0f, (float)(-size * 0.08), 0f);
            bar.transform.localScale = new Vector3((float)(size * 1.10), thickness, 1f);

            _chevronBars = new[] { left, right, bar };
            _chevron.gameObject.SetActive(false);
        }

        /// <summary>
        /// Draws the snake. The head is lighter: you must see where you are going.
        /// </summary>
        /// <param name="animated">
        /// <c>true</c>: segments slide from their previous cell to the new one over the tick duration
        /// (<c>juicy.md</c> §4). <c>false</c>: immediate placement, for a death, a pause or a new game
        /// — a snake sliding towards its starting position would suggest the game began before it was
        /// displayed.
        /// </param>
        public void DrawSnake(IReadOnlyList<Cell> segments, bool animated = true)
        {
            while (_segments.Count < segments.Count)
            {
                // ⚠ Rounded corners (cartoon §3.1): that single sprite change is what takes the game
                // off the graph paper and connects it to the character of the menu and the cover.
                _segments.Add(PrimitiveShapes.RoundedRectangle(
                    _segmentRoot, "Segment" + _segments.Count, UiPalette.SnakeBody, 10, SegmentRadius));
            }

            while (_from.Count < _segments.Count)
            {
                _from.Add(Vector3.zero);
                _to.Add(Vector3.zero);
            }

            // ⚠ Here and not in `Build`: the face is a child of the head segment, which only exists
            // once the pool has been primed by the first draw.
            if (_face == null && _segments.Count > 0)
            {
                BuildFace();
            }

            // One segment too many is hidden, never destroyed: the pool will serve the next game.
            for (int i = segments.Count; i < _segments.Count; i++)
            {
                _segments[i].gameObject.SetActive(false);
            }

            int previousCount = _visibleSegments;
            _visibleSegments = segments.Count;

            double side = _board.CellSize - 2.0;

            for (int i = 0; i < segments.Count; i++)
            {
                SpriteRenderer renderer = _segments[i];
                renderer.gameObject.SetActive(true);
                renderer.color = i == 0 ? UiPalette.SnakeHead : UiPalette.SnakeBody;
                renderer.sortingOrder = i == 0 ? 11 : 10;

                BoardPoint centre = _board.CellCentre(segments[i]);
                var arrival = new Vector3((float)centre.X, (float)centre.Y, 0f);

                // ⚠ A segment that has just appeared has no previous position: starting it from zero
                // would launch it from the centre of the board, across the grid. It is placed on its
                // cell, and it is the pop of §5 that makes it grow.
                bool isNew = i >= previousCount;
                _from[i] = (animated && !isNew) ? _segments[i].transform.localPosition : arrival;
                _to[i] = arrival;

                renderer.transform.localPosition = _from[i];
                renderer.transform.localScale = new Vector3((float)side, (float)side, 1f);
            }

            _slideStart = animated ? Time.timeAsDouble : double.NegativeInfinity;

            if (!animated)
            {
                ClearEnvelopes();
                ApplySlide(1.0);
            }
        }

        /// <summary>
        /// The bite: the head swells perpendicular to its heading, the new tail segment pops in
        /// (<c>juicy.md</c> §5).
        /// </summary>
        /// <param name="headingDirection">Direction applied on the tick of the bite.</param>
        public void SignalBite(Direction headingDirection)
        {
            _gulpDirection = headingDirection;
            _gulpStart = Time.timeAsDouble;

            _popStart = Time.timeAsDouble;
            _popIndex = _visibleSegments - 1;
        }

        /// <summary>
        /// Builds the head's face, once the pool exists (<c>docs/art/cartoon.md</c> §3.3).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>A child of the head segment</b>, as the brief requires: the eyes therefore inherit its
        /// position, its turn tilt (§9) <i>and</i> its gulp (§5) — they squash with it as it swallows,
        /// which a face placed alongside would not do. A circle stays an ellipse under a non-uniform
        /// scale, with no shear: the face's own rotation does not distort it.
        ///
        /// <para>⚠ The disc comes from <see cref="PrimitiveShapes.RoundedSquare"/> with a relative
        /// radius of 0.5: at that radius, the signed distance of the rounded rectangle <b>is</b> that
        /// of a disc. No new factory, and the same sprite shared by both eyes.</para>
        ///
        /// <para>⚠ Colour <see cref="UiPalette.Background"/>, like the menu illustration: the only
        /// role dark enough to stand out against the light head without introducing a colour that
        /// exists nowhere else (<c>docs/art/palette.md</c> §1.2).</para>
        ///
        /// <para><b>No tongue in play</b> (§3.3): it would stick out of the cell and encroach on the
        /// next one every tick, flickering at 8 ticks/s.</para>
        /// </remarks>
        private void BuildFace()
        {
            var pivot = new GameObject("Face");
            pivot.transform.SetParent(_segments[0].transform, false);
            _face = pivot.transform;

            for (int side = -1; side <= 1; side += 2)
            {
                var eye = PrimitiveShapes.RoundedRectangle(_face, "Eye" + side, UiPalette.Background, 12, 0.5f);

                // Fractions of the cell side: the parent already carries the head's scale, so a child
                // at 1 would measure a whole cell.
                eye.transform.localPosition = new Vector3(EyeForward, side * EyeSpread, 0f);
                eye.transform.localScale = new Vector3(2f * EyeRadius, 2f * EyeRadius, 1f);
            }
        }

        /// <summary>
        /// The head tilts in the direction of the turn, straightens over the next tick
        /// (<c>juicy.md</c> §9), and its face looks where it is going (<c>cartoon.md</c> §3.3).
        /// </summary>
        /// <param name="before">Direction applied on the previous tick.</param>
        /// <param name="after">Direction applied on this tick.</param>
        /// <remarks>
        /// ⚠ <b>Purely visual.</b> This rotation lives on the head's <c>Transform</c> and is read by
        /// nobody: collision is computed on the cell, and <c>Board.RejectionAnchor</c> keeps placing
        /// the chevron relative to the cell, never relative to this angle (§9). A chevron following
        /// the tilt would point at a slightly wrong cell edge — at the very moment the game is
        /// explaining a rejection.
        ///
        /// <para>Sorting the turn is left to <see cref="Directions.TurnSign"/> rather than to the
        /// caller: the game reports what it did, the view decides what it shows of it.</para>
        /// </remarks>
        public void SignalDirection(Direction before, Direction after)
        {
            // The face follows the heading every tick, turn or not: it is the SAME information —
            // "here is where the head is going" — and making it arrive through two separate calls is
            // preparing to forget one of them the day one of the callers changes.
            if (_face != null)
            {
                _face.localRotation = Quaternion.Euler(0f, 0f, FaceAngle(after));
            }

            int sign = Directions.TurnSign(before, after);

            if (sign == 0)
            {
                // Straight on: above all do not rearm the envelope, otherwise the head would stay
                // tilted at zero angle permanently and the real tilt would never start again.
                return;
            }

            _turnSign = sign;
            _turnStart = Time.timeAsDouble;
        }

        /// <summary>Highlights the cell where contact happened (<c>juicy.md</c> §6).</summary>
        /// <remarks>
        /// ⚠ For a bite it is the bitten cell; for a wall it is the head's cell, not the targeted one
        /// — the latter is <b>outside the grid</b>, therefore outside the playfield: the flash would
        /// show on the background, beyond the border, where no cell exists. A deliberate deviation
        /// from the brief, which says "the offending cell" without settling that case.
        /// </remarks>
        public void FlashCell(Cell offendingCell)
        {
            BoardPoint centre = _board.CellCentre(offendingCell);
            _deathFlash.transform.localPosition = new Vector3((float)centre.X, (float)centre.Y, 0f);
            _flashStart = Time.timeAsDouble;
            _deathFlash.gameObject.SetActive(true);
        }

        /// <summary>
        /// Freezes everything currently animating, at its arrival value.
        /// </summary>
        /// <remarks>
        /// Called on pause: a snake that kept sliding under the scrim would show a game still
        /// running, exactly what the pause claims not to be doing. ⚠ The death flash is not part of it
        /// — it is triggered <i>by</i> death and must run afterwards.
        /// </remarks>
        public void FreezeAnimations()
        {
            ApplySlide(1.0);
            ClearEnvelopes();
        }

        /// <summary>
        /// Stops the envelopes under way <b>and puts back what they were animating to its resting
        /// value</b>.
        /// </summary>
        /// <remarks>
        /// ⚠ Clearing them without resetting would leave the last intermediate value on screen for
        /// good: an apple frozen at 30 % of its size, a head leaning at 6° — a permanent defect born
        /// of a 150 ms animation, and one nobody would think to look for there.
        /// </remarks>
        private void ClearEnvelopes()
        {
            _slideStart = double.NegativeInfinity;
            _gulpStart = double.NegativeInfinity;
            _popStart = double.NegativeInfinity;
            _popIndex = -1;

            _applePopStart = double.NegativeInfinity;
            SetAppleScale(1.0);

            _turnStart = double.NegativeInfinity;
            StraightenHead();
        }

        /// <summary>Puts the head back level. No effect while the pool is still empty.</summary>
        private void StraightenHead()
        {
            if (_segments.Count > 0)
            {
                _segments[0].transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// The only place where time enters the renderer: every frame, we re-read the envelopes under
        /// way and put back what they describe.
        /// </summary>
        /// <remarks>
        /// ⚠ No envelope writes into <c>Rules/</c>: the logical position stays the one from the tick,
        /// and the chevron anchor keeps being computed on the cell, never on these factors
        /// (<c>juicy.md</c> §11).
        /// </remarks>
        private void Update()
        {
            if (!_built)
            {
                return;
            }

            double now = Time.timeAsDouble;

            // ⚠ Before the guard below: neither the apple nor the death flash lives on a segment.
            // Filing them behind a test on the snake is preparing for them to stop animating mutely
            // the day the pool is empty at some point.
            ApplyApplePop(now);
            ApplyFlash(now);

            if (_visibleSegments == 0)
            {
                return;
            }

            if (_slideStart > double.NegativeInfinity)
            {
                double t = Easing.Progress(_slideStart, _tickDuration, now);
                ApplySlide(t);

                if (t >= 1.0)
                {
                    _slideStart = double.NegativeInfinity;
                }
            }

            ApplyScales(now);
            ApplyTilt(now);
        }

        /// <summary>The pop-in of the apple that has just been placed (<c>juicy.md</c> §7).</summary>
        private void ApplyApplePop(double now)
        {
            if (_applePopStart <= double.NegativeInfinity)
            {
                return;
            }

            double t = Easing.Progress(_applePopStart, ApplePopDuration, now);
            SetAppleScale(Easing.PopIn(t, ApplePopOvershoot));

            if (t >= 1.0)
            {
                _applePopStart = double.NegativeInfinity;
            }
        }

        /// <summary>The head tilt, fading over the tick duration (§9).</summary>
        private void ApplyTilt(double now)
        {
            if (_turnStart <= double.NegativeInfinity)
            {
                return;
            }

            double t = Easing.Progress(_turnStart, _tickDuration, now);
            float angle = _turnSign * TurnAngle * (float)Easing.Falloff(t);
            _segments[0].transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (t >= 1.0)
            {
                _turnStart = double.NegativeInfinity;

                // Put back explicitly: `Falloff(1)` is zero, but it is the EXACT angle that matters
                // here — a residue would build up turn after turn.
                StraightenHead();
            }
        }

        private void ApplySlide(double t)
        {
            for (int i = 0; i < _visibleSegments && i < _segments.Count; i++)
            {
                _segments[i].transform.localPosition = Vector3.Lerp(_from[i], _to[i], (float)t);
            }
        }

        /// <summary>The head's gulp and the new segment's pop, on scale only.</summary>
        private void ApplyScales(double now)
        {
            float side = (float)(_board.CellSize - 2.0);

            if (_gulpStart > double.NegativeInfinity)
            {
                double t = Easing.Progress(_gulpStart, GulpDuration, now);
                float stretched = (float)Easing.Gulp(t, GulpAmplitude);

                // ⚠ The squashed axis is the INVERSE of the stretched one: the head swells without
                // losing area. Two symmetric factors would shrink it as it swallows.
                float squashed = 1f / stretched;
                bool horizontal = _gulpDirection == Direction.East || _gulpDirection == Direction.West;

                _segments[0].transform.localScale = horizontal
                    ? new Vector3(side * squashed, side * stretched, 1f)
                    : new Vector3(side * stretched, side * squashed, 1f);

                if (t >= 1.0)
                {
                    _gulpStart = double.NegativeInfinity;
                    _segments[0].transform.localScale = new Vector3(side, side, 1f);
                }
            }

            if (_popStart > double.NegativeInfinity && _popIndex >= 0 && _popIndex < _visibleSegments)
            {
                double t = Easing.Progress(_popStart, PopDuration, now);
                float factor = (float)Easing.PopIn(t, PopOvershoot);
                _segments[_popIndex].transform.localScale = new Vector3(side * factor, side * factor, 1f);

                if (t >= 1.0)
                {
                    _popStart = double.NegativeInfinity;
                    _popIndex = -1;
                }
            }
        }

        private void ApplyFlash(double now)
        {
            if (_flashStart <= double.NegativeInfinity)
            {
                return;
            }

            double t = Easing.Progress(_flashStart, DeathFlashDuration, now);

            Color colour = UiPalette.Pictogram;
            colour.a = (float)Easing.Pulse(t);
            _deathFlash.color = colour;

            if (t >= 1.0)
            {
                _flashStart = double.NegativeInfinity;
                _deathFlash.gameObject.SetActive(false);
            }
        }

        /// <summary>Shows the chevron at the edge of the head cell, on the rejected side (ART §5.4).</summary>
        public void ShowRejection(Cell head, Direction rejectedDirection, float opacity)
        {
            BoardPoint anchor = _board.RejectionAnchor(head, rejectedDirection);
            _chevron.localPosition = new Vector3((float)anchor.X, (float)anchor.Y, 0f);
            _chevron.localRotation = Quaternion.Euler(0f, 0f, ChevronAngle(rejectedDirection));

            for (int i = 0; i < _chevronBars.Length; i++)
            {
                Color colour = UiPalette.Pictogram;
                colour.a = opacity;
                _chevronBars[i].color = colour;
            }

            _chevron.gameObject.SetActive(true);
        }

        /// <summary>Switches the chevron off.</summary>
        public void HideRejection()
        {
            _chevron.gameObject.SetActive(false);
        }

        /// <summary>The face is drawn looking east: the rest is only a rotation.</summary>
        private static float FaceAngle(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return 90f;
                case Direction.West: return 180f;
                case Direction.South: return 270f;
                default: return 0f;
            }
        }

        /// <summary>The chevron is drawn pointing north: the rest is only a rotation.</summary>
        private static float ChevronAngle(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return 0f;
                case Direction.West: return 90f;
                case Direction.South: return 180f;
                default: return 270f;
            }
        }
    }
}
