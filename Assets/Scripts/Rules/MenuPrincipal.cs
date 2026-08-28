using System;
using System.Collections.Generic;

namespace SnakeSnack.Rules
{
    /// <summary>Les entrées du menu principal (GDD §4.6), dans leur ordre d'affichage.</summary>
    /// <remarks>
    /// ⚠ L'ordre de cette énumération <b>est</b> l'ordre à l'écran : <see cref="MenuPrincipal.Entrees"/>
    /// n'en donne qu'un sous-ensemble filtré. « Jouer » d'abord parce que c'est ce que fait la quasi
    /// totalité des visiteurs d'une page itch ; « Quitter » en dernier parce qu'une entrée qui ferme
    /// le jeu ne doit jamais se trouver sous le curseur au moment où l'on tape Entrée par réflexe.
    /// </remarks>
    public enum EntreeMenu
    {
        /// <summary>Lance une partie : le menu s'efface, le serpent est posé, la partie attend une direction.</summary>
        Jouer,

        /// <summary>Le panneau des commandes et des deux règles qui tuent (GDD §3).</summary>
        CommentJouer,

        /// <summary>Le panneau des crédits — la SIL OFL de Nunito exige l'attribution (docs/CREDITS.md).</summary>
        Credits,

        /// <summary>
        /// Ferme le jeu. ⚠ <b>Absente du build web</b> : <c>Application.Quit()</c> n'y fait rien, et
        /// un bouton mort coûte plus cher au joueur que l'absence de bouton (voir
        /// <see cref="MenuPrincipal.Entrees"/>).
        /// </summary>
        Quitter
    }

    /// <summary>
    /// La composition et la navigation du menu principal — sans moteur, donc testable (GDD §4.6).
    /// </summary>
    public static class MenuPrincipal
    {
        /// <summary>
        /// Les entrées réellement affichées.
        /// </summary>
        /// <param name="quitterDisponible">
        /// Faux dès que la plateforme ne sait pas fermer l'application : c'est le cas du <b>WebGL</b>,
        /// où <c>Application.Quit()</c> est un appel sans effet. Le décider ici plutôt que dans l'UI
        /// permet de tester les deux compositions sans construire deux builds.
        /// </param>
        public static IReadOnlyList<EntreeMenu> Entrees(bool quitterDisponible)
        {
            var entrees = new List<EntreeMenu>(4) { EntreeMenu.Jouer, EntreeMenu.CommentJouer, EntreeMenu.Credits };

            if (quitterDisponible)
            {
                entrees.Add(EntreeMenu.Quitter);
            }

            return entrees;
        }

        /// <summary>
        /// Index sélectionné après un appui directionnel, avec <b>bouclage</b>.
        /// </summary>
        /// <remarks>
        /// Le bouclage n'est pas un confort : sur trois ou quatre entrées, il met « Quitter » à un
        /// appui de « Jouer », et il évite le moment mort où l'on martèle une flèche contre une
        /// butée silencieuse. Le menu n'a pas de retour visuel de refus (celui du jeu, ART §5, est
        /// réservé aux directions refusées <i>en partie</i>) : une butée y serait donc indiscernable
        /// d'une touche non prise en compte.
        ///
        /// <para>⚠ Est et Ouest ne déplacent rien et le disent (<c>false</c>) : la liste est
        /// verticale. Les accepter ferait bouger le curseur sur une touche que le joueur a tapée
        /// pour tourner, réflexe qu'un jeu de serpent installe précisément.</para>
        /// </remarks>
        /// <returns>Vrai si l'index a changé.</returns>
        public static bool Deplacer(int index, int nombre, Direction direction, out int nouvelIndex)
        {
            if (nombre <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nombre), nombre, "Un menu sans entrée ne se navigue pas : la composition est en cause, pas la touche.");
            }

            int pas;
            switch (direction)
            {
                case Direction.Nord: pas = -1; break;
                case Direction.Sud: pas = 1; break;
                default:
                    nouvelIndex = Borner(index, nombre);
                    return false;
            }

            // Le modulo de C# rend un reste négatif pour un dividende négatif : sans le « + nombre »,
            // remonter depuis la première entrée donnerait -1 et ferait lever l'affichage.
            nouvelIndex = ((Borner(index, nombre) + pas) % nombre + nombre) % nombre;
            return nouvelIndex != index;
        }

        /// <summary>
        /// Ramène un index dans les bornes.
        /// </summary>
        /// <remarks>
        /// ⚠ Utile pour de vrai : la composition change entre le bureau et le web (« Quitter »), et
        /// un index mémorisé sur quatre entrées appliqué à une liste de trois désignerait une entrée
        /// inexistante. Plutôt que de lever, on retombe sur la dernière — un menu qui plante au
        /// démarrage d'un build web est un jeu qui ne démarre pas.
        /// </remarks>
        public static int Borner(int index, int nombre)
        {
            if (nombre <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nombre), nombre, "Un menu sans entrée n'a aucun index valide.");
            }

            if (index < 0)
            {
                return 0;
            }

            return index >= nombre ? nombre - 1 : index;
        }
    }
}
