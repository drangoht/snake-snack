using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>Ce que le design impose à la mise en page de l'aire de jeu (GDD §4.3, ART §5.4).</summary>
public class PlateauTests
{
    /// <summary>
    /// Le §4.3 pose le chiffre lui-même : « la case fait min(1280/21, 660/15) = 44 px ». Si ce test
    /// tombe, c'est soit la formule, soit le cadre de référence qui a bougé — dans les deux cas,
    /// toutes les valeurs de lisibilité du GDD sont à relire.
    /// </summary>
    [Fact]
    public void LaCaseDuGddFaitQuaranteQuatrePixels()
    {
        Assert.Equal(44, Plateau.TailleDeCase(Grille.ParDefaut));
    }

    /// <summary>
    /// Le §4.3 en déduit « la grille occupe 924 px de large et laisse ~178 px de marge de chaque
    /// côté — de quoi poser score et record hors de l'aire de jeu ». Ces marges sont la raison pour
    /// laquelle la grille 32 × 18 a été écartée (§7) : les perdre reprendrait ce débat sans le dire.
    /// </summary>
    [Fact]
    public void LAireDeJeuLaisseDesMargesLateralesPourLeScore()
    {
        Plateau plateau = new Plateau(Grille.ParDefaut, Plateau.TailleDeCase(Grille.ParDefaut));

        Assert.Equal(924, plateau.LargeurAire);
        Assert.Equal(660, plateau.HauteurAire);

        int margeParCote = (Plateau.LargeurCadreParDefaut - plateau.LargeurAire) / 2;
        Assert.Equal(178, margeParCote);
    }

    /// <summary>
    /// L'aire doit tenir SOUS le bandeau de HUD, jamais dessous au sens de « recouverte ». Le HUD
    /// qui mange la ligne du haut ne lève rien : il tue seulement le joueur contre un mur qu'il n'a
    /// pas vu.
    /// </summary>
    [Fact]
    public void LAireDeJeuNeRemonteJamaisSousLeBandeauDeHud()
    {
        Plateau plateau = new Plateau(Grille.ParDefaut, Plateau.TailleDeCase(Grille.ParDefaut));

        double hautDeLAire = plateau.DecalageVerticalAire + (plateau.HauteurAire / 2.0);
        double basDuBandeau = (Plateau.HauteurCadreParDefaut / 2.0) - Plateau.HauteurBandeauParDefaut;

        Assert.True(hautDeLAire <= basDuBandeau,
            $"Le haut de l'aire ({hautDeLAire}) dépasse sous le bandeau ({basDuBandeau}).");
    }

    /// <summary>
    /// La case centrale est le point d'ancrage de la pose de départ (§4.3) : elle doit tomber sur
    /// l'axe vertical du cadre, sans quoi « au centre » (§2) devient faux à l'écran alors qu'il
    /// reste vrai en logique.
    /// </summary>
    [Fact]
    public void LaCaseCentraleTombeSurLAxeDuCadre()
    {
        Plateau plateau = new Plateau(Grille.ParDefaut, Plateau.TailleDeCase(Grille.ParDefaut));

        PointPlateau centre = plateau.CentreDeLaCase(Grille.ParDefaut.Centre);

        Assert.Equal(0.0, centre.X, 9);
        Assert.Equal(plateau.DecalageVerticalAire, centre.Y, 9);
    }

    /// <summary>
    /// Nord = Y croissant (convention de <c>Direction</c>) : avancer au nord doit MONTER à l'écran.
    /// Une inversion ici donne un jeu qui répond « à l'envers » sans lever la moindre erreur.
    /// </summary>
    [Fact]
    public void AvancerAuNordMonteAEcran()
    {
        Plateau plateau = new Plateau(Grille.ParDefaut, Plateau.TailleDeCase(Grille.ParDefaut));
        Case depart = Grille.ParDefaut.Centre;

        PointPlateau avant = plateau.CentreDeLaCase(depart);
        PointPlateau apres = plateau.CentreDeLaCase(Directions.Avance(depart, Direction.Nord));

        Assert.True(apres.Y > avant.Y);
        Assert.Equal(plateau.TailleCase, apres.Y - avant.Y, 9);
    }

    /// <summary>Deux cases voisines sont séparées d'exactement une case, sur les deux axes.</summary>
    [Theory]
    [InlineData(Direction.Nord)]
    [InlineData(Direction.Sud)]
    [InlineData(Direction.Est)]
    [InlineData(Direction.Ouest)]
    public void DeuxCasesVoisinesSontSepareesDUneTailleDeCase(Direction direction)
    {
        Plateau plateau = new Plateau(Grille.ParDefaut, Plateau.TailleDeCase(Grille.ParDefaut));
        Case depart = Grille.ParDefaut.Centre;

        PointPlateau avant = plateau.CentreDeLaCase(depart);
        PointPlateau apres = plateau.CentreDeLaCase(Directions.Avance(depart, direction));

        double distance = Math.Abs(apres.X - avant.X) + Math.Abs(apres.Y - avant.Y);
        Assert.Equal(plateau.TailleCase, distance, 9);
    }

    /// <summary>
    /// ART §5.4 : le pictogramme est « ancré au bord de la case tête […] pour ne jamais recouvrir la
    /// case elle-même », et « ne jamais déborder sur la case voisine et se lire comme un obstacle ».
    /// Le test vérifie ces deux bornes, pas la formule qui les produit.
    /// </summary>
    [Theory]
    [InlineData(Direction.Nord)]
    [InlineData(Direction.Sud)]
    [InlineData(Direction.Est)]
    [InlineData(Direction.Ouest)]
    public void LePictogrammeDeRefusNeRecouvreNiLaTeteNiLeCentreDeLaCaseVoisine(Direction refusee)
    {
        Plateau plateau = new Plateau(Grille.ParDefaut, Plateau.TailleDeCase(Grille.ParDefaut));
        Case tete = Grille.ParDefaut.Centre;

        PointPlateau centreTete = plateau.CentreDeLaCase(tete);
        PointPlateau ancrage = plateau.AncrageRefus(tete, refusee);
        PointPlateau centreVoisine = plateau.CentreDeLaCase(Directions.Avance(tete, refusee));

        // Distance le long de l'axe de la direction refusée : les deux autres composantes sont nulles.
        double distanceAncrage = Distance(centreTete, ancrage);
        double distanceVoisine = Distance(centreTete, centreVoisine);
        double demiPictogramme = plateau.TailleMaximalePictogramme / 2.0;

        double bordProche = distanceAncrage - demiPictogramme;
        double bordLointain = distanceAncrage + demiPictogramme;

        Assert.True(bordProche >= plateau.TailleCase / 2.0,
            $"Le pictogramme mord sur la case tête (bord proche à {bordProche}).");
        Assert.True(bordLointain <= distanceVoisine,
            $"Le pictogramme dépasse le centre de la case voisine (bord lointain à {bordLointain}).");
    }

    /// <summary>Le pictogramme part bien du bon côté : celui de la direction refusée.</summary>
    [Fact]
    public void LePictogrammeSePlaceDuCoteDeLaDirectionRefusee()
    {
        Plateau plateau = new Plateau(Grille.ParDefaut, Plateau.TailleDeCase(Grille.ParDefaut));
        Case tete = Grille.ParDefaut.Centre;
        PointPlateau centre = plateau.CentreDeLaCase(tete);

        Assert.True(plateau.AncrageRefus(tete, Direction.Ouest).X < centre.X);
        Assert.True(plateau.AncrageRefus(tete, Direction.Est).X > centre.X);
        Assert.True(plateau.AncrageRefus(tete, Direction.Nord).Y > centre.Y);
        Assert.True(plateau.AncrageRefus(tete, Direction.Sud).Y < centre.Y);
    }

    /// <summary>
    /// La taille de case suit la grille : le §4.3 rend les dimensions réglables sans recompiler, la
    /// mise en page doit donc se recalculer et non rester figée à 44 px.
    /// </summary>
    [Fact]
    public void UneGrillePlusGrandeDonneDesCasesPlusPetites()
    {
        int petite = Plateau.TailleDeCase(Grille.ParDefaut);
        int grande = Plateau.TailleDeCase(new Grille(31, 21));

        Assert.True(grande < petite);
    }

    /// <summary>
    /// L'aire de jeu doit rester DANS le cadre, quelle que soit la grille : un mur hors écran ne se
    /// voit pas, et le §2 exige que toute mort soit imputable à un virage.
    /// </summary>
    [Theory]
    [InlineData(5, 3)]
    [InlineData(21, 15)]
    [InlineData(31, 21)]
    [InlineData(51, 35)]
    public void LAireDeJeuTientToujoursDansLeCadre(int largeur, int hauteur)
    {
        Grille grille = new Grille(largeur, hauteur);
        Plateau plateau = new Plateau(grille, Plateau.TailleDeCase(grille));

        Assert.True(plateau.LargeurAire <= Plateau.LargeurCadreParDefaut);
        Assert.True(plateau.HauteurAire <= Plateau.HauteurCadreParDefaut - Plateau.HauteurBandeauParDefaut);
    }

    private static double Distance(PointPlateau a, PointPlateau b)
    {
        return Math.Sqrt(((b.X - a.X) * (b.X - a.X)) + ((b.Y - a.Y) * (b.Y - a.Y)));
    }
}
