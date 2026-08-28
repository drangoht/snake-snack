# 4.2 — La file d'entrées

Toute touche directionnelle acceptée est **empilée dans une file FIFO de profondeur 2**. À chaque
tick, le jeu dépile **une** entrée, la valide, l'applique ; file vide, il reconduit la direction
courante.

**Validation au tick, contre la direction effectivement appliquée au tick précédent** — jamais au
moment de l'appui, jamais contre la dernière touche tapée. Le contre-exemple qui l'impose : serpent
vers l'est, le joueur tape Nord puis Sud dans le même tick. Ni l'un ni l'autre n'est un demi-tour de
*l'est* ; validés à l'appui, ils passent tous les deux et le tick suivant applique Sud sur un serpent
parti au nord — il se mange la nuque. Validé au tick, Sud est comparé au Nord réellement appliqué,
reconnu comme demi-tour, refusé.

**Refus** : l'entrée refusée est jetée (elle ne bloque pas la file) et le tick reconduit la direction
courante. Le refus **se voit** (§3), sinon le joueur lit « le jeu a raté ma touche » là où le jeu a
appliqué une règle.

**Débordement** : file pleine, la nouvelle touche est **ignorée** — on n'écrase pas la plus ancienne.
Écraser annulerait en silence un virage déjà tapé : le serpent raterait un virage parti des doigts du
joueur. L'appui ignoré porte le **même** retour visible qu'un demi-tour refusé — le chevron barré de
`docs/ART.md` §5. À 125 ms par tick, rien ne permet d'enseigner la nuance entre « demi-tour » et
« troisième virage de trop » : ce qui doit se lire, c'est que l'appui n'a pas compté.
<!-- durées encore au jugé (250 ms d'affichage, 500 ms de prolongation) : seul le game-tester peut
     les trancher au ressenti, aucun banc ne mesure ça. -->

**Pourquoi 2, ni 1 ni 3** (raisonné, à confirmer en jeu). À 1, une chicane (est puis nord, tapée en
moins d'un tick) perd sa seconde moitié : le joueur qui joue *plus vite* que la cadence est puni.
À 3, le serpent exécute une trajectoire décidée 375 ms plus tôt dans une grille qui a changé, et la
mort cesse d'être imputable au dernier virage lu à l'écran (§2). 2 couvre le virage en L d'un seul
geste, soit 250 ms à 8 ticks/s — **profondeur et cadence sont liées** : revoir l'une si l'autre bouge.

**Purges** : la file est vidée à l'entrée en pause et à la mort. Reprendre doit rendre l'état visible
à l'écran, pas exécuter un virage tapé avant la pause. Une direction tapée pendant la pause n'est pas
empilée (§3). Un appui identique à la dernière direction déjà en file (ou à la direction courante si
la file est vide) n'est pas empilé non plus : il ne changerait rien et consommerait une place.

**Un zigzag n'est pas un doublon** (précisé à l'implémentation, 2026-08-27) : direction courante est,
file `[Nord]`, le joueur tape Est — c'est **accepté**. Le test du doublon ne compare qu'à la
*dernière* direction connue, jamais à la direction courante quand la file n'est pas vide : refuser
ici perdrait la seconde moitié d'une chicane Est → Nord → Est réellement voulue.

Règles pressenties : `Assets/Scripts/Rules/FileEntrees.cs` — logique pure, testable sans moteur. Le
contre-exemple Nord/Sud ci-dessus est le premier test à écrire.
