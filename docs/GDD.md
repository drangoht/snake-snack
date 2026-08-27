# GDD — Snake Snack

> **Comment remplir ce document** : skill **`/rediger-le-gdd`** — il mène l'entretien section par
> section, dans l'ordre où les décisions se prennent, et déroule l'exemple complet d'un petit jeu.

**Source de vérité du design.** Toute décision de gameplay y est reportée *immédiatement*, avec ce
qui la justifie. Le code dit *comment* ; ce document dit **pourquoi**.

> Quand une conclusion est réfutée, **la garder et la marquer comme telle** plutôt que la réécrire :
> le raisonnement qui a mené à l'erreur a autant de valeur que la correction, et c'est ce qui évite
> de refaire deux fois le même détour.

## 1. Pitch

**On dirige un serpent qui s'allonge à chaque bouchée, jusqu'à ce que son propre corps ne laisse
plus de passage.**

Verbe du joueur : *diriger*. Ce qui s'y oppose : *sa propre queue* — pas un ennemi, pas un aléa.
Snake canonique, sans twist : le jeu est déjà entier, l'enjeu est la qualité de la sensation, pas
l'ajout de mécaniques.

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
le jeu. Le retour choisi reste à décider avec le directeur artistique — <!-- forme à définir -->.

⚠ **Une capacité doit annoncer sa touche dans le jeu** (HUD, description, écran d'acquisition).

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

> **Bords téléportants (le serpent ressort par le côté opposé).** Écarté pour la 0.1, décidé au
> design, **pas encore contredit par une partie** : une grille close se lit entièrement d'un coup
> d'œil, alors qu'un bord traversant demande de simuler mentalement une continuité invisible. Surtout,
> il rend certaines morts non imputables (« il est ressorti où ? »), ce que le pilier de la §2
> interdit. À rouvrir si les premières parties montrent une mortalité précoce contre les murs.

> **Snacks à effets distincts, bonus temporaires.** Écartés au pitch (§1). Ils déplacent la décision
> « par où passer » vers « atteindre le bon objet », et la mort cesse d'être attribuable à un virage.
> Le jeu retenu est le Snake canonique : l'enjeu est la sensation, pas l'ajout de mécaniques.

> **Manette et tactile.** *Reportés, pas écartés* — voir §3. Chaque périphérique est un chemin de
> plus à rejouer à chaque build, pour un jeu web joué au clavier. À rouvrir sur retour de joueurs
> mobiles.

⚠ Quand une de ces conclusions est réfutée par une partie réelle, **la garder et la marquer comme
telle** plutôt que la réécrire.
