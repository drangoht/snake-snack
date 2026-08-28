# CLAUDE.md — Mémoire de projet

Chargé automatiquement à chaque session : **rester court et stable**. Le détail vit dans des fichiers
chargés **à la demande**, pointés ci-dessous. Plafond : ~80 lignes.

## Le projet

« Snake Snack » — Le classique jeu de Snake
**Moteur : Unity 6000.5.6f1** (C#, URP 2D, package Input System). Publié sur
`https://Drangoht.itch.io/snake-snack`.

## Où lire quoi — et seulement ça

| Besoin | Ouvrir | ⚠ |
|---|---|---|
| Intention de design | `docs/GDD.md` (sommaire) puis **le seul** `docs/gdd/<systeme>.md` concerné | ne pas lire les autres systèmes |
| Remplir un GDD encore à trous | skill **`/rediger-le-gdd`** | |
| Identité visuelle | `docs/ART.md` (sommaire) → le brief concerné | avant de produire un asset ou une UI |
| Localiser du code | skill **`/carte-projet`** | plutôt que Glob/Grep à froid |
| Piège d'un domaine | `docs/PITFALLS_UNITY.md` = **index** → 2-3 fichiers `docs/pitfalls/*.md` | jamais les quatorze |
| Vérifier qu'un changement marche | skill **`/verifier-en-jeu`** | |
| Publier | skill **`/publier-itch`**, ou l'agent `release-manager` | |

**Avant de coder dans un domaine, lire son fichier de pièges ; tout nouveau piège y retourne, dans le
même commit.** C'est ce fichier qui évite qu'un bug se reproduise six mois plus tard.

## Phase actuelle

<!-- Une phase = un objectif court, daté, avec ce qui reste à faire. La mettre à jour, pas
     l'accumuler : l'historique appartient à docs/DEVLOG.md. -->

**Phase 0 — mise en place** (démarrée le 2026-08-27)

- [x] Premier build : `& "tools/build.ps1" -Lancer` — il importe le projet (~20 min) et génère
      `Library/` + `ProjectSettings/`. Rien à ouvrir dans Unity Hub.
- [ ] Remplir `docs/GDD.md` — skill **`/rediger-le-gdd`** : pitch, boucle de jeu et commandes
      avant la première ligne de code ; le reste s'écrit système par système.
- [ ] Créer la page itch (`Kind of project` = HTML pour un jeu web) et publier une 0.1.0.

## Équipe d'agents — déléguer, mais pas à perte

Les neuf agents de `.claude/agents/` sont déjà listés avec leur description en tête de session — ne
pas les redécrire ici. Qui fait quoi **et dans quel ordre** : **`GUIDE-EQUIPE.md`**.

⚠ **Un agent démarre à froid** : il relit CLAUDE.md, le GDD, la carte, les pièges — environ 8 000
jetons avant sa première action, qu'on vient peut-être de lire soi-même. Déléguer quand la tâche est
**dans sa spécialité** *et* **assez grosse pour amortir ça** (concevoir un système, une passe de
test, une release). Une correction de dix lignes, une question, une lecture : la faire soi-même.
Quand on délègue, **passer dans la consigne ce qu'on sait déjà** (fichiers concernés, décision prise,
piège identifié) plutôt que de le lui faire redécouvrir.

⚠ **Un agent qui décrit un état périmé du projet est pire qu'un agent absent** : il donne des
instructions fausses avec autorité. Quand une phase se termine, relire les agents qu'elle concerne.

## Conventions

- **Build** : `& "tools/build.ps1"` → `Build\Windows\SnakeSnack.exe` ; `-Target web` →
  `Build\Web\` ; `-Lancer` enchaîne lancement et capture. ⚠ L'éditeur Unity doit être **fermé**.
- **Publication** : `tools/release_itch.ps1 -Version X.Y.Z` (essayer d'abord avec `-DryRun`). Le
  script pose lui-même `bundleVersion` — **ne pas l'éditer à la main**.
- **La scène est un ARTEFACT** : `Assets/Scenes/Game.unity` est régénérée par `SceneBuilder.Build()`
  à chaque build. On ajoute un objet au jeu **en écrivant du code**, pas en cliquant dans l'éditeur.
  Elle ressort modifiée après chaque build — l'écarter (`git checkout --`) sauf si `SceneBuilder.cs`
  a changé.
- **Logique pure testable** : `Assets/Scripts/Rules/` — classes statiques **sans dépendance moteur**.
  Les `MonoBehaviour` y délèguent. Tests : `dotnet test tests/SnakeSnack.Tests.csproj` (aucun
  moteur, aucun build requis ; rejoués par un hook après chaque édition).
- **Rien d'obsolète** : URP (jamais Built-in), Input System (jamais l'ancien Input Manager). Signaler
  tout avertissement de dépréciation sans attendre qu'on le demande.
- **Aucun chemin d'outil en dur** (Unity, Python) : `tools/environnement.ps1` les résout et les
  mémorise dans `tools/local.settings.json`, non versionné.
- Style : PascalCase classes/méthodes, `_camelCase` champs privés, `readonly` par défaut.
  Commentaires en français, et ils expliquent le **pourquoi**.
- Le tuning se règle **sans recompiler** dès qu'une valeur mérite d'être essayée plusieurs fois.
- Palette UI : <!-- codes hexa --> · Police : <!-- famille -->
- ⚠ Trois pièges qui ne lèvent rien et qu'on rencontre partout — le détail dans le fichier indiqué :
  clavier **AZERTY** (`docs/pitfalls/entrees.md`), `Art/` ≠ `Resources/`
  (`docs/pitfalls/assets-import.md`), code retour d'Unity trompeur (`docs/pitfalls/build.md`).

## Maintenance de la doc

- MAJ **dans le commit** qui rend faux ce qu'ils décrivent : `docs/gdd/`, `/carte-projet`,
  `docs/pitfalls/`. Un document périmé donne une réponse fausse avec autorité.
- `README.md` — MAJ à chaque changement de phase ou ajout majeur.
- ⚠ **Tout document relu à chaque tâche a un plafond** (ce fichier : 80 lignes ; `GDD.md`,
  `/carte-projet` : 150 ; un fichier de pièges ou de système : 150). Atteint, on **scinde** et on
  laisse un index. Sans ce réflexe, le coût par tâche grimpe jusqu'à dépasser la tâche.
