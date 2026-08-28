# Pièges — Build


**⚠ Lancer Unity par l'opérateur `&` en PowerShell rend la main IMMÉDIATEMENT sans rien faire.**
[hérité] Pas de log, `$LASTEXITCODE` vide, et le script poursuit comme si tout allait bien. Utiliser
`Start-Process -Wait`. *Un lancement qui échoue en silence est pire qu'un lancement qui échoue.*

**⚠ Un code retour nul ne distingue pas « construit » de « rien à faire ».** Exiger une **phrase de
réussite explicite** dans le journal (c'est ce que fait `tools/build.ps1`).

**⚠ Pire : Unity quitte avec le code retour 0 alors que le build a ÉCHOUÉ.** Constaté sur un build
Windows dont le journal dit `Build Finished, Result: Failure` (6 erreurs) puis, trente lignes plus
bas, `Exiting batchmode successfully now!` et un code 0. Un script qui se fie au code retour empaquette
et publie un dossier de build incomplet **sans que rien ne l'avertisse**. La phrase de réussite dans
le journal est le seul signal fiable.

**⚠ La DATE d'un artefact de build ne prouve rien** : Unity construit de façon incrémentale, un
fichier identique n'est **pas réécrit**. Un horodatage antérieur au build est normal. Le premier
garde-fou de fraîcheur écrit sur cette base échouait sur des builds parfaitement valides.

**⚠ Les métadonnées Windows d'un `.exe` Unity décrivent le MOTEUR** (« 6000.5.6f1 »), pas le jeu. Un
contrôle qui compare la version de release à ces métadonnées échoue toujours.

**⚠ Seule la version EMBARQUÉE tranche**, parce qu'elle est posée juste avant le build. D'où
`build_stamp.json`, écrit **par** le build : il ne peut pas annoncer une version que le build n'a pas
posée. **Une release a déjà expédié le binaire de la version précédente sans qu'aucune erreur ne soit
levée** — c'est ce contrôle qui l'empêche.

**⚠ Un tampon de build écrit par le script de PUBLICATION survit à sa release.** [hérité] Écrit
seulement au moment de publier, le fichier reste ensuite en place, et tout build local ultérieur
affiche le SHA de la dernière release. *Un garde-fou de fraîcheur qui se trompe est pire que pas de
garde-fou, puisqu'on lui fait confiance.* Il est donc posé par le **build** (`BuildTools.StampGitSha`)
et ignoré par git — c'est un artefact, pas une source.

**⚠ Le build en ligne de commande échoue si l'éditeur Unity est ouvert** (« another Unity instance is
running »). Vérifier `Get-Process Unity` ou `Temp\UnityLockfile` — et **ne jamais tuer l'éditeur** :
attendre, ou travailler sur une copie de `Assets` + `Packages` + `ProjectSettings`.

**⚠ Le premier build d'une plateforme réimporte tous les assets** (plusieurs dizaines de minutes) ;
les suivants sont rapides. Prévoir le timeout en conséquence.

**⚠ La scène régénérée produit un diff énorme et vide de sens.** `SceneBuilder` renumérote tous les
`fileID` : des milliers de lignes ajoutées et autant de retirées pour une scène identique. L'écarter
(`git checkout --`) **sauf si `SceneBuilder.cs` a changé**. Sans l'exclusion correspondante dans
`BuildTools.HasLocalChanges`, tout build se déclarerait issu d'un arbre modifié.

