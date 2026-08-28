# Pièges — Tactile et mobile


**⚠ La moitié du portage mobile vit dans `index.html`, pas dans Unity.** [hérité] Le zoom, le
défilement, le geste de retour depuis le bord, l'appui long qui ouvre un menu système, la barre d'URL
qui mange le bas de l'écran (donc les commandes qui s'y trouvent) : Unity ne peut rien contre ce qui
se passe **avant** lui. Aucun de ces défauts ne se voit dans l'éditeur, aucun ne lève d'erreur, et
chacun rend le jeu injouable au doigt. Le gabarit du projet les traite tous — ne pas les défaire.

**⚠ `maxTouchPoints` est le seul test fiable pour détecter un mobile** : la chaîne d'agent
utilisateur ment (mode bureau d'un téléphone, iPad qui se déclare Mac).

**⚠ Utiliser `dvh` et non `vh`** pour la hauteur du canevas : `vh` ignore la barre d'URL qui se
rétracte, et le bas du jeu se retrouve caché derrière elle.

**⚠ Chrome de bureau ne fournit AUCUN `Touchscreen`.** [hérité] `Touchscreen.current` reste `null`
et tout code tactile sort immédiatement. Dispatcher de vrais `TouchEvent` en JS ne sert à rien —
l'événement se propage, mais le moteur n'a pas de périphérique où le ranger, et **aucune erreur** ne
le dit. Seul un mode `?touch` (qui appelle `TouchSimulation.Enable()`) rend le tactile testable.

