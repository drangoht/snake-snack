using System.Collections.Generic;
using SnakeSnack.Rules;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace SnakeSnack.Gameplay
{
    /// <summary>
    /// Reads the fingers and turns them into the same requests the keyboard makes
    /// (GDD §3, touch — reopened on 2026-08-30).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This class converts and sequences; it judges nothing.</b> Which direction a travel names
    /// is <see cref="Swipe"/>, which control a point lands on is <see cref="TouchPad"/> — both tested
    /// without an engine. What lives here is only what needs a camera and a frame clock.
    ///
    /// <para>⚠ <b>Screen pixels are not reference pixels.</b> A swipe threshold expressed in raw
    /// screen pixels would mean a different gesture on every panel — long on a tablet, twitchy on a
    /// small phone. Everything is converted through
    /// <see cref="Camera.ScreenToWorldPoint(Vector3)"/> first, because the camera is set so that one
    /// world unit is exactly one pixel of the 1280×720 frame (<see cref="Board"/>), which is the unit
    /// the two rules above speak.</para>
    ///
    /// <para>⚠ <b>A finger that lands on a key never also swipes.</b> Without that lock, a thumb
    /// resting on the pad and drifting by 28 px would fire a second, unasked-for turn — the pad would
    /// feel like it had a mind of its own.</para>
    /// </remarks>
    public sealed class TouchInput
    {
        private readonly Camera _camera;
        private readonly TouchPad _pad;
        private readonly bool _hasPad;
        private readonly List<Direction> _directions = new List<Direction>(4);
        private readonly Gesture[] _gestures = new Gesture[MaxFingers];

        private bool _pauseRequested;
        private bool _tapped;

        /// <summary>
        /// Finger slots followed. Ten is what the Input System pools; a Snake needs one, and the
        /// extra slots exist only so a stray palm does not shift the finger that plays.
        /// </summary>
        private const int MaxFingers = 10;

        /// <param name="camera">The orthographic camera, one world unit per reference pixel.</param>
        /// <param name="pad">Where the on-screen controls sit.</param>
        /// <param name="hasPad">
        /// <c>false</c> when the playfield left no margin wide enough for a pad
        /// (<see cref="TouchPad.Fits"/>): the game is then steered by swipes alone, which still
        /// plays, rather than by controls drawn over the cells the player dies against.
        /// </param>
        public TouchInput(Camera camera, TouchPad pad, bool hasPad)
        {
            _camera = camera;
            _pad = pad;
            _hasPad = hasPad;
        }

        /// <summary>
        /// True when the machine has a touchscreen at all.
        /// </summary>
        /// <remarks>
        /// ⚠ Desktop Chrome provides <b>no</b> <see cref="Touchscreen"/>: the property stays false
        /// there and every touch path exits, silently and correctly. Only the <c>?touch</c> mode of
        /// the web template (which calls <c>TouchSimulation.Enable()</c>) makes it true on a desktop
        /// — that is how the port is tested without a phone
        /// (<c>docs/pitfalls/touch-mobile.md</c>).
        /// </remarks>
        /// <remarks>
        /// ⚠ <c>Touchscreen.all</c> is the inherited <c>InputDevice.all</c> — <b>every</b> device, a
        /// keyboard included. Counting it would report a touchscreen on any machine at all; the list
        /// has to be filtered by type.
        /// </remarks>
        public static bool Available
        {
            get { return ActiveScreen() != null; }
        }

        /// <summary>
        /// The touchscreen a finger is actually on — which is not always <see cref="InputSystem"/>'s
        /// "current" one.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>A machine can carry several touchscreens</b>, and this is not a theoretical worry: a
        /// Windows laptop with a digitizer, or a browser reporting touch capability, gives one that
        /// never receives anything, while <c>?touch</c> simulation feeds a second. Reading only
        /// <c>Touchscreen.current</c> polls the idle one and the game answers nothing — with the pad
        /// drawn, the labels in touch mode, and no error anywhere. Measured on the web build, where
        /// the very same code worked on Windows.
        ///
        /// <para>A device carrying a finger wins over one that has just let go, which in turn wins
        /// over <c>current</c>: an idle device must never hide the one being used.</para>
        /// </remarks>
        private static Touchscreen ActiveScreen()
        {
            Touchscreen releasing = null;

            for (int i = 0; i < InputSystem.devices.Count; i++)
            {
                Touchscreen device = InputSystem.devices[i] as Touchscreen;
                if (device == null)
                {
                    continue;
                }

                for (int t = 0; t < device.touches.Count; t++)
                {
                    TouchPhase phase = device.touches[t].phase.ReadValue();

                    if (phase == TouchPhase.Began
                        || phase == TouchPhase.Moved
                        || phase == TouchPhase.Stationary)
                    {
                        return device;
                    }

                    if (releasing == null
                        && (phase == TouchPhase.Ended || phase == TouchPhase.Canceled))
                    {
                        releasing = device;
                    }
                }
            }

            return releasing ?? Touchscreen.current;
        }

        /// <summary>The control currently held, for the view that has to show the press.</summary>
        public TouchTarget PressedTarget { get; private set; }

        /// <summary>Reads this frame's fingers. Call once, before draining the requests below.</summary>
        public void Poll()
        {
            _directions.Clear();
            _pauseRequested = false;
            _tapped = false;

            Touchscreen screen = ActiveScreen();
            if (screen == null)
            {
                PressedTarget = TouchTarget.None;
                return;
            }

            TouchTarget held = TouchTarget.None;
            int count = Mathf.Min(screen.touches.Count, MaxFingers);

            for (int i = 0; i < count; i++)
            {
                TouchControl touch = screen.touches[i];
                TouchPhase phase = touch.phase.ReadValue();
                int id = touch.touchId.ReadValue();

                // ⚠ A phase is a STATE, not an EVENT. `Began` keeps being reported for as long as no
                // new state arrives — measured at six consecutive frames for a single click. Taking
                // it for the press itself queued six turns for one thumb, which the depth-2 queue of
                // §4.2 then spent its whole budget rejecting as duplicates. The press is the
                // TRANSITION: a slot that was not carrying this finger and now is.
                bool down = phase == TouchPhase.Began
                    || phase == TouchPhase.Moved
                    || phase == TouchPhase.Stationary;

                if (down)
                {
                    if (!_gestures[i].Active || _gestures[i].TouchId != id)
                    {
                        Begin(i, touch, id);
                    }
                    else
                    {
                        Continue(i, touch);
                    }
                }
                else if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
                {
                    End(i);
                }

                if (_gestures[i].Active && _gestures[i].Key != TouchTarget.None)
                {
                    held = _gestures[i].Key;
                }
            }

            PressedTarget = held;
        }

        /// <summary>Takes the next direction asked for this frame, in the order it was asked.</summary>
        public bool TryTakeDirection(out Direction direction)
        {
            if (_directions.Count == 0)
            {
                direction = Direction.East;
                return false;
            }

            direction = _directions[0];
            _directions.RemoveAt(0);
            return true;
        }

        /// <summary>True once if the pause control was pressed this frame.</summary>
        public bool TakePause()
        {
            bool requested = _pauseRequested;
            _pauseRequested = false;
            return requested;
        }

        /// <summary>
        /// True once if a finger landed and lifted without travelling — the only press a mobile
        /// player has for "restart" and "confirm".
        /// </summary>
        public bool TakeTap()
        {
            bool tapped = _tapped;
            _tapped = false;
            return tapped;
        }

        private void Begin(int slot, TouchControl touch, int touchId)
        {
            Vector2 point = ToReferenceFrame(touch);
            TouchTarget key = _hasPad ? _pad.HitTest(point.x, point.y) : TouchTarget.None;

            _gestures[slot] = new Gesture
            {
                Active = true,
                TouchId = touchId,
                Origin = point,
                Key = key,
                Turned = false
            };

            if (key == TouchTarget.Pause)
            {
                _pauseRequested = true;
                return;
            }

            if (TouchPad.TryDirection(key, out Direction direction))
            {
                _directions.Add(direction);
            }
        }

        private void Continue(int slot, TouchControl touch)
        {
            Gesture gesture = _gestures[slot];
            if (!gesture.Active || gesture.Key != TouchTarget.None)
            {
                // A finger holding a key is not swiping. Holding a direction does not repeat either:
                // the input queue is two deep (§4.2) and a key repeat would fill it with the same
                // turn, then ignore the one the player actually wants next.
                return;
            }

            Vector2 point = ToReferenceFrame(touch);
            Vector2 travel = point - gesture.Origin;
            SwipeReading reading = Swipe.Read(travel.x, travel.y);

            if (!reading.Recognised)
            {
                return;
            }

            _directions.Add(reading.Direction);

            // ⚠ The origin is re-armed at the point the turn fired, and the gesture stays alive: that
            // is what lets an L-shaped turn be drawn in one stroke without lifting the finger — the
            // exact case the depth-2 queue of §4.2 was sized for.
            gesture.Origin = point;
            gesture.Turned = true;
            _gestures[slot] = gesture;
        }

        private void End(int slot)
        {
            Gesture gesture = _gestures[slot];

            if (gesture.Active && gesture.Key == TouchTarget.None && !gesture.Turned)
            {
                _tapped = true;
            }

            _gestures[slot] = default;
        }

        /// <summary>
        /// A screen point in the 1280×720 reference frame, origin at the centre, Y upwards — the
        /// unit <see cref="Board"/>, <see cref="TouchPad"/> and <see cref="Swipe"/> all speak.
        /// </summary>
        private Vector2 ToReferenceFrame(TouchControl touch)
        {
            Vector2 screen = touch.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            return new Vector2(world.x, world.y);
        }

        private struct Gesture
        {
            public bool Active;

            /// <summary>
            /// Which finger this slot is carrying. ⚠ The slot is a POOL entry, reused by the next
            /// finger: without the id, a new touch landing in a slot whose phase still read
            /// <c>Began</c> would be mistaken for the previous one still being held.
            /// </summary>
            public int TouchId;
            public Vector2 Origin;
            public TouchTarget Key;
            public bool Turned;
        }
    }
}
