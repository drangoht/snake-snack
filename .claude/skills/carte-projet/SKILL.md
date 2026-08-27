---
name: carte-projet
description: Carte/index de Snake Snack (Unity / C#). À invoquer AVANT toute exploration du code pour localiser un système, un écran, une donnée, un asset ou un outil sans repartir de zéro avec Glob/Grep. Contient aussi les checklists de câblage et les points d'entrée.
---

# Carte du projet — Snake Snack

Cette carte dit **où** se trouve chaque chose. Pour le reste : `docs/GDD.md` (**pourquoi** le jeu est
réglé ainsi) · `docs/PITFALLS_UNITY.md` (**quels pièges** guettent) · `CLAUDE.md` (phase courante et
conventions).

> **Maintenir cette carte à jour** : dès que tu ajoutes, supprimes ou renommes un système, un écran,
> une donnée ou un outil, mets à jour la section concernée **dans le même commit**. Une carte périmée
> est pire qu'absente — elle donne une réponse fausse avec autorité. En cas de doute, vérifie le
> fichier avant de l'affirmer ; ne recopie pas aveuglément.

## Arborescence

```
Assets/
  Editor/                     Scripts d'éditeur — NE PARTENT PAS dans le build
    BuildTools.cs             Builds Windows et web, tampon de build, garde-cache web
    RenderPipelineSetup.cs    Active URP sur tous les niveaux de qualité
    SceneBuilder.cs           ⚠ Construit la scène ENTIÈREMENT par code — la scène est un artefact
  Scripts/
    Core/                     Socle : BuildInfo, réglages, types partagés
    Rules/                    ⚠ Logique PURE testable — aucun `using UnityEngine`. Voir §Rules
    Gameplay/                 MonoBehaviour : entités, contrôles, physique
    UI/                       Écrans, HUD, menus
  Settings/                   Assets URP (pipeline, renderer 2D, réglages globaux)
  Resources/                  Chargé PAR CHEMIN à l'exécution — embarqué EN ENTIER dans le binaire
  Art/                        Sources consommées par GUID (planches d'animation, prefabs)
  Scenes/Game.unity           ⚠ ARTEFACT régénéré par SceneBuilder — ne pas éditer à la main
  WebGLTemplates/SnakeSnack/  Page hôte du build web — ⚠ la moitié du portage mobile vit ici
  StreamingAssets/            Fichiers bruts lus au démarrage (tuning JSON, localisation)
ProjectSettings/              bundleVersion (posée par le script de release), icônes, URP
tests/                        xUnit — compile Assets/Scripts/Rules/ PAR CHEMIN. `dotnet test`
tools/                        Build, publication, serveur web local, pilotage du jeu — voir §Outils
docs/                         GDD, pièges, rapports de test, devlog — voir §Docs
.claude/                      Agents, skills, hooks (versionnés au même titre que le code)
```

## §Rules — `Assets/Scripts/Rules/`

Logique pure, sans dépendance moteur, **testée**. C'est ici que vit toute règle chiffrée du jeu.

<!-- Lister ici chaque classe et ce qu'elle décide. Une ligne par règle. -->

| Fichier | Ce qu'il décide | Tests |
|---|---|---|
| `Direction.cs` | Enum `Direction` (Nord/Est/Sud/Ouest) + `Directions` : opposé, **demi-tour**, pas d'une case. ⚠ Nord = Y croissant | via les 3 autres |
| `Case.cs` | Coordonnée entière de grille. ⚠ Existe **parce que `Vector2Int` vient d'UnityEngine** | `GrilleTests` |
| `Cadence.cs` | Pas de temps (GDD §4.1) : 8 ticks/s par défaut, surchargeable ; fourchette conseillée 6–10 ; `CadenceEffective` **ignore la longueur** (cadence constante, §7) ; découpe temps → ticks avec report du reliquat, **plafonnée à 1 tick par image** (retard jeté, §4.1) | `tests/CadenceTests.cs` |
| `FileEntrees.cs` | File d'entrées (GDD §4.2) : FIFO profondeur 2, une entrée par tick, **demi-tour validé au tick** contre la direction appliquée au tick précédent, débordement ignoré, doublon refusé, purge pause/mort. ⚠ **Seule classe à état** de `Rules/` | `tests/FileEntreesTests.cs` |
| `Grille.cs` | Aire de jeu (GDD §4.3) : 21 × 15 réglables, **dimensions paires refusées** (case centrale exacte), pose de départ (10,7)/(9,7)/(8,7) vers l'est, `EstHorsGrille` = le mur mortel du §2 | `tests/GrilleTests.cs` |
| `EXEMPLE_Regle.cs` | Gabarit — à supprimer quand `Rules/` sera bien peuplé. |  |

⚠ Le refus d'une entrée (`ResultatEmpilage`, `ResultatTick.DemiTourRefuse`) est **rendu à
l'appelant**, jamais avalé : le §3 exige que le refus se voie à l'écran. Le câblage moteur de ce
retour visuel reste **à faire** (`Gameplay/` et `UI/` sont vides au 2026-08-27), tout comme la **pause à la perte de focus** (`Application.focusChanged`) que suppose le plafond de rattrapage de `Cadence`, et le **démarrage à l'arrêt** (§4.1 : le premier tick est déclenché par la première direction *applicable*, ce qui se décide avec `Directions.EstDemiTour` côté moteur, pas dans `FileEntrees`).

⚠ `tests/SnakeSnack.Tests.csproj` compile `..\Assets\Scripts\Rules\*.cs` — glob **non récursif**,
et avec `ImplicitUsings`/`Nullable` activés contrairement à Unity. Voir `docs/PITFALLS_UNITY.md`
§ « Logique pure et tests hors moteur » avant d'ajouter un fichier ici.

## §Gameplay — `Assets/Scripts/Gameplay/`

<!-- Lister les MonoBehaviour et ce dont chacun est responsable. -->

## §UI — `Assets/Scripts/UI/`

<!-- Lister les écrans et leur enchaînement. -->

## §Data

<!-- Où vivent les valeurs réglables : StreamingAssets/data/*.json, ScriptableObjects… -->

## §Outils — `tools/`

| Outil | Ce qu'il fait |
|---|---|
| `build.ps1` | **Construit** (Windows/web), et `-Lancer` enchaîne sur la capture. Seul appelant d'Unity |
| `configurer.ps1` | Dit où sont Unity, Python, dotnet, butler — et ce qui manque |
| `environnement.ps1` | Résout et mémorise ces chemins (`local.settings.json`, hors git). À dot-sourcer |
| `release_itch.ps1` | Publie une version (build → butler push → commit). Skill `/publier-itch` |
| `serve_web.py` | Sert `Build/Web` **sans cache navigateur** — indispensable après un rebuild |
| `piloter_jeu.py` | Lance le build Windows, injecte des touches, capture la fenêtre |

⚠ **Aucun chemin d'outil externe en dur nulle part** : `Unity.exe` n'est pas au même endroit d'une
machine à l'autre. Tout passe par `environnement.ps1`.

## §Docs

| Question | Document |
|---|---|
| Phase courante, conventions | `CLAUDE.md` (chargé automatiquement) |
| *Pourquoi* le jeu est réglé ainsi | `docs/GDD.md` — le remplir : skill `/rediger-le-gdd` |
| Quels pièges guettent | `docs/PITFALLS_UNITY.md` |
| Ce qui a été testé / mesuré | `docs/TEST_REPORT.md` |
| Ce qui est réellement sorti | `docs/DEVLOG.md` |
| Publier | `docs/RELEASE.md` + skill `/publier-itch` |
| Le texte de la page store | `docs/ITCH_STORE_PAGE.md` |

## Checklists de câblage

Un contenu ajouté « à moitié » ne lève aucune erreur : il est simplement inerte. Tenir ici la liste
des points à toucher pour chaque type d'ajout.

**Ajouter un élément de contenu** (exemple à adapter) :
1. La donnée (JSON / ScriptableObject).
2. La règle correspondante dans `Rules/` **et son test**.
3. Le câblage moteur (`Gameplay/`).
4. Le **son** — ⚠ une entrée absente de la table audio est muette, sans erreur.
5. Le **sprite** — ⚠ vérifier `Resources/` vs `Art/`, se tromper n'affiche rien d'anormal.
6. Le **texte** (nom, description) et sa clé de localisation.
7. La mise à jour de cette carte, dans le même commit.
