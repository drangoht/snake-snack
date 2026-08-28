using System;
using UnityEngine;
using UnityEngine.UI;

namespace SnakeSnack.UI
{
    /// <summary>
    /// Un panneau de lecture du menu — « Comment jouer », « Crédits » : un voile, une carte, un
    /// titre, un corps de texte et le rappel de la touche qui referme.
    /// </summary>
    /// <remarks>
    /// Classe ordinaire et non <c>MonoBehaviour</c> : elle ne fait rien seule, c'est
    /// <see cref="EcranMenu"/> qui l'anime depuis son propre <c>Update</c>. Deux panneaux, une seule
    /// mise en page — un jour où la carte change, les deux changent ensemble.
    ///
    /// <para>⚠ <b>Le voile intercepte le clic</b> : sans cible de raycast derrière la carte, un clic
    /// à côté du panneau tomberait sur les entrées du menu restées dessous, et le joueur lancerait
    /// une partie en croyant refermer un panneau.</para>
    /// </remarks>
    public sealed class PanneauInfo
    {
        /// <summary>Durée du fondu, à l'ouverture comme à la fermeture.</summary>
        private const float DureeFondu = 0.14f;

        private const float LargeurCarte = 880f;

        /// <summary>
        /// ⚠ Dimensionnée sur le texte le plus long (« Comment jouer », neuf lignes).
        /// <b>Une ligne de Nunito occupe environ 1,36 fois le corps</b>, pas 1,0 : à 19 px et
        /// <c>lineSpacing</c> 1,1, une ligne prend ~28 px. Neuf lignes demandent donc ~260 px, quand
        /// le calcul naïf en annonçait 190 — c'est ce qui avait fait tronquer les deux dernières
        /// lignes du panneau, celles qui énoncent ce qui tue.
        /// </summary>
        private const float HauteurCarte = 480f;

        /// <summary>Épaisseur du cadre ambre, la même que la bordure de l'aire de jeu.</summary>
        private const float EpaisseurCadre = 3f;

        private readonly GameObject _racine;
        private readonly CanvasGroup _groupe;

        private bool _ouvert;

        public PanneauInfo(Transform parent, string nom, string titre, string corps, Action fermeture)
        {
            _racine = new GameObject(nom, typeof(RectTransform));
            _racine.transform.SetParent(parent, false);

            var rect = (RectTransform)_racine.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _groupe = _racine.AddComponent<CanvasGroup>();
            _groupe.alpha = 0f;

            Image voile = FabriqueUi.Voile(_racine.transform, "Voile", UiPalette.VoileDePause);
            voile.raycastTarget = true;
            var clic = voile.gameObject.AddComponent<ZoneCliquable>();
            clic.Cliquee = fermeture;

            // Le cadre ambre est un rectangle légèrement plus grand posé DERRIÈRE la carte : deux
            // images valent mieux que quatre traits, et l'ordre de rendu vient de la hiérarchie.
            FabriqueUi.Rectangle(_racine.transform, "Cadre", UiPalette.BordureAire, Ancre,
                Vector2.zero, new Vector2(LargeurCarte + (2f * EpaisseurCadre), HauteurCarte + (2f * EpaisseurCadre)));

            FabriqueUi.Rectangle(_racine.transform, "Carte", UiPalette.AireDeJeu, Ancre,
                Vector2.zero, new Vector2(LargeurCarte, HauteurCarte));

            Font policeTitres = PolicesUi.Charger(PolicesUi.Titres);
            Font policeCourante = PolicesUi.Charger(PolicesUi.Courante);

            FabriqueUi.Texte(_racine.transform, "Titre", policeTitres, 34, TextAnchor.MiddleCenter,
                UiPalette.TexteHud, Ancre, new Vector2(0f, (HauteurCarte / 2f) - 52f),
                new Vector2(LargeurCarte - 80f, 44f)).text = titre;

            // ⚠ Aligné à gauche et en haut : un texte d'aide centré ligne à ligne se relit mal, et
            // les touches en début de ligne doivent s'aligner verticalement pour se comparer.
            Text texte = FabriqueUi.Texte(_racine.transform, "Corps", policeCourante, 19, TextAnchor.UpperLeft,
                UiPalette.TexteHud, Ancre, new Vector2(0f, -6f), new Vector2(LargeurCarte - 140f, HauteurCarte - 180f));
            texte.text = corps;
            texte.lineSpacing = 1.1f;
            texte.horizontalOverflow = HorizontalWrapMode.Wrap;

            // ⚠ Tronqué, pas débordant (le réglage par défaut de la fabrique) : un texte trop long
            // sortait du cadre ambre et passait PAR-DESSUS le « Échap pour revenir » — ce qui se lit
            // comme un défaut de rendu, pas comme un texte trop long. Coupé dans la carte, le
            // problème est visible et se corrige là où il est : dans TextesUi.
            texte.verticalOverflow = VerticalWrapMode.Truncate;

            FabriqueUi.Texte(_racine.transform, "Retour", policeCourante, 18, TextAnchor.MiddleCenter,
                UiPalette.TexteSecondaire, Ancre, new Vector2(0f, (-HauteurCarte / 2f) + 34f),
                new Vector2(LargeurCarte - 80f, 26f)).text = TextesUi.RetourPanneau;

            _racine.SetActive(false);
        }

        private static Vector2 Ancre
        {
            get { return new Vector2(0.5f, 0.5f); }
        }

        /// <summary>Vrai dès l'appui, avant même que le fondu ait commencé.</summary>
        public bool Demande
        {
            get { return _ouvert; }
        }

        public void Ouvrir()
        {
            _ouvert = true;
            _racine.SetActive(true);
            _groupe.blocksRaycasts = true;
        }

        public void Fermer()
        {
            _ouvert = false;
        }

        /// <summary>
        /// Avance le fondu. À appeler à chaque image, ouvert ou non — c'est cet appel constant qui
        /// éteint le panneau une fois son fondu de sortie terminé.
        /// </summary>
        /// <param name="pas">
        /// ⚠ Le pas de temps <b>non mis à l'échelle</b> que lui passe <see cref="EcranMenu"/> : un
        /// panneau du menu doit s'ouvrir et se fermer même si le temps de jeu venait à être arrêté.
        /// </param>
        public void Animer(float pas)
        {
            if (!_racine.activeSelf)
            {
                return;
            }

            float cible = _ouvert ? 1f : 0f;
            _groupe.alpha = Mathf.MoveTowards(_groupe.alpha, cible, pas / DureeFondu);

            if (!_ouvert && _groupe.alpha <= 0f)
            {
                _groupe.blocksRaycasts = false;
                _racine.SetActive(false);
            }
        }
    }
}
