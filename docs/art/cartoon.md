# Brief — Le cartoon (formes, contours, proportions en jeu)

Sorti de `docs/ART.md` §5. **Arbitré par l'auteur le 2026-08-28** : les recommandations du directeur
artistique sont retenues telles quelles (§7). Ce brief ne touche à aucune mécanique, et ne rouvre
**ni** la palette (`docs/art/palette.md`) **ni** la typographie (`docs/art/typographie.md`), toutes
deux tranchées avec preuve chiffrée le 2026-08-28.

## 1. Diagnostic — le cartoon existe déjà, mais s'arrête à la porte du menu

- Tout le rendu de la partie (`Assets/Scripts/Gameplay/FormesPrimitives.cs`) part d'**un seul sprite
  partagé** : un pixel blanc étiré, en `FilterMode.Point`. Aire, grille, bordure, corps, tête, pomme,
  chevron — chaque rectangle du jeu a des arêtes à 90° strictement nettes, sans le moindre
  anticrénelage. C'est, littéralement, du papier millimétré coloré.
- À l'inverse, `tools/generer_illustration_serpent.py` (l'illustration du menu, déjà validée) dessine
  des **carrés arrondis** (rayon 0,28 du côté), suréchantillonnés ×4 puis réduits en LANCZOS — donc
  lissés —, avec un visage (deux yeux, une langue) sur la tête.
- **Conséquence directe pour la page itch** : le menu et la cover promettent un personnage rond et
  expressif ; dès que la partie commence, ce personnage redevient une suite de rectangles muets. Le
  décalage entre l'affiche et le produit est le symptôme le plus net de « pas assez cartoon ».
- La police (Nunito, déjà ronde — voir `docs/art/typographie.md` §2.1, qui écarte explicitement un
  dessin trop géométrique) et la palette (quatre couleurs chaudes sur socle froid) sont déjà dans le
  bon registre : rien à y reprendre.

## 2. Parti pris

**Étendre à la partie la matière déjà validée pour le menu** — carrés arrondis, lissage, un visage
minimal sur la tête — sans toucher à ce qui porte la lisibilité : aucune couleur, aucune taille,
aucune position de case ne change.

## 3. Ce qui bouge

### 3.1 Sprite « case arrondie » pour le corps et la tête du serpent

Remplace le rectangle plat pour ces deux éléments seulement (pas la bordure, pas la grille — §3.4).

- **Génération** : nouveau script `tools/generer_formes_jeu.py`, même famille que le générateur de
  l'illustration du menu — un PNG carré transparent, un rectangle à coins arrondis, **rayon 0,28 du
  côté** (même ratio que le menu : le personnage du jeu et celui de l'affiche doivent se reconnaître
  comme le même dessin), suréchantillonné puis réduit en LANCZOS. Export **128×128** — assez grand
  pour rester net à 44 px de case et pour un futur zoom.
- **Import** : `Assets/Resources/Formes/case-arrondie.png`, forcé en Sprite (étendre
  `Assets/Editor/ImportIllustrations.cs` à ce nouveau dossier plutôt qu'en écrire un second), en
  **9-slice** (`spriteBorder` ≈ 36 px, le rayon en pixels de l'image) pour que `VuePlateau` continue
  d'étirer le sprite à `TailleCase - 2` sans déformer les coins.
- `FormesPrimitives` gagne une fabrique `RectangleArrondi`, à côté de `Rectangle` (inchangée).
- **`FilterMode` de ce sprite : Bilinear**, pas Point — c'est ce qui donne le lissage. Le carré blanc
  partagé (`Carre()`) reste en Point : tout ce qui doit rester un aplat net (bordure, grille) ne
  change pas.
- **Coût WebGL** : nul à négligeable. Même nombre de `SpriteRenderer` qu'aujourd'hui ; un sprite
  9-slice ajoute quelques triangles par instance, aucun draw call de plus (déjà groupés par texture
  partagée).

### 3.2 La pomme : coins légèrement arrondis

Même traitement que §3.1, rayon 0,18 (celui de `dessiner_pomme` dans le générateur de menu) plutôt
que les angles vifs actuels. Reste un **losange** — voir §4, ce n'est pas négociable — seuls ses
coins s'adoucissent. À vérifier techniquement si le 9-slice de `case-arrondie.png` tourné à 45°
suffit, ou si un second petit PNG dédié (`pomme-arrondie.png`, même script) est plus sûr.

### 3.3 Un visage minimal sur la tête, en jeu

- Deux points de la couleur `Fond` (déjà un rôle de `UiPalette`, exactement l'usage qu'en fait
  l'illustration du menu), posés en enfants du segment de tête, orientés par la direction de marche —
  même logique que `dessiner_tete` : les yeux restent du côté qui regarde devant.
- **Pas de langue en jeu** (contrairement au menu) : elle dépasserait de la case et empiéterait sur
  la case suivante à chaque tick, clignotant au rythme des 8 ticks/s — l'inverse exact de l'interdit
  « pas de clignotement » de `ART.md` §4.
- ✔ **Livré le 2026-08-30 au ratio exact du menu** (rayon 0,11 de la case), après capture à l'échelle
  réelle : le risque annoncé — « ~4-5 px de rayon, sous le seuil de lisibilité » — **ne s'est pas
  vérifié** ; un rayon de 4,6 px fait un œil de 9 px de diamètre, franc à 44 px. Les yeux sont
  **enfants du segment de tête**, donc soumis à son gulp (`juicy.md` §5) et à son inclinaison (§9) —
  un cercle sous échelle non uniforme reste une ellipse, sans cisaillement.

### 3.4 Ce qui pourrait suivre, priorité basse

Arrondir seulement les **quatre coins** de l'aire de jeu (pas toute la longueur des murs) pour
adoucir le cadre sans faire disparaître la métaphore du mur. À n'envisager qu'une fois le reste fait
et testé — voir aussi §4 sur pourquoi le mur, lui, reste dur par défaut.

## 4. Ce qui ne bouge pas, et pourquoi

- **Les 12 rôles de `UiPalette.cs`**, leurs codes hexa et leurs ratios de contraste
  (`docs/art/palette.md`), y compris le compromis daltonisme pomme/corps qui repose sur la forme
  plus que sur la couleur. Changer une teinte sans refaire ces calculs romprait cette garantie sans
  que rien ne le signale.
- **Nunito** : déjà ronde, déjà « casual sans être enfantine » — le brief typo écarte explicitement
  un dessin plus « bulleux » (Baloo 2) pour cette raison précise. C'est déjà le bon registre, rien à
  reprendre.
- **La grille (21×15), la taille de case (44 px), la cadence (8 ticks/s)** : mécaniques et mise en
  page, hors du périmètre de ce brief.
- **La forme du chevron de refus** (`docs/art/retour-refus.md` §5.6) : déjà tranchée et vérifiée à
  l'écran (contraste mesuré 3,81:1). L'arrondir rouvrirait une mesure déjà faite pour un gain
  cosmétique marginal sur un signal qui doit d'abord être vu vite.
- **La silhouette en losange de la pomme** : c'est la forme qui la distingue du serpent avant même la
  couleur (`ART.md` §4) — ce brief adoucit ses coins (§3.2), il ne change jamais sa silhouette.
- **La bordure de l'aire (le mur)** : reste un rectangle à angles vifs par défaut. Un mur n'est pas
  un personnage ; ne pas l'arrondir marque la différence entre ce qui vit (rond) et ce qui tue (dur,
  géométrique) — un choix de lecture, pas un oubli.

## 5. Contraintes techniques

- `Assets/Resources/Formes/`, jamais `Assets/Art/` : chargé par chemin comme l'illustration du menu
  (`docs/pitfalls/assets-import.md`).
- Forcer l'import en Sprite (projet en mode 3D) — étendre le postprocessor existant plutôt qu'en
  écrire un second à maintenir.
- `spriteBorder` (9-slice) posé **à l'import**, jamais dans un `.meta` retouché à la main — il serait
  réécrit au réimport suivant, le même piège déjà documenté pour `textureType`
  (`docs/pitfalls/assets-import.md`).
- Le PNG lui-même reste blanc/transparent : la couleur vient toujours du `SpriteRenderer.color`
  référencé depuis `UiPalette`, jamais cuite dans le fichier — sinon un changement de palette futur
  n'aurait plus d'effet sur ces sprites, l'inverse exact de la règle « un seul endroit du dépôt ».
- **Conséquence sur la page itch** : le menu (`docs/itch/capture-1-menu.png`, `cover.png`) n'a pas
  besoin d'être régénéré — son illustration ne change pas. En revanche
  `docs/itch/capture-2-partie.png` et `capture-3-perdu.png` montrent le rendu **actuel**, en
  rectangles plats : à relancer une fois §3.1–§3.3 implémentés, sinon la page annonce un serpent que
  la partie ne montre plus fidèlement.

## 6. Interdits

- Jamais une nouvelle couleur : tout sprite ajouté s'habille avec un rôle déjà existant de
  `UiPalette.cs`.
- Jamais un fichier chargé par `Resources.Load` posé dans `Assets/Art/`.
- Jamais de `.meta` corrigé à la main pour poser le 9-slice.
- Jamais un détail (yeux, éventuelle langue) qui dépasse du carré de la case — tout doit tenir dans
  les `TailleCase - 2` px déjà réservés au segment.
- Jamais `FilterMode.Bilinear` sur le carré blanc partagé (`FormesPrimitives.Carre()`) : la bordure
  et les traits de grille restent des aplats nets, ce sont des repères de mesure, pas des
  personnages.
- Jamais arrondir le chevron de refus ou changer sa taille sans repasser par
  `docs/art/retour-refus.md` — contraste déjà mesuré, à ne pas rouvrir sans élément neuf.
- Jamais changer la silhouette de la pomme (losange) ou une valeur de `UiPalette.cs` au prétexte de
  ce brief.

## 7. Arbitrage de l'auteur (2026-08-28)

Recommandations du directeur artistique retenues sans modification :

- **Le rounding (§3.1) part en premier**, avec le socle P1 de `juicy.md` — c'est lui seul qui sort le
  jeu du papier millimétré, et il ne dépend d'aucune décision restée ouverte.
- **Le visage en jeu (§3.3) est prototypé, pas décidé** : à voir sur une capture à l'échelle réelle
  (44 px) avant d'être gardé. S'il ne lit pas, le rounding seul fait le travail — ne pas forcer un
  détail illisible. → **Tranché le 2026-08-30 sur capture : gardé tel quel, sans réduire les yeux.**
- **Les coins de l'aire (§3.4) ne bougent pas** : le mur reste dur, par contraste avec la créature
  ronde. Choix de lecture, pas oubli.
- **Les captures itch (§5) sont régénérées dès le rounding livré** : `capture-2-partie.png` et
  `capture-3-perdu.png` montrent le rendu en rectangles plats, et une page qui annonce un serpent que
  la partie ne montre plus est le défaut le plus coûteux de la page store.
- **Le traitement de la pomme (§3.2)** reste tranché à l'implémentation, selon ce que le 9-slice
  supporte réellement en rotation.
