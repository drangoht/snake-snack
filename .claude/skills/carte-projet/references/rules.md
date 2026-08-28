# Carte — `Assets/Scripts/Rules/` (logique pure testée)

## §Rules — `Assets/Scripts/Rules/`

Logique pure, sans dépendance moteur, **testée**. C'est ici que vit toute règle chiffrée du jeu.

<!-- Lister ici chaque classe et ce qu'elle décide. Une ligne par règle. -->

| Fichier | Ce qu'il décide | Tests |
|---|---|---|
| `Direction.cs` | Enum `Direction` (Nord/Est/Sud/Ouest) + `Directions` : opposé, **demi-tour**, pas d'une case. ⚠ Nord = Y croissant | via les 3 autres |
| `Case.cs` | Coordonnée entière de grille. ⚠ Existe **parce que `Vector2Int` vient d'UnityEngine** | `GrilleTests` |
| `Cadence.cs` | Pas de temps (GDD §4.1) : 8 ticks/s par défaut, surchargeable ; fourchette conseillée 6–10 ; `CadenceEffective` **ignore la longueur** (cadence constante, §7) ; découpe temps → ticks avec report du reliquat, **plafonnée à 1 tick par image** (retard jeté, §4.1) | `tests/CadenceTests.cs` |
| `FileEntrees.cs` | File d'entrées (GDD §4.2) : FIFO profondeur 2, une entrée par tick, **demi-tour validé au tick** contre la direction appliquée au tick précédent, débordement ignoré, doublon refusé, purge pause/mort. ⚠ **Seule classe à état** de `Rules/` | `tests/FileEntreesTests.cs` |
| `Grille.cs` | Aire de jeu (GDD §4.3) : 21 × 15 réglables, **dimensions paires refusées** (case centrale exacte), pose de départ (10,7)/(9,7)/(8,7) vers l'est, `EstHorsGrille` = le mur mortel du §2 | `tests/GrilleTests.cs` |
| `Serpent.cs` | Le corps et **la résolution d'un tick** (GDD §4.4) : mur → croissance → morsure → déplacement, dans cet ordre. ⚠ La queue est exclue des obstacles **seulement si on ne mange pas** ; un pas mortel ne bouge pas le serpent et ne mange pas | `tests/SerpentTests.cs` |
| `Pomme.cs` | Où poser la pomme (GDD §4.4) : cases libres, **tirage par énumération** (X croissant dans Y croissant), `GrillePleine` = la victoire. ⚠ Répond « où » et « combien », jamais « quand » | `tests/PommeTests.cs` |
| `Aleatoire.cs` | Le générateur semé des pommes — **SplitMix64 écrit ici** (§4.4). ⚠ Ni `UnityEngine.Random` (état global) ni `System.Random` (suite non stable d'un runtime à l'autre) | `tests/AleatoireTests.cs` |
| `Plateau.cs` | Mise en page de l'aire (GDD §4.3) : taille de case déduite du cadre 1280×720 bandeau déduit, centre de chaque case, ancrage du pictogramme de refus. ⚠ Unité = **pixel du cadre de référence** | `tests/PlateauTests.cs` |
| `Demarrage.cs` | Le départ à l'arrêt (§4.1) : quelle première direction lance la partie, laquelle est refusée | `tests/DemarrageTests.cs` |
| `RetourRefus.cs` | Registre visuel d'un refus (ART §5) : pictogramme, texte de pause, ou silence ; échéances et opacité du retour | `tests/RetourRefusTests.cs` |
| `ReglagesJeu.cs` | Schéma du JSON de tuning + `Valider()`. Voir `data-outils.md` | `tests/ReglagesJeuTests.cs` |
| `Score.cs` | Score et record (GDD §4.5) : +1 par pomme, **record qui monte pendant la partie**, prédicat « record battu » jugé contre le record d'AVANT la partie (égaler ne bat pas), normalisation d'un record abîmé, et `longueur == 3 + score` | `tests/ScoreTests.cs` |
| `EXEMPLE_Regle.cs` | Gabarit — à supprimer quand `Rules/` sera bien peuplé. |  |

⚠ Le refus d'une entrée (`ResultatEmpilage`, `ResultatTick.DemiTourRefuse`) est **rendu à
l'appelant**, jamais avalé : le §3 exige que le refus se voie à l'écran. Le câblage moteur de ce
retour est fait (`JeuSnake.SignalerRefus` → `VuePlateau.AfficherRefus`), tout comme la pause à la
perte de focus et le démarrage à l'arrêt.

⚠ **Le seul aléa du jeu passe par `Aleatoire`**, et l'instance de la partie ne sert qu'à la pomme —
tout autre besoin prend la sienne (`JeuSnake._grainesDeSession`). Pourquoi c'est piégeux :
`docs/pitfalls/logique-pure-tests.md`.

⚠ Avant d'ajouter un fichier ici, lire `docs/pitfalls/logique-pure-tests.md` : le glob du csproj et
le contexte de compilation y réservent deux surprises silencieuses.
