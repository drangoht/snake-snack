using SnakeSnack.Rules;
using SnakeSnack.UI;
using UnityEngine;

namespace SnakeSnack.Gameplay
{
    /// <summary>
    /// Draws the on-screen controls of the mobile port: a directional cross in the right margin, a
    /// pause button in the left one (GDD §3, touch — reopened on 2026-08-30).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This view draws; it decides nothing.</b> Where the controls are and what a finger
    /// landing on them means both live in <see cref="TouchPad"/>, tested without an engine. The
    /// separation matters here more than elsewhere: a button drawn at a different place from the one
    /// that is hit-tested gives a pad that looks right and answers wrong, and no error is raised.
    ///
    /// <para>⚠ <b>Shown only when a touchscreen exists.</b> On a desktop it would be four squares
    /// that do nothing, in the margin the build stamp already uses.</para>
    ///
    /// <para><b>On the colours.</b> The controls reuse existing palette roles — the playfield's own
    /// dark for the keys, the secondary text grey for the glyphs — rather than declaring new ones.
    /// <c>docs/ART.md</c> §1 gives the art director the say on the twelve roles, and a mobile pad was
    /// not among them: reusing is the honest placeholder until that ruling. They are deliberately
    /// quiet: the pad is a tool, not a character, and the eye must stay on the snake.</para>
    /// </remarks>
    public sealed class TouchControlsView : MonoBehaviour
    {
        /// <summary>Corner rounding of a key, relative to its side (<c>cartoon.md</c> §3.1).</summary>
        private const float KeyRadius = 0.22f;

        /// <summary>Opacity of a key at rest. Present, never louder than the game.</summary>
        private const float RestingAlpha = 0.55f;

        /// <summary>Opacity of a key under the thumb — the press must be seen (ART §5).</summary>
        private const float PressedAlpha = 1.0f;

        private static readonly TouchTarget[] Keys =
        {
            TouchTarget.North, TouchTarget.South, TouchTarget.West, TouchTarget.East, TouchTarget.Pause
        };

        private Transform _root;
        private TouchPad _pad;
        private SpriteRenderer[] _keys;
        private SpriteRenderer[][] _glyphs;
        private TouchTarget _pressed = TouchTarget.None;

        public void Build(TouchPad pad)
        {
            _pad = pad;

            var container = new GameObject("TouchControls");
            container.transform.SetParent(transform, false);
            _root = container.transform;

            _keys = new SpriteRenderer[Keys.Length];
            _glyphs = new SpriteRenderer[Keys.Length][];

            for (int i = 0; i < Keys.Length; i++)
            {
                TouchTarget target = Keys[i];
                BoardPoint centre = pad.ButtonCentre(target);

                SpriteRenderer key = PrimitiveShapes.RoundedRectangle(
                    _root, "Key" + target, UiPalette.Playfield, 60, KeyRadius);
                key.transform.localPosition = new Vector3((float)centre.X, (float)centre.Y, 0f);
                key.transform.localScale =
                    new Vector3((float)pad.ButtonSize, (float)pad.ButtonSize, 1f);
                _keys[i] = key;

                _glyphs[i] = target == TouchTarget.Pause
                    ? BuildPauseGlyph(key.transform)
                    : BuildChevron(key.transform, target);
            }

            Refresh();
        }

        /// <summary>Hides or shows the whole pad — the menu takes the screen on its own.</summary>
        public void Show(bool visible)
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        /// <summary>Which key the thumb is holding, so the press is visible.</summary>
        public void SetPressed(TouchTarget target)
        {
            if (_pressed == target)
            {
                return;
            }

            _pressed = target;
            Refresh();
        }

        private void Refresh()
        {
            if (_keys == null)
            {
                return;
            }

            for (int i = 0; i < Keys.Length; i++)
            {
                float alpha = Keys[i] == _pressed ? PressedAlpha : RestingAlpha;
                Fade(_keys[i], alpha);

                SpriteRenderer[] glyph = _glyphs[i];
                for (int j = 0; j < glyph.Length; j++)
                {
                    Fade(glyph[j], alpha);
                }
            }
        }

        private static void Fade(SpriteRenderer renderer, float alpha)
        {
            Color colour = renderer.color;
            colour.a = alpha;
            renderer.color = colour;
        }

        /// <summary>
        /// The arrow of a directional key: two bars at right angles, the same chevron the refusal
        /// pictogram is drawn with (<c>ART.md</c> §5.4). Rotating the pair is what points it.
        /// </summary>
        private SpriteRenderer[] BuildChevron(Transform parent, TouchTarget target)
        {
            double size = _pad.ButtonSize;
            float thickness = Mathf.Max(2f, (float)(size * 0.10));
            float arm = (float)(size * 0.34);

            // localScale on the key is the key's side, so a child expressed in the same reference
            // pixels would be scaled twice: the glyph is built in the key's LOCAL unit square.
            float unit = 1f / (float)size;

            var left = PrimitiveShapes.Rectangle(parent, "ArmA", UiPalette.SecondaryText, 61);
            left.transform.localPosition = new Vector3(-0.09f, 0f, 0f);
            left.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            left.transform.localScale = new Vector3(arm * unit, thickness * unit, 1f);

            var right = PrimitiveShapes.Rectangle(parent, "ArmB", UiPalette.SecondaryText, 61);
            right.transform.localPosition = new Vector3(0.09f, 0f, 0f);
            right.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            right.transform.localScale = new Vector3(arm * unit, thickness * unit, 1f);

            parent.localRotation = Quaternion.Euler(0f, 0f, PointingAngle(target));
            return new[] { left, right };
        }

        /// <summary>
        /// The chevron is drawn pointing up; each key turns it to its own direction.
        /// </summary>
        /// <remarks>
        /// ⚠ Rotating the KEY and not only the glyph is deliberate: a rounded square is
        /// indistinguishable under rotation, so one rotation does for both and there is no second
        /// transform to keep in step.
        /// </remarks>
        private static float PointingAngle(TouchTarget target)
        {
            switch (target)
            {
                case TouchTarget.North:
                    return 0f;
                case TouchTarget.South:
                    return 180f;
                case TouchTarget.West:
                    return 90f;
                case TouchTarget.East:
                    return -90f;
                default:
                    return 0f;
            }
        }

        /// <summary>The pause glyph: two bars, the shape every player already reads as "pause".</summary>
        private SpriteRenderer[] BuildPauseGlyph(Transform parent)
        {
            var left = PrimitiveShapes.Rectangle(parent, "BarA", UiPalette.SecondaryText, 61);
            left.transform.localPosition = new Vector3(-0.13f, 0f, 0f);
            left.transform.localScale = new Vector3(0.11f, 0.42f, 1f);

            var right = PrimitiveShapes.Rectangle(parent, "BarB", UiPalette.SecondaryText, 61);
            right.transform.localPosition = new Vector3(0.13f, 0f, 0f);
            right.transform.localScale = new Vector3(0.11f, 0.42f, 1f);

            return new[] { left, right };
        }
    }
}
