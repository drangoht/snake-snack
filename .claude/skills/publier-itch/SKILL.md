---
name: publier-itch
description: Publier une nouvelle version de Snake Snack sur itch.io (build Unity → Butler push → commit du numéro de version). À invoquer quand l'utilisateur demande de « publier », « release », « pousser sur itch », « sortir une nouvelle version ». Enchaîne le build, le push et le commit via tools/release_itch.ps1.
---

# Publier sur itch.io — Snake Snack

Distribution : **itch.io + Butler** (`Drangoht/snake-snack`). Un `butler push` donne
l'auto-update aux joueurs de l'app itch (patch différentiel wharf) ; les joueurs web sont toujours à
jour, la page servant le build courant. Runbook détaillé : `docs/RELEASE.md`. L'agent
`release-manager` fait la même chose de bout en bout, devlog compris.

## Procédure (dans l'ordre)

### 1. Choisir le numéro de version
Sémantique `MAJEUR.MINEUR.CORRECTIF`. La version courante est `bundleVersion` dans
`ProjectSettings/ProjectSettings.asset` — **ne pas l'éditer à la main**, le script la pose lui-même.
- **correctif** (x.y.**Z**) : bugfix, ajustement mineur ;
- **mineur** (x.**Y**.0) : nouveau contenu, nouvelle mécanique ;
- **majeur** (**X**.0.0) : refonte, rupture.

Si la nature n'est pas évidente, propose le bump et continue sans bloquer.

### 2. Tout committer AVANT de lancer le script
Le tampon de build (`v<version>-<sha>`) désigne le commit publié. Un arbre modifié produit un tampon
suffixé `+`, qui ne correspond à **aucun commit** — le script le signale, il ne l'empêche pas.

⚠ **`Assets/Scenes/Game.unity` ressort modifiée à chaque build** (`SceneBuilder` renumérote tous les
`fileID` : des milliers de lignes de diff pour une scène identique). L'écarter par
`git checkout -- Assets/Scenes/Game.unity`, **sauf si `SceneBuilder.cs` a changé**.

### 3. Essai à blanc, puis publication
Depuis la racine, **sans `-ExecutionPolicy Bypass`** (ce flag est refusé par le classifier auto et
fait échouer l'appel) :
```
& "tools/release_itch.ps1" -Version X.Y.Z -DryRun            # va jusqu'au staging, ne publie rien
& "tools/release_itch.ps1" -Version X.Y.Z                    # canal web (defaut)
& "tools/release_itch.ps1" -Version X.Y.Z -Target windows    # canal telechargeable
```
Timeout large : un build WebGL prend une dizaine de minutes.

Le script enchaîne : `bundleVersion` posée → build Unity (scène régénérée comprise) → vérification
que le build porte **bien** la version demandée → staging propre → `butler push` → commit et push du
numéro de version.

Paramètres utiles : `-SkipBuild` (re-push d'un build qu'on vient de construire soi-même — le script
vérifie tout de même son tampon), `-Channel`, `-Itch user/slug`.

### 4. Vérifier
- Sortie : « Publication OK — version X.Y.Z poussée ». Le tableau `butler status` peut afficher
  l'ancienne version tant que le build est « processing » — c'est normal.
- Ouvrir la page et lancer le jeu : le tampon en bas à droite doit porter la version publiée.

## Prérequis / pièges
- **L'éditeur Unity doit être FERMÉ**, sinon le build en ligne de commande échoue.
- **Butler authentifié** : fourni par l'app itch (dossier `broth`, détecté auto). Si « not
  authorized », lancer une fois `"<butler.exe>" login` (chemin affiché par le script).
- ⚠ **Une release a déjà expédié le binaire de la version PRÉCÉDENTE.** D'où la vérification du
  tampon : le script exige que `build_stamp.json` porte la version demandée. Ne pas la contourner.
- ⚠ **La date du build ne prouve rien** : Unity construit de façon incrémentale, un fichier identique
  n'est pas réécrit. Seule la version embarquée tranche. (Et les métadonnées Windows d'un `.exe`
  Unity décrivent le **moteur**, pas le jeu.)
- ⚠ **Ne jamais tester `$?` après un exe natif en PowerShell 5.1** : `git`, Unity et Butler écrivent
  leur progression sur stderr même quand tout va bien. Seul `$LASTEXITCODE` fait foi.
- ⚠ **Le nom du canal décide si le fichier est jouable dans le navigateur** : `html5` (ou `html`, ou
  `web`) est reconnu comme tel, tout autre nom produit une archive à télécharger — qui s'installe
  parfaitement et ne se joue pas. Rien ne le signale.

## Prérequis côté itch.io, à faire UNE fois
- *Kind of project* = **HTML** (tant que le projet est « Downloadable », le build web se télécharge
  au lieu de se jouer), et le fichier coché **« This file will be played in the browser »**.
- Case **Mobile friendly**, onglet **Classification**, **orientation** déclarée : ces trois réglages
  ne sont dans aucun fichier du dépôt et se vérifient à la main.

## Après la release
MAJ `README.md` / `CLAUDE.md` et le texte de la page store (`docs/ITCH_STORE_PAGE.md`) si la version
change quelque chose de visible. Devlog : rédigé dans `docs/DEVLOG.md`, puis collé sur itch —
⚠ **cocher « Published »**, sans quoi le billet reste en brouillon sans rien dire.
