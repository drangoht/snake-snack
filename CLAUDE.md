# CLAUDE.md — Mémoire de projet

Chargé automatiquement au démarrage de chaque session : **rester court et stable**. Le détail vit
dans des fichiers chargés **à la demande** (pointés ci-dessous), pour limiter le contexte consommé
par session.

## Le projet

« Snake Snack » — Le classique jeu de Snake
**Moteur : Unity 6000.5.6f1** (C#, URP 2D, package Input System). Publié sur
`https://Drangoht.itch.io/snake-snack`.

- **Design complet → `docs/GDD.md`** : le consulter avant toute tâche de design ou d'implémentation,
  et le tenir à jour à chaque décision. Pour le **remplir** (il démarre à l'état de squelette) →
  skill **`/rediger-le-gdd`**.
- **Identité visuelle → `docs/ART.md`** : palette, typo, règles d'accessibilité et briefs tranchés.
  Le consulter avant de produire un asset ou une UI ; le `directeur-artistique` l'écrit, le
  `graphiste` l'exécute.
- **Localiser du code** (système, écran, donnée, outil) → invoquer le skill **`/carte-projet`**
  plutôt que Glob/Grep à froid. Le maintenir à jour dans le même commit qu'un changement structurel.
- **Avant de coder** dans un domaine → lire **`docs/PITFALLS_UNITY.md`**. Y ajouter tout nouveau
  piège découvert : c'est ce fichier qui évite qu'un bug se reproduise six mois plus tard.
- **Vérifier qu'un changement fonctionne** → skill **`/verifier-en-jeu`**.
- **Publier** → skill **`/publier-itch`**, ou déléguer à l'agent `release-manager`.

## Phase actuelle

<!-- Une phase = un objectif court, daté, avec ce qui reste à faire. La mettre à jour, pas
     l'accumuler : l'historique appartient à docs/DEVLOG.md. -->

**Phase 0 — mise en place** (démarrée le 2026-08-27)

- [x] Premier build : `& "tools/build.ps1" -Lancer` — il importe le projet (~20 min) et génère
      `Library/` + `ProjectSettings/`. Rien à ouvrir dans Unity Hub.
- [ ] Remplir `docs/GDD.md` — skill **`/rediger-le-gdd`** : pitch, boucle de jeu et commandes
      avant la première ligne de code ; le reste s'écrit système par système.
- [ ] Créer la page itch (`Kind of project` = HTML pour un jeu web) et publier une 0.1.0.

## Équipe d'agents

Agents dans `.claude/agents/` : `game-designer`, `developpeur`, `game-tester`, `release-manager`,
`directeur-artistique`, `graphiste`, `musicien`, `story-teller`, `marketing`. **Déléguer
proactivement** à l'agent compétent — qui fait quoi et dans quel ordre : **`GUIDE-EQUIPE.md`**.

⚠ **Un agent qui décrit un état périmé du projet est pire qu'un agent absent** : il donne des
instructions fausses avec autorité. Quand une phase se termine, relire les agents qu'elle concerne.

## Conventions

- **Build** : `& "tools/build.ps1"` → `Build\Windows\SnakeSnack.exe` ; `-Target web` →
  `Build\Web\` ; `-Lancer` enchaîne lancement et capture. ⚠ L'éditeur Unity doit être **fermé**.
  ⚠ Ne jamais écrire `Unity.exe` en dur : son chemin dépend de la machine, `tools/environnement.ps1`
  le résout et le mémorise (`-UnityPath`, ou `tools/configurer.ps1`, pour le lui apprendre).
- **Publication** : `tools/release_itch.ps1 -Version X.Y.Z` (essayer d'abord avec `-DryRun`). Le
  script pose lui-même `bundleVersion` — **ne pas l'éditer à la main**.
- **La scène est un ARTEFACT** : `Assets/Scenes/Game.unity` est régénérée par `SceneBuilder.Build()`
  à chaque build. On ajoute un objet au jeu **en écrivant du code**, pas en cliquant dans l'éditeur.
  ⚠ Elle ressort donc modifiée après chaque build — l'écarter (`git checkout --`) sauf si
  `SceneBuilder.cs` a changé.
- **Logique pure testable** : `Assets/Scripts/Rules/` — classes statiques **sans dépendance moteur**.
  Les `MonoBehaviour` y délèguent. Tests : `dotnet test tests/SnakeSnack.Tests.csproj` (aucun
  moteur, aucun build requis ; rejoués automatiquement par un hook après chaque édition).
- **Rien d'obsolète** : URP (jamais Built-in), Input System (jamais l'ancien Input Manager). Signaler
  tout avertissement de dépréciation sans attendre qu'on le demande.
- ⚠ **Clavier AZERTY** : `KeyCode` et `Key` désignent une **position sur un clavier QWERTY**.
  `Key.A` tombe sous la touche marquée **Q**. Proscrire `A`, `Q`, `Z`, `W`, `M` pour les raccourcis
  globaux.
- ⚠ **`Art/` ≠ `Resources/`** : `Art/` est consommé par **GUID**, `Resources/` **par chemin**
  (`Resources.Load`) et embarqué en entier dans le binaire. Se tromper de dossier ne lève rien — le
  jeu affiche l'ancienne image.
- Style de code : PascalCase classes/méthodes, `_camelCase` champs privés, `readonly` par défaut.
  Commentaires en français, et ils expliquent le **pourquoi**.
- Le tuning se règle **sans recompiler** dès qu'une valeur mérite d'être essayée plusieurs fois.
- Palette UI : <!-- codes hexa --> · Police : <!-- famille -->
- **Aucun chemin d'outil en dur** (Unity, Python) : `tools/environnement.ps1` les résout et les
  mémorise dans `tools/local.settings.json`, qui n'est pas versionné.

## Maintenance de la doc

- `README.md` (racine) — MAJ à chaque changement de phase ou ajout majeur.
- `docs/GDD.md` + `/carte-projet` + `docs/PITFALLS_UNITY.md` — MAJ **dans le commit** qui change ce
  qu'ils décrivent. Un document périmé donne une réponse fausse avec autorité.

## Pièges critiques → `docs/PITFALLS_UNITY.md`

Tous les pièges non évidents (import d'assets et `.meta`, cycle de vie des scènes, navigation
clavier/manette, calques d'UI, build web, tests headless, tampon de build) y sont consignés.
**Le consulter avant de coder dans le domaine concerné.**
