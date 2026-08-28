# Pièges — Polices et texte


**⚠ Le repli d'Unity sur les glyphes manquants n'existe QUE sur le bureau.** [hérité]
Avec une police dynamique, `Text` (uGUI) va chercher dans les **polices du système** ce que la police
ne contient pas : des flèches `← → ↑ ↓` sortent correctement sous Windows avec une police qui n'en
contient **aucune**. Un navigateur n'offre aucune police système : le build **WebGL les perd en
silence** — pas de carré blanc, pas d'avertissement, le texte se referme simplement sur le vide.
Constaté sur Smily Volley : bandeaux d'aide amputés, indicateurs de défilement invisibles.

Le repli déclaré à l'import (`fallbackFontReferences` → `LegacyRuntime.ttf`, posé par script sur le
`TrueTypeFontImporter`) **n'y change rien** : essayé, rebâti, les flèches restaient absentes.

**Ce qui marche** : n'écrire que des caractères que la police contient (« Haut/Bas » plutôt que
« ↑ ↓ ») et **dessiner les symboles en sprite**. Vérifier la table `cmap` avant de faire confiance —
un script Python de 20 lignes la lit et répond oui ou non. Et le vérifier **dans le navigateur**,
pas au raisonnement.

**Polices libres** : prendre le `.ttf` **et son `OFL.txt`** dans le dépôt `google/fonts` (SIL OFL) :
`https://raw.githubusercontent.com/google/fonts/main/ofl/<famille>/<Fichier>.ttf`.
⚠ Beaucoup de familles n'existent plus qu'en **version variable** (`Fredoka[wdth,wght].ttf`) :
lister le dossier avant de deviner l'URL
(`https://api.github.com/repos/google/fonts/contents/ofl/<famille>`). ⚠ L'API
`fonts.googleapis.com/css` rend une URL dont le fichier **n'est pas un TTF valide** (signature
`f89b`) : un vrai TTF commence par `00 01 00 00`, et un fichier de 39 Ko contenant du HTML est une
page 404 déguisée.

**⚠ « La famille existe sur Google Fonts » ne veut pas dire « une graisse statique existe ».**
Constaté sur **Nunito** le 2026-08-28, après que le même piège avait fait écarter Fredoka :
`ofl/nunito` ne contient que `Nunito[wght].ttf` et `Nunito-Italic[wght].ttf`, aucun dossier
`static/`. Rien ne le signale — la famille s'affiche normalement sur fonts.google.com (le site sert
le variable), et une URL `static/Nunito-SemiBold.ttf` devinée rend une page 404 de 39 Ko qui
ressemble à un fichier téléchargé.

**Ce qui tranche en une requête de plus** : `ofl/<famille>/upstream_info.md` cite le
`sources/config.yaml` de l'amont. `buildStatic: false` y signifie que les statiques ne sont **pas
absentes par retard de publication, mais jamais construites** — inutile d'aller les chercher
ailleurs, de chercher une release, ou d'attendre. Vérifier les deux : le listing du dossier dit
l'état, `upstream_info.md` dit s'il changera.
