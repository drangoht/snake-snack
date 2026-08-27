using System.Collections.Generic;
using SnakeSnack.Rules;
using SnakeSnack.UI;
using UnityEngine;

namespace SnakeSnack.Gameplay
{
    /// <summary>
    /// Dessine l'aire de jeu, le serpent et le retour de refus. Ne décide de rien.
    /// </summary>
    /// <remarks>
    /// Toute position vient de <see cref="Plateau"/> (GDD §4.3) : cette classe ne refait aucun
    /// calcul de mise en page, elle pose des rectangles là où la règle le dit. C'est ce qui permet
    /// de passer la grille en 25 × 17 sans toucher au rendu.
    ///
    /// <para>⚠ Les segments sont un <b>pool réutilisé</b>, jamais détruits ni recréés à chaque
    /// tick : à 8 ticks/s, créer et détruire des <c>GameObject</c> produirait un ramassage de
    /// mémoire régulier, visible en WebGL sous forme de micro-saccades — et une saccade décale la
    /// lecture d'un virage.</para>
    /// </remarks>
    public sealed class VuePlateau : MonoBehaviour
    {
        private readonly List<SpriteRenderer> _segments = new List<SpriteRenderer>();

        private Plateau _plateau;
        private Transform _racineSegments;
        private Transform _chevron;
        private SpriteRenderer[] _barresChevron;

        /// <summary>Construit l'aire de jeu. À appeler une fois, avant tout dessin.</summary>
        public void Construire(Plateau plateau)
        {
            _plateau = plateau;

            ConstruireAire();
            ConstruireTraitsDeGrille();
            ConstruireBordure();

            var racine = new GameObject("Segments");
            racine.transform.SetParent(transform, false);
            _racineSegments = racine.transform;

            ConstruireChevron();
        }

        /// <summary>Place un rectangle, en pixels du cadre de référence.</summary>
        private static void Poser(SpriteRenderer rendu, double centreX, double centreY, double largeur, double hauteur)
        {
            rendu.transform.localPosition = new Vector3((float)centreX, (float)centreY, 0f);
            rendu.transform.localScale = new Vector3((float)largeur, (float)hauteur, 1f);
        }

        private void ConstruireAire()
        {
            var fond = FormesPrimitives.Rectangle(transform, "Aire", PaletteProvisoire.AireDeJeu, -100);
            Poser(fond, 0.0, _plateau.DecalageVerticalAire, _plateau.LargeurAire, _plateau.HauteurAire);
        }

        /// <summary>
        /// Les traits de grille. Ils ne sont pas décoratifs : sans repère, le joueur ne peut pas
        /// compter les cases qui le séparent d'un mur, et la mort cesse d'être anticipable (§2).
        /// </summary>
        private void ConstruireTraitsDeGrille()
        {
            var racine = new GameObject("Traits");
            racine.transform.SetParent(transform, false);

            double gauche = -_plateau.LargeurAire / 2.0;
            double bas = (-_plateau.HauteurAire / 2.0) + _plateau.DecalageVerticalAire;

            for (int x = 1; x < _plateau.Grille.Largeur; x++)
            {
                var trait = FormesPrimitives.Rectangle(racine.transform, "TraitV" + x, PaletteProvisoire.TraitDeGrille, -90);
                Poser(trait, gauche + (x * _plateau.TailleCase), _plateau.DecalageVerticalAire, 1.0, _plateau.HauteurAire);
            }

            for (int y = 1; y < _plateau.Grille.Hauteur; y++)
            {
                var trait = FormesPrimitives.Rectangle(racine.transform, "TraitH" + y, PaletteProvisoire.TraitDeGrille, -90);
                Poser(trait, 0.0, bas + (y * _plateau.TailleCase), _plateau.LargeurAire, 1.0);
            }
        }

        /// <summary>
        /// La bordure de l'aire. ⚠ Elle porte une règle, pas un ornement : les bords <b>tuent</b>
        /// (§2). Une aire dont la limite ne se voit pas produit des morts inexplicables.
        /// </summary>
        private void ConstruireBordure()
        {
            var racine = new GameObject("Bordure");
            racine.transform.SetParent(transform, false);

            double centreY = _plateau.DecalageVerticalAire;
            double demiLargeur = _plateau.LargeurAire / 2.0;
            double demiHauteur = _plateau.HauteurAire / 2.0;
            const double epaisseur = 3.0;

            var haut = FormesPrimitives.Rectangle(racine.transform, "Haut", PaletteProvisoire.BordureAire, -80);
            Poser(haut, 0.0, centreY + demiHauteur, _plateau.LargeurAire + (2 * epaisseur), epaisseur);

            var bas = FormesPrimitives.Rectangle(racine.transform, "Bas", PaletteProvisoire.BordureAire, -80);
            Poser(bas, 0.0, centreY - demiHauteur, _plateau.LargeurAire + (2 * epaisseur), epaisseur);

            var gauche = FormesPrimitives.Rectangle(racine.transform, "Gauche", PaletteProvisoire.BordureAire, -80);
            Poser(gauche, -demiLargeur, centreY, epaisseur, _plateau.HauteurAire);

            var droite = FormesPrimitives.Rectangle(racine.transform, "Droite", PaletteProvisoire.BordureAire, -80);
            Poser(droite, demiLargeur, centreY, epaisseur, _plateau.HauteurAire);
        }

        /// <summary>
        /// Le chevron barré de <c>docs/ART.md</c> §5.4, dessiné pointant vers le <b>nord</b> ; la
        /// direction refusée n'est ensuite qu'une rotation.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>La barre d'interdiction est perpendiculaire à l'axe du chevron, pas diagonale.</b>
        /// Le brief dit « barré d'un trait diagonal » — mais à 45°, ce trait tombe exactement
        /// parallèle à l'une des deux branches et se lit comme une troisième branche, pas comme une
        /// barre. Écart assumé, reporté dans <c>docs/ART.md</c> §5.4.
        /// </remarks>
        private void ConstruireChevron()
        {
            var go = new GameObject("ChevronRefus");
            go.transform.SetParent(transform, false);
            _chevron = go.transform;

            double taille = _plateau.TailleMaximalePictogramme;
            float epaisseur = Mathf.Max(2f, (float)(taille * 0.20));
            float branche = (float)(taille * 0.62);

            var gauche = FormesPrimitives.Rectangle(_chevron, "BrancheGauche", PaletteProvisoire.Pictogramme, 50);
            gauche.transform.localPosition = new Vector3((float)(-taille * 0.20), (float)(-taille * 0.08), 0f);
            gauche.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            gauche.transform.localScale = new Vector3(branche, epaisseur, 1f);

            var droite = FormesPrimitives.Rectangle(_chevron, "BrancheDroite", PaletteProvisoire.Pictogramme, 50);
            droite.transform.localPosition = new Vector3((float)(taille * 0.20), (float)(-taille * 0.08), 0f);
            droite.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            droite.transform.localScale = new Vector3(branche, epaisseur, 1f);

            var barre = FormesPrimitives.Rectangle(_chevron, "Barre", PaletteProvisoire.Pictogramme, 51);
            barre.transform.localPosition = new Vector3(0f, (float)(-taille * 0.08), 0f);
            barre.transform.localScale = new Vector3((float)(taille * 1.10), epaisseur, 1f);

            _barresChevron = new[] { gauche, droite, barre };
            _chevron.gameObject.SetActive(false);
        }

        /// <summary>Dessine le serpent. La tête est plus claire : on doit voir où l'on va.</summary>
        public void DessinerSerpent(IReadOnlyList<Case> segments)
        {
            while (_segments.Count < segments.Count)
            {
                _segments.Add(FormesPrimitives.Rectangle(
                    _racineSegments, "Segment" + _segments.Count, PaletteProvisoire.CorpsSerpent, 10));
            }

            // Un segment de trop est masqué, jamais détruit : le pool resservira à la partie suivante.
            for (int i = segments.Count; i < _segments.Count; i++)
            {
                _segments[i].gameObject.SetActive(false);
            }

            double cote = _plateau.TailleCase - 2.0;

            for (int i = 0; i < segments.Count; i++)
            {
                SpriteRenderer rendu = _segments[i];
                rendu.gameObject.SetActive(true);
                rendu.color = i == 0 ? PaletteProvisoire.TeteSerpent : PaletteProvisoire.CorpsSerpent;
                rendu.sortingOrder = i == 0 ? 11 : 10;

                PointPlateau centre = _plateau.CentreDeLaCase(segments[i]);
                Poser(rendu, centre.X, centre.Y, cote, cote);
            }
        }

        /// <summary>Montre le chevron au bord de la case tête, du côté refusé (ART §5.4).</summary>
        public void AfficherRefus(Case tete, Direction directionRefusee, float opacite)
        {
            PointPlateau ancrage = _plateau.AncrageRefus(tete, directionRefusee);
            _chevron.localPosition = new Vector3((float)ancrage.X, (float)ancrage.Y, 0f);
            _chevron.localRotation = Quaternion.Euler(0f, 0f, AngleDe(directionRefusee));

            for (int i = 0; i < _barresChevron.Length; i++)
            {
                Color couleur = PaletteProvisoire.Pictogramme;
                couleur.a = opacite;
                _barresChevron[i].color = couleur;
            }

            _chevron.gameObject.SetActive(true);
        }

        /// <summary>Éteint le chevron.</summary>
        public void MasquerRefus()
        {
            _chevron.gameObject.SetActive(false);
        }

        /// <summary>Le chevron est dessiné pointant au nord : le reste n'est qu'une rotation.</summary>
        private static float AngleDe(Direction direction)
        {
            switch (direction)
            {
                case Direction.Nord: return 0f;
                case Direction.Ouest: return 90f;
                case Direction.Sud: return 180f;
                default: return 270f;
            }
        }
    }
}
