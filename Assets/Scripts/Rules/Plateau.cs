#nullable enable
using System;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// Un point de l'écran, en pixels, origine au centre du cadre et <b>Y vers le haut</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ Ce type existe pour la même raison que <see cref="Case"/> : <c>Vector2</c> vient
    /// d'<c>UnityEngine</c>, et l'importer ici rendrait toute la géométrie du plateau intestable
    /// hors moteur. La conversion vers le type du moteur appartient à l'appelant.
    ///
    /// <para>Y vers le haut, comme l'axe Y d'Unity et comme la convention Nord = Y croissant de
    /// <see cref="Direction"/> : aucune inversion nulle part, donc aucune inversion à oublier.</para>
    /// </remarks>
    public readonly struct PointPlateau : IEquatable<PointPlateau>
    {
        public PointPlateau(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }

        public bool Equals(PointPlateau autre)
        {
            return X.Equals(autre.X) && Y.Equals(autre.Y);
        }

        public override bool Equals(object? obj)
        {
            return obj is PointPlateau autre && Equals(autre);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ")";
        }
    }

    /// <summary>
    /// La mise en page de l'aire de jeu (GDD §4.3) : taille de case déduite du cadre, et position à
    /// l'écran de chaque case.
    /// </summary>
    /// <remarks>
    /// Le §4.3 pose le calcul : « dans un cadre web 1280×720 avec un bandeau de HUD d'environ
    /// 60 px, la case fait <c>min(1280/21, 660/15)</c> = 44 px ». C'est une <b>formule chiffrée</b>,
    /// donc elle vit ici et non dans un <c>MonoBehaviour</c> : le jour où la grille devient 25 × 17,
    /// personne n'a à refaire la division à la main.
    ///
    /// <para>⚠ <b>Unité : le pixel du cadre de référence 1280×720.</b> Le câblage moteur règle la
    /// caméra pour qu'une unité monde vaille exactement un pixel de ce cadre
    /// (<c>orthographicSize = 360</c>). Sans cette égalité, toutes les valeurs du GDD §4.3
    /// deviendraient fausses à l'écran sans que rien ne le signale — on verrait juste un jeu « pas
    /// tout à fait à la bonne échelle ».</para>
    /// </remarks>
    public readonly struct Plateau
    {
        /// <summary>Largeur du cadre de référence : 1280 px (GDD §4.3).</summary>
        public const int LargeurCadreParDefaut = 1280;

        /// <summary>Hauteur du cadre de référence : 720 px (GDD §4.3).</summary>
        public const int HauteurCadreParDefaut = 720;

        /// <summary>Hauteur du bandeau de HUD : ~60 px (GDD §4.3).</summary>
        public const int HauteurBandeauParDefaut = 60;

        /// <summary>
        /// Plus grande taille de case entière qui laisse la grille entrer dans le cadre, bandeau de
        /// HUD déduit (GDD §4.3).
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Arrondi vers le bas, et taille entière.</b> Une taille fractionnaire (43,7 px)
        /// placerait les cases entre deux pixels : les traits de grille deviendraient irréguliers,
        /// une ligne sur deux d'un pixel de plus, ce qui ne lève rien et se lit comme un défaut de
        /// dessin. Arrondir vers le haut ferait déborder la grille hors du cadre — le mur mortel du
        /// §2 sortirait de l'écran.
        /// </remarks>
        public static int TailleDeCase(
            Grille grille,
            int largeurCadre = LargeurCadreParDefaut,
            int hauteurCadre = HauteurCadreParDefaut,
            int hauteurBandeau = HauteurBandeauParDefaut)
        {
            if (largeurCadre <= 0 || hauteurCadre <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(largeurCadre), largeurCadre,
                    "Le cadre doit avoir des dimensions strictement positives.");
            }

            if (hauteurBandeau < 0 || hauteurBandeau >= hauteurCadre)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hauteurBandeau), hauteurBandeau,
                    "Le bandeau de HUD doit laisser de la place à l'aire de jeu.");
            }

            double parLaLargeur = (double)largeurCadre / grille.Largeur;
            double parLaHauteur = (double)(hauteurCadre - hauteurBandeau) / grille.Hauteur;
            int taille = (int)Math.Floor(Math.Min(parLaLargeur, parLaHauteur));

            if (taille < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(grille), grille.NombreDeCases,
                    "La grille est trop grande pour le cadre : la case ferait moins d'un pixel.");
            }

            return taille;
        }

        /// <param name="grille">Aire de jeu logique.</param>
        /// <param name="tailleCase">Côté d'une case, en pixels — issu de <see cref="TailleDeCase"/>.</param>
        /// <param name="hauteurBandeau">Hauteur du bandeau de HUD, en pixels.</param>
        public Plateau(Grille grille, int tailleCase, int hauteurBandeau = HauteurBandeauParDefaut)
        {
            if (tailleCase < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tailleCase), tailleCase, "Une case fait au moins un pixel.");
            }

            if (hauteurBandeau < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hauteurBandeau), hauteurBandeau,
                    "Le bandeau de HUD ne peut pas avoir une hauteur négative.");
            }

            Grille = grille;
            TailleCase = tailleCase;
            HauteurBandeau = hauteurBandeau;
        }

        /// <summary>Aire de jeu logique à laquelle cette mise en page s'applique.</summary>
        public Grille Grille { get; }

        /// <summary>Côté d'une case, en pixels.</summary>
        public int TailleCase { get; }

        /// <summary>Hauteur du bandeau de HUD réservé en haut du cadre, en pixels.</summary>
        public int HauteurBandeau { get; }

        /// <summary>Largeur de l'aire de jeu, en pixels (924 par défaut).</summary>
        public int LargeurAire
        {
            get { return Grille.Largeur * TailleCase; }
        }

        /// <summary>Hauteur de l'aire de jeu, en pixels (660 par défaut).</summary>
        public int HauteurAire
        {
            get { return Grille.Hauteur * TailleCase; }
        }

        /// <summary>
        /// Décalage vertical du centre de l'aire de jeu par rapport au centre du cadre.
        /// </summary>
        /// <remarks>
        /// L'aire est centrée dans ce qui reste du cadre <b>une fois le bandeau retiré en haut</b> :
        /// le milieu de <c>[-H/2 ; H/2 - bandeau]</c> vaut <c>-bandeau/2</c>, quelle que soit la
        /// taille de l'aire. Poser l'aire au centre du cadre entier la ferait passer sous le
        /// bandeau — le HUD recouvrirait la ligne de cases du haut, ce qui ne lève rien et tue le
        /// joueur contre un mur qu'il n'a pas vu.
        /// </remarks>
        public double DecalageVerticalAire
        {
            get { return -HauteurBandeau / 2.0; }
        }

        /// <summary>Centre d'une case, en pixels, origine au centre du cadre.</summary>
        public PointPlateau CentreDeLaCase(Case caseVisee)
        {
            double x = ((caseVisee.X + 0.5) * TailleCase) - (LargeurAire / 2.0);
            double y = ((caseVisee.Y + 0.5) * TailleCase) - (HauteurAire / 2.0) + DecalageVerticalAire;
            return new PointPlateau(x, y);
        }

        /// <summary>
        /// Taille maximale du pictogramme de refus : la moitié d'une case (<c>docs/ART.md</c> §5.4).
        /// </summary>
        public double TailleMaximalePictogramme
        {
            get { return TailleCase / 2.0; }
        }

        /// <summary>
        /// Où poser le pictogramme de refus : au bord de la case tête, du côté de la direction
        /// refusée (<c>docs/ART.md</c> §5.4).
        /// </summary>
        /// <remarks>
        /// Le brief dit « ancré au bord de la case tête, côté de la direction refusée, décalé
        /// d'environ un quart de case (~11 px) pour ne jamais recouvrir la case elle-même ». Le bord
        /// est à une demi-case du centre, plus un quart de case de décalage : trois quarts de case.
        /// Avec un pictogramme d'une demi-case au plus, il occupe alors exactement l'espace entre le
        /// bord de la case tête et le centre de la case voisine — jamais sur la tête (on la
        /// cacherait), jamais au-delà du centre de la voisine (on le lirait comme un obstacle posé
        /// sur la grille).
        /// </remarks>
        public PointPlateau AncrageRefus(Case tete, Direction directionRefusee)
        {
            PointPlateau centre = CentreDeLaCase(tete);
            Case pas = Directions.Deplacement(directionRefusee);
            double distance = TailleCase * 0.75;
            return new PointPlateau(centre.X + (pas.X * distance), centre.Y + (pas.Y * distance));
        }
    }
}
