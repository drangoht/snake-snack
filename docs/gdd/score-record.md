# 4.5 — Le score et le record

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

**Égaler son record ne le bat pas** (précisé à l'implémentation, 2026-08-28). Le prédicat compare le
score au record **d'avant la partie**, jamais au record courant — celui-ci vient d'être relevé par le
score lui-même, les comparer donnerait toujours faux. Conséquence assumée : le joueur qui refait
exactement son meilleur score voit bien deux nombres identiques *sans* mention « nouveau record ». Ce
n'est pas le défaut d'affichage que la mention existe pour lever : il n'a rien battu.

**Le record est écrit au tick où il monte**, pas à la mort (précisé à l'implémentation, 2026-08-28).
C'est la conséquence directe de « il monte pendant la partie » : un onglet fermé en cours de partie
doit garder le record atteint. Le comptage signale lui-même les ticks où le record change, ce qui
évite d'écrire le stockage à chaque pomme de chaque partie.

**Placement retenu, provisoire** (2026-08-28) : score **à gauche** du bandeau du haut en texte
principal, record **à droite** en texte secondaire, l'état restant au centre. La hiérarchie se lit
sans lire les étiquettes : c'est le score courant que le joueur suit. À la mort, un **récapitulatif**
s'ajoute entre le titre et « Espace pour rejouer » — les deux nombres sont déjà dans le bandeau, mais
faire remonter le regard tout en haut au moment où l'on décide de rejouer, c'est le perdre. Quand le
record vient d'être battu, ce récapitulatif n'affiche **qu'un seul nombre** (« Nouveau record : 12 »)
plutôt que le même deux fois sous deux étiquettes. ⚠ Ce placement est une décision de développement
faute de brief : le §1 de `docs/ART.md` (palette, typo) est toujours vide, et tout est en gris.
<!-- à demander au directeur-artistique : le brief de ces deux nombres reste ouvert. -->

Règles **écrites** (2026-08-28) : `Assets/Scripts/Rules/Score.cs` — comptage, montée du record,
prédicat « record battu », normalisation d'un record abîmé, et l'égalité `longueur == 3 + score` que
`tests/ScoreTests.cs` vérifie sur le vrai serpent plutôt qu'en commentaire. La lecture/écriture
persistante vit dans `Assets/Scripts/Gameplay/RecordPersistant.cs` (meilleur effort, jamais
bloquant), l'affichage dans `Assets/Scripts/UI/HudJeu.cs`.
