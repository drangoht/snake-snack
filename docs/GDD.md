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
le jeu. **La forme du retour est tranchée → `docs/ART.md` §5** (arbitrage de l'auteur, 2026-08-27) :
un chevron barré, orienté vers la direction refusée, ancré au bord de la case tête ; une ligne de
texte sur l'écran de pause pour une direction tapée en pause. Le refus est un **état à échéance**,
jamais une animation rejouée : marteler la touche prolonge l'affichage sans le faire clignoter.

⚠ **Le doublon ne reçoit aucun retour** — retaper la direction déjà suivie n'est pas une erreur, et
le serpent qui continue tout droit est déjà la confirmation. Le filtrage est **explicite** dans le
code, pour qu'il ne se lise pas comme un oubli à corriger.

⚠ **Une capacité doit annoncer sa touche dans le jeu** (HUD, description, écran d'acquisition).

## 4. Systèmes

<!-- Un titre de niveau 3 par système. Pour chacun : ce qu'il fait, ses valeurs, et la mesure ou
     l'observation qui les justifie. Les valeurs chiffrées vivent dans Assets/Scripts/Rules/. -->

⚠ **Aucune valeur de cette section n'a encore été mesurée** — `docs/TEST_REPORT.md` ne contient
aucune session au 2026-08-27. Chaque chiffre porte sa provenance entre parenthèses.

### 4.1 Le pas de temps

Le serpent avance d'**une case par tick**, jamais entre deux ticks : sa position est toujours sur la
grille, et le tick est l'unité de tout ce qui se mesurera ensuite.

**Cadence : 8 ticks/s, soit 125 ms par tick** (au jugé, à confirmer en jeu ; fourchette à essayer
6 à 10 ticks/s <!-- à mesurer -->). La fenêtre d'entrée d'un virage vaut exactement un tick. À 125 ms
elle est plus courte qu'un temps de réaction visuel simple (200–250 ms, ordre de grandeur admis,
**non mesuré ici**) : on ne peut donc pas *réagir* à un mur qui arrive, il faut avoir décidé une case
à l'avance. C'est la compétence visée — la file (§4.2) évite qu'une fenêtre si courte perde des appuis.

**Cadence constante sur toute la partie** (arbitrage tranché par l'auteur le 2026-08-27, contre la
canonicité du Nokia). La difficulté monte déjà seule : chaque pomme allonge le corps et réduit
l'espace libre — c'est la règle du §1, lisible avant de lancer. Accélérer en plus serait un
multiplicateur empilé dessus, et brouillerait l'attribution de la mort (§2) : le joueur ne saurait
plus s'il a mal planifié ou si le jeu a dépassé ses doigts. (Alternative écartée : §7.)

**Départ à l'arrêt** (§2) : le premier tick est déclenché par la première direction **applicable**,
pas par le chargement de la scène, et pas par un simple appui. Serpent orienté est, corps derrière
lui : un joueur qui tape Ouest voit le refus (§3) et **rien ne bouge** ; la partie démarre au premier
appui qui n'est pas un demi-tour. La règle du demi-tour s'enseigne ainsi d'elle-même, avant qu'aucun
danger n'existe, et personne ne meurt pendant que le joueur lit l'écran.
<!-- Arbitrage de l'auteur, 2026-08-27 : lève une contradiction entre « direction acceptée » (§4.1,
     l'empilage, qui ne juge pas le demi-tour) et « le refus se voit avant le démarrage » (§4.3). La
     variante « l'appui refusé lance quand même la partie » est écartée : le jeu démarrerait tout
     seul sur une touche qu'il vient de refuser. Ce cas particulier vit dans le câblage moteur, pas
     dans FileEntrees. -->

**Le retard de cadence ne se rattrape pas** (arbitrage de l'auteur, 2026-08-27). Perdre le focus de
la fenêtre met le jeu en pause ; hors de ce cas, une image ne fait avancer le serpent que d'**un
tick au maximum**, le retard accumulé est jeté. Sans ce plafond, une seconde de gel (alt-tab,
chargement) fait parcourir huit cases d'un coup, **invisibles** : la mort qui suit n'est imputable à
aucun virage, ce que le §2 interdit. Le prix assumé est une brève dérive de la cadence après un
hoquet — préférable à des cases parcourues hors de la vue du joueur.

Règles pressenties : `Assets/Scripts/Rules/Cadence.cs`. La durée du tick se règle **sans recompiler**
— c'est la valeur du jeu qui sera ré-essayée le plus souvent.

### 4.2 La file d'entrées

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

### 4.3 La grille

**21 × 15 cases, cellules carrées** (315 cases, au jugé). Dimensions **impaires sur les deux axes** :
il existe une case centrale exacte, condition pour que le serpent apparaisse « au centre » (§2) sans
décalage d'une demi-case.

**Pose de départ** : tête sur la case centrale `(10, 7)` en indices 0, corps en `(9, 7)` et `(8, 7)`,
**longueur 3** (repris du §2), **orientation est**, à l'arrêt. Le serpent est immobile mais
**orienté** : la règle de demi-tour s'applique donc dès le premier appui, et le joueur qui tape Ouest
voit le refus avant même que la partie démarre — la règle s'enseigne d'elle-même, sans didacticiel.

**Ce que 315 cases impliquent** (déduit, non mesuré) : le serpent occupe 1 % de la grille au départ ;
il lui faut ~75 pommes pour en occuper le quart, seuil où l'on *suppose* la navigation étouffante.
<!-- à mesurer : à quel score le joueur cesse-t-il de foncer pour commencer à tracer un chemin ? -->

**Durée d'une partie type** (déduit de la cadence §4.1, non mesuré) : sur une grille W×H, la distance
de Manhattan moyenne entre deux cases tirées au hasard vaut ≈ (W+H)/3, soit 12 cases ici — environ
1,5 s par pomme à 8 ticks/s, détours non comptés. Une partie de 25 pommes tient sous la minute :
c'est la durée que suppose la relance à une touche du §2.

**Lisibilité** (calculé) : dans un cadre web 1280×720 avec un bandeau de HUD d'environ 60 px, la case
fait `min(1280/21, 660/15)` = 44 px. La grille occupe 924 px de large et laisse ~178 px de marge de
chaque côté — de quoi poser score et record **hors de l'aire de jeu**, sans recouvrement de calques.

**Bornes de la grille réglable** (déduites de la pose de départ, non issues d'un choix de design) :
largeur ≥ 5 et hauteur ≥ 3 — trois segments alignés depuis la colonne centrale, plus une ligne
au-dessus et une en dessous pour qu'un virage existe. Les dimensions paires sont **refusées à la
construction** : sans case centrale exacte, la pose de départ du §2 n'a pas de sens.
<!-- à valider : ces minima n'ont jamais été joués, ils empêchent seulement un état incohérent. -->

Règles pressenties : `Assets/Scripts/Rules/Grille.cs` — dimensions, case centrale, pose initiale et
test « case hors grille » (le mur mortel du §2). Dimensions réglables **sans recompiler**.

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

> **Cadence qui accélère avec la longueur (le Snake Nokia).** Écartée pour la 0.1, **décidée au
> design, aucune partie jouée** : c'est un multiplicateur, pas une règle nommée — le joueur ne peut
> pas la lire avant de lancer. Elle s'empile sur une difficulté qui monte déjà seule (§4.1), elle
> brouille l'attribution de la mort (§2 : mal planifié, ou dépassé par la cadence ?), et elle rend le
> tick — l'unité de mesure — variable, donc deux parties incomparables au banc. À rouvrir **une fois
> le banc apparié disponible**, pas avant : c'est précisément le genre de réglage qu'une partie
> isolée ne tranche pas.

> **File d'entrées de profondeur 1 (une seule direction retenue).** Écartée au design. Elle perd la
> seconde moitié de toute chicane tapée en moins d'un tick, c'est-à-dire qu'elle punit le joueur qui
> joue *plus vite* que la cadence, et la perte est invisible (§3). C'est l'origine habituelle du
> « ce Snake rate mes virages ». Voir §4.2.

> **Grille 32 × 18 remplissant le 16:9 sans marges.** Écartée au design : dimensions paires, donc pas
> de case centrale exacte (§4.3) ; 576 cases au lieu de 315, soit une partie type qui double de durée
> pour la même décision répétée ; et plus aucune marge où poser le score sans le superposer à l'aire
> de jeu. À rouvrir si les premières parties se révèlent trop courtes ou trop serrées.

> **Retour de refus : variantes écartées.** Le contour de case épaissi (ne dit pas *quelle*
> direction a été refusée) et le retour unique incluant le doublon (bruit à chaque tick d'un joueur
> qui va tout droit) — détail et raisons dans `docs/ART.md` §6, qui tient l'historique des
> décisions visuelles comme cette section tient celles de design.

> **Manette et tactile.** *Reportés, pas écartés* — voir §3. Chaque périphérique est un chemin de
> plus à rejouer à chaque build, pour un jeu web joué au clavier. À rouvrir sur retour de joueurs
> mobiles.

⚠ Quand une de ces conclusions est réfutée par une partie réelle, **la garder et la marquer comme
telle** plutôt que la réécrire.
