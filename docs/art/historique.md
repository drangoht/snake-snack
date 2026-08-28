# Historique des décisions visuelles

Une entrée par brief tranché, dans l'ordre chronologique. Garder les variantes écartées et leur
raison plutôt que les effacer — même convention que le GDD. On ne l'ouvre que pour rouvrir une
décision.

<!-- Une entrée par brief tranché, dans l'ordre chronologique. Garder les variantes écartées et
     leur raison plutôt que les effacer — voir la convention du GDD. -->

- **2026-08-28 — Typographie, méthode d'obtention (§2.2). Famille CONSERVÉE, obtention changée.**
  Le pari du brief est perdu : Nunito **n'a pas** de graisses statiques chez `google/fonts` (aucun
  `static/`, amont en `buildStatic: false`), exactement le motif qui avait fait écarter Fredoka —
  appliqué à la remplaçante. Deux issues étaient possibles : changer encore de famille, ou instancier
  le variable. **L'auteur a tranché pour l'instanciation** : le piège documenté vise l'import d'un
  fichier variable dans Unity, pas une instance extraite, qui est un `.ttf` statique ordinaire. Le
  raisonnement du brief (une ronde sobre, ni géométrique ni bulleuse) n'avait aucune raison d'être
  refait pour un obstacle d'outillage. Changer de famille aurait été **écarté** pour cela : on aurait
  rejoué un choix esthétique validé pour contourner un problème de distribution.
  Conséquence versée au dépôt : `tools/generer_polices.py` (générateur versionné, sha256 de l'amont
  épinglé, contrôle `cmap` bloquant), `Assets/Resources/Polices/` (deux `.ttf` + `OFL.txt`),
  `docs/CREDITS.md`. Nunito ne déclarant **aucun Reserved Font Name**, le nom est conservé.
  Vérifié en jeu **et dans le navigateur** (`docs/TEST_REPORT.md`, 2026-08-28).

- **2026-08-28 — Typographie (§2).** Famille **Nunito** (SIL OFL) retenue, deux graisses (SemiBold /
  ExtraBold), corps relevé de deux points, plancher 18 px. Variante **Fredoka** écartée : la famille
  n'existe plus qu'en fichier variable dans le dépôt `google/fonts` au moment de l'écriture de ce
  brief (`docs/pitfalls/polices-texte.md` documente déjà ce piège pour cette famille précise) — un
  poids fixe importable sans risque n'était pas garanti disponible. Variante **Baloo 2** écartée :
  dessin trop rond et bulleux, lu comme un jeu pour enfants plutôt que « casual » — et son trait, plus
  fin encore que celui de Nunito à poids égal, aurait demandé plus de compensation (graisse, taille)
  pour un gain de personnalité marginal. Détail : [`art/typographie.md`](art/typographie.md).

- **2026-08-28 — Palette (§1).** Recommandation retenue : socle froid quasi noir (fond/aire/grille),
  quatre couleurs chaudes portant chacune une information de gameplay (mur ambre, pomme rouge,
  serpent vert, pictogramme blanc pur). Corrige le défaut constaté le 2026-08-27
  (`docs/TEST_REPORT.md`) : le ratio de contraste pomme/tête passe de 1,41:1 (gris) à 3,36:1 (couleur).
  Variante **« Néon sur noir pur »** écartée (fond `#000000`, serpent cyan, pomme magenta) : un noir
  strict écrase `TraitDeGrille` sur les écrans bas de gamme, l'esthétique néon/sci-fi contredit le
  pitch « canonique, sans twist » du GDD §1, et la paire cyan/magenta aurait pu reproduire le même
  défaut de proximité en luminance que le gris d'origine, sans qu'aucun calcul ne l'ait vérifié.
  Variante **« Vert monochrome, terminal rétro »** écartée (toutes les couleurs de gameplay dans une
  seule teinte verte, à la CRT phosphore) : renonce à l'intérêt même d'avoir une palette — chaque
  paire redevient distinguée par la seule luminance, exactement le problème qui vient d'être payé en
  niveaux de gris ; et la connotation « terminal hacker » ne correspond pas au cadrage « snack »,
  casual, du titre. Détail, ratios chiffrés et point resté ouvert (pomme/corps en daltonisme) :
  [`art/palette.md`](art/palette.md).

- **2026-08-28 — Score et record du bandeau (GDD §4.5) : placement posé PAR DÉFAUT, brief toujours
  ouvert.** Aucun brief n'existait et le lot ne pouvait pas attendre : score à gauche du bandeau en
  texte principal, record à droite en texte secondaire, récapitulatif entre le titre et la relance
  sur l'écran de fin. Tout est en gris (§5.6). ⚠ **Ce n'est pas une décision artistique tranchée** —
  c'est un développeur qui a choisi faute de directeur artistique, exactement ce que le §1 vide
  finit toujours par produire. À reprendre avec la palette et la typographie.
  ⚠ **Mise à jour 2026-08-28** : §1 et §2 sont maintenant tranchés (entrées ci-dessus). Le
  câblage de `HudJeu.cs` sur `UiPalette.TexteHud` / `TexteSecondaire` reste à faire — ce placement
  par défaut n'est toujours pas relu par une décision artistique tant que ce câblage n'est pas fait.

- **2026-08-27 — Retour d'une entrée refusée (§5).** Variante A **retenue et validée par
  l'auteur** (pictogramme ancré à la tête pour `DemiTourRefuse`/`RefuseeFilePleine`, texte sur
  l'écran pause pour
  `RefuseeJeuEnPause`, aucun retour pour `RefuseeDoublon`). Variante B (contour de case) écartée
  pour perte d'information directionnelle. Variante C (un seul retour incluant le doublon) écartée
  pour risque de bruit.
