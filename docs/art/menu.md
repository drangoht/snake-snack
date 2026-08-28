# Brief — Le menu principal et son illustration

Tranché le 2026-08-28. Ce que le menu doit **faire** est dans `docs/gdd/menu.md` ; ce fichier ne
traite que de ce qui se voit.

## 1. Composition

Deux colonnes, dans un cadre de référence 1280×720 :

- **À gauche**, alignés sur un même bord (x = −520 depuis le centre) : le titre `SNAKE SNACK`
  (ExtraBold 64), l'accroche en texte secondaire (SemiBold 21), puis les entrées (ExtraBold 30,
  62 px d'écart). Le bloc d'entrées reste **centré sur lui-même** quel qu'en soit le nombre : le menu
  web, privé de « Quitter », ne se déséquilibre pas.
- **À droite**, l'illustration du serpent (390 px de côté), légèrement au-dessus du centre.
- **En pied**, le rappel des touches du menu, en texte secondaire.

⚠ **Un même bord gauche, pas un centrage.** Des libellés de longueurs différentes centrés ne
commencent pas au même x, et une colonne de menu se lit alors comme un alignement raté.

## 2. Le curseur de sélection

Un **losange rouge** — la pomme du jeu, même forme et même rôle de couleur. Le joueur qui n'a pas
encore lancé de partie apprend d'un coup d'œil ce que la forme rouge veut dire, et le menu ne dépense
pas un symbole de plus. L'entrée sélectionnée passe de `TexteSecondaire` à `TexteHud` **et** grossit
de 7 % : la sélection ne tient donc pas à la seule couleur (§4).

## 3. Les animations

| Ce qui bouge | Comment | Pourquoi |
|---|---|---|
| Ouverture | fondu global 0,42 s, titre et accroche montent de 14 px | l'écran arrive, il n'apparaît pas d'un coup |
| Entrées | cascade, 0,07 s d'écart, chacune glisse de 34 px vers sa place | l'œil est mené du haut vers le bas de la liste, dans l'ordre où il devra la lire |
| Sélection | le curseur **glisse** vers l'entrée visée (lissage exponentiel) | un curseur qui saute ne dit pas d'où il vient |
| Illustration | flottement ±8 px sur 4,2 s, inclinaison ±1,6° sur 5,3 s | deux périodes différentes : le mouvement ne se referme jamais sur lui-même, il ne « boucle » pas à l'œil |
| Sortie | fondu 0,16 s avant que la partie ne s'affiche | la coupure sèche menu → jeu se lit comme un rechargement |

⚠ **Ce n'est pas du clignotement.** Le §4 interdit « le clignotement périodique en boucle sur une
grande surface » : ce qui est visé est une variation d'**opacité**. Ici l'opacité de l'illustration
ne bouge pas — elle se déplace et s'incline. Rien ne scintille, et le menu cesse d'avoir l'air d'une
capture figée.

⚠ **Tout est en temps non mis à l'échelle** : le menu ne dépend pas du temps de jeu.

## 4. L'illustration

**Un serpent enroulé en spirale, fait des mêmes carrés arrondis que le jeu, la tête sortie du dernier
tour et une pomme en losange posée dans l'axe de son regard.**

Produite par `tools/generer_illustration_serpent.py` → `Assets/Resources/Illustrations/serpent-menu.png`
(512×512, fond transparent). Aperçu sur le fond réel du jeu : `docs/verif-illustration-menu.png`
(`--apercu`).

Ce qui fait tenir l'image, et qu'une retouche ne doit pas défaire :

- **Même matière que le jeu.** Le corps est une suite de carrés arrondis espacés, comme les segments
  à l'écran. L'illustration ne promet donc rien que le jeu ne montre.
- **La spirale raconte la boucle de jeu** : le corps s'allonge, s'enroule et finit par n'avoir plus
  de place — c'est le pitch du GDD §1, sans une ligne de texte.
- **La pomme est dans l'axe du regard**, pas simplement à côté : c'est ce qui fait une scène plutôt
  que deux objets voisins.
- **Aucune couleur n'est inventée.** Le générateur **lit** `UiPalette.cs` : corps, tête, pomme,
  fond (les yeux) et blanc (l'éclat) sont les rôles du §1. Un rôle renommé fait échouer le script au
  lieu de produire une image périmée.
- **La queue s'affine et les derniers segments virent vers la couleur de tête** : le regard trouve la
  tête sans avoir à suivre le corps.

⚠ **L'image est recadrée sur son contenu, puis recentrée sur un carré.** Le menu la pose dans un
rectangle fixe : c'est le centre du fichier qui tombe au centre de ce rectangle. Sans recadrage,
retoucher une constante du générateur (un tour de plus, une pomme plus loin) décalerait
l'illustration dans le menu — et on corrigerait dans le mauvais fichier.

⚠ **Import.** `Assets/Editor/ImportIllustrations.cs` force `textureType = Sprite` sur tout
`Resources/Illustrations/`. Sans lui, le projet étant en mode 3D, le PNG est importé en **texture**,
`Resources.Load<Sprite>` rend `null`, et le menu s'affiche sans illustration **sans lever la moindre
erreur** (`docs/pitfalls/assets-import.md`).

## 5. Les panneaux de lecture

Carte 880×480 centrée, fond `AireDeJeu`, cadre `BordureAire` de 3 px — le même ambre que le mur qui
tue, pour que les cadres du jeu forment une famille. Voile de pause par-dessus le menu. Titre
ExtraBold 34 centré, corps SemiBold 19 **aligné à gauche**, rappel de la touche de retour en pied.

⚠ **Une ligne de Nunito occupe environ 1,36 fois le corps**, pas 1,0. Neuf lignes à 19 px demandent
~260 px de haut, pas 190. Le premier jet dimensionnait la carte au calcul naïf : les deux dernières
lignes du panneau — celles qui énoncent ce qui tue — étaient tronquées. Le corps est désormais
**tronqué dans la carte** plutôt que débordant : un texte trop long se voit, au lieu de passer par
dessus le cadre et de ressembler à un défaut de rendu.
