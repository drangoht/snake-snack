# Pièges — Assets et import


**⚠ Ne JAMAIS ignorer les `.meta` dans `.gitignore`.** Unity y stocke le **GUID** de chaque asset.
Un `.meta` manquant fait perdre toutes les références qui pointaient vers l'asset : scripts détachés
de leurs GameObjects, sprites vidés. Le `.gitignore` du projet ne contient aucune règle `*.meta`, et
c'est délibéré.

**⚠ `Art/` et `Resources/` ne se valent pas — et se tromper ne lève rien.** [hérité]
`Resources/` est chargé **par chemin** (`Resources.Load<Sprite>("Ui/bouton")`) et embarqué **en
entier** dans le binaire, même ce qui n'est jamais utilisé. `Art/` est consommé par **référence de
GUID**. Écrire un asset dans le mauvais des deux : le générateur annonce « écrit », et le jeu affiche
l'ancienne image. Tenir une table de destination (`tools/unity_paths.py`) et y faire référence.

**⚠ Un fichier écrit dans `Assets/` n'existe pas tant qu'Unity ne l'a pas réimporté.** Un build en
batchmode s'en charge, mais un éditeur ouvert peut servir l'ancienne version depuis sa base d'assets.
Sur un fichier **nouveau** et ignoré par git, `AssetDatabase.ImportAsset` seul ne suffit pas : il
faut d'abord un `AssetDatabase.Refresh()` pour que la base le découvre.

