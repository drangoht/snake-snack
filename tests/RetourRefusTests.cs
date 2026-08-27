using System;
using System.Collections.Generic;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// Ce que le brief artistique impose au retour d'une entrée refusée (<c>docs/ART.md</c> §5).
/// </summary>
public class RoutageRefusTests
{
    /// <summary>
    /// ART §5.7 : « Jamais de retour pour <c>RefuseeDoublon</c> ». C'est l'interdit le plus facile à
    /// défaire par mégarde en « uniformisant » le routage — d'où un test à lui tout seul.
    /// </summary>
    [Fact]
    public void LeDoublonNeRecoitAucunRetour()
    {
        Assert.Equal(RegistreRefus.Aucun, RoutageRefus.Registre(MotifRefus.Doublon));
    }

    /// <summary>
    /// ART §5.2 : le demi-tour et la file pleine partagent le MÊME pictogramme. À 125 ms par tick,
    /// rien ne permet d'enseigner la nuance ; ce qui doit se lire, c'est que l'appui n'a pas compté.
    /// </summary>
    [Fact]
    public void LeDemiTourEtLaFilePleinePartagentLeMemePictogramme()
    {
        Assert.Equal(RegistreRefus.Pictogramme, RoutageRefus.Registre(MotifRefus.DemiTour));
        Assert.Equal(RegistreRefus.Pictogramme, RoutageRefus.Registre(MotifRefus.FilePleine));
    }

    [Fact]
    public void LaDirectionTapeeEnPauseVaSurLEcranDePause()
    {
        Assert.Equal(RegistreRefus.TextePause, RoutageRefus.Registre(MotifRefus.JeuEnPause));
    }

    /// <summary>
    /// ⚠ Le test qui protège du refus muet : tout motif doit avoir un registre DÉCIDÉ. Ajouter une
    /// valeur à l'énumération sans la router ferait échouer ce test au lieu de produire un refus
    /// invisible — donc, pour le joueur, inexistant (GDD §3).
    /// </summary>
    [Fact]
    public void ToutMotifDeRefusAUnRegistreDecide()
    {
        foreach (MotifRefus motif in Enum.GetValues(typeof(MotifRefus)))
        {
            RoutageRefus.Registre(motif);
        }
    }

    /// <summary>
    /// Même garde-fou côté source : tout résultat d'empilage doit se traduire, ou dire clairement
    /// qu'il n'y a rien à traduire.
    /// </summary>
    [Fact]
    public void ToutResultatDEmpilageSeTraduitEnMotifDeRetour()
    {
        foreach (ResultatEmpilage resultat in Enum.GetValues(typeof(ResultatEmpilage)))
        {
            bool refuse = RoutageRefus.DepuisEmpilage(resultat, out MotifRefus motif);

            Assert.Equal(resultat != ResultatEmpilage.Acceptee, refuse);

            if (refuse)
            {
                RoutageRefus.Registre(motif);
            }
        }
    }

    /// <summary>
    /// ⚠ Le piège que le brief corrigé signale : <c>ResultatEmpilage</c> ne contient PAS le
    /// demi-tour, parce que le demi-tour se juge au tick (GDD §4.2). Une UI branchée sur le seul
    /// <c>Empiler()</c> n'afficherait donc jamais le refus que le §3 impose de rendre visible. Ce
    /// test verrouille la conséquence : aucun résultat d'empilage ne produit le motif demi-tour.
    /// </summary>
    [Fact]
    public void AucunResultatDEmpilageNeProduitLeMotifDemiTour()
    {
        foreach (ResultatEmpilage resultat in Enum.GetValues(typeof(ResultatEmpilage)))
        {
            if (RoutageRefus.DepuisEmpilage(resultat, out MotifRefus motif))
            {
                Assert.NotEqual(MotifRefus.DemiTour, motif);
            }
        }
    }
}

/// <summary>
/// L'anti-répétition du retour (<c>docs/ART.md</c> §5.5) — le point qui traite le martelage.
/// </summary>
public class EtatRetourAEcheanceTests
{
    private const double Affichage = 0.25;   // ART §5.5 : 250 ms
    private const double Plafond = 0.5;      // ART §5.5 : 500 ms
    private const double Fondu = 0.06;

    private static EtatRetourAEcheance Neuf()
    {
        return new EtatRetourAEcheance(Affichage, Plafond, Fondu);
    }

    [Fact]
    public void UneNotificationRendLeRetourVisible()
    {
        EtatRetourAEcheance etat = Neuf();

        Assert.True(etat.Notifier(0.0));
        Assert.True(etat.EstVisible(0.1));
        Assert.False(etat.EstVisible(1.0));
    }

    /// <summary>
    /// ART §5.5 : « une notification reçue pendant que le retour est déjà visible PROLONGE
    /// l'échéance, SANS relancer l'animation d'apparition ». Le <c>false</c> rendu est le signal
    /// qui dit à l'appelant de ne rien rejouer.
    /// </summary>
    [Fact]
    public void UneSecondeNotificationProlongeSansRelancerLApparition()
    {
        EtatRetourAEcheance etat = Neuf();
        etat.Notifier(0.0);

        Assert.False(etat.Notifier(0.2));

        // Prolongé : au-delà de l'échéance initiale de 0,25 s, le retour est encore là...
        Assert.True(etat.EstVisible(0.4));
        // ... et il est à pleine opacité, pas en train de refaire son fondu d'entrée.
        Assert.Equal(1.0, etat.Opacite(0.4), 9);
    }

    /// <summary>
    /// ⚠ L'interdit de l'ART §5.7 : « jamais un retour qui dépasse son plafond de prolongation
    /// continue sans s'éteindre au moins une fois ». Le test rejoue le pire martelage possible — une
    /// notification à CHAQUE image — dans l'ordre réel du moteur (on traite les entrées, puis on
    /// dessine), et exige que l'opacité dessinée retombe à zéro.
    ///
    /// <para>⚠ La mesure porte sur la <b>DURÉE</b> pendant laquelle l'opacité reste nulle, pas sur le
    /// fait qu'elle atteigne zéro. La première version de ce test se contentait de « l'opacité
    /// retombe à zéro » : <b>elle passait au vert sur une implémentation sans aucune protection</b>,
    /// parce qu'une notification qui relance l'état rend forcément l'opacité nulle à l'instant du
    /// redémarrage. L'instant d'extinction existe dans les deux cas ; ce qui distingue une
    /// extinction visible d'une non-extinction, c'est qu'elle dure.
    ///
    /// <para>Version actuelle vue ROUGE avant d'être gardée : en supprimant le temps d'arrêt de
    /// <c>Notifier</c>, la plus longue plage à opacité nulle tombe à une image et le test échoue.</para>
    /// </summary>
    [Fact]
    public void SousMartelageContinuLeRetourResteEteintAssezLongtempsPourSeVoir()
    {
        EtatRetourAEcheance etat = Neuf();
        double debutEteint = -1.0;
        double plusLongueExtinction = 0.0;

        for (int i = 0; i <= 2000; i++)
        {
            double maintenant = i / 1000.0;
            etat.Notifier(maintenant);

            if (etat.Opacite(maintenant) <= 1e-9)
            {
                if (debutEteint < 0.0)
                {
                    debutEteint = maintenant;
                }

                double duree = maintenant - debutEteint;
                if (duree > plusLongueExtinction)
                {
                    plusLongueExtinction = duree;
                }
            }
            else
            {
                debutEteint = -1.0;
            }
        }

        Assert.True(plusLongueExtinction >= Fondu - 0.002,
            $"La plus longue extinction dure {plusLongueExtinction} s : trop courte pour se voir, le plafond ne plafonne rien.");
    }

    /// <summary>
    /// La visibilité continue ne dépasse jamais le plafond, fondu de sortie compris : c'est la
    /// mesure du même interdit, mais chiffrée.
    /// </summary>
    [Fact]
    public void LaVisibiliteContinueNeDepasseJamaisLePlafond()
    {
        EtatRetourAEcheance etat = Neuf();
        double debutVisible = -1.0;
        double pireDuree = 0.0;

        for (int i = 0; i <= 4000; i++)
        {
            double maintenant = i / 1000.0;
            etat.Notifier(maintenant);

            if (etat.Opacite(maintenant) > 1e-9)
            {
                if (debutVisible < 0.0)
                {
                    debutVisible = maintenant;
                }

                double duree = maintenant - debutVisible;
                if (duree > pireDuree)
                {
                    pireDuree = duree;
                }
            }
            else
            {
                debutVisible = -1.0;
            }
        }

        // Plafond + le fondu de sortie, qui vient après l'échéance par construction.
        Assert.True(pireDuree <= Plafond + Fondu + 0.002,
            $"Visibilité continue de {pireDuree} s pour un plafond de {Plafond} s.");
    }

    /// <summary>
    /// ART §5.7 : « une seule enveloppe fondu-entrée/fondu-sortie par déclenchement », jamais de
    /// stroboscope. Le test vérifie qu'une opacité qui a commencé à redescendre ne remonte JAMAIS
    /// sans être d'abord passée par zéro — c'est la définition opérationnelle du « pas de re-flash ».
    /// </summary>
    [Fact]
    public void LOpaciteNeRemonteJamaisSansEtrePasseeParZero()
    {
        EtatRetourAEcheance etat = Neuf();
        double precedente = 0.0;
        bool enDescente = false;

        for (int i = 0; i <= 3000; i++)
        {
            double maintenant = i / 1000.0;
            etat.Notifier(maintenant);
            double opacite = etat.Opacite(maintenant);

            if (opacite < precedente - 1e-9)
            {
                enDescente = true;
            }
            else if (opacite > precedente + 1e-9)
            {
                Assert.False(enDescente && precedente > 1e-9,
                    $"Re-flash à t={maintenant} : l'opacité remonte de {precedente} à {opacite} sans passer par zéro.");
                enDescente = false;
            }

            precedente = opacite;
        }
    }

    /// <summary>
    /// Une notification bien après l'extinction est un NOUVEAU déclenchement : l'appelant doit
    /// repositionner le pictogramme (la tête a bougé) et rejouer l'enveloppe.
    /// </summary>
    [Fact]
    public void ApresExtinctionCompleteUneNotificationRelanceLApparition()
    {
        EtatRetourAEcheance etat = Neuf();
        etat.Notifier(0.0);

        Assert.True(etat.Notifier(5.0));
        Assert.Equal(0.0, etat.Opacite(5.0), 9);
        Assert.Equal(1.0, etat.Opacite(5.0 + Fondu), 9);
    }

    [Fact]
    public void EteindreCoupeLeRetourSurLeChamp()
    {
        EtatRetourAEcheance etat = Neuf();
        etat.Notifier(0.0);

        etat.Eteindre();

        Assert.False(etat.EstVisible(0.1));
        Assert.Equal(0.0, etat.Opacite(0.1), 9);
    }

    /// <summary>
    /// Un plafond plus court que la durée d'affichage éteindrait le retour avant qu'il soit lu :
    /// c'est un réglage incohérent, il doit échouer à la construction et non produire un
    /// scintillement que personne ne saura expliquer.
    /// </summary>
    [Fact]
    public void UnPlafondPlusCourtQueLAffichageEstRefuse()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EtatRetourAEcheance(0.25, 0.1, 0.01));
    }

    [Fact]
    public void UnFonduQuiNeTientPasDeuxFoisDansLAffichageEstRefuse()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EtatRetourAEcheance(0.25, 0.5, 0.2));
    }

    /// <summary>
    /// Le texte de l'écran de pause utilise le même mécanisme avec ses propres durées (ART §5.5 :
    /// 1,5 s, non lié au tick). Il doit tenir la seconde et demie sans clignoter.
    /// </summary>
    [Fact]
    public void LeRegistreDeTextePauseSupporteSesPropresDurees()
    {
        EtatRetourAEcheance texte = new EtatRetourAEcheance(1.5, 3.0, 0.1);
        List<double> opacites = new List<double>();

        texte.Notifier(0.0);
        // De 0,2 s à 1,4 s : toute la tenue, avant l'échéance de 1,5 s.
        for (int i = 0; i <= 12; i++)
        {
            opacites.Add(texte.Opacite(0.2 + (i / 10.0)));
        }

        Assert.All(opacites, o => Assert.True(o > 0.0));
    }
}
