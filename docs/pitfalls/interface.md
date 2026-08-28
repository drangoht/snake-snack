# Pièges — Interface


**⚠ Le HUD peut recouvrir une modale.** L'ordre de tri des canevas est le seul arbitre : deux
canevas au même `sortingOrder` s'empilent dans l'ordre de la hiérarchie, qui n'est pas stable quand
la scène est régénérée par code. Donner un `sortingOrder` explicite à chaque canevas.

**⚠ Un piège de focus se voit seulement à la manette.** Une liste dont le focus ne peut plus sortir
se navigue parfaitement à la souris. Tester chaque écran **au clavier et à la manette**.

**⚠ Invisible se lit inexistant.** [hérité] Une capacité qui n'annonce pas sa touche n'existe pas
pour le joueur : sur un projet précédent, un dash a été joué une partie entière sans que le testeur
sache qu'une touche existait. Un effet passif sans indicateur est cru inactif. C'est un bug
d'ergonomie, pas un détail de présentation.


**⚠ Un survol de souris sélectionne sans que personne n'ait bougé la souris.** uGUI envoie un
« pointeur entré » quand un élément apparaît SOUS un curseur immobile — ouverture d'un écran, retour
au menu, fenêtre qui reprend le premier plan. Un menu dont le survol déplace la sélection voit donc
sa sélection sauter à l'entrée qui se trouve par hasard sous le curseur, et la touche de validation
suivante lance autre chose que ce que l'écran montrait une seconde plus tôt. Constaté le 2026-08-28 :
le curseur reposait sur « Quitter », et le jeu s'est fermé au premier appui. Parade : n'accepter le
survol qu'**après** un déplacement réel du pointeur (`EcranMenu.SurveillerLePointeur`).

**⚠ Une ligne de texte occupe ~1,36 fois le corps de la police, pas 1,0.** Dimensionner un panneau
en multipliant le nombre de lignes par le `fontSize` donne une boîte d'un tiers trop petite. Avec
`VerticalWrapMode.Overflow` (le défaut), le texte sort du cadre et passe par-dessus ce qui suit — ce
qui se lit comme un défaut de rendu ; avec `Truncate`, les dernières lignes disparaissent en
silence. Les deux se sont produits sur le panneau « Comment jouer » le 2026-08-28. Mesurer sur une
capture, pas au calcul.
