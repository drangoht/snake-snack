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
    /// Le retard de cadence NE SE RATTRAPE PAS (§4.1, arbitrage de l'auteur du 2026-08-27) : une
    /// image ne fait avancer le serpent que d'un tick. Sans ce plafond, une seconde de gel fait
    /// parcourir huit cases d'un coup, invisibles, et la mort qui suit n'est imputable à aucun
    /// virage — ce que le §2 interdit.
    /// </summary>
    [Fact]
    public void UnGelDImageNeFaitAvancerQueDUnTick()
    {
        double reste;
        int ticks = Cadence.NombreDeTicks(0.9, Cadence.DureeTickParDefautSecondes, out reste);

        Assert.Equal(1, ticks);
        Assert.Equal(Cadence.PlafondDeRattrapageParDefaut, ticks);
    }

    /// <summary>
    /// LE piège de cette règle : le retard doit être JETÉ, pas reporté. S'il était conservé dans le
    /// reliquat, le plafond ne servirait à rien — les huit cases du gel passeraient en huit images
    /// successives au lieu d'une seule, le joueur les regarderait défiler sans pouvoir agir, et le
    /// défaut que le plafond corrige serait simplement étalé dans le temps.
    ///
    /// Ce test échoue si quelqu'un rend le retard complet dans le reliquat : les dix images qui
    /// suivent le gel joueraient dix ticks au lieu d'un.
    /// </summary>
    [Fact]
    public void LeRetardJeteNeRevientPasAuxImagesSuivantes()
    {
        const double pasDImage = 1.0 / 60.0;
        double dureeTick = Cadence.DureeTickSecondes();

        // Un gel d'environ une seconde : sept ticks dus, un seul joué, six jetés.
        double accumulateur = 0.9;
        Assert.Equal(1, Cadence.NombreDeTicks(accumulateur, dureeTick, out accumulateur));

        // Le reliquat ne porte plus que la fraction sous-tick, jamais le retard.
        Assert.True(accumulateur < dureeTick, $"Le retard a été reporté : reliquat {accumulateur}.");

        int ticksApres = 0;
        for (int image = 0; image < 10; image++)
        {
            accumulateur += pasDImage;
            ticksApres += Cadence.NombreDeTicks(accumulateur, dureeTick, out accumulateur);
        }

        // 10 images à 60 Hz = 167 ms : un seul tick, celui que la cadence normale y place.
        Assert.Equal(1, ticksApres);
    }

    /// <summary>
    /// Le plafond est du tuning comme le reste : quelqu'un voudra l'essayer à 2 sans recompiler.
    /// </summary>
    [Fact]
    public void LePlafondDeRattrapageEstReglable()
    {
        double reste;

        Assert.Equal(2, Cadence.NombreDeTicks(0.9, Cadence.DureeTickParDefautSecondes, out reste, 2));
        Assert.Equal(7, Cadence.NombreDeTicks(0.9, Cadence.DureeTickParDefautSecondes, out reste, 8));

        // Le plafond ne CRÉE pas de ticks : sous le plafond, on joue ce qui est dû, pas davantage.
        Assert.Equal(1, Cadence.NombreDeTicks(0.2, Cadence.DureeTickParDefautSecondes, out reste, 8));
    }

    /// <summary>
    /// Un plafond nul figerait le serpent sans rien lever : c'est la classe de bug que ce dépôt
    /// traque, donc on lève à la lecture du réglage.
    /// </summary>
    [Fact]
    public void UnPlafondInferieurAUnTickEstRefuse()
    {
        double reste;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cadence.NombreDeTicks(0.5, Cadence.DureeTickParDefautSecondes, out reste, 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cadence.NombreDeTicks(0.5, Cadence.DureeTickParDefautSecondes, out reste, -1));
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
