# Carte — le code moteur (`Gameplay/`, `UI/`)

## §Gameplay — `Assets/Scripts/Gameplay/`

| Fichier | Responsabilité |
|---|---|
| `JeuSnake.cs` | **Le seul MonoBehaviour qui décide** : lit le clavier, fait tiquer la cadence, enchaîne les états. Ne porte aucune règle — tout est délégué à `Rules/` |
| `EtatPartie.cs` | Les cinq états : `EnAttente`, `EnCours`, `EnPause`, `Mort`, `Victoire` (grille pleine, §4.4) |
| `VuePlateau.cs` | Dessine aire, traits, bordure, serpent (pool réutilisé), **pomme** (losange, forme distincte des carrés du serpent) et chevron de refus |
| `FormesPrimitives.cs` | Le carré blanc 1×1 px dont tout le rendu est fait — aucun asset importé |
| `RecordPersistant.cs` | Le record entre deux sessions (`PlayerPrefs`, clé `snakesnack.record`). ⚠ **Meilleur effort** : lecture impossible → zéro, écriture impossible → journal, jamais d'erreur bloquante. `Save()` explicite, sinon l'onglet fermé perd le record |

Le seul objet posé dans la scène est `Jeu` (voir `SceneBuilder.BuildJeu`) ; `VuePlateau` et `HudJeu`
sont ajoutés au démarrage par `JeuSnake`, pour qu'aucune référence sérialisée ne puisse se perdre à
la régénération de la scène.

## §UI — `Assets/Scripts/UI/`

| Fichier | Responsabilité |
|---|---|
| `HudJeu.cs` | Construit et pilote les textes : état, commandes, **score et record permanents** (bandeau du haut, §4.5), écrans de pause et de mort avec leur récapitulatif, ligne « touche ignorée » |
| `TextesUi.cs` | **Tous les libellés**, en un seul endroit. Aucun texte en dur ailleurs |
| `UiPalette.cs` | Les **12 rôles de couleur** de `docs/ART.md` §1. ⚠ Le seul endroit du dépôt où une couleur est écrite en dur |
| `BuildStampLabel.cs` | Tampon de version, sur son propre canevas |

Il n'y a pas encore d'écrans distincts : pause et mort sont un voile et deux lignes de texte sur
l'écran de jeu.
