using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// Les courbes du juicy (docs/art/juicy.md §2).
/// </summary>
/// <remarks>
/// Ce qui est vérifié ici ne se voit pas à l'œil sur 150 ms : qu'une enveloppe revienne exactement
/// à sa valeur de repos, qu'aucun facteur ne sorte de sa plage sur une image longue. Ce sont
/// précisément les défauts qui laissent une trace permanente sans que personne ne les rattache à
/// une animation.
/// </remarks>
public class RebondTests
{
    private const double Tolerance = 1e-9;

    // --- Progres ---------------------------------------------------------------------

    [Fact]
    public void LeProgresVaDeZeroAUnSurLaDuree()
    {
        Assert.Equal(0.0, Rebond.Progres(10.0, 0.2, 10.0), 9);
        Assert.Equal(0.5, Rebond.Progres(10.0, 0.2, 10.1), 9);
        Assert.Equal(1.0, Rebond.Progres(10.0, 0.2, 10.2), 9);
    }

    /// <summary>
    /// ⚠ Le cas qui projetterait un segment au-delà de sa case : une image longue — la première
    /// après un chargement WebGL en avale plusieurs centaines de ms — donne un temps écoulé
    /// largement supérieur à la durée du tick.
    /// </summary>
    [Fact]
    public void UneImageTresLongueNeFaitPasDepasserUn()
    {
        Assert.Equal(1.0, Rebond.Progres(10.0, 0.125, 12.0), 9);
    }

    /// <summary>Un « maintenant » antérieur au début (horloge relue après une pause) ne rend pas de négatif.</summary>
    [Fact]
    public void UnTempsAnterieurAuDebutRendZero()
    {
        Assert.Equal(0.0, Rebond.Progres(10.0, 0.125, 9.5), 9);
    }

    [Fact]
    public void UneDureeNulleOuNegativeEstRefusee()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Rebond.Progres(0.0, 0.0, 1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Rebond.Progres(0.0, -0.1, 1.0));
    }

    // --- Impulsion -------------------------------------------------------------------

    [Fact]
    public void LImpulsionPartDeZeroCulmineAUnEtRetombeAZero()
    {
        Assert.Equal(0.0, Rebond.Impulsion(0.0), 9);
        Assert.Equal(1.0, Rebond.Impulsion(0.5), 9);
        Assert.Equal(0.0, Rebond.Impulsion(1.0), 9);
    }

    [Fact]
    public void LImpulsionResteDansSaPlageSurToutLeParcours()
    {
        for (int i = 0; i <= 100; i++)
        {
            double valeur = Rebond.Impulsion(i / 100.0);
            Assert.InRange(valeur, 0.0, 1.0);
        }
    }

    /// <summary>Symétrique : un aller-retour, pas une montée suivie d'une chute brutale.</summary>
    [Fact]
    public void LImpulsionEstSymetriqueAutourDeSonPic()
    {
        for (int i = 0; i <= 50; i++)
        {
            double t = i / 100.0;
            Assert.Equal(Rebond.Impulsion(t), Rebond.Impulsion(1.0 - t), 9);
        }
    }

    // --- Apparition ------------------------------------------------------------------

    /// <summary>
    /// ⚠ Le test qui compte : un segment dont l'échelle finale n'est pas exactement 1 reste
    /// définitivement plus gros que ses voisins, longtemps après la fin de l'animation.
    /// </summary>
    [Fact]
    public void LApparitionFinitExactementAUn()
    {
        Assert.Equal(1.0, Rebond.Apparition(1.0, 0.12), 9);
        Assert.Equal(1.0, Rebond.Apparition(1.5, 0.12), 9);
    }

    [Fact]
    public void LApparitionPartDeZero()
    {
        Assert.Equal(0.0, Rebond.Apparition(0.0, 0.12), 9);
        Assert.Equal(0.0, Rebond.Apparition(-0.3, 0.12), 9);
    }

    [Fact]
    public void LApparitionDepasseUnAvantDYRevenir()
    {
        double maximum = 0.0;
        for (int i = 0; i <= 100; i++)
        {
            maximum = Math.Max(maximum, Rebond.Apparition(i / 100.0, 0.12));
        }

        Assert.True(maximum > 1.0, "Sans dépassement, un pop n'est qu'un fondu : il ne claque pas.");
        Assert.True(maximum <= 1.13, $"Le dépassement doit rester proche de celui demandé, mesuré {maximum:F4}.");
    }

    /// <summary>Un dépassement nul donne une montée simple, qui ne repasse jamais au-dessus de 1.</summary>
    [Fact]
    public void SansDepassementLApparitionNeDepasseJamaisUn()
    {
        for (int i = 0; i <= 100; i++)
        {
            Assert.InRange(Rebond.Apparition(i / 100.0, 0.0), 0.0, 1.0);
        }
    }

    [Fact]
    public void LApparitionEstCroissanteAuDebut()
    {
        double precedent = Rebond.Apparition(0.0, 0.12);
        for (int i = 1; i <= 40; i++)
        {
            double valeur = Rebond.Apparition(i / 100.0, 0.12);
            Assert.True(valeur > precedent, $"Un pop qui recule au début se lit comme un défaut (i={i}).");
            precedent = valeur;
        }
    }

    [Fact]
    public void UnDepassementNegatifEstRefuse()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Rebond.Apparition(0.5, -0.1));
    }

    // --- Gulp ------------------------------------------------------------------------

    [Fact]
    public void LeGulpPartEtRevientAUn()
    {
        Assert.Equal(1.0, Rebond.Gulp(0.0, 0.15), 9);
        Assert.Equal(1.0, Rebond.Gulp(1.0, 0.15), 9);
    }

    [Fact]
    public void LeGulpAtteintSonAmplitudeAuPic()
    {
        Assert.Equal(1.15, Rebond.Gulp(0.5, 0.15), 9);
    }

    /// <summary>
    /// ⚠ Le volume se conserve : l'axe comprimé est l'INVERSE de l'axe étiré, pas son symétrique.
    /// Avec 1 − a, la tête perdrait de la surface au moment où elle doit paraître plus grosse.
    /// </summary>
    [Fact]
    public void LesDeuxAxesDuGulpConserventLaSurface()
    {
        double etire = Rebond.Gulp(0.5, 0.15);
        double comprime = 1.0 / etire;

        Assert.Equal(1.0, etire * comprime, 9);
        Assert.True(comprime > 1.0 - 0.15 + Tolerance,
            "L'axe comprimé doit valoir 1/(1+a), plus grand que 1−a.");
    }

    [Fact]
    public void UneAmplitudeNegativeEstRefusee()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Rebond.Gulp(0.5, -0.05));
    }
}
