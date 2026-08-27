using System;
using System.Collections.Generic;

namespace SnakeSnack.Rules
{
    /// <summary>Sort d'un appui directionnel présenté à la file (GDD §4.2 et §3).</summary>
    /// <remarks>
    /// ⚠ Le refus <b>doit être observable par l'appelant</b> : « invisible se lit inexistant »
    /// (§3). Un appui ignoré sans retour à l'écran est lu comme un appui <i>raté par le jeu</i> là
    /// où le jeu a appliqué une règle. D'où une énumération qui distingue les motifs, et non un
    /// <c>bool</c> : l'UI peut choisir un retour différent par motif si le directeur artistique le
    /// décide.
    /// </remarks>
    public enum ResultatEmpilage
    {
        /// <summary>L'appui est entré dans la file. Il sera validé au tick, pas maintenant.</summary>
        Acceptee,

        /// <summary>
        /// Même direction que la dernière déjà en file (ou que la direction courante si la file est
        /// vide) : elle ne changerait rien et consommerait une place (§4.2).
        /// </summary>
        RefuseeDoublon,

        /// <summary>
        /// File pleine : la nouvelle touche est ignorée, la plus ancienne n'est <b>pas</b> écrasée
        /// (§4.2). Écraser annulerait en silence un virage déjà parti des doigts du joueur.
        /// </summary>
        RefuseeFilePleine,

        /// <summary>Direction tapée pendant la pause : jamais empilée (§3, §4.2).</summary>
        RefuseeJeuEnPause
    }

    /// <summary>Ce qu'un tick a décidé (GDD §4.2).</summary>
    public readonly struct ResultatTick
    {
        public ResultatTick(Direction directionAppliquee, bool entreeConsommee, bool demiTourRefuse, Direction directionRefusee)
        {
            DirectionAppliquee = directionAppliquee;
            EntreeConsommee = entreeConsommee;
            DemiTourRefuse = demiTourRefuse;
            DirectionRefusee = directionRefusee;
        }

        /// <summary>Direction que le serpent suit à ce tick. File vide ou entrée refusée : direction reconduite.</summary>
        public Direction DirectionAppliquee { get; }

        /// <summary>Vrai si une entrée a été dépilée — qu'elle ait été appliquée ou refusée.</summary>
        public bool EntreeConsommee { get; }

        /// <summary>Vrai si l'entrée dépilée était un demi-tour : elle a été jetée, et ça doit se voir (§3).</summary>
        public bool DemiTourRefuse { get; }

        /// <summary>Direction refusée. N'a de sens que si <see cref="DemiTourRefuse"/> est vrai.</summary>
        public Direction DirectionRefusee { get; }
    }

    /// <summary>
    /// La file d'entrées du GDD §4.2 : FIFO de profondeur 2, une entrée dépilée par tick, demi-tour
    /// validé <b>au tick contre la direction effectivement appliquée au tick précédent</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ Contrairement aux autres fichiers de <c>Rules/</c>, cette classe n'est pas statique : la
    /// file <b>est</b> un état. Elle reste sans aucune dépendance moteur — c'est le seul critère qui
    /// compte ici.
    ///
    /// <para><b>Le contre-exemple qui impose la validation au tick</b> (§4.2) : serpent vers l'est,
    /// le joueur tape Nord puis Sud dans le même tick. Ni l'un ni l'autre n'est un demi-tour de
    /// <i>l'est</i> ; validés à l'appui, ils passeraient tous les deux et le tick suivant
    /// appliquerait Sud sur un serpent parti au nord — il se mange la nuque. Validé au tick, Sud est
    /// comparé au Nord <i>réellement appliqué</i>, reconnu comme demi-tour, refusé.</para>
    ///
    /// <para>C'est pourquoi <see cref="Empiler"/> ne teste <b>jamais</b> le demi-tour : le faire
    /// serait exactement la régression que ce contre-exemple décrit.</para>
    /// </remarks>
    public sealed class FileEntrees
    {
        /// <summary>
        /// Profondeur par défaut : 2 (§4.2, raisonné, à confirmer en jeu).
        /// </summary>
        /// <remarks>
        /// À 1, une chicane tapée en moins d'un tick perd sa seconde moitié : le joueur qui joue
        /// <i>plus vite</i> que la cadence est puni (écarté, §7). À 3, le serpent exécute une
        /// trajectoire décidée 375 ms plus tôt dans une grille qui a changé, et la mort cesse d'être
        /// imputable au dernier virage lu à l'écran (§2). 2 couvre le virage en L d'un seul geste,
        /// soit 250 ms à 8 ticks/s. ⚠ <b>Profondeur et cadence sont liées</b> : revoir l'une si
        /// <see cref="Cadence.TicksParSecondeParDefaut"/> bouge.
        /// </remarks>
        public const int ProfondeurParDefaut = 2;

        private readonly Queue<Direction> _file = new Queue<Direction>();
        private readonly int _profondeur;
        private Direction _directionCourante;
        private bool _enPause;

        /// <param name="directionInitiale">
        /// Orientation de la pose de départ (§4.3 : est). Le serpent est immobile mais
        /// <b>orienté</b> : la règle de demi-tour s'applique donc dès le premier tick.
        /// </param>
        /// <param name="profondeur">
        /// Profondeur de la file. Paramétrable pour rester réglable <b>sans recompiler</b> et pour
        /// que les tests puissent éprouver le débordement à d'autres profondeurs.
        /// </param>
        public FileEntrees(Direction directionInitiale, int profondeur = ProfondeurParDefaut)
        {
            if (profondeur < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(profondeur), profondeur, "La file doit pouvoir retenir au moins une entrée.");
            }

            _profondeur = profondeur;
            _directionCourante = directionInitiale;
        }

        /// <summary>Direction effectivement appliquée au dernier tick — la seule référence du demi-tour (§4.2).</summary>
        public Direction DirectionCourante
        {
            get { return _directionCourante; }
        }

        /// <summary>Nombre d'entrées en attente.</summary>
        public int NombreEnAttente
        {
            get { return _file.Count; }
        }

        /// <summary>Profondeur effective de la file.</summary>
        public int Profondeur
        {
            get { return _profondeur; }
        }

        /// <summary>Vrai si le jeu est en pause : aucune direction n'est alors empilée (§3).</summary>
        public bool EnPause
        {
            get { return _enPause; }
        }

        /// <summary>
        /// Présente un appui directionnel à la file. Le demi-tour n'est pas validé ici : il l'est au
        /// tick (§4.2).
        /// </summary>
        /// <returns>Le motif exact, pour que l'appelant puisse le montrer à l'écran (§3).</returns>
        public ResultatEmpilage Empiler(Direction direction)
        {
            // Ordre des tests choisi pour le retour à l'écran : la pause explique tout le reste, et
            // un doublon reste un doublon même quand la file est pleine — annoncer « file pleine »
            // dans ce cas donnerait au joueur une raison fausse.
            if (_enPause)
            {
                return ResultatEmpilage.RefuseeJeuEnPause;
            }

            if (direction == DerniereDirectionConnue())
            {
                return ResultatEmpilage.RefuseeDoublon;
            }

            if (_file.Count >= _profondeur)
            {
                return ResultatEmpilage.RefuseeFilePleine;
            }

            _file.Enqueue(direction);
            return ResultatEmpilage.Acceptee;
        }

        /// <summary>
        /// Fait avancer d'un tick : dépile <b>une</b> entrée, la valide contre la direction
        /// appliquée au tick précédent, l'applique. File vide, la direction courante est reconduite.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Le jeu ne tique pas en pause. Un no-op silencieux ferait avancer le serpent d'une case
        /// pendant une pause sans que rien ne le signale — c'est la classe de bug que ce dépôt
        /// traque, donc on lève.
        /// </exception>
        public ResultatTick Tick()
        {
            if (_enPause)
            {
                throw new InvalidOperationException(
                    "Le jeu ne tique pas pendant la pause : appeler Reprendre() avant de tiquer.");
            }

            if (_file.Count == 0)
            {
                return new ResultatTick(_directionCourante, false, false, _directionCourante);
            }

            Direction demandee = _file.Dequeue();

            if (Directions.EstDemiTour(_directionCourante, demandee))
            {
                // L'entrée refusée est jetée — elle ne bloque pas la file — et le tick reconduit la
                // direction courante (§4.2).
                return new ResultatTick(_directionCourante, true, true, demandee);
            }

            _directionCourante = demandee;
            return new ResultatTick(_directionCourante, true, false, _directionCourante);
        }

        /// <summary>
        /// Entrée en pause : la file est vidée (§4.2).
        /// </summary>
        /// <remarks>
        /// Reprendre doit rendre l'état <b>visible à l'écran</b>, pas exécuter un virage tapé avant
        /// la pause : le joueur a regardé la grille figée et rejoue à partir de ce qu'il voit.
        /// </remarks>
        public void EntrerEnPause()
        {
            _enPause = true;
            _file.Clear();
        }

        /// <summary>Sortie de pause. La file reste vide : elle a été purgée à l'entrée en pause.</summary>
        public void Reprendre()
        {
            _enPause = false;
        }

        /// <summary>
        /// Mort du serpent : la file est vidée (§4.2), pour qu'aucun virage tapé pendant l'agonie ne
        /// soit appliqué à la partie suivante.
        /// </summary>
        public void Mourir()
        {
            _file.Clear();
        }

        /// <summary>
        /// Nouvelle partie : file vide, pause levée, direction remise à l'orientation de départ.
        /// </summary>
        public void Reinitialiser(Direction directionInitiale)
        {
            _file.Clear();
            _enPause = false;
            _directionCourante = directionInitiale;
        }

        /// <summary>
        /// Dernière direction « connue » pour le test de doublon : la dernière en file, ou la
        /// direction courante si la file est vide (§4.2).
        /// </summary>
        private Direction DerniereDirectionConnue()
        {
            if (_file.Count == 0)
            {
                return _directionCourante;
            }

            Direction derniere = _directionCourante;
            foreach (Direction direction in _file)
            {
                derniere = direction;
            }

            return derniere;
        }
    }
}
