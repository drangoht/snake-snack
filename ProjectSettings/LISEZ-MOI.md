# `ProjectSettings/` — ce que le gabarit y pose, et pourquoi

Unity génère ce dossier lui-même au premier import. Deux fichiers y sont livrés d'avance, parce
qu'un projet neuf ne construit pas sans eux ou ne s'ouvre pas avec le bon éditeur.

## `ProjectVersion.txt`

Déclare la version d'Unity du projet. C'est le fichier qu'Unity Hub lit pour décider quel éditeur
ouvrir, et celui que `tools/environnement.ps1` lit pour choisir parmi les éditeurs installés.
`tools/new-game.ps1` y inscrit la version de l'éditeur réellement trouvé sur la machine.

## `BurstAotSettings_StandaloneWindows.json` — Burst désactivé pour le build Windows

**Ce qui se passe sans lui** : le build Windows d'un projet neuf échoue à la toute dernière étape
(`GenerateNativePluginsForAssemblies`), sur

```
BuildFailedException: Burst compiler failed running
Error: Failed to find entry-points:
  Failed to resolve assembly 'Unity.InternalAPIEngineBridge.RenderPipelines.Core.Runtime.Shared'
  in directories: Library\Bee\artifacts\WinPlayerBuildProgram\ManagedStripped
```

Le compilateur Burst cherche une assembly interne d'URP que l'étape de *stripping* vient de
retirer. Rien dans le projet ne demande Burst : il arrive comme dépendance transitive d'URP et de
l'Input System.

**Pourquoi c'est vicieux** : Unity quitte malgré tout avec le **code retour 0** (« Exiting batchmode
successfully now! »). Un script qui se fierait au code retour publierait un dossier de build
incomplet. C'est ce qui justifie que `tools/build.ps1` exige la phrase de réussite écrite par
`BuildTools` dans le journal, et pas seulement un code retour nul.

**Ce qui marche** : `"EnableBurstCompilation": false` pour la cible Standalone. Un jeu 2D dont la
logique tient dans des classes C# ordinaires n'a aucun job Burst à compiler — on ne perd rien.

⚠ Le jour où le projet utilise vraiment le C# Job System ou les Collections natives, remettre
`true` et traiter l'erreur de stripping (`link.xml`, ou `managedStrippingLevel` plus permissif)
plutôt que de garder Burst éteint sans le savoir.

Constaté sur Unity 6000.5.6f1, URP 17.5.0, Burst 1.8.29.
