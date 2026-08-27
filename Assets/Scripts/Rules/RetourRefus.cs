using System;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// Motif de refus tel que la <b>couche de retour visuel</b> le connaît
    /// (<c>docs/ART.md</c> §5.5).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Cette énumération est distincte de <see cref="ResultatEmpilage"/>, et c'est le point
    /// entier de sa présence.</b> Le motif de refus n'a pas une source unique :
    /// <list type="bullet">
    /// <item><see cref="FilePleine"/>, <see cref="JeuEnPause"/> et <see cref="Doublon"/> viennent de
    /// <see cref="FileEntrees.Empiler"/>, au moment de l'appui ;</item>
    /// <item><see cref="DemiTour"/> vient de <see cref="FileEntrees.Tick"/>
    /// (<see cref="ResultatTick.DemiTourRefuse"/>), <b>un tick plus tard</b>, parce que le demi-tour
    /// se juge contre la direction réellement appliquée — contre-exemple Nord/Sud du GDD §4.2 ;</item>
    /// <item><see cref="DemiTour"/> vient <i>aussi</i> de <see cref="Demarrage.Decider"/>, avant même
    /// qu'un tick ait eu lieu (GDD §4.1 : taper Ouest au départ montre le refus et ne lance rien).</item>
    /// </list>
    ///
    /// <para>⚠ <b>Ne jamais « uniformiser » en ajoutant le demi-tour à
    /// <see cref="ResultatEmpilage"/></b> : ce serait déclarer qu'un demi-tour peut être refusé à
    /// l'empilage, exactement l'erreur que <see cref="FileEntrees"/> a été écrite pour rendre
    /// impossible. Corollaire pratique : une UI qui n'écouterait que <c>Empiler()</c> n'afficherait
    /// <b>jamais</b> le refus de demi-tour — le cas que le GDD §3 impose pourtant de rendre
    /// visible.</para>
    /// </remarks>
    public enum MotifRefus
    {
        /// <summary>Demi-tour instantané. Sources : le tick, et la décision de démarrage.</summary>
        DemiTour,

        /// <summary>File pleine : le troisième virage est ignoré. Source : l'empilage.</summary>
        FilePleine,

        /// <summary>Direction tapée pendant la pause. Source : l'empilage.</summary>
        JeuEnPause,

        /// <summary>
        /// Direction déjà suivie. Source : l'empilage. <b>Aucun retour</b> (ART §5.3) — présent dans
        /// l'énumération pour être filtré explicitement, et non passé sous silence.
        /// </summary>
        Doublon
    }

    /// <summary>
    /// Registre de retour visuel d'un motif de refus (<c>docs/ART.md</c> §5.2).
    /// </summary>
    public enum RegistreRefus
    {
        /// <summary>Aucun retour. Le joueur n'a rien raté : rien ne s'affiche.</summary>
        Aucun,

        /// <summary>Chevron barré ancré au bord de la case tête (§5.4, variante A).</summary>
        Pictogramme,

        /// <summary>Ligne de texte ajoutée à l'écran de pause déjà affiché (§5.4).</summary>
        TextePause
    }

    /// <summary>
    /// Traduit les refus des deux sources en motifs de la couche de retour, et dit où chacun va
    /// (<c>docs/ART.md</c> §5.2 et §5.5, tranchés par l'auteur le 2026-08-27).
    /// </summary>
    public static class RoutageRefus
    {
        /// <summary>
        /// Traduit un résultat d'empilage en motif de retour.
        /// </summary>
        /// <param name="motif">Motif traduit. N'a de sens que si la méthode rend <c>true</c>.</param>
        /// <returns><c>false</c> pour <see cref="ResultatEmpilage.Acceptee"/> : rien n'a été refusé.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Résultat d'empilage inconnu — voir la remarque de <see cref="Registre"/>.
        /// </exception>
        public static bool DepuisEmpilage(ResultatEmpilage resultat, out MotifRefus motif)
        {
            switch (resultat)
            {
                case ResultatEmpilage.Acceptee:
                    motif = MotifRefus.Doublon; // Valeur sans emploi : la méthode rend false.
                    return false;

                case ResultatEmpilage.RefuseeDoublon:
                    motif = MotifRefus.Doublon;
                    return true;

                case ResultatEmpilage.RefuseeFilePleine:
                    motif = MotifRefus.FilePleine;
                    return true;

                case ResultatEmpilage.RefuseeJeuEnPause:
                    motif = MotifRefus.JeuEnPause;
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(resultat), resultat, "Résultat d'empilage sans motif de retour décidé (docs/ART.md §5.5).");
            }
        }

        /// <summary>
        /// Registre du motif. <see cref="MotifRefus.Doublon"/> ne reçoit <b>rien</b> — et c'est
        /// écrit noir sur blanc, pas omis.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Motif inconnu. ⚠ <b>Volontairement bruyant</b> : ajouter un motif sans décider de son
        /// registre donnerait un refus muet, donc « invisible, donc inexistant » (GDD §3). Mieux
        /// vaut une exception au premier appui qu'un joueur qui croit le jeu cassé.
        /// </exception>
        public static RegistreRefus Registre(MotifRefus motif)
        {
            switch (motif)
            {
                case MotifRefus.Doublon:
                    // ⚠ FILTRÉ EXPLICITEMENT (ART §5.3), ce n'est pas un oubli à corriger.
                    // Ce n'est pas une erreur : l'intention du joueur (continuer dans cette
                    // direction) est déjà satisfaite par ce qui va s'exécuter, et le serpent qui
                    // continue tout droit EST la confirmation. C'est aussi le motif le plus fréquent
                    // des quatre : l'afficher désensibiliserait au même pictogramme, le seul cas où
                    // ce signe doit rester associé à « j'ai fait une erreur ».
                    return RegistreRefus.Aucun;

                case MotifRefus.DemiTour:
                case MotifRefus.FilePleine:
                    // Le MÊME pictogramme pour les deux (ART §5.2) : à 125 ms par tick, rien ne
                    // permet d'enseigner la nuance entre « demi-tour » et « troisième virage de
                    // trop ». Ce qui doit se lire, c'est que l'appui n'a pas compté.
                    return RegistreRefus.Pictogramme;

                case MotifRefus.JeuEnPause:
                    // Hors de toute pression de temps : la simulation est figée, le joueur peut lire
                    // une phrase.
                    return RegistreRefus.TextePause;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(motif), motif, "Motif de refus sans registre visuel décidé (docs/ART.md §5.2).");
            }
        }
    }

    /// <summary>
    /// L'anti-répétition du retour de refus (<c>docs/ART.md</c> §5.5) : un <b>état à échéance</b>,
    /// jamais une animation rejouée.
    /// </summary>
    /// <remarks>
    /// Le brief impose trois comportements, et chacun corrige un défaut précis :
    /// <list type="number">
    /// <item>une notification affiche le retour et fixe son échéance ;</item>
    /// <item>une notification reçue pendant l'affichage <b>prolonge</b> l'échéance <b>sans relancer
    /// l'apparition</b> — sinon le martelage produit un scintillement ;</item>
    /// <item>un <b>plafond de prolongation continue</b> force l'extinction : « un signal toujours
    /// visible cesse d'être lu comme un signal ».</item>
    /// </list>
    ///
    /// <para>⚠ <b>L'extinction est protégée, fondu de sortie ET temps d'arrêt</b> : une notification
    /// reçue pendant l'un ou l'autre est ignorée. Sans cette protection, le plafond ne plafonne rien
    /// sous martelage — c'est le même piège que le plafond de rattrapage de <see cref="Cadence"/> :
    /// un plafond qui n'oblige à aucune interruption observable n'en est pas un.
    ///
    /// <para>⚠ Et le temps d'arrêt ne peut pas être supprimé « puisque le fondu suffit » : sans lui,
    /// une notification tombant pile à la fin du fondu rallume le retour à l'image même où il vient
    /// de s'éteindre. Le joueur ne voit alors <b>aucune</b> coupure — juste une opacité qui plonge et
    /// remonte en une image. Le premier garde-fou écrit ici mesurait « l'opacité retombe à zéro » :
    /// il passait au vert sur les deux implémentations, parce que l'instant d'extinction existe dans
    /// les deux cas. Ce qui distingue une extinction visible d'une non-extinction, c'est sa
    /// <b>durée</b>, pas son existence.</para>
    ///
    /// <para>Le temps d'arrêt vaut une durée de fondu : il est <b>déduit</b> de ce paramètre plutôt
    /// que posé comme une valeur de plus, pour qu'un seul réglage gouverne toute l'enveloppe.</para>
    ///
    /// <para>⚠ <b>Durées : aucune n'a été essayée en jeu</b> (ART §5.5, « au jugé, à confirmer par le
    /// game-tester »). Elles se règlent sans recompiler via <see cref="ReglagesJeu"/>.</para>
    ///
    /// <para>Classe à état, sans dépendance moteur : le temps lui est <b>fourni</b> à chaque appel
    /// plutôt que lu dans une horloge. C'est ce qui la rend testable en quelques microsecondes, et
    /// ce qui permet de rejouer deux secondes de martelage sans attendre deux secondes.</para>
    /// </remarks>
    public sealed class EtatRetourAEcheance
    {
        private readonly double _dureeAffichage;
        private readonly double _plafondProlongation;
        private readonly double _dureeFondu;

        private bool _actif;
        private double _debut;
        private double _echeance;

        /// <param name="dureeAffichageSecondes">Durée d'affichage par déclenchement (ART §5.5).</param>
        /// <param name="plafondProlongationSecondes">
        /// Durée de visibilité continue au-delà de laquelle le retour s'éteint, quitte à se rallumer
        /// si le martelage continue.
        /// </param>
        /// <param name="dureeFonduSecondes">
        /// Durée de l'enveloppe de fondu, en entrée comme en sortie. Une seule enveloppe par
        /// déclenchement (ART §5.7 : jamais de stroboscope).
        /// </param>
        public EtatRetourAEcheance(double dureeAffichageSecondes, double plafondProlongationSecondes, double dureeFonduSecondes)
        {
            if (dureeAffichageSecondes <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dureeAffichageSecondes), dureeAffichageSecondes,
                    "Un retour qui dure zéro seconde est un retour invisible, donc inexistant (GDD §3).");
            }

            if (plafondProlongationSecondes < dureeAffichageSecondes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plafondProlongationSecondes), plafondProlongationSecondes,
                    "Le plafond de prolongation ne peut pas être plus court que la durée d'affichage : le retour s'éteindrait avant d'avoir été lu.");
            }

            if (dureeFonduSecondes <= 0.0)
            {
                // ⚠ Un fondu nul rendrait le plafond inopérant : l'extinction n'aurait aucune durée,
                // donc rien à voir pour le joueur, et le retour se rallumerait dans la foulée sous
                // martelage. C'est exactement l'interdit de l'ART §5.7.
                throw new ArgumentOutOfRangeException(
                    nameof(dureeFonduSecondes), dureeFonduSecondes,
                    "Sans fondu, l'extinction imposée par le plafond n'a aucune durée : elle serait invisible, donc inexistante (ART §5.7).");
            }

            if (dureeFonduSecondes * 2.0 > dureeAffichageSecondes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dureeFonduSecondes), dureeFonduSecondes,
                    "Les fondus doivent tenir dans la durée d'affichage, sinon le retour n'atteint jamais sa pleine opacité.");
            }

            _dureeAffichage = dureeAffichageSecondes;
            _plafondProlongation = plafondProlongationSecondes;
            _dureeFondu = dureeFonduSecondes;
        }

        /// <summary>Durée d'affichage par déclenchement, en secondes.</summary>
        public double DureeAffichage
        {
            get { return _dureeAffichage; }
        }

        /// <summary>Plafond de visibilité continue, en secondes.</summary>
        public double PlafondProlongation
        {
            get { return _plafondProlongation; }
        }

        /// <summary>
        /// Signale un refus à afficher.
        /// </summary>
        /// <returns>
        /// Vrai <b>uniquement</b> si c'est une nouvelle apparition — c'est-à-dire si l'appelant doit
        /// (re)positionner le pictogramme et jouer l'enveloppe. Faux pour une prolongation ou une
        /// notification tombée pendant l'extinction : dans ces deux cas, ne rien relancer.
        /// </returns>
        public bool Notifier(double maintenant)
        {
            if (_actif && maintenant < _echeance)
            {
                // Prolongation : on repousse l'échéance SANS toucher à _debut, donc sans relancer
                // l'apparition. Le plafond se compte depuis l'apparition, pas depuis le dernier
                // appui : c'est ce qui borne la visibilité continue.
                double repoussee = maintenant + _dureeAffichage;
                double plafonnee = _debut + _plafondProlongation;
                _echeance = repoussee < plafonnee ? repoussee : plafonnee;
                return false;
            }

            if (_actif && maintenant < FinDuTempsDArret)
            {
                // Extinction en cours : fondu de sortie, PUIS temps d'arrêt à opacité nulle. On la
                // laisse aller à son terme (voir les remarques de classe). C'est la seule chose qui
                // garantisse que le plafond produise une coupure d'une durée observable, et pas un
                // simple creux d'une image.
                return false;
            }

            _actif = true;
            _debut = maintenant;
            _echeance = maintenant + _dureeAffichage;
            return true;
        }

        /// <summary>
        /// Fin du temps d'arrêt qui suit le fondu de sortie : avant cet instant, aucune notification
        /// ne peut rallumer le retour.
        /// </summary>
        private double FinDuTempsDArret
        {
            get { return _echeance + (2.0 * _dureeFondu); }
        }

        /// <summary>Vrai tant que le retour a une opacité non nulle.</summary>
        public bool EstVisible(double maintenant)
        {
            return _actif && maintenant >= _debut && maintenant < _echeance + _dureeFondu;
        }

        /// <summary>
        /// Opacité du retour, entre 0 et 1 : montée, tenue, descente. Fonction pure du temps — aucun
        /// état n'est modifié ici, pour qu'un rendu appelé plusieurs fois par image ne fasse pas
        /// dériver l'échéance.
        /// </summary>
        public double Opacite(double maintenant)
        {
            if (!EstVisible(maintenant))
            {
                return 0.0;
            }

            if (_dureeFondu <= 0.0)
            {
                return 1.0;
            }

            if (maintenant < _debut + _dureeFondu)
            {
                return (maintenant - _debut) / _dureeFondu;
            }

            if (maintenant >= _echeance)
            {
                return 1.0 - ((maintenant - _echeance) / _dureeFondu);
            }

            return 1.0;
        }

        /// <summary>Éteint le retour sur-le-champ (changement d'état de partie, nouvelle partie).</summary>
        public void Eteindre()
        {
            _actif = false;
            _debut = 0.0;
            _echeance = 0.0;
        }
    }
}
