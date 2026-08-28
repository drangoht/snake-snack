# Brief — 1. La palette

Sorti de `docs/ART.md` §1 : le raisonnement complet n'intéresse que qui touche `UiPalette.cs` ou
dessine un sprite ; `ART.md` §1 ne garde que la décision et les codes hexa.

## 1.1 Le parti pris

**Un socle froid et presque noir qui ne cherche jamais l'œil, sur lequel seul ce qui compte au jeu
porte une couleur chaude.** Le mur qui tue est ambre, la pomme qui nourrit est rouge, le serpent
reste vert — ni menace ni récompense, juste le joueur — et le blanc pur n'appartient qu'au signal qui
doit toujours gagner (le pictogramme de refus). Fond, aire et grille restent une seule famille grise
bleutée : rien n'y attire l'œil, pour que les quatre couleurs chaudes se voient tout de suite.

Ce parti pris ne change *aucune* forme, *aucune* position, *aucune* taille déjà posées ailleurs
(§5.4, §5.6) — il colore un système déjà validé en niveaux de gris, il ne le redessine pas.

## 1.2 Les 12 rôles — couverture exacte de `PaletteProvisoire.cs`

Douze rôles nommés existaient déjà, en gris, dans `Assets/Scripts/UI/PaletteProvisoire.cs`. Cette
palette **couvre exactement ces douze rôles, sous les mêmes noms** — aucun ajouté, aucun retiré. Le
fichier n'a qu'à être renommé `UiPalette` et ses corps de méthode remplacés ; aucun appelant ne
change.

| Rôle | Gris provisoire | Couleur retenue | Pourquoi cette couleur |
|---|---|---|---|
| `Fond` | 0,07 | `#0A0E13` | Slate quasi noir, jamais pur noir (§1.4) : les marges ne doivent rien réclamer. |
| `AireDeJeu` | 0,13 | `#121821` | Même famille que `Fond`, un cran plus clair : l'aire se détache sans se signaler. |
| `TraitDeGrille` | 0,20 | `#1C2530` | Toujours la même famille froide : aide à compter, ne doit jamais lire comme un objet. |
| `BordureAire` | 0,62 | `#E3A23A` | Ambre : c'est le mur qui tue (GDD §2), seule couleur « alerte » posée à demeure sur tout l'écran. |
| `CorpsSerpent` | 0,58 | `#4E9358` | Vert moyen : le serpent est le joueur, ni danger ni objectif — la seule couleur neutre du jeu. |
| `TeteSerpent` | 0,94 | `#D8F5C4` | Même vert, tiré vers le clair : la case qui compte le plus au tick reste la plus lisible. |
| `Pomme` | 0,80 | `#E5473B` | Rouge chaud, seule couleur de ce hue dans le jeu : rien d'autre ne se confond avec « à manger ». |
| `Pictogramme` | 1,00 | `#FFFFFF` | Blanc pur, réservé : c'est le seul rôle à cette valeur, il doit toujours dominer, quel que soit le fond. |
| `TexteHud` | 0,86 | `#E7EDF2` | Blanc légèrement froid : lisible en permanence sur `Fond`, jamais aussi saturé que `Pictogramme`. |
| `TexteSecondaire` | 0,52 | `#8792A0` | Gris-bleu moyen : hiérarchie sous `TexteHud`, toujours dans la famille froide du fond. |
| `VoileDePause` | noir 62 % | `#000000` à 62 % | Inchangé : un voile achromatique n'entre en concurrence avec aucune des quatre couleurs chaudes. |
| `TamponDeBuild` | blanc 45 % | `#FFFFFF` à 45 % | Inchangé, pour la même raison — et il doit rester lisible quel que soit le fond réel qu'il recouvre. |

Aucun rôle ne manque, aucun n'est de trop : les quatre couleurs chaudes (bordure, corps, tête,
pomme) portent chacune une information de gameplay distincte ; les deux blancs (pictogramme, texte)
et les deux gris froids (fond, aire) restent achromatiques par construction, pour ne jamais rivaliser
avec les quatre premières. Si un treizième rôle apparaît un jour (un boost, un multiplicateur), il
devra se justifier de la même façon avant d'entrer ici — pas se caler entre deux rôles existants.

## 1.3 La preuve du contraste

Ratio WCAG standard : `(L_clair + 0,05) / (L_sombre + 0,05)`, `L` = luminance relative
(conversion sRGB → linéaire standard, gamma 2,4). Seuils de référence : **3:1** pour un objet
graphique / composant d'UI (WCAG 1.4.11), **4,5:1** pour du texte de taille normale, **3:1** pour du
texte large (≥ 24 px ou 19 px gras) — nos textes HUD dépassent tous largement l'un ou l'autre seuil,
posé ci-dessous.

| Paire | Ratio | Verdict | La forme qui double la couleur |
|---|---|---|---|
| Pomme / TeteSerpent | **3,36 : 1** | ✅ corrige le défaut constaté (`docs/TEST_REPORT.md`, 2026-08-27) : en gris, cette paire ne faisait que 1,41 : 1 — c'est elle qui était « un gris voisin ». | Un losange contre un carré, et la pomme fait 0,72 case contre une case pleine pour la tête. |
| Pomme / CorpsSerpent | **1,07 : 1** | ⚠ faible en luminance seule — voir §1.5, point à surveiller. | Même losange isolé contre une chaîne continue de carrés : la silhouette entière diffère, pas seulement une case. |
| TeteSerpent / CorpsSerpent | **3,15 : 1** | ✅ meilleur que le gris d'origine (2,66 : 1). | La tête est aussi la case la plus grosse du groupe articulé — GDD §5.6 : jamais la couleur seule. |
| BordureAire / AireDeJeu | **8,06 : 1** | ✅ nettement au-dessus du gris d'origine (6,00 : 1) — « le mur qui tue » doit sauter aux yeux. | Trait continu qui referme tout le pourtour de la grille, contre un remplissage uni. |
| TexteHud / Fond | **16,4 : 1** | ✅ très large marge au-dessus du seuil texte (4,5 : 1). | — (texte, la forme est la police elle-même, voir §2). |
| TexteSecondaire / Fond | **6,13 : 1** | ✅ au-dessus du seuil texte, y compris si ce texte finit par chevaucher `AireDeJeu` (5,64 : 1 dans ce cas — voir `docs/gdd/grille.md` sur la marge basse absente). | Poids de police plus léger que `TexteHud` (§2) : la hiérarchie se lit aussi sans la couleur. |
| Pictogramme / CorpsSerpent | **3,72 : 1** | ✅ le cas qui compte le plus : ART §5.6 documente que le chevron d'un demi-tour tombe *toujours* sur le corps. Meilleur que le gris d'origine (3,04 : 1) — c'est un vrai correctif, pas un hasard. | Chevron plein barré d'un trait perpendiculaire : aucune autre forme du jeu n'a cette silhouette. |
| Pictogramme / AireDeJeu | **17,8 : 1** | ✅ cas où le pictogramme tombe en case libre (`RefuseeFilePleine`, direction non bloquée). | Idem. |

## 1.4 Contraintes techniques

- **Le projet est en espace colorimétrique Gamma** (`ProjectSettings.asset` :
  `m_ActiveColorSpace: 0`). Les codes hexa ci-dessus se posent **tels quels** (`#RRGGBB` → `/255` →
  `Color`) : aucune reconversion linéaire à faire à la main, exactement comme le fait déjà
  `PaletteProvisoire.Gris()`. Si le projet passe un jour en Linear, cette page devra être rouverte —
  jusque-là, ne pas « corriger » ces valeurs par anticipation.
- **Jamais de couleur en dur ailleurs que dans `UiPalette.cs`.** Un sprite, un shader, un composant
  `Image`/`Text` référencent le rôle nommé, jamais un `#RRGGBB` recopié.
- **`Fond` n'est jamais `#000000` pur** (§1.5, variante écartée) : un noir strict s'écrase sur les
  écrans bas de gamme et rend `TraitDeGrille` invisible chez une partie du public itch.
- **Contraste vérifié sur fond réel** : les ratios ci-dessus opposent chaque paire à ce qui
  l'entoure réellement en jeu (`AireDeJeu`, pas un damier neutre), conformément à `ART.md` §4.

## 1.5 Ce qui reste ouvert

- **Pomme / CorpsSerpent (1,07 : 1)** : la paire la plus faible de cette palette. Rouge et vert sont
  la paire de teintes la plus dure à distinguer pour une deutéranopie (la forme de daltonisme la plus
  fréquente) ; à luminance quasi identique, un joueur concerné perd une bonne partie du contraste
  chromatique en plus du contraste de clarté. La forme (losange isolé contre chaîne de carrés)
  couvre ce cas — c'est exactement pour ce genre de paire que `ART.md` §4 interdit la couleur seule
  — mais je n'ai **aucune vérification en conditions réelles** (simulateur de daltonisme ou retour
  d'un joueur concerné). À confirmer par `game-tester`, capture à l'appui, avant de considérer le
  sujet clos.
- **La marge basse absente** (`docs/gdd/grille.md`, constaté 2026-08-28) reste un arbitrage de mise
  en page, pas un sujet de palette — mais elle change le fond réel sous `TexteSecondaire`
  (`RappelDesCommandes`). Le ratio tient dans les deux cas (§1.3), donc cette palette n'a pas besoin
  d'attendre que l'arbitrage tranche.
