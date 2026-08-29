# Page itch.io — Snake Snack

**Le texte de la page vit ici**, et c'est d'ici qu'on le colle sur itch. Si la page publiée est dans
une autre langue, garder les deux fichiers (`ITCH_STORE_PAGE_EN.md`) et **les corriger ensemble** :
sinon l'un des deux ment, et on ne sait plus lequel.

⚠ **Ce texte doit décrire le jeu TEL QU'IL EST.** Une page qui décrit une fonctionnalité retirée
deux versions plus tôt est le défaut le plus courant et le plus coûteux : le visiteur constate
l'écart et referme. Relire à chaque release qui change quelque chose de visible.

---

## Titre

Snake Snack

## Tagline (une ligne, sous le titre)

Le classique jeu de Snake

## Description

**Dirigez un serpent qui s'allonge à chaque bouchée, jusqu'à ce que son propre corps ne laisse plus
de passage.**

Pas de twist, pas de bonus, pas d'ennemi : ce qui vous arrête, c'est votre propre queue. Les bords
de la grille tuent — ils ne téléportent pas — donc chaque mort reste imputable à un virage, et il y
a toujours une phrase en tête en relançant : « j'aurais dû passer par la droite ».

Une partie se relance en **une touche et zéro attente**.

### Commandes

| Action | Clavier | Tactile |
|---|---|---|
| Tourner (4 directions) | Flèches **ou** ZQSD | — pas en 0.1 |
| Pause / reprise | Échap | — pas en 0.1 |
| Relancer après la mort | Espace | — pas en 0.1 |
| Revenir au menu | Échap (écran de fin) **ou** Retour arrière (pause) | — pas en 0.1 |
| Naviguer le menu | Flèches ou ZQSD, Entrée ou Espace | souris : survol et clic |

⚠ **Le jeu se joue au clavier** : il n'y a aucune commande tactile, une souris ne suffit que pour le
menu.

### Ce qu'il y a dedans

- Une grille close de 21 × 15 cases, une pomme à la fois, un point par pomme.
- Un record qui survit à la fermeture du jeu, et qui se bat strictement.
- Un menu principal : jouer, comment jouer, crédits.
- Une pause, et un refus de demi-tour qui **se voit** plutôt que d'avaler l'appui en silence.
- Un serpent qui **glisse** d'une case à l'autre, des formes arrondies, et une mort qui montre la
  case fautive avant d'afficher l'écran de fin.

**Ce qui n'y est pas encore** : aucun son ni aucune musique. C'est une base jouable, pas une version
finie.

### Crédits

- **Police Nunito** — Vernon Adams, Cyreal, Jacques Le Bailly.
  `Copyright 2014 The Nunito Project Authors (https://github.com/googlefonts/nunito)`
  Sous [SIL Open Font License 1.1](https://scripts.sil.org/OFL) ; instances statiques extraites pour
  ce projet. Le texte intégral de la licence est embarqué dans le jeu (écran « Crédits »).
- Tout le reste — code, illustrations, interface — est produit pour ce projet.
- Fait avec Unity 6000.5.6f1 (URP 2D), à partir du gabarit
  [unity-game-template-with-claude](https://github.com/drangoht/unity-game-template-with-claude).

---

## Réglages du tableau de bord — ⚠ ils ne sont dans AUCUN fichier du dépôt

À vérifier à la main après chaque publication ; ils ont été faux pendant plusieurs versions sur un
projet précédent.

- [x] **Kind of project** = HTML (sinon le build web se télécharge au lieu de se jouer) — posé le 2026-08-28
- [x] Fichier coché **« This file will be played in the browser »** — posé le 2026-08-28
- [x] **Mobile friendly** — décide seule de ce qu'itch propose à un visiteur sur téléphone.
      **Décoché** : le jeu n'a aucune commande tactile, l'annoncer jouable au doigt serait faux.
- [ ] **Orientation** déclarée — sans objet tant que « mobile friendly » est décoché
- [ ] Onglet **Classification** : genre, tags, **décompte de joueurs**, mode multijoueur
- [x] **Cover 630 × 500** — la seule image que voient les visiteurs qui n'ouvrent pas la page.
      Produite par `tools/generer_cover_itch.py`, **inchangée en 0.2.0** : l'illustration du menu
      n'a pas bougé.
- [x] **Captures d'écran** — trois, prises DANS le navigateur sur la page itch elle-même.
      ⚠ Celles de la partie montrent le rendu du jeu : **à régénérer à chaque changement visible**,
      sinon la page annonce un serpent que la partie ne montre plus.

⚠ **Visibilité** : la page est en **Draft** depuis sa création (2026-08-28). Rien n'est public tant
que l'auteur n'a pas cliqué « Publish » lui-même.
