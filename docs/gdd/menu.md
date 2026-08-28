# 4.6 — Le menu principal

**Le jeu s'ouvre sur un menu, et le menu ne s'interpose jamais entre une mort et la partie
suivante.** Arbitrage de l'auteur, 2026-08-28.

## Ce que le menu doit faire, et ce qu'il ne doit pas défaire

Le §2 dit « Espace : nouvelle partie immédiate, **sans menu ni écran intermédiaire** ». Cette phrase
porte sur la **relance après la mort**, pas sur le premier écran du jeu : elle reste vraie. Ce qui
la protégerait mal, et qui a été **écarté** : un écran de fin transformé en petit menu « Rejouer /
Menu ». Il aurait mis un choix — donc une hésitation — exactement là où le §2 veut zéro attente.

| Situation | Touche | Ce qui se passe |
|---|---|---|
| Lancement du jeu | — | le menu, animé, sélection sur « Jouer » |
| Menu | Flèches ou ZQSD (haut/bas) | déplace la sélection, avec **bouclage** |
| Menu | Entrée ou Espace | valide |
| Menu, panneau ouvert | Échap, Entrée, Espace, ou clic | referme le panneau |
| Écran de mort ou de victoire | Espace | **partie immédiate**, inchangé |
| Écran de mort ou de victoire | Échap | retour au menu |
| Partie en cours | Échap | pause, inchangé — **pas** de retour au menu |

⚠ **Depuis une partie en cours, il n'y a aucun chemin vers le menu** : il faut finir la partie.
C'est une conséquence acceptée, pas un oubli — Échap est déjà la pause (§3), et lui donner un second
sens (appui long, double appui) ferait payer à toutes les parties le confort d'un aller-retour rare.
<!-- à rouvrir si un testeur bute dessus : le candidat serait une entrée « Menu » sur l'écran de
     pause, qui est déjà un écran d'arrêt. -->

## Les entrées

Quatre, dans cet ordre : **Jouer**, **Comment jouer**, **Crédits**, **Quitter**.

- **Jouer** en tête : c'est ce que fait la quasi-totalité des visiteurs d'une page itch, et la
  sélection s'y repose à **chaque** ouverture du menu, y compris au retour d'une partie.
- **Comment jouer** existe parce que le rappel des touches du HUD est tassé en une ligne au bas de
  l'écran : le panneau peut, lui, énoncer les deux règles qui tuent (les bords, le corps) et le refus
  du demi-tour. Un appui ignoré sans explication se lit comme une touche ratée (§3).
- **Crédits** n'est pas une décoration : la SIL OFL 1.1 de Nunito **exige** l'attribution
  (`docs/CREDITS.md`), et un texte de licence qui ne vit que dans le dépôt ne remplit pas
  l'obligation pour un joueur qui ne verra jamais le dépôt.
- **Quitter** est **absent du build web** : `Application.Quit()` n'y fait rien. Un bouton mort coûte
  plus cher qu'une entrée manquante — le joueur clique, rien ne se passe, et il doute du reste.

Le **bouclage** de la navigation (de la dernière entrée à la première) tient à ce que le menu n'a
aucun retour de refus : celui du §3 est réservé aux directions refusées *en partie*. Une butée
silencieuse y serait indiscernable d'une touche non reçue.

⚠ **Les directions latérales ne déplacent rien.** Un joueur de Snake tape les flèches gauche et
droite par réflexe ; les accepter ferait sauter la sélection au moment où il essaie simplement de
tourner.

## La souris

Le §3 décide « manette et tactile : pas en 0.1 ». La **souris** n'est pas dans ce lot : le visiteur
d'une page itch a la main dessus, et un menu qui ignore le clic se lit comme un jeu cassé avant
d'avoir démarré. Le survol **déplace la sélection** (il ne dessine pas un second surlignage
concurrent), le clic valide.

⚠ **Le survol ne prend la main qu'une fois la souris réellement déplacée.** Le menu s'ouvre sous un
curseur immobile — au lancement, au retour d'une partie, quand la fenêtre reprend le premier plan —
et le système d'interface envoie alors un « pointeur entré » pour une souris que personne n'a
touchée. Sans ce verrou, la sélection saute à l'entrée qui se trouve par hasard sous le curseur, et
le joueur qui tape Entrée en croyant lancer une partie **quitte le jeu**. Constaté en pilotant le
build le 2026-08-28.

## Ce qui a été écarté

- **Un écran de fin navigable** (« Rejouer / Menu ») — voir ci-dessus, il contredit le §2.
- **Une entrée « Réglages »** : le tuning vit dans un JSON non éditable en jeu, et le §7 a déjà
  écarté la cadence variable. L'entrée n'aurait rien à régler.
- **Le record affiché sur le menu** : il l'est déjà en permanence pendant la partie (§4.5), et le
  menu n'est pas l'endroit où l'on décide de battre un score — c'est l'écran de mort.

Règles pressenties : `Assets/Scripts/Rules/MenuPrincipal.cs` — composition des entrées et navigation,
testées sans moteur. L'écran lui-même : `Assets/Scripts/UI/EcranMenu.cs`.
