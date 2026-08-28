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

