using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SnakeSnack.UI
{
    /// <summary>
    /// Rend une zone d'interface sensible au survol et au clic de souris.
    /// </summary>
    /// <remarks>
    /// Le GDD §3 décide « manette et tactile : pas en 0.1 » — mais la <b>souris</b> n'est pas dans ce
    /// lot : un visiteur qui arrive sur la page itch a la main dessus, et un menu qui ne réagit pas
    /// au clic se lit comme un jeu cassé avant même d'avoir démarré.
    ///
    /// <para>⚠ Le survol <b>déplace la sélection</b> au lieu de dessiner un second surlignage : sans
    /// cela, le clavier et la souris afficheraient deux entrées « courantes » différentes, et le
    /// joueur qui tape Entrée après avoir bougé la souris lancerait l'autre.</para>
    ///
    /// <para>⚠ Cette zone n'existe que si sa cible de raycast existe : un <c>Text</c> a
    /// <c>raycastTarget = false</c> partout dans le jeu, c'est donc une <c>Image</c> transparente
    /// qui reçoit le pointeur. Une zone sans image ne lève rien, elle ne répond simplement jamais.</para>
    /// </remarks>
    public sealed class ZoneCliquable : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        /// <summary>Appelé quand le pointeur entre dans la zone.</summary>
        public Action Survolee;

        /// <summary>Appelé au clic dans la zone.</summary>
        public Action Cliquee;

        public void OnPointerEnter(PointerEventData donnees)
        {
            if (Survolee != null)
            {
                Survolee();
            }
        }

        public void OnPointerClick(PointerEventData donnees)
        {
            if (Cliquee != null)
            {
                Cliquee();
            }
        }
    }
}
