# Carte — le code moteur (`Gameplay/`, `UI/`)

## §Gameplay — `Assets/Scripts/Gameplay/`

| Fichier | Responsabilité |
|---|---|
| `JeuSnake.cs` | **Le seul MonoBehaviour qui décide** : lit le clavier, fait tiquer la cadence, enchaîne les états. Ne porte aucune règle — tout est délégué à `Rules/` |
| `EtatPartie.cs` | Les cinq états : `EnAttente`, `EnCours`, `EnPause`, `Mort`, `Victoire` (grille pleine, §4.4) |
| `VuePlateau.cs` | Dessine aire, traits, bordure, serpent (pool réutilisé), **pomme** (losange, forme distincte des carrés du serpent) et chevron de refus. `Montrer(bool)` éteint le tout d'un bloc quand le menu prend l'écran |
| `FormesPrimitives.cs` | Le carré blanc 1×1 px dont tout le rendu est fait — aucun asset importé |
| `RecordPersistant.cs` | Le record entre deux sessions (`PlayerPrefs`, clé `snakesnack.record`). ⚠ **Meilleur effort** : lecture impossible → zéro, écriture impossible → journal, jamais d'erreur bloquante. `Save()` explicite, sinon l'onglet fermé perd le record |

Le seul objet posé dans la scène est `Jeu` (voir `SceneBuilder.BuildJeu`) ; `VuePlateau`, `HudJeu` et
`EcranMenu` sont ajoutés au démarrage par `JeuSnake`, pour qu'aucune référence sérialisée ne puisse
se perdre à la régénération de la scène.

⚠ **Ordre de tri des canevas** — HUD 100, menu 200, tampon de build 1000. Deux canevas au même ordre
s'empilent selon la hiérarchie, qui n'est pas stable quand la scène est régénérée par code.

## §UI — `Assets/Scripts/UI/`

| Fichier | Responsabilité |
|---|---|
| `HudJeu.cs` | Construit et pilote les textes : état, commandes, **score et record permanents** (bandeau du haut, §4.5), écrans de pause et de mort avec leur récapitulatif, ligne « touche ignorée ». `Montrer(bool)` masque tout le canevas |
| `EcranMenu.cs` | Le **menu principal** (GDD §4.6) : titre, illustration, entrées animées, curseur, souris. Ne décide rien — la navigation vient de `Rules/MenuPrincipal.cs`. Lève `Validee` (Jouer, Quitter) après son fondu de sortie |
| `PanneauInfo.cs` | Les panneaux « Comment jouer » et « Crédits » : voile, carte encadrée d'ambre, fondu. Classe ordinaire, animée par `EcranMenu` |
| `FabriqueUi.cs` | Les briques communes : canevas (avec son **ordre de tri explicite**), texte, rectangle, voile. Un seul endroit où se règlent débordement et raycast |
| `PolicesUi.cs` | Charge les deux graisses et **journalise** l'absence — une police nulle ne dessine aucun pixel, sans erreur |
| `ZoneCliquable.cs` | Survol et clic de souris (`Image` transparente : les `Text` du jeu ne sont pas des cibles de raycast) |
| `Assets/Resources/Illustrations/` | L'illustration du menu, **produite** par `tools/generer_illustration_serpent.py`, importée en Sprite par `Assets/Editor/ImportIllustrations.cs` |
| `TextesUi.cs` | **Tous les libellés**, en un seul endroit. Aucun texte en dur ailleurs |
| `UiPalette.cs` | Les **12 rôles de couleur** de `docs/ART.md` §1. ⚠ Le seul endroit du dépôt où une couleur est écrite en dur |
| `Assets/Resources/Polices/` | Les deux graisses Nunito (SemiBold, ExtraBold) + `OFL.txt`. ⚠ **Produites** par `tools/generer_polices.py`, chargées PAR CHEMIN (`Resources.Load`) — le HUD n'a aucune référence sérialisée |
| `BuildStampLabel.cs` | Tampon de version, sur son propre canevas |

Pause et mort restent un voile et deux lignes de texte sur l'écran de jeu ; le **menu**, lui, est un
écran à part entière, qui masque le plateau et le HUD (`Montrer(false)`) plutôt que de se poser
par-dessus.
