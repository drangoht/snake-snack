# ART — Direction artistique de Snake Snack

> Ce document accueille toute décision de direction artistique du projet. §1 (palette), §2
> (typographie) et §5 (retour d'une entrée refusée) sont tranchés. §3 (grille de sprites et échelle)
> reste **vide et structuré**, pour que la suite s'y range sans reprendre le plan à chaque fois — ne
> pas le remplir par anticipation.

## 1. Palette

**Parti pris** : un socle froid et presque noir (fond, aire, grille) sur lequel seules quatre
couleurs chaudes portent une information de gameplay — le mur en ambre, la pomme en rouge, le
serpent en vert, et un blanc pur réservé au signal qui doit toujours dominer (le pictogramme de
refus). Raisonnement complet, ratios de contraste chiffrés et variantes écartées :
[`art/palette.md`](art/palette.md).

Vit dans `Assets/Scripts/UI/UiPalette.cs` (renommage de `PaletteProvisoire.cs`, même 12 rôles, aucun
appelant à changer). **Jamais de couleur en dur ailleurs** dans le code ou les générateurs.

| Rôle | Couleur |
|---|---|
| `Fond` | `#0A0E13` |
| `AireDeJeu` | `#121821` |
| `TraitDeGrille` | `#1C2530` |
| `BordureAire` | `#E3A23A` |
| `CorpsSerpent` | `#4E9358` |
| `TeteSerpent` | `#D8F5C4` |
| `Pomme` | `#E5473B` |
| `Pictogramme` | `#FFFFFF` |
| `TexteHud` | `#E7EDF2` |
| `TexteSecondaire` | `#8792A0` |
| `VoileDePause` | `#000000` à 62 % |
| `TamponDeBuild` | `#FFFFFF` à 45 % |

⚠ Projet en espace colorimétrique **Gamma** (`ProjectSettings.asset`) : ces codes hexa se posent
tels quels (`/255` → `Color`), aucune reconversion linéaire à faire à la main.

## 2. Typographie

⛔ **BLOQUÉ au 2026-08-28 : Nunito n'existe qu'en fichier VARIABLE.** `ofl/nunito` de `google/fonts`
ne publie aucun `static/`, et l'amont porte `buildStatic: false` — la condition à laquelle
[`art/typographie.md`](art/typographie.md) §2.2 subordonnait cette famille n'est pas remplie. **Le
HUD reste sur la police intégrée** (`LegacyRuntime.ttf`) ; seules les **tailles** du §2.3 sont
câblées. Une famille de remplacement reste à trancher (`docs/TEST_REPORT.md`, BUG-001).

**Famille retenue : Nunito** (SIL OFL), deux graisses seulement — **SemiBold** pour le texte
secondaire, **ExtraBold** pour les titres et les nombres du HUD. Corps relevé de deux points par
rapport aux tailles actuelles du code (plancher : 18 px à la résolution de référence 1280×720),
contours allégés — une police d'affichage ronde a le trait plus fin qu'Arial au même calibre.
Raisonnement complet, tailles par texte, licence et procédure de récupération du `.ttf` statique :
[`art/typographie.md`](art/typographie.md).

⚠ Rappel du piège déjà payé (`docs/pitfalls/polices-texte.md`) : le repli d'Unity sur les glyphes
manquants n'existe QUE sur le bureau — un navigateur WebGL perd en silence tout caractère absent de
la police (flèches ← → ↑ ↓ en tête de liste). N'écrire que des caractères que la police contient, et
dessiner les symboles en sprite. Vérifier la table `cmap` du fichier réellement importé avant de
faire confiance, et vérifier dans le navigateur, pas au raisonnement.

## 3. Grille de sprites et échelle

<!-- À définir au fil des briefs (grille de cases, épaisseur de contour, corps de texte). Ce que le
     §5 pose déjà comme contrainte dure, réutilisable pour tout le reste : case de jeu = 44 px,
     aire de jeu 924×660 dans un cadre 1280×720, bandeau HUD ~60 px, marges latérales ~178 px. -->

## 4. Contraste et accessibilité — règles permanentes

- Jamais une information portée par la seule couleur : toujours doublée d'une différence de forme,
  de position ou de texte.
- Jamais de clignotement périodique en boucle sur une grande surface de l'écran. Une variation
  d'opacité déclenchée une fois (fondu entrée/sortie) est admise ; un stroboscope ne l'est pas.
- Tout sprite se valide sur le **fond réel du jeu**, jamais sur un damier neutre.

## 5. Briefs — un sujet, un fichier

> ⚠ **La numérotation `§5.x` est conservée dans le fichier du brief.** Le code et les tests
> renvoient à « ART §5.4 », « ART §5.7 » en une soixantaine d'endroits : ces renvois restent justes,
> ils désignent la sous-section correspondante de `art/retour-refus.md`. Ne pas renuméroter.

<!-- ⚠ INDEX. Un brief détaillé va dans docs/art/<sujet>.md : il n'intéresse que qui travaille sur
     CE sujet, alors que ce fichier-ci est relu avant chaque asset. Une ligne ici. -->

| Brief | Fichier | Statut |
|---|---|---|
| La palette (§1) | [`art/palette.md`](art/palette.md) | tranché le 2026-08-28 |
| La typographie (§2) | [`art/typographie.md`](art/typographie.md) | tranché le 2026-08-28 |
| Le retour d'une entrée refusée (GDD §3, §4.2) | [`art/retour-refus.md`](art/retour-refus.md) | tranché ; le démenti du 2026-08-27 (contraste du chevron) est levé par §1, à reconfirmer en jeu |

## 6. Historique des décisions

Décisions visuelles déjà tranchées et variantes écartées : [`art/historique.md`](art/historique.md).
Ne pas rouvrir un sujet sans élément neuf.
