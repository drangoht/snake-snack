# Carte — données, outils et documents

## §Data

| Où | Quoi |
|---|---|
| `Assets/StreamingAssets/reglages.json` | Cadence, plafond de rattrapage, dimensions de grille, profondeur de file, durées du retour de refus, **graine des pommes** (`0` = une neuve à chaque partie ; toute autre valeur = mode banc, mêmes pommes à chaque partie) |
| `Assets/Scripts/Rules/ReglagesJeu.cs` | Le schéma correspondant + `Valider()`, qui **corrige jamais en silence** |
| `Assets/Scripts/Core/ChargeurReglages.cs` | La lecture côté moteur |

⚠ Le fichier lu à l'exécution est celui **du build** (`Build/Windows/SnakeSnack_Data/StreamingAssets/`),
pas celui d'`Assets/` : c'est ce qui permet de régler la cadence sans reconstruire. Un rebuild écrase
le premier par le second.

⚠ Champs en `camelCase` dans `ReglagesJeu` — `JsonUtility` associe les clés du JSON aux *champs* par
leur nom exact. Les renommer en PascalCase ferait retomber chaque valeur sur son défaut, sans erreur.

⚠ En WebGL, `StreamingAssets` est une URL : le chargeur n'y lit rien et rend les valeurs du GDD.

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
| Quels pièges guettent | `docs/pitfalls/<domaine>.md` (index : `docs/PITFALLS_UNITY.md`) |
| Ce qui a été testé / mesuré | `docs/TEST_REPORT.md` |
| Ce qui est réellement sorti | `docs/DEVLOG.md` |
| Publier | `docs/RELEASE.md` + skill `/publier-itch` |
| Le texte de la page store | `docs/ITCH_STORE_PAGE.md` |
| L'identité visuelle (palette, typo, contraste) | `docs/ART.md` |
| Un brief détaillé, l'historique des décisions visuelles | `docs/art/` |
