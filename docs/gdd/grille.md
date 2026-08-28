# 4.3 — La grille

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

⚠ **Constaté en jeu le 2026-08-28 : il n'y a de marge qu'en HAUT et sur les CÔTÉS, pas en bas.**
Les 60 px de bandeau sont pris entièrement en haut, donc l'aire de jeu touche le bord bas du cadre.
Le rappel des commandes, ancré en bas, **chevauche la dernière ligne de cases** et se fait rogner par
le bord de l'écran — les jambages des lettres sont coupés sur toute capture. Le score et le record
du §4.5 n'en souffrent pas (ils sont dans le bandeau du haut), mais la ligne des commandes contredit
la phrase ci-dessus. Trois issues, aucune tranchée : réserver un bandeau bas (la case retombe à
`min(1280/21, 600/15)` = 40 px, la grille rétrécit), déplacer le rappel dans une marge latérale, ou
l'accepter par-dessus l'aire en le remontant de quelques pixels.
<!-- à trancher : c'est un arbitrage de mise en page, pas un bug de code. -->

**Bornes de la grille réglable** (déduites de la pose de départ, non issues d'un choix de design) :
largeur ≥ 5 et hauteur ≥ 3 — trois segments alignés depuis la colonne centrale, plus une ligne
au-dessus et une en dessous pour qu'un virage existe. Les dimensions paires sont **refusées à la
construction** : sans case centrale exacte, la pose de départ du §2 n'a pas de sens.
<!-- à valider : ces minima n'ont jamais été joués, ils empêchent seulement un état incohérent. -->

Règles pressenties : `Assets/Scripts/Rules/Grille.cs` — dimensions, case centrale, pose initiale et
test « case hors grille » (le mur mortel du §2). Dimensions réglables **sans recompiler**.
