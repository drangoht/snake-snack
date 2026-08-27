using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// Ce que le GDD §4.4 exige du générateur : une suite <b>reproductible</b> et <b>uniforme</b>.
/// </summary>
public class AleatoireTests
{
    /// <summary>
    /// ⚠ <b>Le test le plus important du fichier, et le seul qui ne peut pas être réécrit.</b> Ces
    /// trois valeurs sont le vecteur de référence de SplitMix64 pour la graine 1. Elles verrouillent
    /// l'algorithme : le jour où quelqu'un « améliore » le mélange, tous les autres tests d'ici
    /// passeraient encore (la suite resterait uniforme et reproductible) — mais chaque banc apparié
    /// déjà enregistré cesserait de correspondre, sans qu'aucun symptôme ne le dise. C'est le
    /// contrat de stabilité du §4.4, écrit en dur.
    /// </summary>
    [Fact]
    public void LaSuiteEstCelleDeSplitMix64()
    {
        Aleatoire alea = new Aleatoire(1UL);

        Assert.Equal(0x910A2DEC89025CC1UL, alea.Suivant());
        Assert.Equal(0xBEEB8DA1658EEC67UL, alea.Suivant());
        Assert.Equal(0xF893A2EEFB32555EUL, alea.Suivant());
    }

    [Fact]
    public void DeuxGenerateursSemesPareilProduisentLaMemeSuite()
    {
        Aleatoire premier = new Aleatoire(20260827UL);
        Aleatoire second = new Aleatoire(20260827UL);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(premier.Suivant(), second.Suivant());
        }
    }

    /// <summary>
    /// Semer 1 puis 2 ne doit pas donner deux suites voisines : les graines de banc s'écrivent à la
    /// main et se suivront. C'est ce que le pas d'or de SplitMix64 garantit.
    /// </summary>
    [Fact]
    public void DesGrainesVoisinesDonnentDesSuitesDifferentes()
    {
        Aleatoire premier = new Aleatoire(1UL);
        Aleatoire second = new Aleatoire(2UL);

        int identiques = 0;
        for (int i = 0; i < 50; i++)
        {
            if (premier.Suivant() == second.Suivant())
            {
                identiques++;
            }
        }

        Assert.Equal(0, identiques);
    }

    /// <summary>
    /// Rejouer une partie, c'est repartir de la graine — pas construire un nouvel objet quelque
    /// part. Sans ça, le §4.4 obligerait l'appelant à conserver la graine de son côté.
    /// </summary>
    [Fact]
    public void ReinitialiserRejoueLaMemeSuite()
    {
        Aleatoire alea = new Aleatoire(7UL);

        ulong[] premiere = { alea.Suivant(), alea.Suivant(), alea.Suivant() };
        alea.Reinitialiser();

        Assert.Equal(premiere[0], alea.Suivant());
        Assert.Equal(premiere[1], alea.Suivant());
        Assert.Equal(premiere[2], alea.Suivant());
    }

    [Fact]
    public void LaGraineEstRelisible()
    {
        Assert.Equal(42UL, new Aleatoire(42UL).Graine);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(315)]
    [InlineData(312)]
    public void EntierResteDansSesBornes(int borne)
    {
        Aleatoire alea = new Aleatoire(99UL);

        for (int i = 0; i < 2000; i++)
        {
            int tirage = alea.Entier(borne);
            Assert.InRange(tirage, 0, borne - 1);
        }
    }

    /// <summary>
    /// Une seule case libre : le tirage n'a qu'une réponse possible. C'est le dernier tick avant la
    /// victoire du §4.4, et il ne doit ni boucler ni sortir des bornes.
    /// </summary>
    [Fact]
    public void EntierSurUneSeuleValeurRendToujoursZero()
    {
        Aleatoire alea = new Aleatoire(3UL);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(0, alea.Entier(1));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EntierRefuseUneBorneVide(int borne)
    {
        Aleatoire alea = new Aleatoire(1UL);

        // Rendre 0 « par défaut » poserait toutes les pommes sur la case (0, 0), sans erreur.
        Assert.Throws<System.ArgumentOutOfRangeException>(() => alea.Entier(borne));
    }

    /// <summary>
    /// Uniformité grossière. Un <c>% borne</c> écrit sans rejet de la frange favoriserait les
    /// petites valeurs — donc, en jeu, le coin bas-gauche de la grille : des pommes qui « tombent
    /// toujours du même côté », ce qu'aucune erreur ne signalerait.
    /// </summary>
    [Fact]
    public void EntierRepartitLesValeursUniformement()
    {
        const int classes = 10;
        const int tirages = 200000;

        Aleatoire alea = new Aleatoire(2026UL);
        int[] comptes = new int[classes];

        for (int i = 0; i < tirages; i++)
        {
            comptes[alea.Entier(classes)]++;
        }

        // Bornes larges (±5 %) : le test doit tomber sur un biais systématique, pas sur le bruit.
        int attendu = tirages / classes;
        for (int i = 0; i < classes; i++)
        {
            Assert.InRange(comptes[i], (int)(attendu * 0.95), (int)(attendu * 1.05));
        }
    }
}
