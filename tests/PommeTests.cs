using System.Collections.Generic;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>Où la pomme a le droit de tomber, et à quel coût (GDD §4.4).</summary>
public class PommeTests
{
    /// <summary>Toutes les cases d'une grille sauf celles listées, dans l'ordre du parcours.</summary>
    private static List<Case> ToutSauf(Grille grille, params Case[] libres)
    {
        List<Case> occupees = new List<Case>();

        for (int y = 0; y < grille.Hauteur; y++)
        {
            for (int x = 0; x < grille.Largeur; x++)
            {
                Case candidate = new Case(x, y);
                if (!System.Array.Exists(libres, libre => libre == candidate))
                {
                    occupees.Add(candidate);
                }
            }
        }

        return occupees;
    }

    [Fact]
    public void LaPoseDeDepartLaisse312CasesLibres()
    {
        Grille grille = Grille.ParDefaut;

        // 315 cases, 3 segments : le chiffre du §4.4 (« 312 pommes sur la grille par défaut »).
        Assert.Equal(312, Pomme.NombreDeCasesLibres(grille, Grille.LongueurInitiale));
        Assert.False(Pomme.GrillePleine(grille, Grille.LongueurInitiale));
    }

    /// <summary>
    /// ⚠ L'ordre de parcours — <b>X croissant dans Y croissant</b> — fait partie du contrat du
    /// §4.4 : c'est lui qui, à graine égale, donne la même partie sur les trois cibles. L'inverser
    /// (Y dans X) ne casserait aucun test d'uniformité, et casserait tout appariement de banc.
    /// </summary>
    [Fact]
    public void LeParcoursVaEnXPuisEnY()
    {
        Grille grille = Grille.ParDefaut;
        List<Case> aucun = new List<Case>();

        Assert.Equal(new Case(0, 0), Pomme.CaseLibreAuRang(grille, aucun, 0));
        Assert.Equal(new Case(1, 0), Pomme.CaseLibreAuRang(grille, aucun, 1));

        // La ligne fait 21 cases : le rang 21 est donc la première case de la ligne suivante.
        Assert.Equal(new Case(20, 0), Pomme.CaseLibreAuRang(grille, aucun, 20));
        Assert.Equal(new Case(0, 1), Pomme.CaseLibreAuRang(grille, aucun, 21));
    }

    /// <summary>Le corps est sauté, il ne décale pas simplement le rang.</summary>
    [Fact]
    public void LeParcoursSauteLesCasesDuCorps()
    {
        Grille grille = new Grille(5, 3);
        List<Case> corps = new List<Case> { new Case(1, 0), new Case(2, 0) };

        Assert.Equal(new Case(0, 0), Pomme.CaseLibreAuRang(grille, corps, 0));
        Assert.Equal(new Case(3, 0), Pomme.CaseLibreAuRang(grille, corps, 1));
        Assert.Equal(new Case(4, 0), Pomme.CaseLibreAuRang(grille, corps, 2));
        Assert.Equal(new Case(0, 1), Pomme.CaseLibreAuRang(grille, corps, 3));
    }

    /// <summary>
    /// Chaque rang désigne une case libre distincte, et l'ensemble couvre exactement les cases
    /// libres. C'est la propriété qui rend le tirage uniforme <b>sans</b> jamais rejeter une case.
    /// </summary>
    [Fact]
    public void LesRangsEnumerentExactementLesCasesLibres()
    {
        Grille grille = new Grille(5, 3);
        List<Case> corps = new List<Case> { new Case(2, 1), new Case(1, 1), new Case(0, 1) };

        int libres = Pomme.NombreDeCasesLibres(grille, corps.Count);
        HashSet<Case> vues = new HashSet<Case>();

        for (int rang = 0; rang < libres; rang++)
        {
            Case tiree = Pomme.CaseLibreAuRang(grille, corps, rang);

            Assert.DoesNotContain(tiree, corps);
            Assert.True(grille.Contient(tiree));
            Assert.True(vues.Add(tiree), "La case " + tiree + " est rendue deux fois.");
        }

        Assert.Equal(libres, vues.Count);
    }

    [Fact]
    public void UnRangHorsBornesEstRefuse()
    {
        Grille grille = new Grille(5, 3);
        List<Case> corps = new List<Case> { new Case(0, 0) };

        Assert.Throws<System.ArgumentOutOfRangeException>(() => Pomme.CaseLibreAuRang(grille, corps, -1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => Pomme.CaseLibreAuRang(grille, corps, 14));
    }

    /// <summary>
    /// ⚠ <b>Le tick d'avant la victoire.</b> Une seule case libre : le tirage doit la rendre, tout
    /// de suite. C'est la position où « tirer au hasard et recommencer si c'est occupé » ferait 15
    /// tours en moyenne ici, et une infinité en espérance sur la vraie grille (§4.4).
    /// </summary>
    [Fact]
    public void LaDerniereCaseLibreEstTireeSansDetour()
    {
        Grille grille = new Grille(5, 3);
        Case seuleLibre = new Case(3, 1);
        List<Case> corps = ToutSauf(grille, seuleLibre);

        Assert.Equal(14, corps.Count);
        Assert.Equal(1, Pomme.NombreDeCasesLibres(grille, corps.Count));
        Assert.Equal(seuleLibre, Pomme.CaseLibreAuRang(grille, corps, 0));
        Assert.Equal(seuleLibre, Pomme.Tirer(grille, corps, new Aleatoire(1UL)));
    }

    /// <summary>
    /// ⚠ <b>Le tirage consomme exactement UN nombre du générateur</b>, quel que soit le remplissage
    /// de la grille. C'est ce qui rend un banc apparié possible : deux parties à même graine et même
    /// suite d'appuis restent alignées, pomme après pomme. Une implémentation qui rejetterait les
    /// cases occupées consommerait un nombre variable et ferait diverger l'appariement — sans
    /// qu'aucun autre test d'ici ne tombe.
    /// </summary>
    [Fact]
    public void LeTirageNeConsommeQuUnSeulNombre()
    {
        Grille grille = Grille.ParDefaut;
        List<Case> corps = new List<Case>(Grille.ParDefaut.PoseDeDepart().Segments);

        Aleatoire joueur = new Aleatoire(123UL);
        Aleatoire temoin = new Aleatoire(123UL);

        Case tiree = Pomme.Tirer(grille, corps, joueur);

        int rang = temoin.Entier(Pomme.NombreDeCasesLibres(grille, corps.Count));
        Assert.Equal(Pomme.CaseLibreAuRang(grille, corps, rang), tiree);

        // Les deux générateurs sont au même point : le tirage n'en a pas consommé un de plus.
        Assert.Equal(temoin.Suivant(), joueur.Suivant());
    }

    /// <summary>
    /// La pomme ne tombe jamais sur le serpent — et pas « presque jamais » : la garantie tient au
    /// parcours, pas à la chance. Mille tirages sur une grille très remplie le montrent.
    /// </summary>
    [Fact]
    public void LaPommeNeTombeJamaisSurLeSerpent()
    {
        Grille grille = new Grille(5, 3);
        List<Case> corps = new List<Case>
        {
            new Case(0, 0), new Case(1, 0), new Case(2, 0), new Case(3, 0),
            new Case(4, 0), new Case(4, 1), new Case(3, 1)
        };

        Aleatoire alea = new Aleatoire(2026UL);

        for (int i = 0; i < 1000; i++)
        {
            Case pomme = Pomme.Tirer(grille, corps, alea);

            Assert.True(grille.Contient(pomme));
            Assert.DoesNotContain(pomme, corps);
        }
    }

    /// <summary>
    /// Grille pleine = victoire (§4.4), à traiter <b>avant</b> le tirage. Cet état est hors de
    /// portée humaine et doit néanmoins être écrit : sans lui, le tirage part sur un intervalle vide.
    /// </summary>
    [Fact]
    public void LaGrillePleineEstDetecteeEtLeTirageLeRefuse()
    {
        Grille grille = new Grille(5, 3);
        List<Case> partout = ToutSauf(grille);

        Assert.Equal(15, partout.Count);
        Assert.True(Pomme.GrillePleine(grille, partout.Count));
        Assert.Equal(0, Pomme.NombreDeCasesLibres(grille, partout.Count));

        // Levée, jamais une case rendue « quelque part » : l'appelant doit avoir vu la victoire.
        Assert.Throws<System.InvalidOperationException>(() => Pomme.Tirer(grille, partout, new Aleatoire(1UL)));
    }

    [Fact]
    public void UnSerpentPlusGrandQueLaGrilleEstRefuse()
    {
        Grille grille = new Grille(5, 3);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => Pomme.NombreDeCasesLibres(grille, 16));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => Pomme.NombreDeCasesLibres(grille, -1));
    }
}
