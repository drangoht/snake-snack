using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SnakeSnack.UI
{
    /// <summary>
    /// Makes an interface area respond to mouse hover and clicks.
    /// </summary>
    /// <remarks>
    /// GDD §3 decides "gamepad and touch: not in 0.1" — but the <b>mouse</b> is not part of that
    /// batch: a visitor landing on the itch page has their hand on it, and a menu that does not react
    /// to a click reads as a broken game before it has even started.
    ///
    /// <para>⚠ Hovering <b>moves the selection</b> instead of drawing a second highlight: without
    /// that, keyboard and mouse would show two different "current" entries, and a player pressing
    /// Enter after moving the mouse would launch the other one.</para>
    ///
    /// <para>⚠ This area only exists if its raycast target does: a <c>Text</c> has
    /// <c>raycastTarget = false</c> everywhere in the game, so it is a transparent <c>Image</c> that
    /// receives the pointer. An area with no image raises nothing, it simply never responds.</para>
    /// </remarks>
    public sealed class ClickableArea : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        /// <summary>Called when the pointer enters the area.</summary>
        public Action Hovered;

        /// <summary>Called on a click inside the area.</summary>
        public Action Clicked;

        public void OnPointerEnter(PointerEventData data)
        {
            if (Hovered != null)
            {
                Hovered();
            }
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (Clicked != null)
            {
                Clicked();
            }
        }
    }
}
