using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>Ce que le design impose à l'aire de jeu (GDD §4.3, et le mur mortel du §2).</summary>
public class GrilleTests
{
    [Fact]
    public void LaGrilleParDefautEstCelleDuGdd()
    {
        Grille grille = Grille.ParDefaut;

        Assert.Equal(21, grille.Largeur);
        Assert.Equal(15, grille.Hauteur);
        Assert.Equal(315, grille.NombreDeCases);
    }

    /// <summary>
    /// La case centrale doit être EXACTE : c'est la condition pour que le serpent apparaisse « au
    /// centre » (§2) sans décalage d'une demi-case. Le test ne vérifie pas la formule mais la
    /// propriété : autant de colonnes à gauche qu'à droite, autant de lignes dessous que dessus.
    /// </summary>
    [Fact]
    public void LaCaseCentraleLaisseAutantDeCasesDeChaqueCote()
    {
        foreach (Grille grille in new[] { Grille.ParDefaut, new Grille(5, 3), new Grille(31, 21) })
        {
            Case centre = grille.Centre;
            Assert.Equal(centre.X, grille.Largeur - 1 - centre.X);
            Assert.Equal(centre.Y, grille.Hauteur - 1 - centre.Y);
        }
    }

    [Fact]
    public void LaCaseCentraleDeLaGrilleParDefautEstBienDixSept()
    {
        Assert.Equal(new Case(10, 7), Grille.ParDefaut.Centre);
    }

    /// <summary>
    /// Une dimension paire ne lève rien à l'exécution : elle produit une pose décalée d'une
    /// demi-case que personne ne voit avant une capture d'écran. D'où l'échec à la construction.
    /// C'est aussi ce qui a fait écarter la grille 32 × 18 (§7).
    /// </summary>
    [Fact]
    public void UneDimensionPaireEstRefusee()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grille(20, 15));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grille(21, 14));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grille(32, 18));
    }

    /// <summary>Une grille trop étroite ne pourrait pas porter la pose de départ.</summary>
    [Fact]
    public void UneGrilleTropPetitePourLaPoseDeDepartEstRefusee()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grille(3, 15));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grille(21, 1));
    }

    /// <summary>Les dimensions sont réglables sans recompiler : toute grille impaire valide tient.</summary>
    [Fact]
    public void LesDimensionsSontReglables()
    {
        Grille grille = new Grille(31, 21);

        Assert.Equal(651, grille.NombreDeCases);
        Assert.Equal(new Case(15, 10), grille.Centre);
    }

    /// <summary>
    /// La pose exacte du §4.3 : tête (10, 7), corps (9, 7) et (8, 7), longueur 3, orientation est.
    /// </summary>
    [Fact]
    public void LaPoseDeDepartEstCelleDuGdd()
    {
        PoseInitiale pose = Grille.ParDefaut.PoseDeDepart();

        Assert.Equal(3, pose.Longueur);
        Assert.Equal(Direction.Est, pose.Orientation);
        Assert.Equal(new Case(10, 7), pose.Tete);
        Assert.Equal(new Case(9, 7), pose.Segments[1]);
        Assert.Equal(new Case(8, 7), pose.Segments[2]);
    }

    /// <summary>
    /// Le corps s'étend DERRIÈRE la tête. Posé devant, le serpent se mangerait au premier tick :
    /// une partie qui ne démarre jamais, sans aucune erreur pour l'expliquer.
    /// </summary>
    [Fact]
    public void LeCorpsEstDerriereLaTeteParRapportALOrientation()
    {
        PoseInitiale pose = Grille.ParDefaut.PoseDeDepart();
        Case devantLaTete = Directions.Avance(pose.Tete, pose.Orientation);

        foreach (Case segment in pose.Segments)
        {
            Assert.NotEqual(devantLaTete, segment);
        }
    }

    /// <summary>La pose de départ tient entièrement dans l'aire de jeu, quelle que soit la grille.</summary>
    [Fact]
    public void LaPoseDeDepartTientDansLaGrille()
    {
        foreach (Grille grille in new[] { Grille.ParDefaut, new Grille(5, 3), new Grille(31, 21) })
        {
            PoseInitiale pose = grille.PoseDeDepart();
            foreach (Case segment in pose.Segments)
            {
                Assert.True(grille.Contient(segment), $"Segment {segment} hors de la grille {grille.Largeur}x{grille.Hauteur}.");
            }
        }
    }

    /// <summary>
    /// LE mur mortel du §2 : un pas de plus depuis chacun des quatre bords sort de la grille.
    /// Les bords tuent, ils ne téléportent pas (§7).
    /// </summary>
    [Fact]
    public void UnPasAuDelaDeChaqueBordEstHorsGrille()
    {
        Grille grille = Grille.ParDefaut;

        Case surLeBordEst = new Case(grille.Largeur - 1, grille.Centre.Y);
        Case surLeBordOuest = new Case(0, grille.Centre.Y);
        Case surLeBordNord = new Case(grille.Centre.X, grille.Hauteur - 1);
        Case surLeBordSud = new Case(grille.Centre.X, 0);

        Assert.True(grille.Contient(surLeBordEst));
        Assert.True(grille.Contient(surLeBordOuest));
        Assert.True(grille.Contient(surLeBordNord));
        Assert.True(grille.Contient(surLeBordSud));

        Assert.True(grille.EstHorsGrille(Directions.Avance(surLeBordEst, Direction.Est)));
        Assert.True(grille.EstHorsGrille(Directions.Avance(surLeBordOuest, Direction.Ouest)));
        Assert.True(grille.EstHorsGrille(Directions.Avance(surLeBordNord, Direction.Nord)));
        Assert.True(grille.EstHorsGrille(Directions.Avance(surLeBordSud, Direction.Sud)));
    }

    /// <summary>
    /// Les quatre coins appartiennent à la grille, et les quatre cases diagonales juste dehors n'y
    /// sont pas : un test de bornes écrit avec un &lt;= de trop passe tous les tests de bord et
    /// échoue ici.
    /// </summary>
    [Fact]
    public void LesQuatreCoinsSontDansLaGrilleMaisPasLeursVoisinsDiagonaux()
    {
        Grille grille = Grille.ParDefaut;
        int xMax = grille.Largeur - 1;
        int yMax = grille.Hauteur - 1;

        Assert.True(grille.Contient(new Case(0, 0)));
        Assert.True(grille.Contient(new Case(xMax, 0)));
        Assert.True(grille.Contient(new Case(0, yMax)));
        Assert.True(grille.Contient(new Case(xMax, yMax)));

        Assert.True(grille.EstHorsGrille(new Case(-1, -1)));
        Assert.True(grille.EstHorsGrille(new Case(xMax + 1, -1)));
        Assert.True(grille.EstHorsGrille(new Case(-1, yMax + 1)));
        Assert.True(grille.EstHorsGrille(new Case(xMax + 1, yMax + 1)));
    }

    /// <summary>
    /// Aucun modulo nulle part : une sortie de grille reste une sortie de grille, même très loin.
    /// Le jour où quelqu'un « répare » un index négatif par un modulo, il réintroduit en silence
    /// les bords traversants écartés au §7 et la mort cesse d'être imputable (§2).
    /// </summary>
    [Fact]
    public void UneSortieDeGrilleNEstJamaisRameneeParUnModulo()
    {
        Grille grille = Grille.ParDefaut;

        Assert.True(grille.EstHorsGrille(new Case(grille.Largeur, 7)));
        Assert.True(grille.EstHorsGrille(new Case(-1, 7)));
        Assert.True(grille.EstHorsGrille(new Case(10, grille.Hauteur)));
        Assert.True(grille.EstHorsGrille(new Case(10, -1)));
    }

    /// <summary>
    /// Le corps du serpent est testé contre la tête à chaque tick : deux cases de mêmes
    /// coordonnées doivent être égales et se ranger dans le même seau de hachage, sinon la
    /// collision avec soi-même — le seul adversaire du jeu (§1) — ne se déclenche jamais.
    /// </summary>
    [Fact]
    public void DeuxCasesDeMemesCoordonneesSontLaMemeCase()
    {
        Assert.Equal(new Case(4, 9), new Case(4, 9));
        Assert.True(new Case(4, 9) == new Case(4, 9));
        Assert.True(new Case(4, 9) != new Case(9, 4));
        Assert.Equal(new Case(4, 9).GetHashCode(), new Case(4, 9).GetHashCode());
    }
}
