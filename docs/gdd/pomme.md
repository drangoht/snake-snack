# 4.4 — La pomme

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

Le score du §4.5 est **compté depuis le 2026-08-28** : l'étape 6 incrémente avant de tirer la
nouvelle pomme, et avant de constater la grille pleine — la pomme qui remplit la grille a été
mangée, l'écran de victoire doit afficher le score qui l'inclut.
