using System;

namespace SnakeSnack.Rules
{
    /// <summary>
    /// Les courbes du juicy (<c>docs/art/juicy.md</c> §2) : la forme d'un retour dans le temps.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Aucune dépendance moteur, et c'est ce qui rend le juicy vérifiable.</b> Une animation
    /// est ce qu'on remarque le moins quand elle est fausse : un dépassement qui ne revient pas
    /// exactement à 1, une impulsion qui ne retombe pas à 0, une progression qui dépasse 1 sur une
    /// image longue — rien de tout cela ne lève, et rien ne se voit à l'œil sur 150 ms. Ici, chaque
    /// courbe est une fonction pure de <c>(début, durée, maintenant)</c>, donc rejouée en
    /// microsecondes par <c>dotnet test</c> sans build ni moteur.
    ///
    /// <para>⚠ <b>Ces fonctions ne décident de rien.</b> Elles rendent un facteur ; c'est la couche
    /// présentation (<c>VuePlateau</c>, <c>HudJeu</c>) qui choisit ce qu'elle en fait — une échelle,
    /// une opacité, une taille de caméra. Aucune valeur lue par une règle de jeu (collision,
    /// ancrage du chevron) ne passe par ici : le juicy observe l'état, il ne le nourrit jamais
    /// (<c>docs/art/juicy.md</c> §11).</para>
    /// </remarks>
    public static class Rebond
    {
        /// <summary>
        /// Avancement linéaire dans <c>[0, 1]</c> d'une enveloppe démarrée à <paramref name="debut"/>.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Borné aux deux extrémités.</b> Une image longue — la toute première après le
        /// chargement WebGL en avale facilement plusieurs centaines de millisecondes — ferait sinon
        /// sortir le facteur de sa plage et projetterait un segment au-delà de sa case cible. Un
        /// <c>maintenant</c> antérieur au début (horloge relue après une pause) rendrait un négatif.
        /// </remarks>
        public static double Progres(double debut, double duree, double maintenant)
        {
            if (duree <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duree), duree,
                    "Une enveloppe de durée nulle n'a aucune image pour être vue : elle serait invisible, donc inexistante (docs/art/juicy.md §11).");
            }

            double t = (maintenant - debut) / duree;

            if (t <= 0.0)
            {
                return 0.0;
            }

            return t >= 1.0 ? 1.0 : t;
        }

        /// <summary>
        /// Aller-retour : <c>0 → 1 → 0</c>, pic à mi-parcours. Le flash de la case fautive, le bond
        /// du score, le micro-zoom de la mort (<c>juicy.md</c> §5, §6, §8).
        /// </summary>
        /// <remarks>
        /// Sinusoïde plutôt que deux segments de droite : un triangle marque un angle net au pic et
        /// aux extrémités, que l'œil lit comme un à-coup — exactement ce qu'un retour de juicy doit
        /// éviter.
        /// </remarks>
        public static double Impulsion(double t)
        {
            if (t <= 0.0 || t >= 1.0)
            {
                return 0.0;
            }

            return Math.Sin(t * Math.PI);
        }

        /// <summary>
        /// Apparition avec dépassement : <c>0 → au-delà de 1 → 1</c>. Le pop du nouveau segment de
        /// queue et celui de la pomme (<c>juicy.md</c> §5, §7).
        /// </summary>
        /// <param name="t">Avancement dans <c>[0, 1]</c>, tel que rendu par <see cref="Progres"/>.</param>
        /// <param name="depassement">
        /// Hauteur du dépassement, en fraction : <c>0.12</c> pour un pic à 1,12. Zéro donne une
        /// montée simple, sans rebond.
        /// </param>
        /// <remarks>
        /// ⚠ <b>Le retour à exactement 1 en fin d'enveloppe n'est pas négociable</b> : ce facteur
        /// multiplie la taille d'un segment qui, lui, reste posé pour toute la partie. Une erreur de
        /// 1 % laisserait un segment définitivement plus gros que ses voisins — un défaut permanent
        /// né d'une animation de 140 ms, et que personne ne penserait à chercher là.
        /// </remarks>
        public static double Apparition(double t, double depassement)
        {
            if (depassement < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(depassement), depassement,
                    "Un dépassement négatif ferait rétrécir l'objet avant de l'agrandir : ce n'est pas un pop, c'est un défaut.");
            }

            if (t <= 0.0)
            {
                return 0.0;
            }

            if (t >= 1.0)
            {
                return 1.0;
            }

            // Montée en ease-out (le gros du chemin est fait tôt : c'est ce qui donne le « claquement »),
            // puis une bosse qui s'annule aux deux bouts pour le dépassement.
            double montee = 1.0 - Math.Pow(1.0 - t, 3.0);
            return montee + (depassement * Math.Sin(t * Math.PI));
        }

        /// <summary>
        /// Compression puis retour : rend le facteur d'écrasement du « gulp » de la tête
        /// (<c>juicy.md</c> §5), à appliquer sur un axe et son inverse sur l'autre.
        /// </summary>
        /// <param name="t">Avancement dans <c>[0, 1]</c>.</param>
        /// <param name="amplitude">Écart maximal, en fraction : <c>0.15</c> pour 1,15 / 0,85.</param>
        /// <remarks>
        /// ⚠ Le volume est conservé : l'axe étiré vaut <c>1 + a</c> quand l'axe comprimé vaut
        /// <c>1 / (1 + a)</c>, et non <c>1 - a</c>. Deux facteurs symétriques feraient perdre de la
        /// surface à la tête au moment précis où elle doit paraître plus grosse — elle avalerait en
        /// rapetissant.
        /// </remarks>
        public static double Gulp(double t, double amplitude)
        {
            if (amplitude < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amplitude), amplitude,
                    "Une amplitude négative inverserait la compression : la tête s'étirerait dans l'axe de la marche au lieu de gonfler.");
            }

            return 1.0 + (amplitude * Impulsion(t));
        }
    }
}
