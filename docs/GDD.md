# GDD — Snake Snack

> **Comment remplir ce document** : skill **`/rediger-le-gdd`** — il mène l'entretien section par
> section, dans l'ordre où les décisions se prennent, et déroule l'exemple complet d'un petit jeu.

**Source de vérité du design.** Toute décision de gameplay y est reportée *immédiatement*, avec ce
qui la justifie. Le code dit *comment* ; ce document dit **pourquoi**.

> Quand une conclusion est réfutée, **la garder et la marquer comme telle** plutôt que la réécrire :
> le raisonnement qui a mené à l'erreur a autant de valeur que la correction, et c'est ce qui évite
> de refaire deux fois le même détour.

## 1. Pitch

Le classique jeu de Snake

Une phrase qui dit **ce que le joueur fait**, pas ce qu'est l'univers. Si le verbe principal n'y est
pas, le pitch n'est pas encore trouvé.

## 2. La boucle de jeu

<!--
Décrire le cycle que le joueur répète, du lancement à la fin d'une partie. Une boucle qui ne tient
pas en cinq lignes est une boucle qu'on n'a pas encore comprise.

    entrée dans la partie  →  ...  →  ...  →  fin  →  ce qui donne envie de relancer
-->

## 3. Commandes

| Action | Clavier | Manette | Tactile |
|---|---|---|---|
| | | | |

⚠ **Une capacité doit annoncer sa touche dans le jeu** (HUD, description, écran d'acquisition).
Invisible se lit inexistant.

⚠ Clavier **AZERTY** : `Key.A` tombe sous la touche marquée Q. Proscrire `A`, `Q`, `Z`, `W`, `M`
pour les raccourcis globaux.

## 4. Systèmes

<!-- Un titre de niveau 3 par système. Pour chacun : ce qu'il fait, ses valeurs, et la mesure ou
     l'observation qui les justifie. Les valeurs chiffrées vivent dans Assets/Scripts/Rules/. -->

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

<!-- La liste la plus utile du document : elle évite de rouvrir dix fois le même débat. -->
