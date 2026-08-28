# GDD — Snake Snack

> **Comment remplir ce document** : skill **`/rediger-le-gdd`** — il mène l'entretien section par
> section, dans l'ordre où les décisions se prennent, et déroule l'exemple complet d'un petit jeu.

**Source de vérité du design.** Toute décision de gameplay y est reportée *immédiatement*, avec ce
qui la justifie. Le code dit *comment* ; ce document dit **pourquoi**.

⚠ **Ce fichier reste un sommaire : ~150 lignes, plafond.** Le détail d'un système va dans
`docs/gdd/<systeme>.md` (§4), les décisions écartées dans `docs/gdd/ecarte.md` (§7). Il est relu par
chaque agent avant chaque tâche : ce qu'on y ajoute, on le paie à toutes les tâches suivantes, y
compris celles qui n'ont rien à voir.

> Quand une conclusion est réfutée, **la garder et la marquer comme telle** plutôt que la réécrire :
> le raisonnement qui a mené à l'erreur a autant de valeur que la correction, et c'est ce qui évite
> de refaire deux fois le même détour.

## 1. Pitch

**On dirige un serpent qui s'allonge à chaque bouchée, jusqu'à ce que son propre corps ne laisse
plus de passage.**

Verbe du joueur : *diriger*. Ce qui s'y oppose : *sa propre queue* — pas un ennemi, pas un aléa.
Snake canonique, sans twist : le jeu est déjà entier, l'enjeu est la qualité de la sensation, pas
l'ajout de mécaniques. « Canonique » vaut pour les *mécaniques*, pas pour les *réglages* : la cadence
qui accélère du Snake Nokia n'est pas héritée (§4.1, et §7 pour la raison).

## 2. La boucle de jeu

```
apparition au centre, trois segments à l'arrêt  →  orienter la tête ; le serpent avance seul,
   une case par tick
   →  pomme avalée : +1 segment, +1 point, une nouvelle pomme apparaît ailleurs
   →  l'espace encore libre se réduit à chaque bouchée
   →  la tête touche le corps ou un mur : mort, score et record affichés sur place
   →  Espace : nouvelle partie immédiate, sans menu ni écran intermédiaire
```

**Les bords tuent, ils ne téléportent pas.** Une grille close se lit d'un coup d'œil dès la première
seconde, et toute mort reste imputable à un virage — jamais à un serpent qui a « disparu quelque
part ». (La téléportation a été envisagée puis écartée : voir §7.)

**Ce qui donne envie de relancer** : la relance coûte une touche et zéro attente, et la partie
précédente a laissé une phrase en tête — « j'aurais dû passer par la droite ». C'est cette phrase
qui relance, pas le bouton. Elle n'existe que si la mort est toujours attribuable à une décision du
joueur : d'où l'absence d'aléa hostile dans tout le jeu.

## 3. Commandes

| Action | Clavier | Manette | Tactile |
|---|---|---|---|
| Tourner (4 directions) | Flèches **ou** ZQSD | — pas en 0.1 | — pas en 0.1 |
| Pause / reprise | Échap | — pas en 0.1 | — pas en 0.1 |
| Relancer après la mort | Espace | — pas en 0.1 | — pas en 0.1 |

Manette et tactile sont **décidés vides**, pas oubliés : le jeu se joue au clavier sur la page itch,
et chaque périphérique supplémentaire est un chemin à rejouer à chaque build. À rouvrir si des
retours de joueurs mobiles arrivent (voir §7).

⚠ **Déclaration côté code** : les touches Z, Q, S, D d'un clavier français se déclarent
`Key.W`, `Key.A`, `Key.S`, `Key.D`. Écrire `Key.Z` pour la touche marquée Z vise en réalité le W —
aucune erreur n'est levée, le jeu répond simplement à la mauvaise touche.

⚠ **Deux entrées sont refusées, et le refus doit se voir** :
- le **demi-tour instantané** (le serpent se mangerait à la nuque) ;
- toute direction tapée pendant la pause.

Invisible se lit inexistant : un appui ignoré sans retour à l'écran est lu comme un appui *raté* par
le jeu. **La forme du retour est tranchée → `docs/art/retour-refus.md`** (arbitrage de l'auteur, 2026-08-27) :
un chevron barré, orienté vers la direction refusée, ancré au bord de la case tête ; une ligne de
texte sur l'écran de pause pour une direction tapée en pause. Le refus est un **état à échéance**,
jamais une animation rejouée : marteler la touche prolonge l'affichage sans le faire clignoter.

⚠ **Le doublon ne reçoit aucun retour** — retaper la direction déjà suivie n'est pas une erreur, et
le serpent qui continue tout droit est déjà la confirmation. Le filtrage est **explicite** dans le
code, pour qu'il ne se lise pas comme un oubli à corriger.

⚠ **Une capacité doit annoncer sa touche dans le jeu** (HUD, description, écran d'acquisition).

## 4. Systèmes

<!-- ⚠ CETTE SECTION EST UN INDEX. Un système = un fichier docs/gdd/<systeme>.md, une ligne ici.
     Ce qu'on écrit dans un fichier de système n'est relu que par qui touche à CE système. -->

| Système | Fichier | En une phrase |
|---|---|---|
| **4.1** Le pas de temps | [`gdd/pas-de-temps.md`](gdd/pas-de-temps.md) | Le serpent avance d'une case par tick, jamais entre deux — l'unité de toute mesure ultérieure. |
| **4.2** La file d'entrées | [`gdd/file-entrees.md`](gdd/file-entrees.md) | File FIFO de profondeur 2 : une entrée dépilée, validée et appliquée par tick. |
| **4.3** La grille | [`gdd/grille.md`](gdd/grille.md) | 21 × 15 cases carrées, impaires sur les deux axes pour qu'une case centrale exacte existe. |
| **4.4** La pomme | [`gdd/pomme.md`](gdd/pomme.md) | Une seule pomme à tout instant, posée avant le premier appui pour que le départ ait une cible. |
| **4.5** Le score et le record | [`gdd/score-record.md`](gdd/score-record.md) | +1 par pomme, rien d'autre ; le record survit à la fermeture et se bat strictement. |

## 5. Progression et difficulté

<!--
Règles acquises, à respecter sauf raison neuve :
- Un cran de difficulté ajoute une RÈGLE NOMMÉE, pas un multiplicateur. Le joueur doit pouvoir la
  lire avant de lancer et comprendre pourquoi il a perdu.
- Avant d'ajouter une contrainte, vérifier ce qu'elle DONNE au joueur : une contrainte qui distribue
  aussi son antidote ne durcit rien.
- Un levier optionnel n'est pas une règle : une règle s'applique à toute partie.
- Jamais un mur de patience sur un affrontement clé : plus dangereux vaut mieux que plus long.
-->

## 6. Ce qui a été mesuré

<!--
Renvoyer vers docs/TEST_REPORT.md, et consigner ici la CONCLUSION, pas la donnée brute.

⚠ Une partie isolée ne tranche rien : la variance entre deux parties peut atteindre un facteur 2,4
avant même que le réglage testé n'agisse. Un verdict d'équilibrage se prend au banc apparié, sur le
test des signes.
-->

## 7. Ce qui a été écarté, et pourquoi

<!-- INDEX. Le raisonnement complet de chaque décision est dans docs/gdd/ecarte.md : on ne l'ouvre
     que pour rouvrir un débat, pas à chaque tâche. Une ligne ici par sujet tranché. -->

Détail et raisons : [`gdd/ecarte.md`](gdd/ecarte.md). Sujets déjà tranchés — **ne pas les rouvrir
sans élément neuf** :

- Bords téléportants (le serpent ressort par le côté opposé)
- Snacks à effets distincts, bonus temporaires
- Cadence qui accélère avec la longueur (le Snake Nokia)
- File d'entrées de profondeur 1 (une seule direction retenue)
- Grille 32 × 18 remplissant le 16:9 sans marges
- Retour de refus : variantes écartées
- Tirage de la pomme par rejet (« tirer une case au hasard, retirer tant qu'elle est occupée »)
- Contraindre l'apparition de la pomme (distance minimale à la tête, interdiction dans le prolongement immédiat)
- Plusieurs pommes simultanées
- Pomme à durée de vie limitée (elle disparaît et réapparaît ailleurs)
- `UnityEngine.Random` ou `System.Random` pour le tirage de la pomme
- Score pondéré (bonus de rapidité, points liés au temps ou à la longueur)
- Manette et tactile
