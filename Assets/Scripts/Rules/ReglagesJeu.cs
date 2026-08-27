using System;
using System.Collections.Generic;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// Les valeurs de tuning du jeu, telles qu'elles sont écrites dans
    /// <c>Assets/StreamingAssets/reglages.json</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Champs publics en camelCase, et c'est délibéré</b> : <c>JsonUtility</c> associe les clés
    /// du JSON aux <i>champs</i> par leur nom exact. Renommer un champ en PascalCase (la convention
    /// du projet) ferait silencieusement retomber la valeur sur son défaut — le fichier de réglages
    /// n'aurait plus aucun effet, sans une ligne d'erreur. C'est la seule entorse à la convention de
    /// nommage du dépôt, et elle est confinée à ce type.
    ///
    /// <para>⚠ <b>Aucune valeur n'est appliquée sans passer par <see cref="Valider"/></b> : un
    /// fichier de tuning est édité à la main, donc il contient tôt ou tard une largeur paire ou une
    /// cadence à zéro. Un zéro qui passe fige le jeu ; une dimension paire décale la pose de départ
    /// d'une demi-case (§4.3). Ni l'un ni l'autre ne lève quoi que ce soit à l'exécution.</para>
    ///
    /// <para>Ce type n'a aucune dépendance moteur : <c>[Serializable]</c> vient de
    /// <c>System</c>, pas d'<c>UnityEngine</c>. Il est donc lisible par <c>JsonUtility</c> côté
    /// Unity et testable par <c>dotnet test</c> côté logique pure.</para>
    /// </remarks>
    [Serializable]
    public sealed class ReglagesJeu
    {
        /// <summary>Cadence du jeu, en ticks par seconde (GDD §4.1 : 8, fourchette 6–10).</summary>
        public double ticksParSeconde = Cadence.TicksParSecondeParDefaut;

        /// <summary>Ticks joués au maximum sur une image (GDD §4.1 : 1, le retard est jeté).</summary>
        public int plafondDeRattrapage = Cadence.PlafondDeRattrapageParDefaut;

        /// <summary>Colonnes de la grille — <b>impair obligatoire</b> (GDD §4.3).</summary>
        public int largeurGrille = Grille.LargeurParDefaut;

        /// <summary>Lignes de la grille — <b>impair obligatoire</b> (GDD §4.3).</summary>
        public int hauteurGrille = Grille.HauteurParDefaut;

        /// <summary>Profondeur de la file d'entrées (GDD §4.2 : 2, liée à la cadence).</summary>
        public int profondeurFile = FileEntrees.ProfondeurParDefaut;

        /// <summary>Durée d'affichage du pictogramme de refus (ART §5.5 : 250 ms, au jugé).</summary>
        public double dureeAffichageRefusSecondes = 0.25;

        /// <summary>Plafond de prolongation continue du pictogramme (ART §5.5 : 500 ms, au jugé).</summary>
        public double plafondProlongationRefusSecondes = 0.5;

        /// <summary>Durée d'affichage de la ligne de texte sur l'écran de pause (ART §5.5 : 1,5 s).</summary>
        public double dureeTextePauseSecondes = 1.5;

        /// <summary>
        /// Durée du fondu d'entrée et de sortie du retour de refus.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Cette valeur ne vient pas du brief.</b> L'ART §5.5 impose que le retour « s'éteigne
        /// une fois » quand il atteint son plafond, et le §5.7 parle d'« une seule enveloppe
        /// fondu-entrée/fondu-sortie par déclenchement » — mais aucune durée n'est donnée pour ces
        /// fondus. 60 ms est posé ici <b>au jugé par le développeur</b> (environ un demi-tick à
        /// 8 ticks/s) : assez long pour que l'extinction imposée par le plafond soit visible, assez
        /// court pour ne pas retarder la lecture du signal. À trancher par le game-tester au même
        /// titre que les trois autres durées.
        /// </remarks>
        public double dureeFonduRefusSecondes = 0.06;

        /// <summary>Un jeu de valeurs identique aux constantes du GDD.</summary>
        public static ReglagesJeu ParDefaut()
        {
            return new ReglagesJeu();
        }

        /// <summary>
        /// Rend un jeu de réglages sûr, et la liste de ce qui a dû être corrigé.
        /// </summary>
        /// <param name="anomalies">
        /// Ce qui n'allait pas, en clair. ⚠ <b>Jamais vide en silence</b> : une correction muette
        /// donnerait un joueur qui édite son JSON, ne voit aucun changement, et n'a aucun moyen de
        /// savoir pourquoi. L'appelant moteur doit les journaliser.
        /// </param>
        /// <remarks>
        /// La règle de correction est toujours la même : <b>retomber sur le défaut du GDD</b> plutôt
        /// que sur une valeur voisine bricolée. Une grille de 20 colonnes ne devient pas 21 — elle
        /// redevient 21 × 15, parce qu'une correction partielle donnerait une aire de jeu que
        /// personne n'a décidée.
        ///
        /// <para>La fourchette conseillée 6–10 ticks/s (§4.1) est signalée mais <b>pas corrigée</b> :
        /// c'est un conseil de design, et le sortir de la fourchette est justement ce qu'on veut
        /// pouvoir essayer sans recompiler.</para>
        /// </remarks>
        public ReglagesJeu Valider(out IList<string> anomalies)
        {
            List<string> constats = new List<string>();
            ReglagesJeu sain = new ReglagesJeu();

            sain.ticksParSeconde = ValiderDouble(
                ticksParSeconde, Cadence.TicksParSecondeParDefaut, "ticksParSeconde", constats);

            if (!Cadence.EstDansLaFourchetteConseillee(sain.ticksParSeconde))
            {
                constats.Add("ticksParSeconde = " + sain.ticksParSeconde + " sort de la fourchette conseillée "
                             + Cadence.CadenceMinimaleConseillee + "–" + Cadence.CadenceMaximaleConseillee
                             + " (GDD §4.1) — valeur conservée, c'est un conseil, pas une borne.");
            }

            if (plafondDeRattrapage < 1)
            {
                constats.Add("plafondDeRattrapage = " + plafondDeRattrapage
                             + " : une image doit pouvoir jouer au moins un tick, sinon le serpent n'avance jamais. Repli sur "
                             + Cadence.PlafondDeRattrapageParDefaut + ".");
                sain.plafondDeRattrapage = Cadence.PlafondDeRattrapageParDefaut;
            }
            else
            {
                sain.plafondDeRattrapage = plafondDeRattrapage;
            }

            try
            {
                Grille essai = new Grille(largeurGrille, hauteurGrille);

                // ⚠ Deuxième garde, et elle n'est PAS redondante : le GDD §4.3 ne donne que des
                // bornes basses (largeur ≥ 5, hauteur ≥ 3, imposées par la pose de départ). La borne
                // haute, elle, vient du cadre : une grille de 1001 colonnes est parfaitement valide
                // pour `Grille` et ne tient dans aucun écran — sa case ferait moins d'un pixel.
                // Sans cet essai, le jeu lèverait au premier lancement, dans le rendu, sur un
                // réglage que la logique venait d'accepter.
                Plateau.TailleDeCase(essai);

                sain.largeurGrille = essai.Largeur;
                sain.hauteurGrille = essai.Hauteur;
            }
            catch (ArgumentOutOfRangeException erreur)
            {
                constats.Add("Grille " + largeurGrille + " × " + hauteurGrille + " refusée (" + erreur.Message
                             + ") — repli sur " + Grille.LargeurParDefaut + " × " + Grille.HauteurParDefaut + ".");
                sain.largeurGrille = Grille.LargeurParDefaut;
                sain.hauteurGrille = Grille.HauteurParDefaut;
            }

            if (profondeurFile < 1)
            {
                constats.Add("profondeurFile = " + profondeurFile
                             + " : la file doit retenir au moins une entrée. Repli sur "
                             + FileEntrees.ProfondeurParDefaut + ".");
                sain.profondeurFile = FileEntrees.ProfondeurParDefaut;
            }
            else
            {
                sain.profondeurFile = profondeurFile;
            }

            ReglagesJeu defauts = new ReglagesJeu();

            sain.dureeAffichageRefusSecondes = ValiderDouble(
                dureeAffichageRefusSecondes, defauts.dureeAffichageRefusSecondes, "dureeAffichageRefusSecondes", constats);

            sain.plafondProlongationRefusSecondes = ValiderDouble(
                plafondProlongationRefusSecondes, defauts.plafondProlongationRefusSecondes, "plafondProlongationRefusSecondes", constats);

            sain.dureeTextePauseSecondes = ValiderDouble(
                dureeTextePauseSecondes, defauts.dureeTextePauseSecondes, "dureeTextePauseSecondes", constats);

            // ⚠ Strictement positif : un fondu nul rendrait le plafond de prolongation inopérant.
            // L'extinction qu'il impose n'aurait aucune durée, donc rien à voir pour le joueur, et
            // le pictogramme resterait allumé en permanence sous martelage (ART §5.7).
            sain.dureeFonduRefusSecondes = ValiderDouble(
                dureeFonduRefusSecondes, defauts.dureeFonduRefusSecondes, "dureeFonduRefusSecondes", constats);

            // Un plafond plus court que la durée d'affichage éteindrait le retour avant qu'il ait
            // été lu — l'inverse exact de ce que l'ART §5.5 en attend.
            if (sain.plafondProlongationRefusSecondes < sain.dureeAffichageRefusSecondes)
            {
                constats.Add("plafondProlongationRefusSecondes (" + sain.plafondProlongationRefusSecondes
                             + ") est plus court que dureeAffichageRefusSecondes (" + sain.dureeAffichageRefusSecondes
                             + ") : le retour s'éteindrait avant d'avoir été lu. Aligné sur la durée d'affichage.");
                sain.plafondProlongationRefusSecondes = sain.dureeAffichageRefusSecondes;
            }

            // Deux fondus doivent tenir dans la durée d'affichage, faute de quoi le pictogramme
            // n'atteint jamais sa pleine opacité : le joueur voit un scintillement, pas un signe.
            double fonduMaximal = sain.dureeAffichageRefusSecondes / 2.0;
            if (sain.dureeFonduRefusSecondes > fonduMaximal)
            {
                constats.Add("dureeFonduRefusSecondes (" + sain.dureeFonduRefusSecondes
                             + ") ne tient pas deux fois dans dureeAffichageRefusSecondes ("
                             + sain.dureeAffichageRefusSecondes + ") : le pictogramme n'atteindrait jamais sa pleine opacité. Ramené à "
                             + fonduMaximal + ".");
                sain.dureeFonduRefusSecondes = fonduMaximal;
            }

            anomalies = constats;
            return sain;
        }

        /// <summary>Une durée doit être un nombre fini strictement positif, sinon on retombe au défaut.</summary>
        private static double ValiderDouble(double valeur, double defaut, string nom, ICollection<string> constats)
        {
            if (double.IsNaN(valeur) || double.IsInfinity(valeur) || valeur <= 0.0)
            {
                constats.Add(nom + " = " + valeur + " n'est pas un nombre fini strictement positif. Repli sur " + defaut + ".");
                return defaut;
            }

            return valeur;
        }
    }
}
