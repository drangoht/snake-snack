# Runbook de publication — Snake Snack

Version courte : skill **`/publier-itch`**. Bout en bout, devlog compris : agent
**`release-manager`**. Ce document est le détail, à lire quand quelque chose sort de l'ordinaire.

## Ce qui est publié, et où

| Canal itch | Ce qu'il porte | Comment le joueur l'obtient |
|---|---|---|
| `html5` | `Build/Web` | La page sert le build courant — toujours à jour |
| `windows` | `Build/Windows` | Auto-update via l'app itch, ou téléchargement |

⚠ **Le nom du canal décide si le fichier est jouable dans le navigateur.** `html5`, `html` ou `web`
sont reconnus comme tels ; tout autre nom produit une archive à télécharger, qui s'installe
parfaitement et **ne se joue pas**. Rien ne le signale.

## Prérequis, à faire UNE fois

1. **Créer la page** sur `https://Drangoht.itch.io/snake-snack`.
2. **`Kind of project` = HTML** pour un jeu web (tant que le projet est « Downloadable », le build
   web se télécharge au lieu de se jouer), et cocher sur le fichier
   **« This file will be played in the browser »**.
3. **Butler authentifié** : installer l'app itch.io (elle fournit et met à jour `butler.exe` dans son
   dossier `broth`, que le script détecte seul). Si « not authorized », lancer une fois
   `"<butler.exe>" login`.
4. Vérifier les trois réglages qui **ne sont dans aucun fichier du dépôt** : case **Mobile friendly**,
   onglet **Classification** (dont le décompte de joueurs), **orientation** déclarée.

## La procédure

```
1. Choisir le semver           x.y.Z correctif · x.Y.0 contenu · X.0.0 refonte
2. TOUT committer              sinon le tampon porte un « + » et ne désigne aucun commit
3. Fermer l'éditeur Unity      sinon le build en ligne de commande échoue
4. & "tools/release_itch.ps1" -Version X.Y.Z -DryRun
5. & "tools/release_itch.ps1" -Version X.Y.Z
6. Vérifier                    lancer le jeu publié, lire le tampon en bas à droite
7. MAJ doc                     README, CLAUDE.md, docs/ITCH_STORE_PAGE.md si visible
8. Devlog                      docs/DEVLOG.md, puis collé sur itch — COCHER « Published »
```

Depuis la racine, **sans `-ExecutionPolicy Bypass`** : ce flag est refusé par le classifier
automatique et fait échouer l'appel.

## Ce que le script fait, et pourquoi chaque étape existe

1. **Pose `bundleVersion`** dans `ProjectSettings.asset`. C'est elle que lit `Application.version`,
   donc le tampon affiché en jeu. La laisser derrière ferait s'annoncer le build sous un ancien
   numéro.
2. **Construit** (scène régénérée comprise), et exige une **phrase de réussite explicite** dans le
   journal : un code retour nul ne distingue pas « construit » de « rien à faire ».
3. **Vérifie le tampon produit PAR le build.** ⚠ Une release a déjà expédié le binaire de la version
   **précédente** sans qu'aucune erreur ne soit levée. Ni la date (build incrémental : un fichier
   identique n'est pas réécrit) ni les métadonnées Windows (qui décrivent le **moteur**) ne
   permettent de s'en apercevoir. Seule la version embarquée tranche. **Ne pas contourner ce
   contrôle.**
4. **Prépare un dossier de distribution propre**, sans les symboles Burst qu'Unity nomme elle-même
   « DoNotShip ». Butler diffe fichier par fichier : on pousse un dossier, pas une archive.
5. **`butler push`** avec `--userversion`.
6. **Commit + push** du numéro de version (et du manifeste, pour la cible Windows uniquement — un
   joueur web est toujours à jour, et lui pousser un manifeste annoncerait aux joueurs Windows une
   mise à jour qui n'existe pas).

## Diagnostic

| Symptôme | Cause |
|---|---|
| « Unity.exe … n'est pas reconnu » | Chemin d'Unity jamais donné : `& "tools/configurer.ps1" -UnityPath "…"` |
| « another Unity instance is running » | L'éditeur est ouvert. Ne pas le tuer : attendre. |
| Build « échoue » mais aucun log écrit | Unity lancé par `&` au lieu de `Start-Process -Wait` |
| « Le build porte la version X » | Build périmé : `-SkipBuild` sur un dossier non reconstruit |
| Tampon suffixé `+` | L'arbre était modifié au moment du build — committer d'abord |
| Tampon `dev` | git indisponible pendant le build |
| « not authorized » (butler) | `"<butler.exe>" login`, une fois |
| Le build web se télécharge au lieu de se jouer | Canal mal nommé, ou `Kind of project` ≠ HTML |
| « git push echoue » alors que tout va bien | Un `$?` testé après un exe natif. Seul `$LASTEXITCODE` fait foi. |

## Après la release

- Le tableau `butler status` peut afficher l'ancienne version tant que le build est
  « processing » — c'est normal.
- ⚠ `Assets/Scenes/Game.unity` ressort modifiée (tous les `fileID` renumérotés) : l'écarter, **sauf
  si `SceneBuilder.cs` a changé**.
