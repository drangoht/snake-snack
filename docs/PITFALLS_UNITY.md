# Pièges Unity — index

**Le contenu le plus précieux du dépôt.** Chaque entrée correspond à un défaut réellement rencontré,
qui **n'a produit ni erreur de compilation, ni exception, ni avertissement** — seulement un jeu qui
se comporte mal. C'est la catégorie de bug qu'on met des heures à trouver et trente secondes à
corriger.

> **Ce fichier est un index : n'ouvrir que le domaine concerné.** Il a été découpé parce qu'il
> grossit sans fin — le lire en entier avant chaque tâche coûtait plus cher que la tâche elle-même.
> Ouvrir deux ou trois domaines pertinents, jamais les quatorze.

Les entrées marquées **[hérité]** viennent de projets précédents (Chimera Protocol, Smily Volley) :
elles n'ont pas été revérifiées ici, mais elles ont chacune coûté au moins une régression.

## Où chercher

| Fichier | Ouvrir quand on touche à… | Mots-clés |
|---|---|---|
| [`pitfalls/assets-import.md`](pitfalls/assets-import.md) | ajouter ou régénérer un asset | `.meta`, GUID, `Art/` vs `Resources/`, `AssetDatabase.Refresh` |
| [`pitfalls/rendu-urp.md`](pitfalls/rendu-urp.md) | rendu, caméra, lumière, matériaux | `QualitySettings`, Renderer 2D, `Light2D`, sprite noir |
| [`pitfalls/polices-texte.md`](pitfalls/polices-texte.md) | police, texte affiché, symboles | repli de glyphes, `cmap`, flèches perdues en WebGL, SIL OFL |
| [`pitfalls/entrees.md`](pitfalls/entrees.md) | commandes, clavier, manette | `activeInputHandler`, AZERTY / QWERTY, `InputSystemUIInputModule`, première touche perdue |
| [`pitfalls/interface.md`](pitfalls/interface.md) | HUD, menus, modales, navigation | ordre de tri des canevas, piège de focus, affordance |
| [`pitfalls/boucle-temps.md`](pitfalls/boucle-temps.md) | tick, vitesse, pause, rattrapage | plafond de rattrapage qui étale le retard, perte de focus |
| [`pitfalls/logique-pure-tests.md`](pitfalls/logique-pure-tests.md) | `Assets/Scripts/Rules/`, `dotnet test` | test jamais vu rouge, compilation plus permissive qu'Unity, glob non récursif du csproj |
| [`pitfalls/build.md`](pitfalls/build.md) | `build.ps1`, versionnage, tampon de build | code retour trompeur, éditeur ouvert, scène régénérée |
| [`pitfalls/build-web.md`](pitfalls/build-web.md) | cible WebGL, page de jeu | stripping, cache navigateur, canal `html5`, dossier `Data/` |
| [`pitfalls/tactile-mobile.md`](pitfalls/tactile-mobile.md) | portage tactile, `index.html` | `maxTouchPoints`, `dvh`, `devicePixelRatio`, pas de `Touchscreen` sur bureau |
| [`pitfalls/tests-pilotage.md`](pitfalls/tests-pilotage.md) | tests headless, `piloter_jeu.py`, captures | focus, splash Unity, fenêtre de capture |
| [`pitfalls/powershell.md`](pitfalls/powershell.md) | écrire ou modifier un script `.ps1` | `$?` après un exe natif, `$ErrorActionPreference`, `-DryRun` |
| [`pitfalls/audio.md`](pitfalls/audio.md) | musique, effets sonores, mixage | table de correspondance muette, geste utilisateur requis, prouver que le son sort |
| [`pitfalls/publication-itch.md`](pitfalls/publication-itch.md) | page store, devlog, itch.io | Redactor, Selectize, cache de page, iframe cross-origin |

## Ajouter un piège

Dans le fichier du domaine, **dans le commit qui l'a découvert**. Règle d'admission stricte :

> Une entrée décrit un défaut qui ne lève **aucune** erreur. Ni compilation, ni exception, ni
> avertissement — seulement un comportement faux. Une bonne pratique ordinaire n'y a pas sa place.

Chaque entrée dit **ce qui se passe**, **pourquoi ça ne se voit pas**, et **ce qui marche**. Le
symptôme observé vaut mieux que la règle abstraite.

⚠ **Un fichier de domaine qui dépasse ~150 lignes se scinde** (`build.md` → `build.md` +
`build-versionnage.md`), et cette table le suit. Sans quoi on revient au monolithe qu'on vient de
défaire.
