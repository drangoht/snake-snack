# 4.1 — Le pas de temps

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
