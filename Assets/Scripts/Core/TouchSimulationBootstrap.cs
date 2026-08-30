using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace SnakeSnack.Core
{
    /// <summary>
    /// Turns the mouse into a touchscreen on demand, so the mobile port can be looked at without a
    /// phone (<c>docs/pitfalls/touch-mobile.md</c>).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Why this exists at all.</b> Desktop Chrome provides <b>no</b>
    /// <c>Touchscreen</c>: <c>Touchscreen.current</c> stays <c>null</c>, every touch path in the game
    /// exits immediately, and <b>no error says so</b>. Dispatching real <c>TouchEvent</c>s from
    /// JavaScript does not help — they propagate, but the engine has no device to file them under.
    /// Without this switch the only way to see the on-screen pad is to publish and open the page on a
    /// phone, which is the opposite of verifying before shipping.
    ///
    /// <para><b>How to use it.</b> Web: add <c>?touch</c> to the URL. Windows: launch with
    /// <c>-touch</c>. In both cases <see cref="TouchSimulation"/> then reports the mouse as a finger,
    /// so the pad appears and answers.</para>
    ///
    /// <para>⚠ <b>It answers REAL clicks only.</b> A synthetic <c>PointerEvent</c> dispatched from JS
    /// does not reach it. Driving it from a browser session means a genuine click-drag
    /// (<c>left_click_drag</c>), not an event written into the page.</para>
    ///
    /// <para>⚠ <b>Never on by default.</b> Simulation makes <c>Touchscreen.current</c> non-null, which
    /// is precisely the test the game uses to decide it is on a phone: left on, a desktop player
    /// would get the on-screen pad and be told to "tap to play again".</para>
    /// </remarks>
    public static class TouchSimulationBootstrap
    {
        private const string Switch = "touch";

        private static bool _enabled;

        /// <summary>
        /// Runs before the first scene loads, so the simulated device exists by the time
        /// <c>SnakeGame.Awake</c> asks whether there is a touchscreen.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>The order is the whole point.</b> <c>BeforeSceneLoad</c> and not
        /// <c>AfterSceneLoad</c>: the game reads the presence of a touchscreen once, when it builds
        /// its controls and its labels. Enabling the simulation one frame later would give a session
        /// that reads fingers and still shows the keyboard's instructions.
        /// </remarks>
        /// <remarks>
        /// ⚠ <b>Also called explicitly from <c>SnakeGame.Awake</c>, and that is not belt-and-braces
        /// but the fix.</b> On the WebGL build this attribute did <b>not</b> fire — the log line
        /// below never appeared, while the very same build on Windows printed it. Two plausible
        /// causes, and the explicit call settles both without having to tell them apart: IL2CPP
        /// managed stripping is free to remove a class nothing references, and
        /// <see cref="Application.absoluteURL"/> is not documented as populated this early.
        /// <see cref="_enabled"/> keeps the second call from doing the work twice.
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnableIfAsked()
        {
            if (_enabled || !Asked())
            {
                return;
            }

            _enabled = true;

            EnhancedTouchSupport.Enable();
            TouchSimulation.Enable();
            Debug.Log("Touch simulation enabled: the mouse now reports as a finger.");
        }

        private static bool Asked()
        {
            string url = Application.absoluteURL;
            if (!string.IsNullOrEmpty(url)
                && url.IndexOf(Switch, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (string.Equals(arguments[i], "-" + Switch, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
