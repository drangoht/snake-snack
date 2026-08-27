Ce dossier est copie tel quel a cote de `index.html` dans le build web.

Y deposer ce que la PAGE consomme, et rien d'autre : `favicon.png`, une image d'attente, une
police pour l'ecran de chargement. Ces fichiers ne passent pas par Unity : ils ne sont ni
importes, ni compresses, ni references par GUID -- c'est le HTML qui les designe par chemin
relatif (`TemplateData/favicon.png`).

Ne pas y mettre d'asset du jeu : ce qu'Unity doit charger va dans `Assets/Resources/` (par
chemin) ou `Assets/StreamingAssets/` (fichiers bruts telecharges au demarrage).

Pour une police libre et redistribuable, prendre le `.ttf` **et son `OFL.txt`** dans le depot
`google/fonts` (SIL Open Font License) :
`https://raw.githubusercontent.com/google/fonts/main/ofl/<famille>/<Fichier>.ttf`
