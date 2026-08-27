using System;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// Le pas de temps du jeu (GDD §4.1) : le serpent avance d'une case par tick, jamais entre
    /// deux ticks. Le tick est l'unité de mesure de tout ce qui sera réglé ensuite.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Dépendance du câblage moteur, non couverte ici</b> : l'arbitrage du 2026-08-27 sur le
    /// rattrapage (§4.1) suppose que <b>perdre le focus de la fenêtre met le jeu en pause</b>. Ça
    /// s'écrit avec <c>Application.focusChanged</c>, donc côté <c>Gameplay/</c> — pas dans
    /// <c>Rules/</c>. Sans cette pause, un alt-tab reste jouable mais fait perdre au joueur tout le
    /// temps passé hors de la fenêtre : le plafond de rattrapage jette le retard, il ne le rend pas.
    /// </remarks>
    public static class Cadence
    {
        /// <summary>
        /// Cadence par défaut, en ticks par seconde.
        /// </summary>
        /// <remarks>
        /// ⚠ Valeur posée <b>au jugé, à confirmer en jeu</b> — aucune session n'est consignée dans
        /// <c>docs/TEST_REPORT.md</c> au 2026-08-27. Fourchette à essayer : 6 à 10 ticks/s
        /// (<see cref="CadenceMinimaleConseillee"/> / <see cref="CadenceMaximaleConseillee"/>).
        /// Le raisonnement du §4.1 : la fenêtre d'entrée d'un virage vaut exactement un tick, donc
        /// 125 ms — plus court qu'un temps de réaction visuel simple. On ne réagit pas à un mur qui
        /// arrive, on décide une case à l'avance ; c'est la compétence visée.
        ///
        /// <para>⚠ <b>Profondeur de file et cadence sont liées</b> (§4.2) : la file de profondeur 2
        /// couvre un virage en L d'un seul geste, soit 250 ms à 8 ticks/s. Revoir
        /// <see cref="FileEntrees.ProfondeurParDefaut"/> si cette valeur bouge.</para>
        /// </remarks>
        public const double TicksParSecondeParDefaut = 8.0;

        /// <summary>Durée d'un tick à la cadence par défaut : 125 ms.</summary>
        public const double DureeTickParDefautSecondes = 1.0 / TicksParSecondeParDefaut;

        /// <summary>Borne basse de la fourchette à essayer en jeu (§4.1) — pas une limite dure.</summary>
        public const double CadenceMinimaleConseillee = 6.0;

        /// <summary>Borne haute de la fourchette à essayer en jeu (§4.1) — pas une limite dure.</summary>
        public const double CadenceMaximaleConseillee = 10.0;

        /// <summary>
        /// Durée d'un tick, en secondes, pour une cadence donnée.
        /// </summary>
        /// <remarks>
        /// La surcharge par paramètre est ce qui rend la cadence réglable <b>sans recompiler</b>
        /// (§4.1) : l'appelant moteur lit la valeur d'un JSON de <c>StreamingAssets</c> et la passe
        /// ici. La constante n'est que le repli quand aucun réglage n'est fourni.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Cadence nulle, négative ou non finie. Volontairement <b>pas de clamp silencieux</b> :
        /// un fichier de tuning mal saisi doit se voir tout de suite, pas produire un jeu figé ou
        /// un tick de durée infinie que personne ne saurait expliquer.
        /// </exception>
        public static double DureeTickSecondes(double ticksParSeconde = TicksParSecondeParDefaut)
        {
            if (double.IsNaN(ticksParSeconde) || double.IsInfinity(ticksParSeconde) || ticksParSeconde <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ticksParSeconde),
                    ticksParSeconde,
                    "La cadence doit être un nombre fini strictement positif (ticks par seconde).");
            }

            return 1.0 / ticksParSeconde;
        }

        /// <summary>
        /// Vrai si la cadence tombe dans la fourchette que le design se propose d'essayer (§4.1).
        /// </summary>
        /// <remarks>
        /// Sert à <b>avertir</b> l'appelant qui charge un fichier de tuning, pas à refuser la
        /// valeur : hors fourchette reste jouable, et c'est justement ce qu'on veut pouvoir
        /// essayer. La distinction « invalide » / « inhabituel » appartient au game-designer.
        /// </remarks>
        public static bool EstDansLaFourchetteConseillee(double ticksParSeconde)
        {
            return ticksParSeconde >= CadenceMinimaleConseillee
                   && ticksParSeconde <= CadenceMaximaleConseillee;
        }

        /// <summary>
        /// Cadence réellement appliquée à un instant de la partie. Elle <b>ne dépend ni de la
        /// longueur du serpent ni du score</b> : elle vaut la cadence de base, toujours.
        /// </summary>
        /// <remarks>
        /// ⚠ Cette méthode existe pour <b>verrouiller une décision</b>, pas pour calculer :
        /// « cadence constante sur toute la partie », arbitrée par l'auteur le 2026-08-27 contre la
        /// canonicité du Snake Nokia (§4.1, écarté détaillé en §7). L'accélération avec la longueur
        /// est un multiplicateur, pas une règle nommée : elle s'empile sur une difficulté qui monte
        /// déjà seule, elle brouille l'attribution de la mort (§2), et elle rend le tick variable,
        /// donc deux parties incomparables au banc.
        ///
        /// <para><paramref name="longueurDuSerpent"/> est ignoré <b>exprès</b> : c'est le point de
        /// passage obligé qu'un futur « et si on accélérait un peu ? » modifierait, et le test qui
        /// l'accompagne le ferait alors tomber. Rouvrir le sujet passe par le §7, pas par ce
        /// fichier.</para>
        /// </remarks>
        public static double CadenceEffective(double cadenceDeBase, int longueurDuSerpent)
        {
            return cadenceDeBase;
        }

        /// <summary>
        /// Plafond de rattrapage par défaut : <b>1 tick par image</b> (§4.1, arbitrage de l'auteur
        /// du 2026-08-27).
        /// </summary>
        /// <remarks>
        /// Sans plafond, une seconde de gel (alt-tab, chargement) fait parcourir huit cases d'un
        /// coup, <b>invisibles</b> : la mort qui suit n'est imputable à aucun virage, ce que le §2
        /// interdit. Le prix assumé est une brève dérive de la cadence après un hoquet — préférable
        /// à des cases parcourues hors de la vue du joueur.
        ///
        /// <para>Paramétrable comme le reste du tuning : quelqu'un voudra l'essayer à 2.</para>
        /// </remarks>
        public const int PlafondDeRattrapageParDefaut = 1;

        /// <summary>
        /// Découpe un temps accumulé en nombre de ticks à jouer, plafonné, et rend le reliquat.
        /// </summary>
        /// <param name="tempsAccumuleSecondes">Temps écoulé non encore converti en ticks.</param>
        /// <param name="dureeTickSecondes">Durée d'un tick, issue de <see cref="DureeTickSecondes"/>.</param>
        /// <param name="reste">Reliquat à reporter sur l'image suivante — toujours &lt; un tick.</param>
        /// <param name="plafondDeRattrapage">Ticks joués au maximum sur une image (§4.1).</param>
        /// <remarks>
        /// <b>Deux comportements qui coexistent, et qu'il ne faut pas confondre :</b>
        ///
        /// <para>1. <b>En régime normal, le reliquat est reporté, pas jeté.</b> Remettre
        /// l'accumulateur à zéro à chaque tick fait dériver la cadence réelle vers le bas dès que le
        /// pas d'image ne divise pas la durée du tick (à 60 Hz, 125 ms tombe entre deux images). Une
        /// dérive de quelques pourcents ne lève rien mais fausse toute mesure de durée de partie.</para>
        ///
        /// <para>2. <b>Au-delà du plafond, le retard est PERDU</b> (§4.1). ⚠ C'est le piège de cette
        /// règle : le reliquat rendu est <b>toujours la seule fraction sous-tick</b>, jamais le
        /// retard complet. Reporter le retard complet rendrait le plafond parfaitement inopérant —
        /// les huit cases d'une seconde de gel passeraient en huit images successives au lieu d'une
        /// seule, et le joueur les verrait défiler sans pouvoir rien y faire, ce qui est exactement
        /// le défaut que le plafond corrige. Conserver la fraction sous-tick, elle, ne rattrape rien
        /// (elle vaut moins d'un tick par construction) : elle garde seulement la phase du tick, et
        /// c'est ce qui laisse le régime normal identique au comportement d'avant le plafond.</para>
        /// </remarks>
        public static int NombreDeTicks(
            double tempsAccumuleSecondes,
            double dureeTickSecondes,
            out double reste,
            int plafondDeRattrapage = PlafondDeRattrapageParDefaut)
        {
            if (dureeTickSecondes <= 0.0 || double.IsNaN(dureeTickSecondes) || double.IsInfinity(dureeTickSecondes))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dureeTickSecondes),
                    dureeTickSecondes,
                    "La durée d'un tick doit être un nombre fini strictement positif.");
            }

            if (plafondDeRattrapage < 1)
            {
                // Un plafond nul figerait le jeu sans rien lever : le serpent n'avancerait plus.
                throw new ArgumentOutOfRangeException(
                    nameof(plafondDeRattrapage),
                    plafondDeRattrapage,
                    "Une image doit pouvoir jouer au moins un tick.");
            }

            if (double.IsNaN(tempsAccumuleSecondes) || double.IsInfinity(tempsAccumuleSecondes))
            {
                // Un accumulateur non fini vient forcément d'un delta d'image aberrant en amont :
                // le laisser passer produirait un nombre de ticks négatif (cast d'infini) et un
                // serpent qui recule. Mieux vaut que l'appelant le découvre ici.
                throw new ArgumentOutOfRangeException(
                    nameof(tempsAccumuleSecondes),
                    tempsAccumuleSecondes,
                    "Le temps accumulé doit être un nombre fini.");
            }

            if (tempsAccumuleSecondes <= 0.0)
            {
                reste = 0.0;
                return 0;
            }

            double ticksDus = Math.Floor(tempsAccumuleSecondes / dureeTickSecondes);

            // La fraction sous-tick, et rien d'autre. Calculée à partir des ticks DUS et non des
            // ticks joués : c'est ce qui jette le retard au lieu de le reporter.
            double fractionSousTick = tempsAccumuleSecondes - (ticksDus * dureeTickSecondes);
            reste = fractionSousTick >= 0.0 && fractionSousTick < dureeTickSecondes ? fractionSousTick : 0.0;

            if (ticksDus > plafondDeRattrapage)
            {
                // Retour anticipé : au-delà du plafond, `ticksDus` peut dépasser la capacité d'un
                // int (durée de tick minuscule), et son cast rendrait un nombre négatif.
                return plafondDeRattrapage;
            }

            return (int)ticksDus;
        }
    }
}
