---
name: carte-projet
description: Carte/index de Snake Snack (Unity / C#). À invoquer AVANT toute exploration du code pour localiser un système, un écran, une donnée, un asset ou un outil sans repartir de zéro avec Glob/Grep. Contient aussi les checklists de câblage et les points d'entrée.
---

# Carte du projet — Snake Snack

Cette carte dit **où** se trouve chaque chose. Pour le reste : `docs/GDD.md` (**pourquoi** le jeu est
réglé ainsi ; §4 renvoie à `docs/gdd/<systeme>.md`) · `docs/PITFALLS_UNITY.md` (index des **pièges**,
ouvrir le domaine) · `CLAUDE.md` (phase courante et
conventions).

> **Maintenir cette carte à jour** : dès que tu ajoutes, supprimes ou renommes un système, un écran,
> une donnée ou un outil, mets à jour la section concernée **dans le même commit**. Une carte périmée
> est pire qu'absente — elle donne une réponse fausse avec autorité. En cas de doute, vérifie le
> fichier avant de l'affirmer ; ne recopie pas aveuglément.

> ⚠ **Plafond : ~150 lignes.** Cette carte est chargée en entier à chaque invocation, et elle grossit
> avec le projet. Elle dit **où**, en une ligne par entrée. Le *comment* appartient au code, le
> *pourquoi* au GDD, et un défaut qui ne lève aucune erreur appartient à `docs/pitfalls/`. Au-delà
> du plafond, sortir une section dans un fichier voisin plutôt que d'allonger.

## Arborescence

```
Assets/
  Editor/                     Scripts d'éditeur — NE PARTENT PAS dans le build
    BuildTools.cs             Builds Windows et web, tampon de build, garde-cache web
    RenderPipelineSetup.cs    Active URP sur tous les niveaux de qualité
    SceneBuilder.cs           ⚠ Construit la scène ENTIÈREMENT par code — la scène est un artefact
  Scripts/
    Core/                     Socle : BuildInfo, réglages, types partagés
    Rules/                    ⚠ Logique PURE testable — aucun `using UnityEngine`. Voir `references/rules.md`
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
tools/                        Build, publication, serveur web local, pilotage du jeu — voir `references/data-outils.md`
docs/                         GDD, pièges, rapports de test, devlog — voir `references/data-outils.md`
.claude/                      Agents, skills, hooks (versionnés au même titre que le code)
```

## Où est le détail

Cette page dit **quelle couche** ouvrir ; le détail par fichier vit à côté, et **on n'ouvre que la
couche concernée** — c'est tout l'intérêt du découpage.

| Ce qu'on cherche | Ouvrir |
|---|---|
| Une règle chiffrée, une décision de jeu testée (cadence, grille, score, pomme, aléa) | `.claude/skills/carte-projet/references/rules.md` |
| Un `MonoBehaviour`, la scène, l'affichage, l'entrée clavier, le HUD | `.claude/skills/carte-projet/references/moteur.md` |
| Un JSON de tuning, un script `tools/`, un document de `docs/` | `.claude/skills/carte-projet/references/data-outils.md` |

⚠ Ces trois fichiers se maintiennent comme cette page : une ligne par entrée, dans le commit qui
change ce qu'ils décrivent.


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
