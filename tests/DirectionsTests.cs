using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// Les opérations pures sur les directions — ici le sens du virage (docs/art/juicy.md §9).
/// </summary>
/// <remarks>
/// Une inclinaison du mauvais côté ne lève rien et ne se voit pas non plus : à 8° sur 125 ms,
/// personne ne dira « elle penche à l'envers », le jeu paraîtra seulement un peu bizarre. D'où ces
/// tests, qui fixent le signe une fois pour toutes.
/// </remarks>
public class DirectionsTests
{
    /// <summary>
    /// Le signe suit la convention d'Unity : Z croissant tourne dans le sens anti-horaire, donc un
    /// virage à gauche rend +1.
    /// </summary>
    [Theory]
    [InlineData(Direction.Nord, Direction.Ouest)]
    [InlineData(Direction.Ouest, Direction.Sud)]
    [InlineData(Direction.Sud, Direction.Est)]
    [InlineData(Direction.Est, Direction.Nord)]
    public void UnVirageAGaucheRendPlusUn(Direction avant, Direction apres)
    {
        Assert.Equal(1, Directions.SensDuVirage(avant, apres));
    }

    [Theory]
    [InlineData(Direction.Nord, Direction.Est)]
    [InlineData(Direction.Est, Direction.Sud)]
    [InlineData(Direction.Sud, Direction.Ouest)]
    [InlineData(Direction.Ouest, Direction.Nord)]
    public void UnVirageADroiteRendMoinsUn(Direction avant, Direction apres)
    {
        Assert.Equal(-1, Directions.SensDuVirage(avant, apres));
    }

    [Fact]
    public void AllerToutDroitNEstPasUnVirage()
    {
        foreach (Direction direction in Directions.Toutes())
        {
            Assert.Equal(0, Directions.SensDuVirage(direction, direction));
        }
    }

    /// <summary>
    /// ⚠ Le demi-tour n'arrive pas en jeu (la file le refuse au tick, GDD §4.2) — mais s'il
    /// arrivait, pencher d'un côté plutôt que de l'autre serait une invention : les deux quarts de
    /// tour sont également faux. Zéro veut dire « rien à montrer ».
    /// </summary>
    [Fact]
    public void UnDemiTourNePencheDAucunCote()
    {
        foreach (Direction direction in Directions.Toutes())
        {
            Assert.Equal(0, Directions.SensDuVirage(direction, Directions.Oppose(direction)));
        }
    }

    /// <summary>Le virage inverse penche de l'autre côté, exactement — jamais d'asymétrie.</summary>
    [Fact]
    public void LeVirageInverseRendLeSensOppose()
    {
        foreach (Direction avant in Directions.Toutes())
        {
            foreach (Direction apres in Directions.Toutes())
            {
                Assert.Equal(
                    -Directions.SensDuVirage(avant, apres),
                    Directions.SensDuVirage(apres, avant));
            }
        }
    }
}
