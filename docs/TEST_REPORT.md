# Rapport de test — Snake Snack

Fichier **cumulatif**. Chaque session ajoute une section **en tête** (le plus récent en premier),
datée, avec la version testée.

> **Ne jamais réécrire une section passée.** Si une conclusion ancienne est réfutée, ajouter la
> réfutation et **marquer l'ancienne comme telle** : le raisonnement qui a mené à l'erreur a autant
> de valeur que la correction. C'est ce fichier qui évite de re-signaler un bug connu et de refaire
> un test déjà tranché.

## Session du 2026-08-30 — v0.2.0-3b2c0cc+ (build Windows)

**Portée** : les trois retours P2/P3 du juicy livrés ce jour (pop-in de la pomme §7, bond du
récapitulatif §8, inclinaison de la tête au virage §9) **et la dette de vérification du P1** — gulp,
pop de queue, flash de la case fautive, hitstop, micro-zoom, jamais constatés à l'écran depuis leur
livraison en 0.2.0. **Non testé** : le build web (aucune de ces animations n'y a été rejouée), le
pop du nouveau segment de queue (voir plus bas), la victoire.

**Méthode** : deux scripts jetables (non versés dans `tools/`) qui importent `piloter_jeu`, prennent
leurs propres captures en rafale (celles de `piloter_jeu` redonnent le focus et dorment 0,2 s :
trop lent pour suivre une enveloppe de 150 ms) et analysent chaque image en numpy — surface et boîte
englobante par teinte de `UiPalette`. La cadence est ralentie à 1,5–3 ticks/s **dans la copie de
build de `reglages.json` seulement**, puis restaurée. Pour §8, le record persistant a été abaissé à
0 dans le registre et **remis à 24 dans un `finally`**. Le second script embarque un **bot** qui lit
la position de la tête et de la pomme à l'écran et va la chercher : manger et mourir à un instant
précis est hors de portée d'un scénario de touches à l'aveugle.

**Le point de méthode** : chaque valeur attendue a été écrite **avant** la mesure, en pixels.

| Retour | Prédit | Mesuré | |
|---|---|---|---|
| §7 pop de la pomme | 0 → pic à ×1,08 → repos, en 150 ms | 0 → 30×30 → 28×28 px en ~130 ms | ✔ |
| §9 inclinaison, amplitude | boîte 42 → **44,4** px à 8° (carré *arrondi*) | 42 → 44 px | ✔ |
| §9 inclinaison, durée | plus rien de lisible après ~45 % du tick | bosse de 135 ms sur un tick de 333 ms | ✔ |
| §9 inclinaison, sens | virage à gauche : bord haut décalé à droite | +3 à +7,5 px à gauche, −3,5 à −9 px à droite | ✔ |
| §5 gulp | boîte 42×42 → ~37×48, **surface constante** | 38×46 px, surface 1 616 → 1 632 px | ✔ |
| §6 flash de la case fautive | une case s'allume, ~1 760 px | 1 763 px sur l'image du contact | ✔ |
| §6 hitstop | le flash précède l'écran de fin d'au moins une image | flash à t, récapitulatif encore vide ; voile 68 ms plus tard | ✔ |
| §6 micro-zoom | bordure de l'aire +1,7 % puis retour exact | 930 → **946** px (+1,72 %), retour à 930 | ✔ |
| §8 bond du record (bandeau) | surface ×1,69 au pic, 220 ms | 563 → 686 px (×1,22 à l'échantillon) | ✔ |
| §8 bond du récapitulatif | même bond rejoué une fois à l'ouverture | 1 182 → 1 570 → 1 182 px, trois fois de suite | ✔ |
| §5 bond du score | surface ×1,39 au pic, 160 ms | jamais capté près du pic | ⚠ |
| §5 pop du segment de queue | +1 645 px de corps étalés sur 140 ms | hausse étalée sur ~1 s : mesure non concluante | ⚠ |

**Les deux réserves, sans les habiller** :
- **Le bond du score** n'est pas prouvé *à l'écran*. Une capture coûte 50 à 85 ms ; une enveloppe de
  160 ms peut n'être échantillonnée qu'à son début et à sa fin. Ce qui est prouvé, c'est le
  mécanisme : le bond du record et celui du récapitulatif passent par **la même méthode
  `HudJeu.AppliquerBond`**, avec une ampleur plus grande et une durée plus longue, et tous deux ont
  été constatés.
- **Le pop du nouveau segment de queue** n'a pas pu être isolé : le compte de pixels du corps varie
  aussi avec le glissement, et sa hausse d'un segment s'étale sur bien plus que les 140 ms de
  l'enveloppe. Ni infirmé ni confirmé — à reprendre avec une mesure qui suit la seule case de la
  queue, ou à l'œil sur un ralenti.

**Aucun bug trouvé.** Deux pièges de *mesure* ont en revanche produit deux conclusions fausses avant
d'être corrigés (une boîte englobante étirée à 638 px par des pixels parasites, un bond de score noyé
dans la barre de titre de la fenêtre Windows) : versés dans `docs/pitfalls/tests-pilotage.md`.

## Session du 2026-08-28 (2) — v1.0-a735c8d+ (builds Windows ET web)

**Portée** : la typographie d'`ART.md` §2 réellement câblée (famille, graisses, tailles), et le
correctif de BUG-002. **Première session à vérifier dans un navigateur**, ce que la précédente
n'avait pas fait. **Non testé** : l'écran de mort et la victoire, la page itch, le tactile, une
fenêtre de navigateur étroite (les tailles du §2.3 sont calées sur 1280×720 et *rétrécissent* en
dessous — jamais mesuré ailleurs qu'en plein cadre).

**Méthode** : `tools/generer_polices.py`, puis `tools/build.ps1` et `tools/build.ps1 -Target web`.
Windows : `tools/piloter_jeu.py --touches "haut,echap,bas"`. Web : `tools/serve_web.py --port 8099`
et un script jetable qui lance Chrome sur l'URL, **clique dans le canevas**, injecte les mêmes
touches et capture la fenêtre. Captures : `docs/verif-police-attente.png`,
`docs/verif-police-pause.png`, `docs/verif-web-accents.png`.

**Le point de méthode qui compte** : la valeur attendue a été écrite avant la capture, en rangées de
pixels. Le canevas de 720 px se projette sur les rangées client 38..757 ; la boîte du rappel des
commandes, haute de 24 px et ancrée à 14 px du bas, a donc son bas en rangée **755** — prédiction :
« aucun pixel de texte sous 755, le jambage du `g` descend jusqu'à ~754 ». Mesuré : corps du texte
jusqu'à 749, jambages sur 750-753, **rien en 754**. Avant correctif, le texte s'arrêtait net en 756,
tronqué par le bord.

### Ce qui fonctionne
- **Nunito est à l'écran**, dans ses deux graisses : le bandeau (`Score` / `Record` / état) et les
  titres sont visiblement plus gras que le rappel des commandes et les sous-titres. La hiérarchie
  se lit **sans lire les mots**, ce qui est tout l'objet du §2.2.
- **BUG-002 corrigé** : jambages de `g`, `p`, `q` entiers, mesure ci-dessus à l'appui.
- **Les accents survivent au WebGL.** « Touche ignorée » s'affiche avec son `é` **dans Chrome**, sur
  le build web servi en local. C'est la seule vérification qui compte pour ce piège : sur le bureau,
  un glyphe manquant est masqué par le repli sur les polices système, en silence.
- La `cmap` du fichier **instancié** (pas de l'amont) porte les **125 caractères exigés** — ASCII
  32-126 plus les 30 accents français — sur 938 glyphes. Le générateur refuse d'écrire s'il en
  manque un.
- Import Unity propre : `TrueTypeFontImporter`, `includeFontData: 1`, 129,6 ko embarqués par
  graisse. **0 avertissement, 0 erreur** de compilation sur les deux builds. `dotnet test` : 157.
- Le scénario complet passe **à l'identique** dans le navigateur : démarrage sur la première
  direction, pause, ligne de refus. Aucune divergence bureau / web constatée.

### [BUG-001] — RÉSOLU (ouvert dans la session précédente)
Résolution retenue par l'auteur : **instancier** Nunito plutôt que changer de famille.
`tools/generer_polices.py` fige `wght=600` et `wght=800` avec `fontTools.varLib.instancer` et écrit
`Assets/Resources/Polices/`. Le piège documenté vise l'import d'un fichier *variable* dans Unity ;
une instance extraite est un `.ttf` statique ordinaire. Licence vérifiée avant de nommer les
fichiers : Nunito ne déclare **aucun Reserved Font Name**, le nom est donc conservé légalement
(`docs/CREDITS.md`). ⚠ Le constat de la session précédente **reste vrai et ne doit pas être effacé** :
`google/fonts` ne publie aucune graisse statique de Nunito, et l'amont ne les construit pas.

### [BUG-002] — RÉSOLU (ouvert dans la session précédente)
Ancrage du `RappelDesCommandes` remonté de 10 à 14 px : la boîte de 24 px tient entière, avec 2 px de
reste. ⚠ **Ce correctif ne referme pas l'arbitrage de fond** : il n'y a toujours **aucune marge sous
l'aire de jeu**, la ligne se lit toujours par-dessus la dernière rangée de cases et contre la bordure
ambre. `docs/gdd/grille.md` reste ouvert chez `game-designer` — ni la grille ni le gabarit du §3
n'ont été touchés.

### [BUG-003] Le tampon de build est à 14 px, sous le plancher de 18 px
Sévérité : Cosmétique
Contexte : `SceneBuilder.cs`, tampon de version en bas à droite, présent sur tous les écrans.
Observé / Attendu : il est construit à 14 px sur la police intégrée `LegacyRuntime.ttf`, alors que
`ART.md` §2.5 interdit « un texte en dessous de 18 px à la résolution de référence » et que tout le
reste du HUD est passé à Nunito.
Hypothèse : le tampon a été écrit avant le brief, et il est volontairement discret — le relever à
18 px et le passer en Nunito le rendrait plus voyant qu'on ne le souhaite peut-être.
Assigné à : `directeur-artistique` (soit le tampon sort du périmètre du §2 et le brief doit le dire,
soit il s'y plie). **Non modifié** : c'est un arbitrage de discrétion, pas une correction technique.

### Ressenti
**Nunito tient ce que le brief lui demandait.** À 18 px, le rappel des commandes reste confortable là
où la police intégrée devenait sèche ; à 56 px, « PAUSE » en ExtraBold est franchement plus présent
qu'avant, sans devenir enfantin. Le pari « ronde mais sobre » est gagné sur un écran de bureau.

**Ce dont je n'ai aucune preuve** : le rendu sur la fenêtre réduite d'une page itch. Toutes les
mesures de cette session sont prises en plein cadre 1280×720, et le §2.3 dit explicitement que ces
corps *rétrécissent* en dessous. Le plancher de 18 px a été posé pour ça, mais il n'a jamais été
regardé à l'échelle où il compte. C'est le premier test à faire après la mise en ligne de la 0.1.0.

**Le chevron de refus** reste ce que la session précédente en disait : une tache blanche de
12 × 24 px, lisible comme « quelque chose a été refusé », pas comme « cette direction-là ». Reporté
après la 0.1.0 par décision de l'auteur — aucun travail engagé dessus.

## Session du 2026-08-28 — v1.0-b5fa662+ (build Windows)

**Portée** : la palette d'`ART.md` §1 câblée dans `UiPalette.cs`, et les tailles de texte du §2.3
câblées dans `HudJeu`. Deux affirmations de doc à confirmer sur un vrai build : le pictogramme de
refus posé sur le corps du serpent (`art/retour-refus.md` §5.6) et la pomme rendue lisible par le
rouge (session du 2026-08-27 ci-dessous). **Non testé** : les graisses de police (Nunito bloquée,
voir plus bas), le build web et la page itch, l'écran de pause, l'écran de mort, la victoire.

**Méthode** : `tools/build.ps1`, puis `tools/piloter_jeu.py --lancer --attendre 6 --touches gauche
--pleine-resolution`. Captures : `docs/verif-palette-attente.png` (écran d'attente) et
`docs/verif-refus-chevron.png` (demi-tour refusé). Le pictogramme ne vit que **250 ms** : impossible
à saisir avec l'outil, qui attend 0,25 s après une touche puis capture. `dureeAffichageRefusSecondes`
et `plafondProlongationRefusSecondes` ont donc été portées à **6 s** dans
`Build/Windows/SnakeSnack_Data/StreamingAssets/reglages.json` (le fichier du build, pas celui
d'`Assets/`), et les valeurs par défaut remises après coup. Le pictogramme est alors capturé **sur
son plateau d'opacité**, à 1,0 — donc au meilleur de sa lisibilité, ce qui est le bon cadre pour
juger d'un contraste, pas de la fugacité.

**Le point de méthode qui compte** : les couleurs attendues et la position du chevron ont été
**écrites avant la première capture** (0,75 case à l'ouest du centre de la tête, donc sur le premier
segment de corps). Le chevron mesuré est centré en x = 617,5 px pour un centre de tête à 650 px et
une case de 45 px : 650 − 0,75 × 44 = 617. Ce n'est donc pas « un pictogramme est apparu quelque
part » qui est constaté.

### Ce qui fonctionne
- **Les 12 rôles sont à l'écran, et les ratios du brief se retrouvent à la mesure** (WCAG calculé sur
  les pixels de la capture, pas sur les codes hexa) : pomme/tête **3,35** (brief 3,36) ·
  tête/corps **3,17** (3,15) · bordure/aire **7,94** (8,06) · texteHud/fond **16,40** (16,40) ·
  texteSecondaire/fond **6,13** (6,13) · pictogramme/corps **3,81** (3,72) · pictogramme/aire
  **17,82** (17,80). Aucun écart au-delà de l'arrondi.
- **Le §5.6 est clos : le blanc pur suffit.** Le chevron tombe bien sur le corps vert, en
  `#FFFFFF` plein (154 pixels blancs purs mesurés, aucune atténuation), et il se voit immédiatement.
  Le repli « contour sombre » n'est pas nécessaire et n'a pas été appliqué.
- **La pomme ne se confond plus avec la tête** : de 1,41 : 1 en gris (session précédente) à
  **3,35 : 1**. C'est le seul objet rouge de l'écran ; l'œil va dessus sans le chercher.
- Le demi-tour tapé avant le départ **ne lance pas la partie** (GDD §4.1) : le bandeau affiche
  toujours « Une direction pour commencer » et le serpent n'a pas bougé d'un pixel entre les deux
  captures.
- Les rôles posés sur un `Image`/`Text` uGUI et sur le fond de caméra sortent **au pixel exact** ;
  ceux posés sur un `SpriteRenderer` sortent 1 à 2 unités plus sombres sur R et G (reporté dans
  `art/palette.md` §1.4 — sous 1 %, sans conséquence).
- Build sans erreur ni avertissement nouveau ; `dotnet test` : **157 verts**.

### [BUG-001] Nunito n'existe qu'en fichier variable : la typographie ne peut pas être câblée
Sévérité : Majeur (bloque la moitié d'`ART.md` §2)
Contexte : import de police, avant tout téléchargement.
Reproduction : `GET https://api.github.com/repos/google/fonts/contents/ofl/nunito` — aucun dossier
`static/`, seulement `Nunito[wght].ttf` et `Nunito-Italic[wght].ttf`. Et
`ofl/nunito/upstream_info.md` note `buildStatic: false` dans le `config.yaml` amont : les statiques
ne sont pas en retard de publication, elles ne sont **jamais construites**.
Observé / Attendu : attendu `static/Nunito-SemiBold.ttf` et `static/Nunito-ExtraBold.ttf` ; obtenu
deux fichiers variables. C'est la condition même à laquelle le brief §2.2 subordonnait Nunito, et
exactement ce qui avait fait écarter Fredoka.
Hypothèse : aucune — l'amont le déclare.
Conséquence : le HUD reste sur `LegacyRuntime.ttf`. **Rien n'a été improvisé** (ni instance du
variable, ni sous-ensemble de poids). Les **tailles** du §2.3 sont câblées, les **graisses** non.
La table `cmap` n'a pas été sondée : aucun fichier n'a été récupéré.
Assigné à : `directeur-artistique` (choisir une famille dont `static/` est listé, ou trancher
explicitement l'usage d'une instance statique extraite du variable).

### [BUG-002] Les jambages du rappel des commandes sont coupés par le bord bas
Sévérité : Cosmétique
Contexte : écran de jeu, en permanence. Déjà décrit dans `docs/gdd/grille.md` (marge basse absente) —
cette session le **mesure** au lieu de le constater.
Reproduction : lancer le jeu, regarder « diriger », « Echap », « pause » en bas de l'écran.
Observé / Attendu : la boîte du texte est ancrée à 10 px du bas et haute de 24 px, son bas tombe donc
**2 px sous le bord de l'écran** ; les jambages de `g`, `p`, `q` sont tronqués, et la ligne chevauche
la bordure ambre de l'aire, qui occupe la toute dernière rangée de pixels.
Hypothèse : ce n'est pas le corps du texte qui coupe — la coupe existait à 15 px et existe à 18 px
depuis `ART.md` §2.3 — c'est l'ancrage, et la marge basse que la mise en page n'a pas réservée.
Assigné à : `game-designer` (l'arbitrage de `docs/gdd/grille.md` est toujours ouvert : bandeau bas,
rappel en marge latérale, ou acceptation en remontant le texte).

### Ressenti
**La pomme.** Sa taille (0,72 case) n'est **plus** un problème maintenant que la couleur porte :
le losange rouge est le seul objet chaud sur un fond froid, on le trouve sans balayer la grille. Ce
qui gênait était la valeur voisine de la tête, pas les dimensions. Je ne recommande pas de
l'agrandir : la grossir la rapprocherait d'une case pleine et affaiblirait la différence de
silhouette, qui est justement ce qui la sauve en daltonisme.

**Le pictogramme de refus.** La couleur est réglée, la **forme** ne l'est pas. À une demi-case
(`Plateau.TailleMaximalePictogramme`), le chevron barré occupe 12 × 24 px à l'écran : il se lit comme
une **tache blanche** apparue sur le serpent, pas comme un chevron barré. Le signal « quelque chose a
été refusé » passe donc, mais pas le « c'est *cette* direction ». Sur 250 ms de vie réelle, je doute
qu'un joueur distingue jamais le dessin. C'est une question de forme et d'échelle, pas de palette :
je ne l'ai pas touchée.

**Daltonisme (§1.5 de `art/palette.md`).** Simulation deutéranope (Viénot 1999) appliquée à la
capture : pomme, corps du serpent et bordure ambre virent tous à des olives proches — la teinte ne
sépare plus rien. Restent la forme (losange isolé contre chaîne de carrés contre trait continu) et la
clarté de la tête, et elles suffisent à jouer. Une matrice sur une capture n'est pas un joueur
concerné : le point reste ouvert.

## Session du 2026-08-27 — v1.0-80a7645+ (build Windows)

**Portée** : la pomme (GDD §4.4) — apparition avant le premier appui, tirage semé, croissance à la
bouchée, remplacement dans le même tick. **Non testé en jeu** : la victoire (grille pleine,
inatteignable à la main — couverte par `dotnet test` seul), la mort par morsure, le build web. Le
score et le record (§4.5) n'existent pas encore.

**Méthode** : `tools/build.ps1`, puis `tools/piloter_jeu.py --lancer --maintenir droite`. Graine
**543** et cadence **1 tick/s** posées dans
`Build/Windows/SnakeSnack_Data/StreamingAssets/reglages.json` (le fichier du build, pas celui
d'`Assets/`) — les réglages par défaut ont été remis après coup.

**Le point de méthode qui compte** : les deux premières pommes de la graine 543 ont été **calculées
hors du jeu**, par une réimplémentation de SplitMix64 et du parcours d'énumération, *avant* de
lancer le jeu : `(13, 7)` puis `(18, 10)`. La capture correspond exactement. Ce n'est donc pas
« une pomme est apparue quelque part » qui est constaté, mais la suite entière — générateur, ordre
de parcours, tirage sur l'état final du tick.

### Ce qui fonctionne
- La pomme est **posée avant le premier appui**, serpent encore immobile (§4.4). Constaté à l'écran.
- Elle se distingue du serpent **par la forme** : losange contre carrés, et plus petite que la case.
- Manger allonge le serpent **à la bouchée** : 3 segments avant, 4 après, tête à la bonne case.
- La pomme suivante apparaît **dans le tick même**, à la case prédite. Aucune image sans pomme.
- Le bandeau repasse de « Une direction pour commencer » à vide au démarrage de la partie.

### Ressenti
Le losange est lisible mais **petit** (0,72 case) et d'un gris voisin de celui de la tête. Rien de
gênant à 21 × 15 sur un écran de bureau ; à revoir quand la palette de `docs/ART.md` §1 existera, et
à re-regarder sur la page itch, où l'image est plus petite.

<!-- Gabarit d'une session, à copier en TÊTE de fichier :

## Session du AAAA-MM-JJ — v<version>-<sha>

**Portée** : ce qui a été testé, et ce qui ne l'a pas été.
**Méthode** : commandes utilisées (tools/piloter_jeu.py …), options, graine.

### Ce qui fonctionne
- …

### [BUG-XXX] Titre court
Sévérité : Bloquant / Majeur / Mineur / Cosmétique
Contexte : (écran, version, options)
Reproduction : (étapes précises, graine si applicable)
Observé / Attendu :
Hypothèse : (cause probable si évidente)
Assigné à : developpeur | game-designer

### Ressenti
Ce que la mesure ne peut pas dire. C'est la seule source sur ce point, et elle a déjà eu raison
contre le banc.

-->
