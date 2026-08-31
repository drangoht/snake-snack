using System;
using System.Collections.Generic;
using SnakeSnack.Audio;
using SnakeSnack.Rules;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SnakeSnack.UI
{
    /// <summary>
    /// The main menu (GDD §4.6): title, snake illustration, navigable entries and reading panels.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>No rule is decided here</b>: the composition of the entries and the navigation come from
    /// <see cref="MainMenu"/>, tested without an engine. This component reads the result, draws it and
    /// animates it — nothing more. It is the same split as <c>SnakeGame</c> and <c>Rules/</c>, and for
    /// the same reason: a rule copied into an <c>Update()</c> becomes a second truth.
    ///
    /// <para>⚠ <b>Everything is built in code</b>, like the HUD: the scene is regenerated on every
    /// build (<c>SceneBuilder</c>), and a lost serialised reference would raise nothing — it would
    /// give an incomplete menu.</para>
    ///
    /// <para>⚠ <b>The time used is <c>unscaledTime</c></b>: the menu appears at a moment when nothing
    /// guarantees game time is advancing, and a frozen opening animation would pass for a frozen
    /// game.</para>
    /// </remarks>
    public sealed class MenuScreen : MonoBehaviour
    {
        /// <summary>Above the HUD (100), below the build stamp (1000) — <c>docs/pitfalls/interface.md</c>.</summary>
        private const int SortingOrder = 200;

        // --- Layout, in pixels of the 1280x720 reference frame, origin at the centre -----------

        /// <summary>Left edge of the text column.</summary>
        private const float ColumnX = -520f;

        /// <summary>Offset of the label from the row's edge: that is where the cursor sits.</summary>
        private const float LabelOffset = 46f;

        private const float RowWidth = 470f;
        private const float RowHeight = 48f;
        private const float RowSpacing = 62f;

        /// <summary>Vertical centre of the entry block — the block stays centred whatever its entry count.</summary>
        private const float RowsCentre = -66f;

        private const float IllustrationX = 300f;
        private const float IllustrationY = 24f;
        private const float IllustrationSide = 390f;

        // --- Animations -----------------------------------------------------------------------

        private const float OpenDuration = 0.42f;
        private const float RowFadeDuration = 0.26f;
        private const float RowsDelay = 0.10f;
        private const float PerRowDelay = 0.07f;
        private const float CloseDuration = 0.16f;

        /// <summary>How far an entry slides to the right as it appears.</summary>
        private const float RowSlide = 34f;

        /// <summary>
        /// Drift of the illustration.
        /// </summary>
        /// <remarks>
        /// ⚠ <c>docs/ART.md</c> §4 forbids "periodic looping flicker over a large area": what it
        /// targets is a variation of <b>opacity</b>. Here opacity does not move — the image travels
        /// 8 px over a 4 s period and tilts by 1.6°. Nothing flickers, and the menu stops looking like
        /// a frozen screenshot.
        /// </remarks>
        private const float DriftPeriod = 4.2f;

        private const float DriftAmplitude = 8f;
        private const float SwayPeriod = 5.3f;
        private const float SwayAmplitude = 1.6f;

        /// <summary>Catch-up speed of the cursor and the highlight (exponential smoothing).</summary>
        private const float SelectionSpeed = 16f;

        private const float SelectionGrowth = 1.07f;

        private enum Phase
        {
            /// <summary>Nothing on screen, no cost: <c>Update</c> returns straight away.</summary>
            Closed,

            Opening,
            Idle,
            Closing
        }

        /// <summary>
        /// Raised when an entry that <b>commits the application</b> has been confirmed, once the
        /// fade-out is finished.
        /// </summary>
        /// <remarks>
        /// "How to play" and "Credits" are not part of it: they open a panel, stay in the menu, and
        /// therefore concern only this component. Only <see cref="MenuEntry.Play"/> and
        /// <see cref="MenuEntry.Quit"/> come back up.
        /// </remarks>
        /// <summary>
        /// The footer line, kept so it can be rewritten if a touchscreen shows up late.
        /// </summary>
        /// <remarks>
        /// ⚠ It is the only label of this screen that names a control, so it is the only one that
        /// becomes a lie when the player turns out to have fingers rather than keys (GDD §3, touch).
        /// </remarks>
        private Text _footer;

        public event Action<MenuEntry> Confirmed;

        /// <summary>Rewrites the labels that name a control (GDD §3, touch).</summary>
        public void RefreshControlLabels()
        {
            if (_footer != null)
            {
                _footer.text = UiText.MenuFooter;
            }

            // The mute label names the M key on a keyboard and stays bare on touch: it depends on
            // the device too, and a label that keeps naming a key to a phone player says the game
            // was not meant for them (docs/pitfalls/interface.md).
            RefreshMuteLabel();
        }


        private readonly List<RectTransform> _rows = new List<RectTransform>();
        private readonly List<Text> _labels = new List<Text>();

        /// <summary>Opacity of each row, set by the cascading opening animation.</summary>
        private readonly List<float> _opacities = new List<float>();

        /// <summary>Highlight progress of each row: 0 at rest, 1 selected.</summary>
        private readonly List<float> _highlights = new List<float>();

        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _illustration;
        private RectTransform _title;
        private RectTransform _tagline;
        private RectTransform _cursor;
        private Image _cursorImage;

        private IReadOnlyList<MenuEntry> _entries;
        private int _index;

        private InfoPanel _help;
        private InfoPanel _credits;

        private Phase _phase = Phase.Closed;
        private float _clock;
        private float _cursorY;
        private MenuEntry _confirmedEntry;

        /// <summary>Pointer position when the menu opened — see <see cref="Hover"/>.</summary>
        private Vector2 _pointerAtOpening;

        private bool _pointerMoved;

        /// <summary>The sound effects, or <c>null</c> when nobody supplied any.</summary>
        /// <remarks>
        /// ⚠ Optional on purpose: this screen is built by <c>SnakeGame</c>, but nothing must make it
        /// depend on sound to work. A menu that throws because there is no audio would be a menu
        /// broken by a missing .wav.
        /// </remarks>
        private SfxPlayer _sfx;

        /// <summary>The mute button's label — it states the state, so it has to follow it.</summary>
        private Text _muteLabel;

        /// <summary>True as long as the menu owns the screen, fade-out included.</summary>
        public bool Active
        {
            get { return _phase != Phase.Closed; }
        }

        /// <summary>True when a reading panel is open: Esc closes it instead of doing nothing.</summary>
        public bool PanelOpen
        {
            get { return _help.Requested || _credits.Requested; }
        }

        private bool Interactive
        {
            get { return _phase == Phase.Idle; }
        }

        private void Awake()
        {
            Build();
        }

        private void Build()
        {
            // ⚠ The availability of "Quit" is decided by the PLATFORM, not by a compilation directive:
            // the web build's menu can therefore be tested in the editor by changing a single value,
            // without producing a twenty-minute WebGL build.
            _entries = MainMenu.Entries(Application.platform != RuntimePlatform.WebGLPlayer);

            Canvas canvas = UiFactory.Canvas(transform, "Menu Canvas", SortingOrder);
            _root = canvas.gameObject;
            _group = _root.AddComponent<CanvasGroup>();

            // Opaque background: the menu never shows the playfield through transparency, even if a
            // caller forgot to hide it. It also intercepts clicks (raycastTarget).
            Image background = UiFactory.Scrim(_root.transform, "Background", UiPalette.Background);
            background.raycastTarget = true;

            BuildIllustration(_root.transform);

            Font headingFont = UiFonts.Load(UiFonts.Headings);
            Font bodyFont = UiFonts.Load(UiFonts.Body);

            Text title = UiFactory.Text(_root.transform, "GameTitle", headingFont, 64, TextAnchor.MiddleLeft,
                UiPalette.HudText, Centre, new Vector2(ColumnX, 172f), new Vector2(640f, 84f));
            title.text = UiText.GameTitle;
            _title = AlignLeft(title.rectTransform, ColumnX, 172f);

            Text tagline = UiFactory.Text(_root.transform, "Tagline", bodyFont, 21, TextAnchor.MiddleLeft,
                UiPalette.SecondaryText, Centre, new Vector2(ColumnX, 118f), new Vector2(640f, 30f));
            tagline.text = UiText.MenuTagline;
            _tagline = AlignLeft(tagline.rectTransform, ColumnX + 4f, 118f);

            BuildCursor(_root.transform);
            BuildRows(_root.transform, headingFont);

            _footer = UiFactory.Text(_root.transform, "MenuFooter", bodyFont, 18, TextAnchor.LowerCenter,
                UiPalette.SecondaryText, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1100f, 24f));
            _footer.text = UiText.MenuFooter;

            BuildMuteButton(_root.transform, bodyFont);

            // ⚠ The panels are created AFTER the entries: at equal sorting order, uGUI stacks in
            // hierarchy order, and a panel created before would go under the entries it is meant to
            // cover.
            _help = new InfoPanel(_root.transform, "HelpPanel",
                UiText.HowToPlayTitle, UiText.HowToPlayBody, () => Back());
            _credits = new InfoPanel(_root.transform, "CreditsPanel",
                UiText.CreditsTitle, UiText.CreditsBody, () => Back());

            _root.SetActive(false);
        }

        private static Vector2 Centre
        {
            get { return new Vector2(0.5f, 0.5f); }
        }

        /// <summary>
        /// Rests a rectangle on its left edge.
        /// </summary>
        /// <remarks>
        /// ⚠ <see cref="UiFactory"/> centres the pivot, which suits the whole HUD but not a column:
        /// with a centred pivot, two texts of different lengths do not start at the same x, and the
        /// menu column then reads as a failed alignment.
        /// </remarks>
        private static RectTransform AlignLeft(RectTransform rect, float x, float y)
        {
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }

        /// <summary>The mute button, top right of the menu.</summary>
        /// <remarks>
        /// ⚠ <b>It exists for the web.</b> <c>settings.json</c> is not read under WebGL, so neither
        /// volume applies there: without this button and its M key, a visitor on itch.io who wants
        /// silence can only close the tab. It sits on the menu because that is where the music plays,
        /// and where every player passes before playing.
        ///
        /// <para>⚠ Known limit, accepted: there is <b>no</b> mute button during a game. A touch
        /// player who wants silence mid-run has to come back to the menu. The playfield margins
        /// already carry the pause button and the directional pad, and a third control there would
        /// cost more than it gives — the choice is remembered, so it is made once.</para>
        /// </remarks>
        private void BuildMuteButton(Transform parent, Font font)
        {
            var go = new GameObject("MuteButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(240f, 52f);
            rect.anchoredPosition = new Vector2(-24f, -18f);

            // Transparent raycast target, like the menu entries: the game's Texts are not raycast
            // targets, so the pointer would have nothing to touch.
            Image area = UiFactory.Scrim(rect, "Area", new Color(0f, 0f, 0f, 0f));
            area.raycastTarget = true;

            var clickable = area.gameObject.AddComponent<ClickableArea>();
            clickable.Clicked = () =>
            {
                AudioMute.Toggle();

                // ⚠ Played AFTER the toggle, so that UNMUTING is heard: it is the only feedback that
                // the button did anything, on a screen where the music takes a moment to come back.
                // Muting stays silent, which is exactly what was asked for.
                if (_sfx != null)
                {
                    _sfx.Play(Sfx.MenuConfirm);
                }
            };

            _muteLabel = UiFactory.Text(rect, "Label", font, 18, TextAnchor.MiddleRight,
                UiPalette.SecondaryText, Centre, Vector2.zero, new Vector2(232f, 40f));

            // ⚠ Subscribed, not polled: the M key flips the state from SnakeGame, and a label that
            // only refreshed on a click would tell the opposite of the truth after a keypress.
            AudioMute.Changed += RefreshMuteLabel;
            RefreshMuteLabel();
        }

        private void OnDestroy()
        {
            // A static event outlives what subscribed to it: without this, a destroyed menu keeps
            // being called and Unity raises a MissingReferenceException far from the cause.
            AudioMute.Changed -= RefreshMuteLabel;
        }

        /// <summary>Puts the label back in step with the state — and with the input device.</summary>
        private void RefreshMuteLabel()
        {
            if (_muteLabel != null)
            {
                _muteLabel.text = UiText.SoundToggle(AudioMute.Muted);
            }
        }

        private void BuildIllustration(Transform parent)
        {
            // ⚠ Loaded BY PATH from Resources/: the menu holds no serialised reference (the scene is
            // regenerated on every build). The PNG is produced by
            // `tools/generate_snake_illustration.py` and imported as a Sprite by
            // `Assets/Editor/ImportIllustrations.cs`.
            Sprite illustration = Resources.Load<Sprite>("Illustrations/snake-menu");

            var go = new GameObject("Illustration");
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;

            _illustration = go.GetComponent<RectTransform>();
            _illustration.anchorMin = Centre;
            _illustration.anchorMax = Centre;
            _illustration.pivot = Centre;
            _illustration.anchoredPosition = new Vector2(IllustrationX, IllustrationY);
            _illustration.sizeDelta = new Vector2(IllustrationSide, IllustrationSide);

            if (illustration == null)
            {
                // ⚠ This case raises nothing on its own: `Resources.Load<Sprite>` returns `null` both
                // when the file is missing and when it is imported as a TEXTURE instead of a sprite
                // (docs/pitfalls/assets-import.md). Without this message, the menu would simply appear
                // with its illustration missing, and the cause would be invisible.
                Debug.LogError("Illustration not found: Resources/Illustrations/snake-menu — "
                               + "re-run \"py tools/generate_snake_illustration.py\" then a build. "
                               + "If the file exists, check that it is imported as a Sprite.");
                go.SetActive(false);
                return;
            }

            image.sprite = illustration;
        }

        /// <summary>
        /// The selection cursor: a red <b>diamond</b>, the game's apple.
        /// </summary>
        /// <remarks>
        /// ⚠ The shape reuses the apple's (ART §4: information never rests on colour alone). A player
        /// who has not yet started a game learns at a glance what the red shape means, and the menu
        /// does not spend one more symbol on it.
        /// </remarks>
        private void BuildCursor(Transform parent)
        {
            _cursorImage = UiFactory.Rectangle(parent, "Cursor", UiPalette.Apple, Centre,
                new Vector2(ColumnX + 16f, 0f), new Vector2(16f, 16f));
            _cursor = _cursorImage.rectTransform;
            _cursor.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private void BuildRows(Transform parent, Font font)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                // The RectTransform is requested AT CREATION: added afterwards to a GameObject that
                // already carries a Transform, it forces Unity to replace the component, which works
                // but depends on an implementation detail.
                var go = new GameObject("Entry" + _entries[i], typeof(RectTransform));
                go.transform.SetParent(parent, false);

                var row = (RectTransform)go.transform;
                row.anchorMin = Centre;
                row.anchorMax = Centre;
                row.pivot = new Vector2(0f, 0.5f);
                row.sizeDelta = new Vector2(RowWidth, RowHeight);
                row.anchoredPosition = new Vector2(ColumnX, RowY(i));

                // Hover area: a fully transparent image. This is not a palette colour, it is a
                // raycast target — every `Text` in the game has `raycastTarget = false`, so the mouse
                // would have nothing to touch.
                Image area = UiFactory.Scrim(row, "Area", new Color(0f, 0f, 0f, 0f));
                area.raycastTarget = true;

                int rank = i; // ⚠ captured in a local: `i` would be _entries.Count
                var clickable = area.gameObject.AddComponent<ClickableArea>();
                clickable.Hovered = () => Hover(rank);
                clickable.Clicked = () => { Hover(rank); Confirm(); };

                Text label = UiFactory.Text(row, "Label", font, 30, TextAnchor.MiddleLeft,
                    UiPalette.SecondaryText, new Vector2(0f, 0.5f), Vector2.zero,
                    new Vector2(RowWidth - LabelOffset, 40f));
                label.text = UiText.EntryLabel(_entries[i]);
                AlignLeft(label.rectTransform, LabelOffset, 0f);

                _rows.Add(row);
                _labels.Add(label);
                _opacities.Add(1f);
                _highlights.Add(0f);
            }
        }

        /// <summary>Y of an entry: the block stays centred on <see cref="RowsCentre"/>.</summary>
        private float RowY(int rank)
        {
            float halfBlock = (_entries.Count - 1) * RowSpacing / 2f;
            return RowsCentre + halfBlock - (rank * RowSpacing);
        }

        /// <summary>Shows the menu and replays its opening animation.</summary>
        /// <remarks>
        /// ⚠ Selection returns to the <b>first entry</b> on every opening, including on the way back
        /// from a game: "Play" is what the player wants nine times out of ten, and finding the cursor
        /// where it was left is only useful in a long menu.
        /// </remarks>
        /// <summary>Gives the menu the voice it uses for its cursor and its confirmations.</summary>
        public void UseSfx(SfxPlayer sfx)
        {
            _sfx = sfx;
        }

        public void Open()
        {
            _root.SetActive(true);
            _group.alpha = 0f;
            _group.blocksRaycasts = true;

            _phase = Phase.Opening;
            _clock = 0f;
            _index = 0;

            _help.Close();
            _credits.Close();

            // The cursor is PLACED, not interpolated: at opening it has no previous position to slide
            // from, and a slide down from the top of the screen would read as a defect.
            _cursorY = RowY(0);

            // ⚠ The pointer only takes over once it has MOVED (see Hover).
            _pointerAtOpening = PointerPosition();
            _pointerMoved = false;

            for (int i = 0; i < _rows.Count; i++)
            {
                _opacities[i] = 0f;
                _highlights[i] = i == 0 ? 1f : 0f;
            }

            ApplySelection();
        }

        /// <summary>Closes at once, with no fade and no event. For a caller taking over.</summary>
        public void CloseImmediately()
        {
            _phase = Phase.Closed;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _root.SetActive(false);
        }

        /// <summary>Moves the selection (GDD §4.6: wrap-around, and nothing on sideways directions).</summary>
        public void Move(Direction direction)
        {
            if (!Interactive || PanelOpen)
            {
                return;
            }

            int next;
            if (MainMenu.Move(_index, _entries.Count, direction, out next))
            {
                // ⚠ Only when the index actually changes. MainMenu.Move answers true for a movement
                // that stays put at the end of the list, and a cursor sound on a cursor that did not
                // move tells the player they moved when they did not.
                if (next != _index && _sfx != null)
                {
                    _sfx.Play(Sfx.MenuMove);
                }

                _index = next;
            }
        }

        /// <summary>Confirms the current entry — or closes the open panel.</summary>
        public void Confirm()
        {
            if (!Interactive)
            {
                return;
            }

            if (_sfx != null)
            {
                _sfx.Play(Sfx.MenuConfirm);
            }

            if (PanelOpen)
            {
                // Enter and Space also close: a panel that closes ONLY with Esc traps the player who
                // has just learned that this menu confirms with Enter.
                Back();
                return;
            }

            MenuEntry entry = _entries[MainMenu.Clamp(_index, _entries.Count)];

            switch (entry)
            {
                case MenuEntry.HowToPlay:
                    _help.Open();
                    return;

                case MenuEntry.Credits:
                    _credits.Open();
                    return;

                default:
                    _confirmedEntry = entry;
                    _phase = Phase.Closing;
                    _clock = 0f;
                    _group.blocksRaycasts = false;
                    return;
            }
        }

        /// <summary>Closes the open panel. Returns true if there was one.</summary>
        public bool Back()
        {
            if (!PanelOpen)
            {
                return false;
            }

            _help.Close();
            _credits.Close();
            return true;
        }

        /// <summary>
        /// The pointer entered an entry: it becomes the current selection.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Until the mouse has moved, it selects nothing</b>, and that is not a theoretical
        /// precaution: the menu opens under a still cursor — at game launch, on the way back from a
        /// game, when the window regains the foreground — and uGUI then sends a "pointer entered" for
        /// a mouse nobody touched. Without that lock, the selection jumps to whichever entry happens
        /// to sit under the cursor, and the player pressing Enter while thinking they are starting a
        /// game <b>quits the game</b>. Observed on 2026-08-28, while driving the build: the cursor sat
        /// on "Quit" and the game closed on the first key.
        /// </remarks>
        private void Hover(int rank)
        {
            if (!Interactive || PanelOpen || !_pointerMoved)
            {
                return;
            }

            int next = MainMenu.Clamp(rank, _entries.Count);

            // The mouse gets the same sound as the arrows — an interface that answers to the
            // keyboard and stays mute to the mouse reads as half broken.
            if (next != _index && _sfx != null)
            {
                _sfx.Play(Sfx.MenuMove);
            }

            _index = next;
        }

        /// <summary>True as soon as the pointer has moved since the menu opened.</summary>
        /// <remarks>
        /// The threshold is in squared screen pixels: two or three pixels of drift are not an
        /// intention, and an optical mouse produces them on its own.
        /// </remarks>
        private void WatchPointer()
        {
            if (_pointerMoved)
            {
                return;
            }

            _pointerMoved = (PointerPosition() - _pointerAtOpening).sqrMagnitude > 16f;
        }

        private static Vector2 PointerPosition()
        {
            Mouse mouse = Mouse.current;
            return mouse == null ? Vector2.zero : mouse.position.ReadValue();
        }

        private void Update()
        {
            if (_phase == Phase.Closed)
            {
                return;
            }

            float step = Time.unscaledDeltaTime;
            _clock += step;

            if (_phase == Phase.Opening)
            {
                AnimateOpening();
            }
            else if (_phase == Phase.Closing)
            {
                _group.alpha = Mathf.Clamp01(1f - (_clock / CloseDuration));

                if (_clock >= CloseDuration)
                {
                    Finish();
                    return;
                }
            }

            WatchPointer();
            AnimateIllustration();
            AnimateSelection(step);

            _help.Animate(step);
            _credits.Animate(step);
        }

        private void AnimateOpening()
        {
            float progress = Ease(Mathf.Clamp01(_clock / OpenDuration));
            _group.alpha = progress;

            // Title and tagline rise by a few pixels: the movement says "this has just arrived"
            // without the player having to wait.
            _title.anchoredPosition = new Vector2(ColumnX, 172f - (14f * (1f - progress)));
            _tagline.anchoredPosition = new Vector2(ColumnX + 4f, 118f - (10f * (1f - progress)));

            for (int i = 0; i < _rows.Count; i++)
            {
                float t = Ease(Mathf.Clamp01(
                    (_clock - RowsDelay - (i * PerRowDelay)) / RowFadeDuration));

                _opacities[i] = t;
                _rows[i].anchoredPosition = new Vector2(ColumnX - (RowSlide * (1f - t)), RowY(i));
            }

            if (_clock >= TotalOpeningDuration)
            {
                _phase = Phase.Idle;
                SettleAtRest();
            }
        }

        private float TotalOpeningDuration
        {
            get
            {
                float cascade = RowsDelay + ((_rows.Count - 1) * PerRowDelay) + RowFadeDuration;
                return Mathf.Max(OpenDuration, cascade);
            }
        }

        private void SettleAtRest()
        {
            _group.alpha = 1f;
            _title.anchoredPosition = new Vector2(ColumnX, 172f);
            _tagline.anchoredPosition = new Vector2(ColumnX + 4f, 118f);

            for (int i = 0; i < _rows.Count; i++)
            {
                _opacities[i] = 1f;
                _rows[i].anchoredPosition = new Vector2(ColumnX, RowY(i));
            }
        }

        private void AnimateIllustration()
        {
            if (_illustration == null)
            {
                return;
            }

            float time = Time.unscaledTime;
            float drift = Mathf.Sin(time * 2f * Mathf.PI / DriftPeriod) * DriftAmplitude;
            float sway = Mathf.Sin(time * 2f * Mathf.PI / SwayPeriod) * SwayAmplitude;

            float scale = _phase == Phase.Opening
                ? Mathf.Lerp(0.93f, 1f, Ease(Mathf.Clamp01(_clock / OpenDuration)))
                : 1f;

            _illustration.anchoredPosition = new Vector2(IllustrationX, IllustrationY + drift);
            _illustration.localRotation = Quaternion.Euler(0f, 0f, sway);
            _illustration.localScale = new Vector3(scale, scale, 1f);
        }

        private void AnimateSelection(float step)
        {
            // Exponential smoothing: frame-rate independent, unlike a `Lerp(a, b, 0.2f)` per frame
            // which runs twice as fast at 120 Hz as at 60.
            float catchUp = 1f - Mathf.Exp(-SelectionSpeed * step);

            _cursorY = Mathf.Lerp(_cursorY, RowY(_index), catchUp);

            for (int i = 0; i < _rows.Count; i++)
            {
                _highlights[i] = Mathf.Lerp(_highlights[i], i == _index ? 1f : 0f, catchUp);
            }

            ApplySelection();
        }

        private void ApplySelection()
        {
            _cursor.anchoredPosition = new Vector2(ColumnX + 16f, _cursorY);

            Color red = UiPalette.Apple;
            red.a = _opacities[MainMenu.Clamp(_index, _opacities.Count)];
            _cursorImage.color = red;

            for (int i = 0; i < _rows.Count; i++)
            {
                // Opacity comes from the cascading opening, the tint from the selection: both
                // animations write the same colour, so they must be composed rather than overwrite
                // each other.
                Color colour = Color.Lerp(UiPalette.SecondaryText, UiPalette.HudText, _highlights[i]);
                colour.a = _opacities[i];
                _labels[i].color = colour;

                float scale = Mathf.Lerp(1f, SelectionGrowth, _highlights[i]);
                _rows[i].localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void Finish()
        {
            _phase = Phase.Closed;
            _group.alpha = 0f;
            _root.SetActive(false);

            if (Confirmed != null)
            {
                Confirmed(_confirmedEntry);
            }
        }

        /// <summary>Cubic ease-out: fast at the start, settled at the end.</summary>
        private static float Ease(float t)
        {
            float remaining = 1f - t;
            return 1f - (remaining * remaining * remaining);
        }
    }
}
