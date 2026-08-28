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

⛔ **VÉRIFIÉ LE 2026-08-28 : ces fichiers statiques N'EXISTENT PAS. La famille est bloquée.**
`GET https://api.github.com/repos/google/fonts/contents/ofl/nunito` ne rend **aucun dossier
`static/`**, seulement `Nunito[wght].ttf`, `Nunito-Italic[wght].ttf`, `OFL.txt`, `METADATA.pb`,
`DESCRIPTION.en_us.html` et `upstream_info.md`. Et l'amont le confirme sans ambiguïté :
`upstream_info.md` note que le `sources/config.yaml` du dépôt `googlefonts/nunito` porte
**`buildStatic: false`** — les statiques ne sont pas seulement absentes du dépôt de distribution,
elles ne sont **pas construites du tout**. C'est exactement ce qui avait fait écarter Fredoka, et le
pari du paragraphe ci-dessus (« la plus probable à disposer de statiques ») est perdu.

Conformément à la consigne de ce brief, **rien n'a été improvisé** : ni instanciation du fichier
variable, ni sous-ensemble de poids fabriqué à la main. Le HUD reste sur la police intégrée
(`LegacyRuntime.ttf`) jusqu'à ce que la famille soit rouverte par le directeur artistique. Les
**tailles du §2.3 sont, elles, câblées** — elles ne dépendent pas de la famille.

⚠ **Ce que le prochain candidat doit prouver avant d'être retenu** : le dossier `ofl/<famille>` de
`google/fonts` contient un `static/` **listé**, ET `upstream_info.md` ne porte pas
`buildStatic: false`. Vérifier les deux : le second explique le premier, et évite de conclure « la
famille est peut-être juste en retard de publication ».

**Couverture de glyphes à vérifier avant tout import** — *non fait au 2026-08-28 : aucun fichier
n'a été récupéré, il n'y avait rien à sonder.* Reste dû dès qu'une famille est retenue. (script `cmap`, voir pitfall) : ASCII
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

**Câblées le 2026-08-28** dans `HudJeu.Construire` (colonne « taille retenue »). Les **graisses** ne
le sont pas — voir le blocage du §2.2. ⚠ Le relevé de `RappelDesCommandes` à 18 px rend plus visible
la marge basse absente de `docs/gdd/grille.md` : ce texte est ancré à 10 px du bas dans une boîte de
24 px, dont le bas tombe donc **2 px sous le bord de l'écran**, et ses jambages sont coupés. Ce n'est
pas la taille qui coupe (elle coupait déjà à 15 px), mais l'ancrage — l'arbitrage de mise en page
reste entier.

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
