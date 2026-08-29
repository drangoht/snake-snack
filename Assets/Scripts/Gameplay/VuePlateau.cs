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
        /// <summary>Rayon des coins du serpent, en fraction du côté (<c>docs/art/cartoon.md</c> §3.1).</summary>
        private const float RayonSegment = 0.28f;

        /// <summary>Rayon des coins de la pomme — plus serré, pour qu'elle reste un losange franc (§3.2).</summary>
        private const float RayonPomme = 0.18f;

        private const double DureeGulp = 0.090;
        private const double DureePop = 0.140;
        private const double DureeFlashMort = 0.220;
        private const double AmplitudeGulp = 0.15;
        private const double DepassementPop = 0.12;

        private readonly List<SpriteRenderer> _segments = new List<SpriteRenderer>();

        /// <summary>Position d'où part chaque segment de rendu, et celle où il va (juicy §4).</summary>
        private readonly List<Vector3> _departs = new List<Vector3>();
        private readonly List<Vector3> _arrivees = new List<Vector3>();

        private Plateau _plateau;

        /// <summary>Conteneur de tout ce que cette vue dessine — voir <see cref="Montrer"/>.</summary>
        private Transform _racine;

        private Transform _racineSegments;
        private Transform _chevron;
        private SpriteRenderer[] _barresChevron;
        private SpriteRenderer _pomme;
        private SpriteRenderer _flashMort;

        private int _segmentsVisibles;
        private double _dureeTick = Cadence.DureeTickParDefautSecondes;

        /// <summary>
        /// ⚠ Un drapeau, et non un test de nullité sur <c>_plateau</c> : <see cref="Plateau"/> est un
        /// <b>type valeur</b>, il vaut donc « zéro » et jamais « rien ». <c>Update</c> tourne dès que
        /// le composant existe, c'est-à-dire avant <see cref="Construire"/> — sans cette garde, la
        /// première image lirait un plateau vide.
        /// </summary>
        private bool _construit;

        /// <summary>Enveloppes en cours. <c>double.NegativeInfinity</c> = éteinte.</summary>
        private double _debutGlissement = double.NegativeInfinity;
        private double _debutGulp = double.NegativeInfinity;
        private double _debutPop = double.NegativeInfinity;
        private double _debutFlash = double.NegativeInfinity;

        private int _indexPop = -1;
        private Direction _directionGulp = Direction.Est;

        /// <summary>
        /// Durée d'un tick, pour que le glissement dure exactement le temps d'une case.
        /// </summary>
        /// <remarks>
        /// ⚠ Reçue de <see cref="JeuSnake"/> plutôt que recopiée : la cadence est réglable
        /// (<c>reglages.json</c>), et une interpolation figée à 125 ms sur un jeu retuné à 6 ticks/s
        /// verrait le serpent arriver puis attendre — un mouvement saccadé qu'aucune erreur ne
        /// signalerait.
        /// </remarks>
        public void ReglerDureeTick(double dureeTickSecondes)
        {
            _dureeTick = dureeTickSecondes;
        }

        /// <summary>Construit l'aire de jeu. À appeler une fois, avant tout dessin.</summary>
        public void Construire(Plateau plateau)
        {
            _plateau = plateau;

            // ⚠ Tout le rendu du plateau est parenté à CE conteneur, et non au composant lui-même :
            // c'est lui qu'on éteint d'un bloc quand le menu prend l'écran (GDD §4.6). Éteindre le
            // GameObject du composant arrêterait aussi JeuSnake et le HUD, qui vivent dessus.
            var conteneur = new GameObject("Plateau");
            conteneur.transform.SetParent(transform, false);
            _racine = conteneur.transform;

            ConstruireAire();
            ConstruireTraitsDeGrille();
            ConstruireBordure();

            ConstruirePomme();

            var racine = new GameObject("Segments");
            racine.transform.SetParent(_racine, false);
            _racineSegments = racine.transform;

            ConstruireFlashMort();
            ConstruireChevron();

            _construit = true;
        }

        /// <summary>
        /// La case qui a tué, mise en évidence le temps d'un aller-retour (<c>juicy.md</c> §6).
        /// </summary>
        /// <remarks>
        /// ⚠ Un carré NET, pas arrondi : c'est un signal, pas une créature. Il réutilise
        /// <see cref="UiPalette.Pictogramme"/>, déjà réservé à ce qui doit dominer — aucun rôle de
        /// couleur n'est ajouté pour un effet (<c>juicy.md</c> §11).
        /// </remarks>
        private void ConstruireFlashMort()
        {
            _flashMort = FormesPrimitives.Rectangle(_racine, "FlashMort", UiPalette.Pictogramme, 20);

            double cote = _plateau.TailleCase - 2.0;
            _flashMort.transform.localScale = new Vector3((float)cote, (float)cote, 1f);
            _flashMort.gameObject.SetActive(false);
        }

        /// <summary>
        /// Montre ou masque tout le plateau (aire, grille, serpent, pomme, chevron).
        /// </summary>
        /// <remarks>
        /// ⚠ Masquer plutôt que détruire : le pool de segments, la pomme et le chevron sont
        /// reconstruits une seule fois pour toute la session. Un aller-retour au menu qui
        /// détruirait puis reconstruirait tout produirait un ramassage de mémoire pile au moment
        /// où la partie démarre.
        /// </remarks>
        public void Montrer(bool visible)
        {
            _racine.gameObject.SetActive(visible);
        }

        /// <summary>
        /// La pomme : un carré tourné à 45°, donc un <b>losange</b>.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>La forme porte l'information, pas la couleur</b> (<c>docs/ART.md</c> §4, et le §5.6
        /// qui impose de construire en niveaux de gris tant que la palette n'est pas tranchée) : le
        /// serpent est fait de carrés pleins qui remplissent presque la case, la pomme est un
        /// losange plus petit et centré. Un joueur qui ne distingue pas deux gris voisins la trouve
        /// quand même. Le jour où la palette arrive, la lisibilité ne repose donc pas dessus.
        ///
        /// <para>La diagonale du losange vaut 0,72 case ; le côté du carré à tourner est cette
        /// diagonale divisée par racine de 2. Poser directement 0,72 en côté donnerait un losange
        /// qui déborde de sa case et vient toucher ses voisines.</para>
        /// </remarks>
        private void ConstruirePomme()
        {
            // ⚠ Coins adoucis, silhouette inchangée (cartoon §3.2) : le losange reste ce qui
            // distingue la pomme du serpent avant même la couleur, y compris pour un daltonien.
            _pomme = FormesPrimitives.RectangleArrondi(_racine, "Pomme", UiPalette.Pomme, 5, RayonPomme);

            float cote = (float)(_plateau.TailleCase * 0.72 / Mathf.Sqrt(2f));
            _pomme.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _pomme.transform.localScale = new Vector3(cote, cote, 1f);
            _pomme.gameObject.SetActive(false);
        }

        /// <summary>
        /// Pose la pomme sur sa case (GDD §4.4).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Aucune image ne doit s'afficher sans pomme</b> (§4.4) : elle est remplacée dans le
        /// tick même où elle est mangée. Une grille vide, même une fraction de seconde, se lit comme
        /// un bug et non comme une transition — d'où le fait que cette méthode ne masque jamais,
        /// elle déplace.
        /// </remarks>
        public void DessinerPomme(Case caseDeLaPomme)
        {
            PointPlateau centre = _plateau.CentreDeLaCase(caseDeLaPomme);

            // ⚠ La position seule est écrite : l'échelle a été posée à la construction, et la
            // réécrire ici avec `Poser` effacerait la rotation à 45° — le losange redeviendrait un
            // carré, indiscernable d'un segment de serpent.
            _pomme.transform.localPosition = new Vector3((float)centre.X, (float)centre.Y, 0f);
            _pomme.gameObject.SetActive(true);
        }

        /// <summary>Retire la pomme de l'écran — uniquement à la victoire, quand il n'y en a plus.</summary>
        public void MasquerPomme()
        {
            _pomme.gameObject.SetActive(false);
        }

        /// <summary>Place un rectangle, en pixels du cadre de référence.</summary>
        private static void Poser(SpriteRenderer rendu, double centreX, double centreY, double largeur, double hauteur)
        {
            rendu.transform.localPosition = new Vector3((float)centreX, (float)centreY, 0f);
            rendu.transform.localScale = new Vector3((float)largeur, (float)hauteur, 1f);
        }

        private void ConstruireAire()
        {
            var fond = FormesPrimitives.Rectangle(_racine, "Aire", UiPalette.AireDeJeu, -100);
            Poser(fond, 0.0, _plateau.DecalageVerticalAire, _plateau.LargeurAire, _plateau.HauteurAire);
        }

        /// <summary>
        /// Les traits de grille. Ils ne sont pas décoratifs : sans repère, le joueur ne peut pas
        /// compter les cases qui le séparent d'un mur, et la mort cesse d'être anticipable (§2).
        /// </summary>
        private void ConstruireTraitsDeGrille()
        {
            var racine = new GameObject("Traits");
            racine.transform.SetParent(_racine, false);

            double gauche = -_plateau.LargeurAire / 2.0;
            double bas = (-_plateau.HauteurAire / 2.0) + _plateau.DecalageVerticalAire;

            for (int x = 1; x < _plateau.Grille.Largeur; x++)
            {
                var trait = FormesPrimitives.Rectangle(racine.transform, "TraitV" + x, UiPalette.TraitDeGrille, -90);
                Poser(trait, gauche + (x * _plateau.TailleCase), _plateau.DecalageVerticalAire, 1.0, _plateau.HauteurAire);
            }

            for (int y = 1; y < _plateau.Grille.Hauteur; y++)
            {
                var trait = FormesPrimitives.Rectangle(racine.transform, "TraitH" + y, UiPalette.TraitDeGrille, -90);
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
            racine.transform.SetParent(_racine, false);

            double centreY = _plateau.DecalageVerticalAire;
            double demiLargeur = _plateau.LargeurAire / 2.0;
            double demiHauteur = _plateau.HauteurAire / 2.0;
            const double epaisseur = 3.0;

            var haut = FormesPrimitives.Rectangle(racine.transform, "Haut", UiPalette.BordureAire, -80);
            Poser(haut, 0.0, centreY + demiHauteur, _plateau.LargeurAire + (2 * epaisseur), epaisseur);

            var bas = FormesPrimitives.Rectangle(racine.transform, "Bas", UiPalette.BordureAire, -80);
            Poser(bas, 0.0, centreY - demiHauteur, _plateau.LargeurAire + (2 * epaisseur), epaisseur);

            var gauche = FormesPrimitives.Rectangle(racine.transform, "Gauche", UiPalette.BordureAire, -80);
            Poser(gauche, -demiLargeur, centreY, epaisseur, _plateau.HauteurAire);

            var droite = FormesPrimitives.Rectangle(racine.transform, "Droite", UiPalette.BordureAire, -80);
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
            go.transform.SetParent(_racine, false);
            _chevron = go.transform;

            double taille = _plateau.TailleMaximalePictogramme;
            float epaisseur = Mathf.Max(2f, (float)(taille * 0.20));
            float branche = (float)(taille * 0.62);

            var gauche = FormesPrimitives.Rectangle(_chevron, "BrancheGauche", UiPalette.Pictogramme, 50);
            gauche.transform.localPosition = new Vector3((float)(-taille * 0.20), (float)(-taille * 0.08), 0f);
            gauche.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            gauche.transform.localScale = new Vector3(branche, epaisseur, 1f);

            var droite = FormesPrimitives.Rectangle(_chevron, "BrancheDroite", UiPalette.Pictogramme, 50);
            droite.transform.localPosition = new Vector3((float)(taille * 0.20), (float)(-taille * 0.08), 0f);
            droite.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            droite.transform.localScale = new Vector3(branche, epaisseur, 1f);

            var barre = FormesPrimitives.Rectangle(_chevron, "Barre", UiPalette.Pictogramme, 51);
            barre.transform.localPosition = new Vector3(0f, (float)(-taille * 0.08), 0f);
            barre.transform.localScale = new Vector3((float)(taille * 1.10), epaisseur, 1f);

            _barresChevron = new[] { gauche, droite, barre };
            _chevron.gameObject.SetActive(false);
        }

        /// <summary>
        /// Dessine le serpent. La tête est plus claire : on doit voir où l'on va.
        /// </summary>
        /// <param name="anime">
        /// <c>true</c> : les segments glissent de leur case précédente vers la nouvelle sur la durée
        /// du tick (<c>juicy.md</c> §4). <c>false</c> : pose immédiate, pour une mort, une pause ou
        /// une nouvelle partie — un serpent qui glisserait vers sa position de départ donnerait
        /// l'impression que la partie a commencé avant l'affichage.
        /// </param>
        public void DessinerSerpent(IReadOnlyList<Case> segments, bool anime = true)
        {
            while (_segments.Count < segments.Count)
            {
                // ⚠ Coins arrondis (cartoon §3.1) : c'est ce seul changement de sprite qui sort la
                // partie du papier millimétré et la raccorde au personnage du menu et de la cover.
                _segments.Add(FormesPrimitives.RectangleArrondi(
                    _racineSegments, "Segment" + _segments.Count, UiPalette.CorpsSerpent, 10, RayonSegment));
            }

            while (_departs.Count < _segments.Count)
            {
                _departs.Add(Vector3.zero);
                _arrivees.Add(Vector3.zero);
            }

            // Un segment de trop est masqué, jamais détruit : le pool resservira à la partie suivante.
            for (int i = segments.Count; i < _segments.Count; i++)
            {
                _segments[i].gameObject.SetActive(false);
            }

            int ancienNombre = _segmentsVisibles;
            _segmentsVisibles = segments.Count;

            double cote = _plateau.TailleCase - 2.0;

            for (int i = 0; i < segments.Count; i++)
            {
                SpriteRenderer rendu = _segments[i];
                rendu.gameObject.SetActive(true);
                rendu.color = i == 0 ? UiPalette.TeteSerpent : UiPalette.CorpsSerpent;
                rendu.sortingOrder = i == 0 ? 11 : 10;

                PointPlateau centre = _plateau.CentreDeLaCase(segments[i]);
                var arrivee = new Vector3((float)centre.X, (float)centre.Y, 0f);

                // ⚠ Un segment qui vient d'apparaître n'a pas de position précédente : le faire
                // partir de zéro le lancerait depuis le centre du plateau, en travers de la grille.
                // Il est posé sur sa case, et c'est le pop de §5 qui le fait grandir.
                bool nouveau = i >= ancienNombre;
                _departs[i] = (anime && !nouveau) ? _segments[i].transform.localPosition : arrivee;
                _arrivees[i] = arrivee;

                rendu.transform.localPosition = _departs[i];
                rendu.transform.localScale = new Vector3((float)cote, (float)cote, 1f);
            }

            _debutGlissement = anime ? Time.timeAsDouble : double.NegativeInfinity;

            if (!anime)
            {
                EteindreEnveloppes();
                AppliquerGlissement(1.0);
            }
        }

        /// <summary>
        /// La bouchée : la tête gonfle perpendiculairement à sa marche, le nouveau segment de queue
        /// apparaît en pop (<c>juicy.md</c> §5).
        /// </summary>
        /// <param name="directionMarche">Direction appliquée au tick de la bouchée.</param>
        public void SignalerBouchee(Direction directionMarche)
        {
            _directionGulp = directionMarche;
            _debutGulp = Time.timeAsDouble;

            _debutPop = Time.timeAsDouble;
            _indexPop = _segmentsVisibles - 1;
        }

        /// <summary>Met en évidence la case où le contact a eu lieu (<c>juicy.md</c> §6).</summary>
        /// <remarks>
        /// ⚠ Pour une morsure, c'est la case mordue ; pour un mur, c'est la case de la tête, et non
        /// celle visée — cette dernière est <b>hors de la grille</b>, donc hors de l'aire : le flash
        /// s'y afficherait sur le fond, au-delà de la bordure, là où aucune case n'existe. Écart
        /// assumé par rapport au brief, qui dit « la case fautive » sans trancher ce cas.
        /// </remarks>
        public void FlasherCase(Case caseFautive)
        {
            PointPlateau centre = _plateau.CentreDeLaCase(caseFautive);
            _flashMort.transform.localPosition = new Vector3((float)centre.X, (float)centre.Y, 0f);
            _debutFlash = Time.timeAsDouble;
            _flashMort.gameObject.SetActive(true);
        }

        /// <summary>
        /// Fige tout ce qui est en cours d'animation, sur sa valeur d'arrivée.
        /// </summary>
        /// <remarks>
        /// Appelé à la pause : un serpent qui continuerait de glisser sous le voile montrerait un
        /// jeu qui tourne encore, exactement ce que la pause dit ne pas faire. ⚠ Le flash de mort
        /// n'en fait pas partie — il est déclenché <i>par</i> la mort et doit se dérouler après.
        /// </remarks>
        public void FigerAnimations()
        {
            AppliquerGlissement(1.0);
            EteindreEnveloppes();
        }

        private void EteindreEnveloppes()
        {
            _debutGlissement = double.NegativeInfinity;
            _debutGulp = double.NegativeInfinity;
            _debutPop = double.NegativeInfinity;
            _indexPop = -1;
        }

        /// <summary>
        /// Le seul endroit où le temps entre dans le rendu : chaque image, on relit les enveloppes
        /// en cours et on repose ce qu'elles décrivent.
        /// </summary>
        /// <remarks>
        /// ⚠ Aucune enveloppe n'écrit dans <c>Rules/</c> : la position logique reste celle du tick,
        /// et l'ancrage du chevron continue de se calculer sur la case, jamais sur ces facteurs
        /// (<c>juicy.md</c> §11).
        /// </remarks>
        private void Update()
        {
            if (!_construit || _segmentsVisibles == 0)
            {
                return;
            }

            double maintenant = Time.timeAsDouble;

            if (_debutGlissement > double.NegativeInfinity)
            {
                double t = Rebond.Progres(_debutGlissement, _dureeTick, maintenant);
                AppliquerGlissement(t);

                if (t >= 1.0)
                {
                    _debutGlissement = double.NegativeInfinity;
                }
            }

            AppliquerEchelles(maintenant);
            AppliquerFlash(maintenant);
        }

        private void AppliquerGlissement(double t)
        {
            for (int i = 0; i < _segmentsVisibles && i < _segments.Count; i++)
            {
                _segments[i].transform.localPosition = Vector3.Lerp(_departs[i], _arrivees[i], (float)t);
            }
        }

        /// <summary>Le gulp de la tête et le pop du nouveau segment, sur l'échelle uniquement.</summary>
        private void AppliquerEchelles(double maintenant)
        {
            float cote = (float)(_plateau.TailleCase - 2.0);

            if (_debutGulp > double.NegativeInfinity)
            {
                double t = Rebond.Progres(_debutGulp, DureeGulp, maintenant);
                float etire = (float)Rebond.Gulp(t, AmplitudeGulp);

                // ⚠ L'axe comprimé est l'INVERSE de l'axe étiré : la tête gonfle sans perdre de
                // surface. Deux facteurs symétriques la feraient rapetisser en avalant.
                float comprime = 1f / etire;
                bool horizontale = _directionGulp == Direction.Est || _directionGulp == Direction.Ouest;

                _segments[0].transform.localScale = horizontale
                    ? new Vector3(cote * comprime, cote * etire, 1f)
                    : new Vector3(cote * etire, cote * comprime, 1f);

                if (t >= 1.0)
                {
                    _debutGulp = double.NegativeInfinity;
                    _segments[0].transform.localScale = new Vector3(cote, cote, 1f);
                }
            }

            if (_debutPop > double.NegativeInfinity && _indexPop >= 0 && _indexPop < _segmentsVisibles)
            {
                double t = Rebond.Progres(_debutPop, DureePop, maintenant);
                float facteur = (float)Rebond.Apparition(t, DepassementPop);
                _segments[_indexPop].transform.localScale = new Vector3(cote * facteur, cote * facteur, 1f);

                if (t >= 1.0)
                {
                    _debutPop = double.NegativeInfinity;
                    _indexPop = -1;
                }
            }
        }

        private void AppliquerFlash(double maintenant)
        {
            if (_debutFlash <= double.NegativeInfinity)
            {
                return;
            }

            double t = Rebond.Progres(_debutFlash, DureeFlashMort, maintenant);

            Color couleur = UiPalette.Pictogramme;
            couleur.a = (float)Rebond.Impulsion(t);
            _flashMort.color = couleur;

            if (t >= 1.0)
            {
                _debutFlash = double.NegativeInfinity;
                _flashMort.gameObject.SetActive(false);
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
                Color couleur = UiPalette.Pictogramme;
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
