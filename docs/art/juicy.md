# Brief — Le juicy (retours de jeu)

Sorti de `docs/ART.md` §5. **Arbitré par l'auteur le 2026-08-28** : les recommandations du directeur
artistique sont retenues telles quelles (§12). Aucune mécanique n'est touchée — ce brief change ce
que le joueur *ressent* d'un événement déjà décidé par le GDD, jamais ce que l'événement *fait*.

## 1. Diagnostic — pourquoi le jeu paraît sec

Lu dans `Assets/Scripts/Gameplay/VuePlateau.cs` et `JeuSnake.cs` :

- **Le serpent téléporte.** `DessinerSerpent` écrit `localPosition` à chaque tick : chaque segment
  saute d'une case à l'autre toutes les 125 ms, sans image intermédiaire. Un mouvement qui ne se voit
  jamais se dérouler ne peut pas être senti, aussi juste soit la règle derrière.
- **Manger ne fait rien voir.** Le nouveau segment apparaît à sa taille finale dès le tick où il
  existe. La seule action positive du jeu — celle qui porte toute la boucle du GDD §2 — n'a aucune
  contrepartie visuelle au-delà du chiffre qui change.
- **Mourir ne fait rien voir.** Rien ne montre *où* le contact a eu lieu, alors que le GDD fait
  reposer la valeur de la relance sur « la mort est toujours attribuable à une décision ».
  Aujourd'hui cette attribution passe par la mémoire du joueur, jamais par un signal du jeu.
- **Le menu, lui, est déjà juicy** (`docs/art/menu.md`) : fondus, cascade, curseur qui glisse. Le
  savoir-faire existe, il s'arrête à la porte de la partie.

## 2. Principe technique commun

- **Couche présentation uniquement.** Tout vit dans `Gameplay/` ou `UI/`, jamais dans `Rules/` : la
  position logique, le tick et la collision restent inchangés au pixel et à la milliseconde près.
- **Temps** : ce qui suit le déroulement du jeu (mouvement, bouchée, virage) utilise `Time.deltaTime`
  et doit s'arrêter avec la pause. Ce qui reste visible à la pause ou à la mort reste sur
  `Time.unscaledTimeAsDouble`, comme `EtatRetourAEcheance`.
- **Réutiliser le pool.** Aucun `Instantiate`/`Destroy` par tick : les mêmes segments, la même pomme
  et les mêmes `Text` reçoivent une position, une échelle ou une rotation qui varie.
- **Utilitaire d'easing** : une classe pure `Rules/Rebond.cs`, sur le modèle d'`EtatRetourAEcheance`
  — un `Progres(debut, duree, maintenant)` rendant une courbe avec léger dépassement, testable sans
  moteur. Évite la même formule recopiée dans `VuePlateau` et `HudJeu`.
- **Tuning sans recompiler** : chaque durée et amplitude ci-dessous est candidate à
  `Assets/StreamingAssets/reglages.json` (champs dans `Rules/ReglagesJeu.cs`, lecture par
  `Core/ChargeurReglages.cs`), au même titre que les durées du retour de refus — aucune n'a été
  essayée en jeu, toutes sont « au jugé ».
  ⚠ **Ce tuning ne vaut que sur le build bureau** : en WebGL, `ChargeurReglages` rend les valeurs par
  défaut sans rien lire (`streamingAssetsPath` y est une URL). Régler le juicy se fait donc sur
  Windows, et ce qu'on y fige part tel quel sur itch.

## 3. Priorités

| # | Retour | Pourquoi il prime | État |
|---|---|---|---|
| P1 | Interpolation du mouvement (§4) | Le socle : sans lui, tout le reste s'anime sur un jeu qui téléporte. | livré 0.2.0, vu |
| P1 | Mort — case fautive + hitstop + micro-zoom (§6) | Sert directement « la mort s'impute à une décision ». | livré 0.2.0, mesuré |
| P1 | Bouchée — gulp + pop de queue + bond du score (§5) | La seule action positive ; c'est elle qui donne envie de continuer. | livré 0.2.0, gulp mesuré |
| P2 | Apparition de la pomme (§7) | Peu coûteux, gain net, cohérent avec P1. | livré, mesuré |
| P2 | Record battu (§8) | Rare mais gratuit ; seul moment de fierté du jeu. | livré, mesuré |
| P3 | Inclinaison de la tête au virage (§9) | Agréable, non indispensable — le tracé du corps suffit à lire un virage. | livré, mesuré |
| — | Écarté (§10) | Coût ou risque de lisibilité supérieur au gain. | — |

**Mesuré** = constaté à l'écran sur le jeu qui tourne, chiffre attendu écrit avant la capture
(`docs/TEST_REPORT.md`, session du 2026-08-30). Deux réserves y sont nommées : le **pop du nouveau
segment de queue** n'a pas pu être isolé du compte de pixels du corps, et le **bond du score** n'a
jamais été échantillonné près de son pic — son mécanisme est celui, prouvé, du bond du record.

## 4. Le socle — interpolation du mouvement

Chaque segment lerpe, à chaque image, de sa case précédente vers sa case cible, sur la durée du tick
courant (`Cadence.DureeTickSecondes`, jamais une constante recopiée : si la cadence est retunée,
l'interpolation suit). **Linéaire, sans easing** — le GDD §4.1 fixe une cadence constante, un easing
donnerait l'impression fausse d'une accélération à chaque case.

- Un segment qui vient d'apparaître ne s'interpole pas depuis une position inexistante : il est posé
  sur sa case à échelle 0, et c'est le pop-in de §5 qui le fait grandir.
- L'interpolation se fige net à la mort ou à l'entrée en pause — pas de glissement fantôme.
- **Coût WebGL** : un `Vector3.Lerp` par segment et par image, au pire ~300, en pratique quelques
  dizaines. Négligeable, aucun shader, aucun draw call de plus.

## 5. Bouchée (pomme mangée)

| Retour | Durée | Amplitude | Coût |
|---|---|---|---|
| Tête : compression perpendiculaire à la marche (« gulp ») | 90 ms, ease-out | échelle 1,15 / 0,85 → 1,0 | nul (transform) |
| Nouveau segment de queue : apparition en pop | 140 ms | échelle 0 → 1,12 → 1,0 | nul |
| Score du bandeau : bond d'échelle | 160 ms | 1,0 → 1,18 → 1,0 | nul (UI) |

Aucune couleur ni sprite nouveau : uniquement des transforms sur des objets déjà poolés.
⚠ La durée la plus longue (160 ms) dépasse un tick (125 ms) : deux bouchées rapprochées relancent
chacune leur propre enveloppe, sans que la première soit coupée en silence.

## 6. Mort

| Retour | Durée | Amplitude | Coût |
|---|---|---|---|
| La case fautive (mur touché ou segment mordu) flashe | 220 ms, un aller-retour | opacité 0 → 1 → 0, couleur `Pictogramme` | nul |
| Hitstop avant le voile et le texte de fin | 70–90 ms | — (délai) | nul |
| Micro-zoom caméra — un impact, pas un shake | 150 ms | `orthographicSize` 360 → 354 → 360 (≈ 1,7 %) | nul |

- Le blanc réutilise `UiPalette.Pictogramme`, déjà réservé au signal qui doit dominer (ART §1.2).
- Pendant le hitstop, **aucune entrée n'est lue**, Espace compris : un joueur qui martèle la relance
  juste avant sa mort ne doit pas redémarrer pendant que l'écran retient l'image de l'impact.
- **Aucun déplacement latéral de caméra** (§10) : seule l'échelle respire une fois. Un shake
  déplacerait les cases au moment précis où le joueur doit voir laquelle l'a tué.

## 7. Apparition de la pomme

Pop-in à l'apparition (nouvelle partie ou après une bouchée) : 150 ms, échelle 0 → 1,08 → 1,0. P2.

⚠ **La respiration idle est écartée** (arbitrage du 2026-08-28) : un mouvement continu sous 8 ticks/s
risquait de détourner l'œil de la tête, et aucun retour en jeu ne venait le démentir. À rouvrir
seulement si `game-tester` constate que la pomme se cherche trop longtemps du regard.

## 8. Record battu

Au tick où `Score.RecordBattu` bascule à vrai : le nombre `Record` du bandeau fait un bond d'échelle
(1,0 → 1,3 → 1,0 sur 220 ms), **sans changement de couleur**. Le même bond rejoue une fois sur le
récapitulatif « Nouveau record » à l'ouverture de l'écran de fin.

⚠ Ne pas emprunter `Pictogramme` (réservé au refus) ni `Pomme` (réservée à la nourriture) : la règle
« une couleur = un rôle » (`docs/art/palette.md` §1.2) est un acquis, pas un obstacle à contourner.

## 9. Virage (direction acceptée)

La tête s'incline de ±8° dans le sens du virage, et revient à 0° sur la durée du tick suivant
(ease-out). Purement visuel — **ne touche pas** `Plateau.AncrageRefus`, qui continue de positionner
le chevron par rapport à la case, jamais par rapport à cette rotation. P3 : un virage réussi est déjà
lisible par le tracé du corps.

## 10. Ce qui est écarté

- **Traînée/rémanence sur un virage** — coût de pool pour un gain marginal : à 8 ticks/s le joueur a
  le temps de lire un virage sans aide.
- **Shake de caméra** — contredit « lisibilité avant le style » : déplace les cases au moment où leur
  position exacte compte le plus.
- **Ralenti (`Time.timeScale`) à la mort** — interagirait avec l'`unscaledTime` déjà utilisé par la
  pause et le refus ; le délai d'affichage de §6 obtient le même effet sans ce risque.
- **Respiration idle de la pomme** (§7) et **bond de clic dans le menu** — le menu a déjà son langage
  d'animation tranché (`docs/art/menu.md`).

## 11. Interdits

- Jamais de couleur en dur, ni de rôle de couleur nouveau pour un effet : réutiliser un rôle de
  `UiPalette` selon ce qu'il signifie déjà (§6, §8).
- Jamais de particule ni de post-processing : tout se fait en transform et en `Color.a` sur les
  composants déjà présents.
- Jamais de shake latéral de caméra, jamais de `Time.timeScale` modifié (§10).
- Jamais d'`Instantiate`/`Destroy` par tick — tout vit sur des objets déjà poolés.
- Jamais une animation qui modifie une valeur *lue par `Rules/`* (collision, ancrage du chevron) : la
  présentation observe l'état du jeu, elle ne le nourrit jamais.
- Jamais un retour qui clignote en boucle : une seule enveloppe par déclenchement, comme le retour de
  refus (`docs/art/retour-refus.md` §5.5).
- Jamais bloquer une entrée légitime plus longtemps que le hitstop annoncé (§6) : un délai qui
  s'allonge finirait par se lire comme un jeu qui ne répond plus.

## 12. Arbitrage de l'auteur, et ce qu'il est devenu

**2026-08-28** — recommandations du directeur artistique retenues sans modification : livrer **P1
d'abord** avec le rounding de `cartoon.md` §3.1, mesurer, puis décider du reste ; **micro-zoom
gardé** (§6) ; **respiration de la pomme écartée** (§7). Nom `Rules/Rebond.cs` (§2) validé.
**2026-08-30** — P2 et P3 livrés à leur tour, sur décision de l'auteur de finir le juicy avant les
assets et le son. **Ce brief est clos** : ce qui reste ouvert est nommé sous le tableau du §3.
