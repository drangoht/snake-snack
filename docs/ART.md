# ART — Direction artistique de Snake Snack

> Ce document accueille toute décision de direction artistique du projet. Il démarre avec un seul
> brief rempli (§5 — le retour d'une entrée refusée) parce que c'est le seul trou qui bloquait
> aujourd'hui l'UI. Les autres sections sont posées **vides et structurées**, pour que la suite
> s'y range sans reprendre le plan à chaque fois — ne pas les remplir par anticipation.

## 1. Palette

<!-- À définir. Vit dans Assets/Scripts/UI/UiPalette.cs (ou équivalent) une fois posée — jamais de
     couleur en dur ailleurs dans le code ou les générateurs. Tant que cette section est vide,
     aucun brief de ce document ne doit référencer un code hexa : travailler en contraste de forme
     et en valeurs relatives (clair/sombre), voir §5. -->

## 2. Typographie

<!-- À définir. Rappel du piège déjà payé (docs/pitfalls/polices-texte.md) : le repli d'Unity sur les
     glyphes manquants n'existe QUE sur le bureau — un navigateur WebGL perd en silence tout
     caractère absent de la police (flèches ← → ↑ ↓ en tête de liste). Conséquence pour tout texte
     du jeu : n'écrire que des caractères que la police contient, et dessiner les symboles en
     sprite. Vérifier la table `cmap` avant de faire confiance, et vérifier dans le navigateur, pas
     au raisonnement. -->

## 3. Grille de sprites et échelle

<!-- À définir au fil des briefs (grille de cases, épaisseur de contour, corps de texte). Ce que le
     §5 pose déjà comme contrainte dure, réutilisable pour tout le reste : case de jeu = 44 px,
     aire de jeu 924×660 dans un cadre 1280×720, bandeau HUD ~60 px, marges latérales ~178 px. -->

## 4. Contraste et accessibilité — règles permanentes

- Jamais une information portée par la seule couleur : toujours doublée d'une différence de forme,
  de position ou de texte.
- Jamais de clignotement périodique en boucle sur une grande surface de l'écran. Une variation
  d'opacité déclenchée une fois (fondu entrée/sortie) est admise ; un stroboscope ne l'est pas.
- Tout sprite se valide sur le **fond réel du jeu**, jamais sur un damier neutre.

## 5. Briefs — un sujet, un fichier

> ⚠ **La numérotation `§5.x` est conservée dans le fichier du brief.** Le code et les tests
> renvoient à « ART §5.4 », « ART §5.7 » en une soixantaine d'endroits : ces renvois restent justes,
> ils désignent la sous-section correspondante de `art/retour-refus.md`. Ne pas renuméroter.

<!-- ⚠ INDEX. Un brief détaillé va dans docs/art/<sujet>.md : il n'intéresse que qui travaille sur
     CE sujet, alors que ce fichier-ci est relu avant chaque asset. Une ligne ici. -->

| Brief | Fichier | Statut |
|---|---|---|
| Le retour d'une entrée refusée (GDD §3, §4.2) | [`art/retour-refus.md`](art/retour-refus.md) | tranché, partiellement démenti en jeu le 2026-08-27 |

## 6. Historique des décisions

Décisions visuelles déjà tranchées et variantes écartées : [`art/historique.md`](art/historique.md).
Ne pas rouvrir un sujet sans élément neuf.
