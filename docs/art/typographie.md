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
variable `Nunito[wght].ttf`.

⚠ **Ces fichiers statiques N'EXISTENT PAS — et la famille est conservée quand même.**
`GET https://api.github.com/repos/google/fonts/contents/ofl/nunito` (2026-08-28) ne rend aucun
dossier `static/`, seulement `Nunito[wght].ttf` et `Nunito-Italic[wght].ttf` ; `upstream_info.md`
confirme `buildStatic: false` dans le `config.yaml` amont — les statiques ne sont pas en retard de
publication, elles ne sont **jamais construites**.

✅ **Tranché par l'auteur le 2026-08-28 : on INSTANCIE, on ne change pas de famille.**
`tools/generer_polices.py` récupère le variable, en fige `wght=600` et `wght=800` avec
`fontTools.varLib.instancer`, et écrit `Assets/Resources/Polices/Nunito-SemiBold.ttf` et
`Nunito-ExtraBold.ttf`. Le piège de `docs/pitfalls/polices-texte.md` vise l'import d'un fichier
**variable** dans Unity ; une instance extraite est un `.ttf` statique ordinaire, et Unity l'importe
comme tel (`TrueTypeFontImporter`, `includeFontData: 1` — vérifié dans le `.meta` généré).

**Pourquoi un générateur versionné et pas deux `.ttf` déposés à la main** : personne ne saurait dans
six mois de quelle source, à quel poids et avec quelle version de `fonttools` ils viennent. Le
script épingle en plus le **sha256 de l'amont** — `main` bouge, et une régénération silencieuse un
an plus tard produirait d'autres fichiers que ceux du dépôt sans que rien ne le signale.

**Licence — le nom « Nunito » peut être conservé.** L'`OFL.txt` de Nunito ne déclare **aucun
Reserved Font Name** : sa ligne de copyright est `Copyright 2014 The Nunito Project Authors
(https://github.com/googlefonts/nunito)`, sans le suffixe `with Reserved Font Name` qui déclencherait
la clause 5 de la SIL OFL. Une instance modifiée peut donc rester « Nunito SemiBold » / « Nunito
ExtraBold ». ⚠ **Ce contrôle est à refaire pour toute autre famille** : la plupart en portent un, et
le renommage y devient obligatoire. `OFL.txt` est versé à côté des `.ttf` (il part donc aussi dans le
binaire, comme la licence l'exige) et l'attribution est dans `docs/CREDITS.md`.

**Couverture de glyphes — vérifiée le 2026-08-28 sur le fichier INSTANCIÉ** (c'est lui qui est
importé, pas l'amont) : `tools/generer_polices.py` lit la table `cmap` et refuse d'écrire s'il
manque un seul des **125 caractères exigés** — ASCII 32-126 plus le jeu d'accents ci-dessous. Les
deux graisses les portent tous (938 glyphes au total). Le contrôle est **rejoué à chaque
régénération**, et `py tools/generer_polices.py --verifier` le rejoue seul, sans rien réécrire.
Le jeu exigé va au-delà du seul `é` qu'emploie `TextesUi.cs` aujourd'hui, pour
qu'un texte ajouté demain n'oblige pas à rouvrir ce brief :
`à â ä ç é è ê ë î ï ô ö ù û ü À Â Ä Ç É È Ê Ë Î Ï Ô Ö Ù Û Ü`.

⚠ **Et vérifié DANS LE NAVIGATEUR, pas au raisonnement** (2026-08-28, `docs/TEST_REPORT.md`) : build
web servi par `tools/serve_web.py`, écran de pause, « Touche ignorée » — l'accent est bien rendu. La
`cmap` prouve que les 125 caractères sont dans le fichier ; le navigateur prouve que la chaîne
entière tient (instance → import Unity → embarquement → rastérisation WebGL). Aucune police système
ne peut plus masquer un manque à cet endroit.

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

**Tailles ET graisses câblées le 2026-08-28** dans `HudJeu.Construire`, telles quelles.
⚠ **Jamais de `FontStyle.Bold` par-dessus** : la graisse vient du fichier. Le gras synthétique
d'uGUI s'ajouterait au dessin déjà gras et boucherait les contre-formes rondes de Nunito, exactement
ce que le §2.4 interdit aux contours épais.

⚠ `RappelDesCommandes` est ancré à **14 px** du bas, pas 10 : le pivot étant au centre d'une boîte
de 24 px, 10 px faisait tomber le bas de la boîte 2 px **sous** l'écran et tronquait les jambages de
`g`, `p`, `q` (mesuré sur build, corrigé le 2026-08-28). Ce n'était pas le corps du texte qui coupait
— il coupait déjà à 15 px — mais l'ancrage. ⚠ **La marge basse reste absente sous l'aire de jeu**
(`docs/gdd/grille.md`) : ce correctif fait tenir le texte à l'écran, il ne tranche pas cet
arbitrage-là.

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
