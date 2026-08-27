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

### 4.4 La pomme

**Une seule pomme sur la grille, à tout instant.** Elle est posée à la mise en place de la partie,
donc **avant le premier appui** : le départ est à l'arrêt (§4.1), le joueur regarde l'écran et
choisit sa première direction — s'il n'y avait rien à viser, ce choix serait aveugle. Elle est
remplacée **dans le tick même** où elle est mangée : aucune image ne doit s'afficher sans pomme, une
grille vide une fraction de seconde se lit comme un bug, pas comme une transition.

**Manger n'est jamais obligatoire** — ni faim, ni minuteur, ni pomme qui expire. C'est ce qui rend
*toute* position de pomme légitime : elle ne peut ni bloquer, ni blesser, ni forcer un trajet. Un
joueur qui juge le chemin trop risqué tourne en rond et ne perd rien d'autre que du temps. La mort
reste donc imputable au virage qui l'a engagé (§2), quelle que soit la case tirée.

**Tirage : énumération, pas rejet.** Le nombre de cases libres vaut `Grille.NombreDeCases −
longueur` (le serpent occupe exactement `longueur` cases distinctes, sinon il serait mort). On tire
`k` uniformément dans `[0, nbLibres)`, puis on **parcourt la grille dans un ordre fixe** (X croissant
dans Y croissant) en sautant les cases du corps, et on s'arrête à la `k`-ième libre. Un seul
parcours, **au plus 315 cases**, aucune allocation, coût borné et identique quel que soit le remplissage.

⚠ **« Tirer au hasard, retirer tant que c'est occupé » est le piège de ce système.** Sur une grille
presque pleine, l'espérance du nombre de tirages tend vers l'infini et le jeu **se fige sans lever la
moindre erreur** — pas d'exception, pas de log, juste une image qui ne revient pas. Le défaut
n'apparaît qu'en fin de partie longue, c'est-à-dire jamais pendant les tests. Écarté, voir §7.

**Aucune contrainte de placement** : pas de distance minimale à la tête, pas d'interdiction dans le
prolongement immédiat. Contraindre retirerait au joueur des pommes *favorables* (rien de plus) tout
en changeant le nombre de cases éligibles, donc en rendant chaque banc plus difficile à décrire. La
fréquence des pommes « offertes » à une ou deux cases est une question de ressenti, pas de sécurité.
<!-- à mesurer : ces pommes très proches dévaluent-elles le score aux yeux du joueur ? Ressenti. -->

**Résolution d'un tick, dans cet ordre exact** (l'ambiguïté ici produit un bug d'une case, invisible
en lecture et évident à l'écran) :

1. Dépiler et valider la direction (§4.2) → `cible = Directions.Avance(tête, direction)`.
2. `Grille.EstHorsGrille(cible)` → **mort** (mur, §2).
3. `mange = (cible == pomme)`.
4. Collision corps : `cible` comparée aux segments, **queue exclue si `!mange`** → mort si touchée.
5. Insérer `cible` **en tête** ; retirer la queue **seulement si `!mange`**.
6. Si `mange` : score +1, puis tirer la nouvelle pomme **sur l'état final du tick**.

**Le serpent s'allonge par la tête, au tick où la tête entre sur la case de la pomme** — pas au tick
suivant, pas par ajout d'un segment derrière la queue. C'est la **queue qui ne bouge pas** pendant ce
seul tick ; la longueur passe de N à N+1 immédiatement, et vaut toujours `3 + score`. Corollaire de
l'étape 4 : hors croissance, la tête **peut** entrer sur la case que la queue libère au même tick —
la queue s'en va visiblement à l'écran, refuser ce coup tuerait sur un mouvement qui paraît libre.
<!-- L'exclusion de la queue à l'étape 4 est écrite même si l'étape 6 garantit qu'une pomme
     n'apparaît jamais sur une case occupée : la règle ne doit pas dépendre d'une garantie posée
     ailleurs. -->

**Grille pleine = victoire.** Après l'étape 6, si `longueur == Grille.NombreDeCases`, il n'existe
plus de case libre : la partie s'arrête en **victoire**, soit 312 pommes sur la grille par défaut.
Même écran, même place et même relance à une touche que la mort (§2), avec un libellé distinct.
Cet état n'est pas un ornement : sans lui, le tirage part sur `[0, 0)` et casse ou boucle. Il est
hors de portée humaine <!-- à mesurer : score médian réel --> et doit néanmoins être écrit.

**Aléa reproductible.** Le tirage consomme un générateur **explicite, propre à la partie**, semé par
un entier. La graine se règle **sans recompiler**, par le même fichier de tuning que la cadence et la
grille ; absente, elle est dérivée de l'horloge et **journalisée au démarrage** pour rester rejouable.
À graine et suite d'appuis identiques, une partie se rejoue à l'identique — c'est la condition du
banc apparié réclamé en §4.1 et §4.3, pas un confort de développement.

**« Propre à la partie » vaut aussi pour la relance** (précisé à l'implémentation, 2026-08-27). Le
générateur est re-semé à **chaque** nouvelle partie, pas une fois par session :
- **graine fixée** dans le fichier de tuning → toutes les parties rejouent la même suite de pommes.
  C'est le **mode banc**, et non un mode de jeu : sans lui, une partie ne se rejoue qu'une fois.
- **graine absente** (valeur 0, qui sert de sentinelle) → chaque partie reçoit une graine neuve,
  **journalisée elle aussi**. Sans ce point, le joueur qui appuie sur Espace rejouerait indéfiniment
  les mêmes pommes, et « relancer » perdrait ce qui donne envie de relancer (§2).

Ces graines de partie sont tirées par un **second** générateur, semé une fois par l'horloge — pas par
celui des pommes, qu'un tirage supplémentaire décalerait. C'est la première application de la règle
« tout autre besoin d'aléa prend une instance séparée » ci-dessous.

⚠ **La résolution réelle de l'horloge sous Windows est d'environ 15 ms**, pas 100 ns : deux parties
relancées coup sur coup en tireraient la même graine si celle-ci venait directement de l'horloge.
Le second générateur évite ce cas — qui ne serait apparu qu'à l'usage, sous la forme « deux parties
d'affilée ont les mêmes pommes, parfois ».

⚠ **Ni `UnityEngine.Random` ni `System.Random`** : le premier est un état global partagé et
indisponible dans `Rules/` ; la suite du second **n'est pas contractuellement stable** d'un runtime à
l'autre, et un banc dont les pommes changent entre `dotnet test`, le build bureau et le build WebGL
ne compare plus rien. Le générateur est écrit dans `Rules/`, son algorithme est le nôtre.

⚠ **Rien d'autre que la pomme ne tire dans ce générateur.** Un effet visuel qui y puiserait un
nombre décalerait toute la suite et casserait l'appariement, sans qu'aucun test ne tombe. Tout autre
besoin d'aléa (cosmétique, audio) prend une instance séparée.

Règles **écrites** (2026-08-27) : `Assets/Scripts/Rules/Pomme.cs` (tirage par énumération) et
`Assets/Scripts/Rules/Aleatoire.cs` (générateur semé, **SplitMix64**, algorithme écrit dans le
dépôt). La résolution du tick appartient à la règle qui fait déjà avancer le serpent
(`Assets/Scripts/Rules/Serpent.cs`), pas à `Pomme.cs` : la pomme répond « où » et « combien »,
jamais « quand ». Seule l'étape 6 — remplacer la pomme ou constater la grille pleine — vit dans le
câblage moteur (`JeuSnake.JouerUnTick`), parce qu'elle touche à l'état de la partie et au rendu.

⚠ **Le score du §4.5 n'est pas encore compté** : la pomme allonge bien le serpent, mais rien
n'affiche ni ne persiste de nombre. La longueur vaut `3 + score`, donc l'information *existe* à
l'écran — elle n'est simplement pas lisible comme un score.

### 4.5 Le score et le record

**Le score compte les pommes mangées de la partie en cours, +1 par pomme, rien d'autre** : ni temps,
ni bonus de rapidité, ni longueur. La longueur vaut `3 + score` — l'afficher aussi n'ajouterait
qu'un second nombre à lire pour la même information. Un score pondéré est écarté (§7) : il
introduirait une pression de temps invisible et une mort attribuable à « trop lent » plutôt qu'à un
virage (§2).

**Score et record affichés en permanence**, hors de l'aire de jeu (bandeau et marges du §4.3), pas
seulement à la mort. Un objectif qu'on ne découvre qu'une fois perdu ne se vise pas : c'est le record
lu pendant la partie qui transforme la relance en « battre 14 ». Le placement exact et la typographie
sont un brief à ouvrir dans `docs/ART.md`. <!-- à demander au directeur-artistique -->

**Le record est le plus haut score jamais atteint, et il monte pendant la partie**, dès que le score
courant le dépasse — pas à la mort. Le score est monotone croissant : attendre la fin ferait afficher
un record inférieur au score courant, ce qui se lit comme un bug, et perdrait le record d'un onglet
fermé en cours de partie.

⚠ **Quand le record vient d'être battu, score et record sont le même nombre** — deux valeurs égales
côte à côte se lisent comme un défaut d'affichage. L'écran de fin doit le dire explicitement
(mention « nouveau record »), sinon le seul moment gratifiant du jeu passe pour un bug. Brief à
ouvrir dans `docs/ART.md`. <!-- à demander au directeur-artistique -->

**Persistance** : le record survit à la fermeture du jeu, stocké côté moteur sous une clé nommée
(`PlayerPrefs`, adaptateur dans `Gameplay/` — `Rules/` reste sans dépendance moteur). ⚠ En WebGL le
stockage est lié à l'origine du site et **peut disparaître** (navigation privée, purge du
navigateur) : c'est du meilleur effort. Un record illisible ou absent repart de zéro **sans erreur
bloquante** — le jeu ne doit jamais refuser de démarrer pour un compteur.

Règles pressenties : `Assets/Scripts/Rules/Score.cs` — comptage, comparaison au record, prédicat
« record battu ». La lecture/écriture persistante et l'affichage vivent hors de `Rules/`.

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

> **Tirage de la pomme par rejet (« tirer une case au hasard, retirer tant qu'elle est occupée »).**
> Écarté au design, **aucune partie jouée**. Le coût du tirage croît avec le remplissage et n'a
> **aucune borne** : sur une grille presque pleine le jeu se fige, sans exception ni log — un défaut
> qui n'apparaît qu'en toute fin de partie longue, donc jamais pendant les tests. L'énumération
> (§4.4) coûte au plus 315 cases, toujours. À rouvrir **seulement** si un profilage WebGL montre que
> l'énumération pèse, et alors en hybride **borné** (N rejets puis repli sur l'énumération), jamais
> en rejet nu. <!-- à mesurer : coût réel du parcours, en WebGL -->

> **Contraindre l'apparition de la pomme (distance minimale à la tête, interdiction dans le
> prolongement immédiat).** Écarté au design. Une pomme ne peut ni bloquer ni tuer, et manger n'est
> jamais obligatoire (§4.4) : aucune position ne rend une mort non imputable, la contrainte ne
> protégerait donc de rien. Elle retirerait seulement des tirages *favorables* et changerait le
> nombre de cases éligibles, rendant chaque banc plus lourd à décrire. À rouvrir si le
> `game-tester` rapporte que les pommes offertes à une ou deux cases dévaluent le score — c'est du
> ressenti, aucun banc ne le tranche.

> **Plusieurs pommes simultanées.** Écartées au design. Une contrainte se juge à ce qu'elle *donne*
> : deux pommes ne raccourcissent pas seulement le trajet, elles offrent une **cible de secours**
> quand la première devient inatteignable — le joueur y gagne plus qu'il n'y perd. Elles diluent
> aussi la décision « par où passer », qui est le verbe du §1. À rouvrir si le `TEST_REPORT` montre
> que le trajet entre deux pommes est ressenti comme du temps mort.

> **Pomme à durée de vie limitée (elle disparaît et réapparaît ailleurs).** Écartée au design. C'est
> un aléa hostile : le joueur s'engage dans un couloir pour une cible qui s'évapore, et la mort qui
> suit n'est plus imputable à son virage mais à un minuteur qu'il ne contrôle pas (§2). C'est aussi
> un mur de patience déguisé. Non rouverte pour la 0.1.

> **`UnityEngine.Random` ou `System.Random` pour le tirage de la pomme.** Écartés au design. Le
> premier est un état global partagé, indisponible dans `Rules/`. Le second **ne garantit pas** la
> même suite d'un runtime à l'autre : un banc apparié dont les pommes diffèrent entre `dotnet test`,
> le build bureau et le build WebGL ne compare plus rien, et l'écart serait attribué au réglage
> testé. À rouvrir si .NET publie un contrat de stabilité de séquence — pas avant.

> **Score pondéré (bonus de rapidité, points liés au temps ou à la longueur).** Écarté au design.
> Il ajoute une pression de temps que rien n'affiche, et fait basculer l'explication de la défaite
> de « j'aurais dû passer par la droite » (§2) vers « j'ai été trop lent ». La longueur, elle, vaut
> déjà `3 + score` : ce serait le même nombre affiché deux fois. À rouvrir si le score brut se
> révèle ne donner aucune raison de relancer une fois le record posé.

> **Manette et tactile.** *Reportés, pas écartés* — voir §3. Chaque périphérique est un chemin de
> plus à rejouer à chaque build, pour un jeu web joué au clavier. À rouvrir sur retour de joueurs
> mobiles.

⚠ Quand une de ces conclusions est réfutée par une partie réelle, **la garder et la marquer comme
telle** plutôt que la réécrire.
