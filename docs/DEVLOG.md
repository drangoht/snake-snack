# Devlog — Snake Snack

Source de vérité des notes de version : ce qui est **réellement sorti**, dans l'ordre décroissant.
Chaque entrée est écrite ici avant d'être collée sur itch — rien n'est publié qui ne soit ici.

Écrire **pour le joueur**, pas pour l'historique git : « les blobs ne restent plus collés au filet »
et non « fix(BlobController): clamp horizontal velocity ».

## v0.2.0 — Le serpent glisse, et il a perdu ses angles droits (2026-08-29)

**Nouveautés**
- **Le serpent glisse d'une case à l'autre** au lieu de sauter. Les règles n'ont pas bougé d'un
  millimètre — c'est toujours une case par tick, au même rythme — mais on le voit enfin se déplacer.
- **Fini le papier millimétré** : le corps, la tête et la pomme ont des coins arrondis. Le serpent
  du menu et celui de la partie sont enfin le même animal.
- **Avaler se sent** : la tête gonfle sur la bouchée, le nouvel anneau apparaît d'un coup à la queue,
  et le score fait un bond quand il monte.
- **On voit enfin ce qui vous a tué** : la case fautive s'illumine, le jeu marque un très court temps
  d'arrêt, et l'écran de fin n'arrive qu'après — le temps de comprendre.

**Équilibrage**
- Rien. Ni la vitesse, ni la grille, ni le score, ni le tirage des pommes n'ont changé : une partie
  se joue exactement comme en 0.1.0.

**Corrections**
- Marteler Espace juste avant de mourir ne saute plus l'écran de fin. La partie repartait aussitôt,
  sans qu'on ait vu ni son score ni ce qui l'avait arrêtée.

**Toujours pas là** : le son et la musique.

## v0.1.0 — Première version jouable (2026-08-28)

**Nouveautés**
- Le jeu de Snake au complet : un serpent qui s'allonge à chaque pomme, des murs qui tuent, un score
  et un record qui survit à la fermeture du jeu.
- Un menu principal — jouer, comment jouer, crédits — au clavier comme à la souris.
- Une pause, et un demi-tour refusé qui **se voit** au lieu d'être avalé en silence.
- Jouable directement dans le navigateur.

<!-- Gabarit d'une entrée, à copier en TÊTE de fichier :

## vX.Y.Z — <résumé en quelques mots> (AAAA-MM-JJ)

**Nouveautés**
- …

**Équilibrage**
- …

**Corrections**
- …

-->
