# Brief — 2. Typographie

Sorti de `docs/ART.md` §2 : le raisonnement complet n'intéresse que qui importe la police ou ajoute
un texte ; `ART.md` §2 ne garde que la famille retenue et le rappel du piège WebGL.

## 2.1 Le parti pris

**Une seule famille, ronde mais sobre, en deux graisses seulement — le texte doit se lire à la
taille d'une page itch avant de se lire comme un choix de style.** Le jeu s'appelle « Snack » :
un dessin trop géométrique (Arial, Roboto) contredirait l'esprit casual du titre ; mais un dessin
trop rond et bulleux se lirait comme un jeu pour enfants, ce que « Snake Snack » n'annonce pas.

## 2.2 Famille retenue : Nunito (SIL OFL)

**Nunito**, deux graisses : **SemiBold** pour le texte secondaire et permanent, **ExtraBold** pour
les titres et les nombres du HUD. Pas de graisse Regular dans le jeu : à ces tailles et sur un
rendu WebGL redimensionné, un trait trop fin de police ronde disparaît avant de se lire (rappel du
piège général : *docs/pitfalls/polices-texte.md*, et l'avertissement générique sur les polices
d'affichage rondes — trait plus fin qu'Arial au même calibre).

**Pourquoi Nunito et pas une police plus « jeu vidéo »** : c'est une des familles Google Fonts les
plus anciennes et les plus utilisées en UI, donc la plus probable à disposer de fichiers **statiques
par graisse** (`static/Nunito-SemiBold.ttf`, `static/Nunito-ExtraBold.ttf`) à côté du fichier
variable `Nunito[wght].ttf` — **à vérifier avant de télécharger** (`GET
https://api.github.com/repos/google/fonts/contents/ofl/nunito`, même procédure que documentée dans
le pitfall : lister le dossier, ne pas deviner l'URL). Si aucun `static/` n'existe au moment de
l'import, remonter ici avant de basculer sur une variante variable — ne pas improviser un
sous-ensemble de poids à la main.

**Couverture de glyphes à vérifier avant tout import** (script `cmap`, voir pitfall) : ASCII
imprimable (32-126) **plus** les accents français utilisés par `TextesUi.cs` — à ce jour uniquement
`é` (minuscule), mais vérifier tout le jeu de voyelles accentuées + `ç` pour ne pas relire ce brief
au prochain texte ajouté : `à â ä ç é è ê ë î ï ô ö ù û ü À Â Ä Ç É È Ê Ë Î Ï Ô Ö Ù Û Ü`. Nunito
couvre normalement le Latin étendu, mais « normalement » n'est pas une vérification — la table
`cmap` réelle du fichier statique récupéré tranche, pas ce paragraphe.

## 2.3 Tailles — corps relevé de deux points, référence 1280×720

Le canevas HUD (`HudJeu.Construire`) utilise `CanvasScaler.ScaleWithScreenSize` sur une résolution
de référence 1280×720 : ces tailles sont donc les corps réels affichés en plein cadre, et
*rétrécissent* proportionnellement sur la fenêtre plus petite d'une page itch — c'est l'inverse d'une
marge de sécurité, d'où le relevé de corps.

| Texte | Taille actuelle | Taille retenue | Graisse |
|---|---|---|---|
| `RappelDesCommandes` (le plus petit, donc le plus à risque) | 15 px | **18 px** | SemiBold |
| `RefusEnPause` | 18 px | **20 px** | SemiBold |
| `SousTitrePause` / `SousTitreVictoire` / `SousTitreMort` | 20 px | **22 px** | SemiBold |
| `Etat`, `Score`, `Record` (bandeau permanent) | 22 px | **24 px** | ExtraBold |
| `RecapFin` | 24 px | **26 px** | ExtraBold |
| `Titre` (Pause / Perdu / Gagné) | 54 px | **56 px** | ExtraBold |

Plancher retenu pour tout futur texte : **18 px** à cette résolution de référence — en dessous, le
downscale itch le rend illisible avant même le poids de la police.

## 2.4 Contraintes techniques

- Police au format **TrueType statique** (`.ttf`), jamais le fichier variable importé tel quel.
- Chaque `.ttf` embarque son `OFL.txt` à côté dans le dépôt et dans `docs/CREDITS.md` (SIL OFL,
  attribution requise).
- Couleur de texte toujours via `UiPalette.TexteHud` / `UiPalette.TexteSecondaire` (§1) — jamais un
  `Color` posé en dur dans `HudJeu.cs` ou ailleurs.
- Si un contour (`Outline`) ou une ombre est ajouté un jour pour lisibilité sur fond variable :
  **≤ 1 px**. Un liseré plus épais referme les contre-formes d'une lettre ronde (le `a`, le `e`, le
  `o` se bouchent) au lieu de la détourer. Préférer une plaque de fond semi-opaque derrière le texte
  à un contour épais si le contraste ne suffit pas.

## 2.5 Interdits

- Jamais de caractère flèche Unicode (`← → ↑ ↓`) dans un composant `Text` — perte silencieuse en
  WebGL, déjà interdit par `ART.md` §5.7 et `docs/pitfalls/polices-texte.md`. Tout symbole
  directionnel est un sprite.
- Jamais un fichier de police **variable** importé comme s'il fixait un poids : un poids se choisit
  à l'import, pas au runtime.
- Jamais un texte en dessous de 18 px à la résolution de référence 1280×720.
- Jamais de graisse Regular dans ce jeu : SemiBold est le plancher.
- Jamais un caractère non vérifié dans la table `cmap` du fichier réellement importé — vérifier
  dans le navigateur (build web), pas au raisonnement sur le bureau.
