using UnityEngine;

namespace SnakeSnack.UI
{
    /// <summary>
    /// Les deux graisses du jeu (<c>docs/ART.md</c> §2), chargées depuis <c>Resources/Polices/</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Une police absente ne lève rien</b> : <c>Resources.Load</c> rend <c>null</c>, le
    /// <c>Text</c> garde une police nulle et ne dessine aucun pixel — pas de carré blanc, pas
    /// d'exception, un écran vide. D'où l'erreur explicite ET le repli sur la police intégrée : un
    /// texte moche se voit, un texte absent se cherche pendant une heure.
    ///
    /// <para>Les deux <c>.ttf</c> sont <b>produits</b> par <c>tools/generer_polices.py</c> :
    /// google/fonts ne publie Nunito qu'en fichier variable, le script en instancie <c>wght=600</c>
    /// et <c>wght=800</c>. Ils ne se retéléchargent pas à la main.</para>
    ///
    /// <para>Ce chargement vit ici, et pas dans chaque écran, parce que le repli est une décision
    /// unique : deux copies finiraient par diverger, et la deuxième oublierait de journaliser.</para>
    /// </remarks>
    public static class PolicesUi
    {
        /// <summary>Texte secondaire et courant : Nunito SemiBold.</summary>
        public const string Courante = "Nunito-SemiBold";

        /// <summary>Titres et nombres : Nunito ExtraBold. Il n'existe pas de Regular (ART §2.2).</summary>
        public const string Titres = "Nunito-ExtraBold";

        /// <summary>Charge une graisse par son nom de fichier, sans extension.</summary>
        public static Font Charger(string nom)
        {
            Font police = Resources.Load<Font>("Polices/" + nom);
            if (police != null)
            {
                return police;
            }

            Debug.LogError("Police introuvable : Resources/Polices/" + nom
                           + " — relancer « py tools/generer_polices.py ». Repli sur la police intégrée.");
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
