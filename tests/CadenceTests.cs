using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>Ce que le design impose au pas de temps (GDD §4.1, et l'écarté du §7).</summary>
public class CadenceTests
{
    [Fact]
    public void HuitTicksParSecondeFontUnTickDeCentVingtCinqMillisecondes()
    {
        Assert.Equal(8.0, Cadence.TicksParSecondeParDefaut);
        Assert.Equal(0.125, Cadence.DureeTickParDefautSecondes, 12);
        Assert.Equal(0.125, Cadence.DureeTickSecondes(), 12);
    }

    /// <summary>
    /// La cadence est LA valeur qui sera ré-essayée le plus souvent (§4.1) : elle doit se régler
    /// depuis l'appelant, sans recompiler. La constante n'est qu'un repli.
    /// </summary>
    [Fact]
    public void LaCadenceSeSurchargeDepuisLAppelant()
    {
        Assert.Equal(1.0 / 6.0, Cadence.DureeTickSecondes(6.0), 12);
        Assert.Equal(0.1, Cadence.DureeTickSecondes(10.0), 12);
    }

    /// <summary>
    /// La valeur par défaut est posée AU JUGÉ, dans une fourchette 6–10 à essayer en jeu : elle
    /// doit donc y tomber. Si quelqu'un déplace la valeur par défaut hors de la fourchette, c'est
    /// que la fourchette a bougé aussi — et ça se discute au GDD, pas ici.
    /// </summary>
    [Fact]
    public void LaValeurParDefautTombeDansLaFourchetteAEssayer()
    {
        Assert.True(Cadence.EstDansLaFourchetteConseillee(Cadence.TicksParSecondeParDefaut));
        Assert.True(Cadence.EstDansLaFourchetteConseillee(6.0));
        Assert.True(Cadence.EstDansLaFourchetteConseillee(10.0));
        Assert.False(Cadence.EstDansLaFourchetteConseillee(5.9));
        Assert.False(Cadence.EstDansLaFourchetteConseillee(10.1));
    }

    /// <summary>
    /// Hors fourchette reste JOUABLE : c'est justement ce qu'on veut pouvoir essayer. La fourchette
    /// avertit, elle ne refuse pas — sinon régler la cadence redeviendrait un travail de code.
    /// </summary>
    [Fact]
    public void UneCadenceHorsFourchetteResteCalculable()
    {
        Assert.Equal(1.0 / 20.0, Cadence.DureeTickSecondes(20.0), 12);
        Assert.Equal(1.0 / 2.0, Cadence.DureeTickSecondes(2.0), 12);
    }

    /// <summary>
    /// Pas de clamp silencieux : un fichier de tuning mal saisi doit se voir tout de suite, pas
    /// produire un jeu figé ou un tick de durée infinie que personne ne saurait expliquer.
    /// </summary>
    [Fact]
    public void UneCadenceAberranteLeveAuLieuDEtreRabotee()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Cadence.DureeTickSecondes(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cadence.DureeTickSecondes(-8.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cadence.DureeTickSecondes(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cadence.DureeTickSecondes(double.PositiveInfinity));
    }

    /// <summary>
    /// LA décision du §4.1, arbitrée par l'auteur le 2026-08-27 contre la canonicité du Nokia :
    /// cadence CONSTANTE sur toute la partie. L'accélération avec la longueur est un multiplicateur,
    /// pas une règle nommée ; elle brouille l'attribution de la mort (§2) et rend le tick variable,
    /// donc deux parties incomparables au banc (§7).
    ///
    /// Ce test est le garde-fou de cette décision : si un jour on la rouvre, on la rouvre au GDD.
    /// </summary>
    [Fact]
    public void LaCadenceNeDependNiDeLaLongueurDuSerpentNiDuTempsDeJeu()
    {
        double reference = Cadence.CadenceEffective(Cadence.TicksParSecondeParDefaut, Grille.LongueurInitiale);

        for (int longueur = Grille.LongueurInitiale; longueur <= Grille.ParDefaut.NombreDeCases; longueur++)
        {
            Assert.Equal(reference, Cadence.CadenceEffective(Cadence.TicksParSecondeParDefaut, longueur), 12);
        }
    }

    /// <summary>
    /// Le serpent avance d'une case PAR TICK, jamais entre deux ticks (§4.1) : un pas d'image plus
    /// court qu'un tick ne produit aucun mouvement, et le temps s'accumule.
    /// </summary>
    [Fact]
    public void UnPasDImagePlusCourtQuUnTickNeFaitPasAvancerLeSerpent()
    {
        double reste;
        Assert.Equal(0, Cadence.NombreDeTicks(1.0 / 60.0, Cadence.DureeTickParDefautSecondes, out reste));
        Assert.Equal(1.0 / 60.0, reste, 12);
    }

    /// <summary>
    /// Le reliquat est REPORTÉ, pas jeté. Remettre l'accumulateur à zéro à chaque tick fait dériver
    /// la cadence réelle vers le bas dès que le pas d'image ne divise pas la durée du tick — à
    /// 60 Hz, 125 ms tombe entre deux images. La dérive ne lève rien : elle fausse simplement toute
    /// mesure de durée de partie, donc tout futur banc d'équilibrage (§6).
    ///
    /// Dix secondes simulées à 60 Hz doivent donner 80 ticks à un près, jamais 75.
    /// </summary>
    [Fact]
    public void LeReliquatEstReporteDoncLaCadenceNeDerivePas()
    {
        const double pasDImage = 1.0 / 60.0;
        double dureeTick = Cadence.DureeTickSecondes();

        double accumulateur = 0.0;
        int ticks = 0;

        for (int image = 0; image < 600; image++)
        {
            accumulateur += pasDImage;
            ticks += Cadence.NombreDeTicks(accumulateur, dureeTick, out accumulateur);
        }

        Assert.InRange(ticks, 79, 80);

        // Le reliquat restant est toujours strictement inférieur à un tick : sans ça, un tick
        // « en retard » serait perdu au lieu d'être rattrapé à l'image suivante.
        Assert.True(accumulateur >= 0.0 && accumulateur < dureeTick,
            $"Reliquat hors bornes : {accumulateur} (durée d'un tick : {dureeTick}).");
    }

    /// <summary>
    /// Un gel d'image (chargement, alt-tab) accumule plusieurs ticks : ils sont rendus d'un coup à
    /// l'appelant, à lui de décider s'il les rejoue tous.
    ///
    /// ⚠ Le GDD §4 ne dit RIEN de ce rattrapage. Ce test constate le comportement actuel — il ne
    /// tranche pas le design. Un plafond de rattrapage est une VALEUR : elle appartient au
    /// game-designer.
    /// </summary>
    [Fact]
    public void UnGelDImageAccumulePlusieursTicks()
    {
        double reste;
        int ticks = Cadence.NombreDeTicks(1.0, Cadence.DureeTickParDefautSecondes, out reste);

        Assert.Equal(8, ticks);
        Assert.Equal(0.0, reste, 12);
    }

    /// <summary>
    /// La fenêtre d'entrée d'un virage vaut exactement un tick (§4.1). Elle doit rester plus courte
    /// qu'un temps de réaction visuel simple (200–250 ms, ordre de grandeur admis, NON mesuré ici) :
    /// c'est ce qui impose de décider une case à l'avance plutôt que de réagir au mur qui arrive.
    /// Si la valeur par défaut passait au-dessus, la compétence visée par le §4.1 changerait de
    /// nature — ça se rediscute au GDD.
    /// </summary>
    [Fact]
    public void LaFenetreDeVirageResteSousLeTempsDeReactionVisuel()
    {
        Assert.True(Cadence.DureeTickSecondes() < 0.200,
            "La fenêtre de virage dépasse le temps de réaction visuel : le §4.1 devient faux.");
    }
}
