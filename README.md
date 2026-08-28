# Snake Snack

Le classique jeu de Snake

Unity 6000.5.6f1 · C# · URP 2D · Input System · publié sur
[itch.io](https://Drangoht.itch.io/snake-snack).

**Où en est le jeu** (2026-08-28) : les mécaniques sont complètes — serpent, pomme, score, record
persistant, pause, mort, refus du demi-tour — et le jeu s'ouvre sur un **menu principal** (jouer,
comment jouer, crédits, quitter), navigable au clavier comme à la souris. Restent au programme avant
une première version publiée : la sensation (retours, « juicy »), les assets graphiques, les effets
sonores et la musique.

> Ce dépôt est né du gabarit
> **[unity-game-template-with-claude](https://github.com/drangoht/unity-game-template-with-claude)**.
> Les fichiers `.claude/`, `docs/PITFALLS_UNITY.md` et `tools/` en viennent : ils portent
> l'expérience de projets précédents. **Les corriger ici quand le projet leur donne tort**, et
> remonter au gabarit ce qui est généralisable.

## Démarrer

```powershell
# Construire, lancer, capturer -- sans jamais ouvrir l'editeur
& "tools/build.ps1" -Lancer
```

C'est tout : le premier build **importe le projet lui-même** (il génère `Library/` et
`ProjectSettings/` — compter ~20 min), il n'y a donc rien à ouvrir dans Unity Hub au préalable.

`build.ps1` trouve Unity seul parmi les installations d'Unity Hub, puis retient son chemin dans
`tools/local.settings.json` (non versionné). S'il ne le trouve pas — Unity installé sur un autre
disque, par exemple — le lui apprendre une fois :

```powershell
& "tools/configurer.ps1" -UnityPath "<dossier-unity>\6000.5.6f1\Editor\Unity.exe"
```

## Structure

```
Assets/
  Editor/            BuildTools · RenderPipelineSetup · SceneBuilder
  Scripts/Core/      BuildInfo et socle
  Scripts/Rules/     ⚠ Logique PURE testable — aucun `using UnityEngine`
  Scripts/Gameplay/  MonoBehaviour
  Scripts/UI/        Écrans, HUD
  Settings/          Assets URP (pipeline, renderer 2D)
  Scenes/Game.unity  ⚠ ARTEFACT régénéré par SceneBuilder
  WebGLTemplates/    Page hôte du build web
tests/               xUnit sur Assets/Scripts/Rules/ — aucun moteur requis
tools/               build.ps1 · configurer.ps1 · environnement.ps1 · release_itch.ps1
                     serve_web.py · piloter_jeu.py
docs/                GDD (+ gdd/) · PITFALLS_UNITY (+ pitfalls/) · RELEASE · DEVLOG · TEST_REPORT
.claude/             9 agents · 3 skills · 2 hooks
```

## Les trois choix structurants

**1. La scène est un artefact, pas une source.** `Assets/Scenes/Game.unity` est régénérée par
`SceneBuilder.Build()` à chaque build. On ajoute un objet au jeu **en écrivant du code**, pas en
cliquant dans l'éditeur. En échange, tout le jeu se pilote en ligne de commande — construire,
lancer, injecter des touches, capturer — donc se vérifie sans main humaine.
⚠ Corollaire : la scène ressort modifiée après chaque build. L'écarter (`git checkout --`) sauf si
`SceneBuilder.cs` a changé.

**2. Toute règle chiffrée vit dans `Assets/Scripts/Rules/`**, en classe statique sans dépendance
moteur, avec ses tests. `dotnet test tests/SnakeSnack.Tests.csproj` s'exécute en quelques
secondes, sans Unity — et un hook les rejoue après chaque édition.

**3. Le binaire porte son identité.** Chaque build écrit `build_stamp.json` et un tampon
`v<version>-<sha>` affiché en bas à droite. Ni la date d'un fichier (build incrémental) ni les
métadonnées Windows (qui décrivent le moteur) ne disent quelle version on regarde. Ce tampon, si.

## Commandes utiles

| But | Commande |
|---|---|
| Build Windows | `& "tools/build.ps1"` |
| Build web | `& "tools/build.ps1" -Target web` |
| Build + lancement + capture | `& "tools/build.ps1" -Lancer -Capture docs/x.png` |
| Tests | `dotnet test tests/SnakeSnack.Tests.csproj` |
| Lancer et capturer | `py tools/piloter_jeu.py --lancer --capture docs/x.png` |
| Servir le build web | `py tools/serve_web.py` (⚠ **sans cache** — voir PITFALLS) |
| Publier | `& "tools/release_itch.ps1" -Version X.Y.Z -DryRun` |
| Régénérer les polices | `py tools/generer_polices.py` |
| Régénérer l'illustration du menu | `py tools/generer_illustration_serpent.py --apercu` |
| Dire où sont Unity et Python | `& "tools/configurer.ps1" -UnityPath "..."` |

⚠ **L'éditeur Unity doit être fermé** pour tout build en ligne de commande — `build.ps1` refuse de
partir sinon.
⚠ N'appeler `Unity.exe` à la main que si l'on sait pourquoi : son chemin change d'une machine à
l'autre, et lancé par `&` en PowerShell il rend la main **immédiatement sans rien faire**
(`Start-Process -Wait`). `build.ps1` traite les deux.

## Travailler avec Claude Code

9 agents et 3 skills sont livrés avec le projet — voir **`GUIDE-EQUIPE.md`**.

Les trois réflexes qui comptent :
- **`/carte-projet`** avant d'explorer le code ;
- **`docs/pitfalls/<domaine>.md`** avant de coder dans ce domaine (index : `docs/PITFALLS_UNITY.md`) ;
- **`/verifier-en-jeu`** avant d'écrire « ça devrait marcher ».

## Publier

`/publier-itch`, ou l'agent `release-manager` pour le faire de bout en bout, devlog compris.
Runbook : `docs/RELEASE.md`.

## Licences

<!-- Polices : SIL OFL (joindre OFL.txt à côté du .ttf). Sons : CC0 / Kenney.
     Musique : vérifier l'usage commercial. Détail dans docs/CREDITS.md. -->
