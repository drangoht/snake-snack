using System;
using UnityEngine;
using UnityEngine.UI;

namespace SnakeSnack.UI
{
    /// <summary>
    /// A reading panel of the menu — "How to play", "Credits": a scrim, a card, a title, a body of
    /// text and the reminder of the key that closes it.
    /// </summary>
    /// <remarks>
    /// A plain class rather than a <c>MonoBehaviour</c>: it does nothing on its own, it is
    /// <see cref="MenuScreen"/> that animates it from its own <c>Update</c>. Two panels, one layout —
    /// the day the card changes, both change together.
    ///
    /// <para>⚠ <b>The scrim intercepts the click</b>: with no raycast target behind the card, a click
    /// next to the panel would land on the menu entries still underneath, and the player would start
    /// a game while thinking they were closing a panel.</para>
    /// </remarks>
    public sealed class InfoPanel
    {
        /// <summary>Fade duration, opening as well as closing.</summary>
        private const float FadeDuration = 0.14f;

        private const float CardWidth = 880f;

        /// <summary>
        /// ⚠ Sized on the longest text ("How to play", nine lines). <b>One line of Nunito takes about
        /// 1.36 times the font size</b>, not 1.0: at 19 px with <c>lineSpacing</c> 1.1, a line takes
        /// ~28 px. Nine lines therefore need ~260 px, where the naive calculation announced 190 —
        /// which is what truncated the panel's last two lines, the ones stating what kills.
        /// </summary>
        private const float CardHeight = 480f;

        /// <summary>Thickness of the amber frame, the same as the playfield border.</summary>
        private const float FrameThickness = 3f;

        private readonly GameObject _root;
        private readonly CanvasGroup _group;

        private bool _open;

        public InfoPanel(Transform parent, string name, string title, string body, Action onClose)
        {
            _root = new GameObject(name, typeof(RectTransform));
            _root.transform.SetParent(parent, false);

            var rect = (RectTransform)_root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _group = _root.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            Image scrim = UiFactory.Scrim(_root.transform, "Scrim", UiPalette.PauseScrim);
            scrim.raycastTarget = true;
            var click = scrim.gameObject.AddComponent<ClickableArea>();
            click.Clicked = onClose;

            // The amber frame is a slightly larger rectangle placed BEHIND the card: two images beat
            // four lines, and the render order comes from the hierarchy.
            UiFactory.Rectangle(_root.transform, "Frame", UiPalette.PlayfieldBorder, Anchor,
                Vector2.zero, new Vector2(CardWidth + (2f * FrameThickness), CardHeight + (2f * FrameThickness)));

            UiFactory.Rectangle(_root.transform, "Card", UiPalette.Playfield, Anchor,
                Vector2.zero, new Vector2(CardWidth, CardHeight));

            Font headingFont = UiFonts.Load(UiFonts.Headings);
            Font bodyFont = UiFonts.Load(UiFonts.Body);

            UiFactory.Text(_root.transform, "Title", headingFont, 34, TextAnchor.MiddleCenter,
                UiPalette.HudText, Anchor, new Vector2(0f, (CardHeight / 2f) - 52f),
                new Vector2(CardWidth - 80f, 44f)).text = title;

            // ⚠ Aligned left and top: help text centred line by line is hard to re-read, and keys at
            // the start of a line must line up vertically to be compared.
            Text text = UiFactory.Text(_root.transform, "Body", bodyFont, 19, TextAnchor.UpperLeft,
                UiPalette.HudText, Anchor, new Vector2(0f, -6f), new Vector2(CardWidth - 140f, CardHeight - 180f));
            text.text = body;
            text.lineSpacing = 1.1f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;

            // ⚠ Truncated, not overflowing (the factory's default): text that was too long left the
            // amber frame and went OVER the "Esc to go back" line — which reads as a rendering
            // defect, not as text that is too long. Cut inside the card, the problem is visible and
            // gets fixed where it belongs: in UiText.
            text.verticalOverflow = VerticalWrapMode.Truncate;

            UiFactory.Text(_root.transform, "Back", bodyFont, 18, TextAnchor.MiddleCenter,
                UiPalette.SecondaryText, Anchor, new Vector2(0f, (-CardHeight / 2f) + 34f),
                new Vector2(CardWidth - 80f, 26f)).text = UiText.PanelBack;

            _root.SetActive(false);
        }

        private static Vector2 Anchor
        {
            get { return new Vector2(0.5f, 0.5f); }
        }

        /// <summary>True from the press onwards, before the fade has even started.</summary>
        public bool Requested
        {
            get { return _open; }
        }

        public void Open()
        {
            _open = true;
            _root.SetActive(true);
            _group.blocksRaycasts = true;
        }

        public void Close()
        {
            _open = false;
        }

        /// <summary>
        /// Advances the fade. To be called every frame, open or not — that constant call is what
        /// switches the panel off once its fade-out has finished.
        /// </summary>
        /// <param name="step">
        /// ⚠ The <b>unscaled</b> time step handed over by <see cref="MenuScreen"/>: a menu panel must
        /// open and close even if game time were ever stopped.
        /// </param>
        public void Animate(float step)
        {
            if (!_root.activeSelf)
            {
                return;
            }

            float target = _open ? 1f : 0f;
            _group.alpha = Mathf.MoveTowards(_group.alpha, target, step / FadeDuration);

            if (!_open && _group.alpha <= 0f)
            {
                _group.blocksRaycasts = false;
                _root.SetActive(false);
            }
        }
    }
}
