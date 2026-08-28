---
name: rediger-le-gdd
description: Construire docs/GDD.md de Snake Snack pas à pas, par entretien avec l'auteur du jeu — une section à la fois, dans l'ordre, avec les commandes à lancer entre chaque et un exemple complet déroulé sur un Snake. À invoquer au démarrage d'un projet quand le GDD est encore le squelette à trous, et chaque fois qu'on s'apprête à implémenter un système dont la section du GDD est restée vide.
---

# Rédiger le GDD — Snake Snack

`docs/GDD.md` est la **source de vérité du design** : le code dit *comment*, le GDD dit **pourquoi**.
Ce skill dit comment le remplir sans le transformer en roman d'intentions que personne ne relit.

> **Un GDD ne se rédige pas d'un bloc avant de commencer.** Écrit en entier le premier jour, il
> décrit un jeu qui n'existe pas encore et devient faux dès le premier prototype. Trois sections se
> rédigent **avant la première ligne de code** ; les quatre autres s'écrivent au moment où la
> décision est prise, et pas après.

## Ce qui se rédige quand

| Quand | Sections | Pourquoi à ce moment |
|---|---|---|
| **Avant de coder** | §1 pitch · §2 boucle · §3 commandes | Sans le verbe, la boucle et les touches, il n'y a rien à implémenter |
| **À chaque système construit** | §4 systèmes · §5 progression | Une valeur se justifie au moment où on la choisit, jamais de mémoire |
| **En continu** | §6 mesuré · §7 écarté | §7 est la section qui évite de rouvrir dix fois le même débat |

## L'ordre des commandes

```
1.  /rediger-le-gdd              ← ici : entretien, §1 à §3 remplies et commitées
2.  demande au game-designer de détailler <le premier système>     → §4
3.  demande au developpeur d'implémenter <ce système> + ses tests
4.  /verifier-en-jeu             ← ce que le prototype dément revient en §4 et §7
5.  /carte-projet                ← mettre la carte à jour, même commit
    (répéter 2→5 par système ; §5 dès qu'il y a un deuxième cran de difficulté)
6.  /publier-itch                ← 0.1.0, puis §6 se remplit avec de vraies parties
```

⚠ L'étape 4 n'est pas facultative. **Le GDD écrit avant le prototype est une hypothèse**, et une
partie de ce qui s'y trouve à ce stade est fausse — ce qui est normal, à condition de revenir
l'amender. Un GDD que le jeu qui tourne n'a jamais corrigé n'a jamais servi.

## Comment mener l'entretien

Pour Claude, quand ce skill est invoqué :

1. **Lire `docs/GDD.md` d'abord.** Ne jamais réécrire ce qui est déjà rempli ; repérer la première
   section encore à l'état de commentaire `<!-- -->` et partir de là.
2. **Une question à la fois**, via `AskUserQuestion`, avec **deux ou trois réponses déjà rédigées**
   plus l'option libre. Une page blanche ne produit pas de design ; un choix entre trois formulations
   concrètes, oui. C'est là qu'est le travail : proposer, pas interroger.
3. **Écrire dans `docs/GDD.md` dès qu'une section est validée**, et committer. Une section validée
   qui reste dans la conversation est perdue à la fin de la session.
4. **Ne jamais inventer un chiffre.** Une valeur non essayée s'écrit `<!-- à mesurer -->` ou avec sa
   provenance (« repris de *Blobby Volley* », « au jugé, à confirmer »). Un nombre écrit sans source
   sera cité six mois plus tard comme s'il avait été mesuré.
5. **Reformuler ce que l'auteur dit, en plus court.** Le GDD est relu par des agents : ce qui n'y
   tient pas en cinq lignes n'y sera pas lu.

---

# Les sept étapes, déroulées sur un Snake

L'exemple suivant construit le GDD complet d'un petit jeu de serpent. Il n'est pas là pour être
copié : il montre **le niveau de précision attendu** à chaque section.

## Étape 1 — Le pitch (§1)

**Une phrase qui dit ce que le joueur *fait*.** Si le verbe principal n'y est pas, le pitch n'est pas
encore trouvé — et si le pitch n'est pas trouvé, rien de ce qui suit ne peut l'être.

Question à poser : *« En une phrase, qu'est-ce que le joueur fait, et qu'est-ce qui l'en empêche ? »*

| ✗ Pas un pitch | ✓ Un pitch |
|---|---|
| « Un jeu de serpent rétro en pixel art. » | « On dirige un serpent qui s'allonge à chaque bouchée, jusqu'à ce que son propre corps ne laisse plus de passage. » |
| Décrit l'univers et le style ; aucun verbe de joueur | Verbe : diriger. Obstacle : soi-même |

**Le test** : la phrase contient-elle ce qui **s'oppose** au joueur ? « On mange des pommes » n'est
pas un jeu. « Chaque pomme mangée réduit l'espace où l'on peut encore tourner » en est un.

## Étape 2 — La boucle de jeu (§2)

Le cycle que le joueur répète, du lancement à la fin de partie, **en cinq lignes**. Une boucle qui
n'y tient pas est une boucle qu'on n'a pas encore comprise.

```
apparition au centre, trois segments  →  orienter la tête, le serpent avance tout seul
   →  atteindre la pomme : +1 segment, +1 point, une nouvelle pomme apparaît ailleurs
   →  l'espace libre se réduit à chaque bouchée
   →  la tête touche le corps (ou un mur) : mort, score affiché
   →  relance immédiate : « j'aurais dû passer par la droite »
```

⚠ **La dernière flèche est la plus importante, et c'est celle qu'on oublie.** « Ce qui donne envie de
relancer » n'est pas un bouton *Rejouer* : ici c'est le fait que la mort soit toujours attribuable à
un virage précis, jamais à un aléa. Si cette ligne ne se remplit pas, le problème est dans le jeu,
pas dans le document.

## Étape 3 — Les commandes (§3)

La table du squelette se remplit **entièrement**, y compris les colonnes qui resteront vides : une
case qui assume son vide (« — pas de manette en 0.1 ») vaut mieux qu'une colonne effacée dont
personne ne sait si elle a été décidée ou oubliée.

| Action | Clavier | Manette | Tactile |
|---|---|---|---|
| Tourner (4 directions) | Flèches ou **ZQSD** | D-pad | Balayage dans la direction |
| Pause | Échap | Start | Bouton en haut à droite |
| Relancer après la mort | Espace | A | Toucher n'importe où |

⚠ **AZERTY.** `Key` et `KeyCode` désignent une **position sur un clavier QWERTY**. Les touches Z, Q,
S, D d'un clavier français se déclarent donc `Key.W`, `Key.A`, `Key.S`, `Key.D` — écrire `Key.Z` pour
la touche marquée Z vise en réalité le W. Aucune erreur n'est levée : le jeu répond simplement à la
mauvaise touche.

⚠ **Invisible se lit inexistant.** Le demi-tour instantané est interdit (le serpent se mangerait à la
nuque) — donc le refus doit **se voir**, sinon le joueur conclut que le jeu a raté son appui. Toute
règle qui annule une entrée du joueur doit s'annoncer à l'écran.

## Étape 4 — Les systèmes (§4)

Un titre de niveau 3 par système. Pour chacun : **ce qu'il fait, ses valeurs, et ce qui les
justifie**. Les valeurs chiffrées vivent dans `Assets/Scripts/Rules/` — le GDD porte le *pourquoi*,
le code porte le nombre, et les deux se citent.

C'est ici qu'on délègue, un système à la fois :

```
demande au game-designer de spécifier le déplacement du serpent
demande au developpeur d'implémenter Rules/Cadence.cs et ses tests
/verifier-en-jeu
```

Exemple rédigé :

### Le pas de temps

Le serpent avance d'**une case par tick**, 8 ticks/seconde (`Rules/Cadence.cs`). L'entrée n'oriente
pas la tête immédiatement : elle **met en file** la direction, appliquée au tick suivant.

*Pourquoi une file plutôt qu'une orientation directe* : à 8 ticks/s, deux virages tapés en moins de
125 ms se recouvraient, et le second effaçait le premier — le joueur voyait le serpent ignorer un
virage qu'il avait bien tapé. La file en retient **deux au maximum** ; au-delà, on ne joue plus, on
tape en avance.

### La pomme

Apparaît sur une case tirée uniformément **parmi les cases libres**, et non « au hasard sur la
grille, puis on retire tant que c'est occupé » : sur une grille presque pleine, la seconde méthode
fige le jeu pendant un temps indéterminé sans lever la moindre erreur.

## Étape 5 — Progression et difficulté (§5)

**Un cran de difficulté ajoute une règle nommée, pas un multiplicateur.** Le joueur doit pouvoir la
lire avant de lancer, et comprendre après coup pourquoi il a perdu.

| ✗ Ce qu'on écrit spontanément | ✓ Ce qui tient |
|---|---|
| « La vitesse augmente de 8 % par pomme » | **Murs** : au-delà de 10 pommes, les bords cessent de téléporter et tuent |
| Ni lisible, ni nommable, ni anticipable | Une phrase, lue avant de lancer, qui change la façon de jouer |

Vérifier aussi qu'une contrainte **ne distribue pas son antidote** : « la grille rétrécit, mais une
pomme dorée la rouvre » ne durcit rien — elle déplace le jeu vers la course à la pomme dorée.

## Étape 6 — Ce qui a été mesuré (§6)

Renvoyer vers `docs/TEST_REPORT.md` pour la donnée brute ; **consigner ici la conclusion**.

> **Tick à 8/s plutôt qu'à 10/s.** 20 parties appariées sur graines fixées, même joueur. Le score
> médian ne bouge pas — le joueur s'adapte — mais **17 morts sur 20 à 10/s surviennent dans les
> 300 ms qui suivent un virage**, contre 6 sur 20 à 8/s. Ce n'est pas la difficulté qui montait,
> c'est la fenêtre d'entrée qui devenait plus courte que le temps de réaction. Retenu : 8/s.

⚠ **Une partie isolée ne tranche rien** : la variance entre deux parties peut atteindre un facteur
2,4 avant même que le réglage testé n'agisse. Un verdict se prend au banc apparié, sur le test des
signes — l'effet va-t-il dans le même sens sur chaque paire ? — pas sur le delta médian.

## Étape 7 — Ce qui a été écarté, et pourquoi (§7)

La section la plus utile du document, et la seule que personne ne pense à écrire.

> **Bonus temporaires (ralenti, traverse-mur, aimant).** Écartés. Essayés en 0.2 : ils déplacent la
> décision « par où passer » vers « atteindre le bonus », et la mort cesse d'être imputable à un
> virage — ce que le pilier de la §2 interdit.
>
> **Deuxième serpent en local.** Reporté, pas écarté : demande un partage de clavier et une
> condition de fin de partie qui n'existent pas. À rouvrir après la 1.0.

⚠ **Quand une conclusion est réfutée, la garder et la marquer comme telle** plutôt que la réécrire.
Le raisonnement qui a mené à l'erreur a autant de valeur que la correction : c'est lui qui évite de
refaire deux fois le même détour.

---

## Les cinq défauts qu'on voit revenir

1. **Le GDD écrit en entier avant le prototype.** Il décrit un jeu qui n'existe pas ; personne ne le
   corrige ensuite, et il finit par mentir avec autorité. Trois sections, puis du code.
2. **Un pitch sans verbe.** « Un roguelite spatial atmosphérique » ne permet d'implémenter *rien*.
3. **Des chiffres sans provenance.** Indiscernables d'une mesure au bout de trois mois.
4. **La section « écarté » laissée vide.** Le même débat se rouvre à chaque session, avec les mêmes
   arguments et la même conclusion.
5. **Un GDD à jour du design, mais pas de ce qui l'a démenti.** Le document n'enregistre plus que les
   succès : c'est devenu une plaquette, pas un outil de travail.

## Après

- `/carte-projet` — où vit ce que le GDD décrit, à mettre à jour dans le même commit.
- `/verifier-en-jeu` — la seule chose qui puisse donner tort au GDD.
- `docs/pitfalls/<domaine>.md` — à lire avant de coder le système qu'on vient de spécifier
  (index : `docs/PITFALLS_UNITY.md`).
