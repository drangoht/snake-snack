using System.Collections.Generic;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>Ce que le design impose au corps du serpent (GDD §2 : le mur et le corps tuent).</summary>
public class SerpentTests
{
    private static Serpent PoseDeDepart()
    {
        return new Serpent(Grille.ParDefaut.PoseDeDepart().Segments);
    }

    [Fact]
    public void LeSerpentDeDepartEstCeluiDuGdd()
    {
        Serpent serpent = PoseDeDepart();

        Assert.Equal(3, serpent.Longueur);
        Assert.Equal(new Case(10, 7), serpent.Segments[0]);
        Assert.Equal(new Case(9, 7), serpent.Segments[1]);
        Assert.Equal(new Case(8, 7), serpent.Segments[2]);
    }

    [Fact]
    public void AvancerDeplaceLaTeteEtTireLeCorpsDerriereElle()
    {
        Serpent serpent = PoseDeDepart();

        ResultatDeplacement resultat = serpent.Avancer(Direction.Nord, Grille.ParDefaut);

        Assert.Equal(ResultatDeplacement.Avance, resultat);
        Assert.Equal(new Case(10, 8), serpent.Segments[0]);
        Assert.Equal(new Case(10, 7), serpent.Segments[1]);
        Assert.Equal(new Case(9, 7), serpent.Segments[2]);
        Assert.Equal(3, serpent.Longueur);
    }

    /// <summary>
    /// Les quatre murs tuent (§2, « les bords tuent, ils ne téléportent pas »). Le test attaque les
    /// quatre bords : un modulo glissé quelque part « pour éviter un index négatif » réintroduirait
    /// les bords traversants écartés au §7, et seul un des quatre bords le montrerait.
    /// </summary>
    [Theory]
    [InlineData(Direction.Est)]
    [InlineData(Direction.Ouest)]
    [InlineData(Direction.Nord)]
    [InlineData(Direction.Sud)]
    public void SortirDeLaGrilleTue(Direction direction)
    {
        Grille grille = Grille.ParDefaut;
        Case bord = direction switch
        {
            Direction.Est => new Case(grille.Largeur - 1, 7),
            Direction.Ouest => new Case(0, 7),
            Direction.Nord => new Case(10, grille.Hauteur - 1),
            _ => new Case(10, 0)
        };

        Serpent serpent = new Serpent(new[] { bord });

        Assert.Equal(ResultatDeplacement.MortMur, serpent.Avancer(direction, grille));
    }

    /// <summary>
    /// ⚠ Un déplacement mortel ne doit PAS bouger le serpent : le rendu dessinerait une tête hors de
    /// l'aire de jeu à l'image de la mort, et le joueur verrait le serpent traverser le mur — ce que
    /// le §2 interdit de laisser croire.
    /// </summary>
    [Fact]
    public void LaMortNeDeplacePasLeSerpent()
    {
        Grille grille = Grille.ParDefaut;
        Serpent serpent = new Serpent(new[] { new Case(20, 7), new Case(19, 7), new Case(18, 7) });

        serpent.Avancer(Direction.Est, grille);

        Assert.Equal(new Case(20, 7), serpent.Segments[0]);
        Assert.Equal(new Case(19, 7), serpent.Segments[1]);
        Assert.Equal(new Case(18, 7), serpent.Segments[2]);
    }

    /// <summary>
    /// Le corps tue (§1 : « jusqu'à ce que son propre corps ne laisse plus de passage »). Depuis la
    /// pomme, ce cas est la fin normale d'une partie : c'est le serpent lui-même qui est l'adversaire.
    /// </summary>
    [Fact]
    public void SeMordreTue()
    {
        // Serpent enroulé : la tête en (2,2) regarde vers l'est, et (3,2) est un segment de corps.
        Serpent serpent = new Serpent(new[]
        {
            new Case(2, 2), new Case(2, 3), new Case(3, 3), new Case(3, 2), new Case(3, 1)
        });

        Assert.Equal(ResultatDeplacement.MortMorsure, serpent.Avancer(Direction.Est, Grille.ParDefaut));
    }

    /// <summary>
    /// ⚠ La case de la queue se libère dans le MÊME tick : y entrer n'est pas une morsure. Compter
    /// la queue parmi les obstacles produit une mort sur une case que le joueur a vue se vider —
    /// inexplicable, donc non imputable à un virage (§2).
    /// </summary>
    [Fact]
    public void EntrerDansLaCaseQueLaQueueLibereNEstPasUneMorsure()
    {
        Serpent serpent = new Serpent(new[]
        {
            new Case(2, 1), new Case(1, 1), new Case(1, 0), new Case(2, 0)
        });

        ResultatDeplacement resultat = serpent.Avancer(Direction.Sud, Grille.ParDefaut);

        Assert.Equal(ResultatDeplacement.Avance, resultat);
        Assert.Equal(new Case(2, 0), serpent.Tete);
    }

    /// <summary>
    /// À trois segments, se mordre est géométriquement impossible : tant que le joueur n'a pas mangé
    /// sa première pomme, seul le mur peut le tuer. Le test le prouve au lieu de le supposer — il
    /// rejoue toutes les trajectoires non-demi-tour, loin des murs.
    /// </summary>
    [Fact]
    public void ATroisSegmentsAucuneTrajectoireNeProduitDeMorsure()
    {
        Grille grille = Grille.ParDefaut;

        foreach (Direction premiere in Directions.Toutes())
        {
            foreach (Direction seconde in Directions.Toutes())
            {
                Serpent serpent = PoseDeDepart();
                Direction courante = Grille.OrientationInitiale;
                List<Direction> plan = new List<Direction> { premiere, seconde, premiere, seconde };

                foreach (Direction voulue in plan)
                {
                    // Le demi-tour n'atteint jamais le serpent : la file le refuse au tick (§4.2).
                    Direction appliquee = Directions.EstDemiTour(courante, voulue) ? courante : voulue;
                    ResultatDeplacement resultat = serpent.Avancer(appliquee, grille);
                    courante = appliquee;

                    Assert.NotEqual(ResultatDeplacement.MortMorsure, resultat);

                    if (resultat != ResultatDeplacement.Avance)
                    {
                        break; // Sorti de la grille : le reste du plan n'a plus de sens.
                    }
                }
            }
        }
    }

    // ---- La pomme (GDD §4.4) -----------------------------------------------------------------

    /// <summary>
    /// ⚠ <b>Le serpent s'allonge par la TÊTE, au tick même où elle entre sur la pomme</b> (§4.4) —
    /// pas au tick suivant, pas par un segment ajouté derrière la queue. C'est la <b>queue qui ne
    /// bouge pas</b> pendant ce seul tick : ce test vérifie donc que le dernier segment est resté
    /// exactement où il était. Ajouter le segment par la queue donnerait la même longueur et une
    /// forme fausse d'une case, invisible en lecture.
    /// </summary>
    [Fact]
    public void MangerAllongeParLaTeteEtLaisseLaQueueEnPlace()
    {
        Serpent serpent = PoseDeDepart();
        Case pomme = new Case(11, 7);

        bool mange;
        ResultatDeplacement resultat = serpent.Avancer(Direction.Est, Grille.ParDefaut, pomme, out mange);

        Assert.Equal(ResultatDeplacement.Avance, resultat);
        Assert.True(mange);
        Assert.Equal(4, serpent.Longueur);
        Assert.Equal(new Case(11, 7), serpent.Segments[0]);
        Assert.Equal(new Case(10, 7), serpent.Segments[1]);
        Assert.Equal(new Case(9, 7), serpent.Segments[2]);
        Assert.Equal(new Case(8, 7), serpent.Segments[3]);
    }

    /// <summary>Passer à côté de la pomme ne mange pas et n'allonge pas.</summary>
    [Fact]
    public void PasserACoteDeLaPommeNeMangePas()
    {
        Serpent serpent = PoseDeDepart();

        bool mange;
        serpent.Avancer(Direction.Nord, Grille.ParDefaut, new Case(11, 7), out mange);

        Assert.False(mange);
        Assert.Equal(3, serpent.Longueur);
    }

    /// <summary>La longueur vaut toujours <c>3 + score</c> (§4.5) : cinq pommes, huit segments.</summary>
    [Fact]
    public void LaLongueurVautTroisPlusLeNombreDePommes()
    {
        Grille grille = Grille.ParDefaut;
        Serpent serpent = PoseDeDepart();

        for (int i = 1; i <= 5; i++)
        {
            // Une pomme posée pile devant la tête, cinq fois de suite.
            Case pomme = Directions.Avance(serpent.Tete, Direction.Est);

            bool mange;
            serpent.Avancer(Direction.Est, grille, pomme, out mange);

            Assert.True(mange);
            Assert.Equal(Grille.LongueurInitiale + i, serpent.Longueur);
        }
    }

    /// <summary>
    /// ⚠ <b>Le cas qui distingue une implémentation juste d'une implémentation qui « marche ».</b>
    /// Hors croissance, la tête peut entrer sur la case que la queue libère (test plus haut). Au
    /// tick d'une pomme, la queue ne bouge pas : cette même case redevient un obstacle, et y entrer
    /// tue. Traiter la queue de la même façon dans les deux cas donne, au choix, un serpent qui se
    /// traverse ou une mort sur une case qui paraît libre — et aucune erreur pour le dire.
    ///
    /// <para>La situation est artificielle : le §4.4 garantit qu'une pomme n'apparaît jamais sur le
    /// corps. La règle est écrite sans s'appuyer sur cette garantie, posée ailleurs — et le test
    /// aussi.</para>
    /// </summary>
    [Fact]
    public void EntrerSurLaCaseDeLaQueueEnMangeantEstUneMorsure()
    {
        Case queue = new Case(2, 0);
        Serpent serpent = new Serpent(new[]
        {
            new Case(2, 1), new Case(1, 1), new Case(1, 0), queue
        });

        bool mange;
        ResultatDeplacement resultat = serpent.Avancer(Direction.Sud, Grille.ParDefaut, queue, out mange);

        Assert.Equal(ResultatDeplacement.MortMorsure, resultat);
        Assert.False(mange);
        Assert.Equal(4, serpent.Longueur);
    }

    /// <summary>
    /// Un pas mortel ne mange pas. Sans ça, le score monterait d'un point sur le tick de la mort, et
    /// l'écran de fin afficherait un nombre que le joueur n'a jamais vu à l'écran.
    /// </summary>
    [Fact]
    public void MourirContreUnMurNeMangePas()
    {
        Grille grille = Grille.ParDefaut;
        Serpent serpent = new Serpent(new[] { new Case(20, 7), new Case(19, 7) });

        bool mange;
        ResultatDeplacement resultat = serpent.Avancer(Direction.Est, grille, new Case(20, 7), out mange);

        Assert.Equal(ResultatDeplacement.MortMur, resultat);
        Assert.False(mange);
        Assert.Equal(2, serpent.Longueur);
    }

    /// <summary>Sans pomme sur la grille, le tick est celui d'avant : rien ne change.</summary>
    [Fact]
    public void SansPommeLeTickEstUnSimpleDeplacement()
    {
        Serpent serpent = PoseDeDepart();

        bool mange;
        ResultatDeplacement resultat = serpent.Avancer(Direction.Est, Grille.ParDefaut, null, out mange);

        Assert.Equal(ResultatDeplacement.Avance, resultat);
        Assert.False(mange);
        Assert.Equal(3, serpent.Longueur);
    }
}
