# ART — Direction artistique de Snake Snack

> Ce document accueille toute décision de direction artistique du projet. Il démarre avec un seul
> brief rempli (§5 — le retour d'une entrée refusée) parce que c'est le seul trou qui bloquait
> aujourd'hui l'UI. Les autres sections sont posées **vides et structurées**, pour que la suite
> s'y range sans reprendre le plan à chaque fois — ne pas les remplir par anticipation.

## 1. Palette

<!-- À définir. Vit dans Assets/Scripts/UI/UiPalette.cs (ou équivalent) une fois posée — jamais de
     couleur en dur ailleurs dans le code ou les générateurs. Tant que cette section est vide,
     aucun brief de ce document ne doit référencer un code hexa : travailler en contraste de forme
     et en valeurs relatives (clair/sombre), voir §5. -->

## 2. Typographie

<!-- À définir. Rappel du piège déjà payé (docs/PITFALLS_UNITY.md) : le repli d'Unity sur les
     glyphes manquants n'existe QUE sur le bureau — un navigateur WebGL perd en silence tout
     caractère absent de la police (flèches ← → ↑ ↓ en tête de liste). Conséquence pour tout texte
     du jeu : n'écrire que des caractères que la police contient, et dessiner les symboles en
     sprite. Vérifier la table `cmap` avant de faire confiance, et vérifier dans le navigateur, pas
     au raisonnement. -->

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

## 5. Le retour d'une entrée refusée (GDD §3, §4.2)

### 5.1 Le problème

Quatre motifs de refus existent dans `Assets/Scripts/Rules/FileEntrees.cs`
(`ResultatEmpilage`) :

| Motif | Ce qu'il signifie | Fréquence attendue en jeu |
|---|---|---|
| `DemiTourRefuse` | Le joueur a demandé l'inverse de sa direction actuelle. | Rare une fois la règle apprise — mais possible en panique, dans un virage serré. |
| `RefuseeJeuEnPause` | Direction tapée pendant la pause. | Rare, et sans pression de temps : le jeu est figé. |
| `RefuseeFilePleine` | Deux virages déjà en attente, le troisième est ignoré. | Occasionnelle, en rafale de martelage — c'est le cas de bruit signalé par le game-designer. |
| `RefuseeDoublon` | La direction demandée est déjà celle qui va s'appliquer. | **Le plus fréquent des quatre** — le joueur retape par réflexe le cap qu'il suit déjà. ⚠ La fréquence exacte dépend du câblage : lu en `wasPressedThisFrame`, maintenir une touche ne produit **qu'un seul** événement, pas un par image. <!-- à observer une fois câblé --> |

Le GDD (§3) impose explicitement un retour visible pour `DemiTourRefuse` et
`RefuseeJeuEnPause`. Il laisse `RefuseeFilePleine` « à confirmer au ressenti » (§4.2) et ne dit
rien de `RefuseeDoublon` — c'est le trou que ce brief comble.

### 5.2 Décision : un seul retour, ou des retours distincts ?

**Distincts, en deux registres — pas un seul retour uniforme, pas quatre retours indépendants.**

Le critère qui tranche n'est pas la sévérité de la règle, c'est le **risque de bruit** signalé par
le game-designer : un retour qui se déclenche à chaque appui refusé, en martelage, cesse d'être lu
comme une règle et devient un défaut visuel. Deux motifs sont à risque de martelage réel
(`DemiTourRefuse` et `RefuseeFilePleine` surviennent pendant que le jeu tique, sous pression) ; un
troisième (`RefuseeJeuEnPause`) survient hors de toute pression de temps, la simulation étant
figée ; le dernier (`RefuseeDoublon`) n'est pas un événement à signaler du tout — voir §5.3.

**Traitement retenu :**

| Motif | Reçoit un retour ? | Registre |
|---|---|---|
| `DemiTourRefuse` | Oui | Pictogramme ancré à la tête (§5.4) |
| `RefuseeFilePleine` | Oui, même pictogramme | Pictogramme ancré à la tête (§5.4) |
| `RefuseeJeuEnPause` | Oui | Texte sur l'écran pause déjà affiché (§5.4) |
| `RefuseeDoublon` | **Non** | — (§5.3) |

`DemiTourRefuse` et `RefuseeFilePleine` partagent le **même** pictogramme plutôt que deux dessins
distincts : à 125 ms par tick, le temps disponible ne permet pas d'enseigner une nuance entre
« vous avez fait demi-tour » et « vous avez tapé un troisième virage de trop » — ce qui compte à cet
instant, c'est que le joueur voie que **son appui n'a pas compté**, pas pourquoi précisément.
Réutiliser un seul signe réduit aussi le nombre d'assets et le rend plus vite reconnaissable :
le joueur n'a qu'une seule forme à apprendre pour toute la classe « ça n'a pas pris ».

### 5.3 Le cas `RefuseeDoublon` — pas de retour, et pourquoi

`RefuseeDoublon` se déclenche quand la direction demandée est déjà la dernière direction connue
(courante, ou dernière en file). C'est l'état **le plus courant** d'une partie : un joueur qui va
tout droit et retape sa direction par réflexe, ou la maintient, obtient ce motif à presque chaque
tick où il ne change pas de cap. Trois raisons de ne rien afficher :

1. **Ce n'est pas une erreur.** Contrairement au demi-tour, à la pause ou au débordement de file,
   rien n'a été « raté » — l'intention du joueur (continuer dans cette direction) est déjà
   satisfaite par ce qui va s'exécuter. Signaler un refus ici mentirait sur la nature de
   l'événement.
2. **La confirmation existe déjà** : le serpent continue exactement où le joueur l'a envoyé. C'est
   le retour, et il est gratuit — lui superposer un pictogramme n'ajoute aucune information.
3. **C'est le risque de bruit le plus élevé des quatre.** Un signal fréquent s'éteint de lui-même
   dans la lecture du joueur (effet d'alarme ignorée) et, pire, désensibilise au **même** pictogramme
   utilisé pour le demi-tour — le seul cas où ce signe doit rester associé à « vous avez fait une
   erreur ». ⚠ Cet argument repose sur une fréquence **supposée**, pas observée (voir §5.1) ; c'est
   le point 1 qui tranche seul, et il tient quelle que soit la fréquence réelle.

Décision : `RefuseeDoublon` est filtré **avant** d'atteindre la couche de retour visuel, explicitement
(voir l'API §5.5) — pas par omission silencieuse, pour qu'un lecteur futur ne le lise pas comme un
oubli à corriger.

### 5.4 Variantes proposées

Trois variantes, la troisième écartée d'emblée pour la raison qui vient d'être posée en §5.3 ;
elle reste écrite pour mémoire.

---

**Variante A (recommandée) — Pictogramme directionnel barré ancré à la tête + texte sur l'écran
pause**

- *Pictogramme* (`DemiTourRefuse`, `RefuseeFilePleine`) : un chevron plein, orienté vers la
  direction demandée, barré d'un trait diagonal (grammaire « sens interdit »). Ancré au bord de la
  case tête, côté de la direction refusée, décalé d'environ un quart de case (~11 px) pour ne
  jamais recouvrir la case elle-même. Taille maximale : la moitié d'une case (22 px), pour ne
  jamais déborder sur la case voisine et se lire comme un obstacle.
- *Texte pause* (`RefuseeJeuEnPause`) : une ligne ajoutée à l'écran de pause déjà affiché (pas un
  nouvel écran), du type « Touche ignorée — le jeu est en pause ». Aucune flèche, aucun symbole
  directionnel : uniquement des caractères ASCII, sans dépendance de police à risque.
- Contraste porté par la **forme** (barre diagonale + chevron), jamais par la seule couleur —
  applicable dès aujourd'hui en niveaux de gris, sans attendre la palette.

**Variante B — Contour de la case tête qui change d'épaisseur**

Au lieu d'un pictogramme séparé, la case tête elle-même gagne un contour plus épais ou hachuré
pendant le refus. Avantage : aucun sprite directionnel à dessiner, un seul asset (un contour) sert
aux trois motifs actifs. Inconvénient : ne montre pas *quelle* direction a été refusée — un joueur
qui enchaîne les tentatives ne sait pas laquelle a échoué, seulement qu'une a échoué. Moins
informatif que A pour un coût de production à peine inférieur.

**Variante C (écartée) — Un seul pictogramme pour les quatre motifs, `RefuseeDoublon` inclus**

Techniquement la plus simple (aucun filtre à écrire), mais directement contredite par §5.3 :
elle allumerait le signal à presque chaque tick d'un joueur qui va tout droit, ce qui est
précisément le bruit que le game-designer a signalé comme le vrai piège de cette tâche. Écartée
sans réserve.

---

**Variante A retenue** (tranchée par l'auteur le 2026-08-27). Elle distingue ce qui doit
s'apprendre vite (le pictogramme, lu en un regard resté sur la grille) de ce qui peut se lire
posément (le texte de pause, sans contrainte de tick), et elle respecte la contrainte
forme-avant-couleur dès sa description, sans attendre que la palette existe.

### 5.5 Spécification de la variante retenue

**Anti-répétition (le point qui traite le martelage)** — commun aux deux registres :

Le retour n'est pas un événement qui rejoue une animation à chaque notification ; c'est un **état**
avec une échéance d'extinction :
- une notification affiche le retour et fixe son échéance à *maintenant + durée d'affichage* ;
- une notification reçue pendant que le retour est déjà visible **prolonge** l'échéance du même
  montant, **sans relancer l'animation d'apparition** — pas de re-flash, pas de scintillement sous
  martelage ;
- un plafond de prolongation continue empêche le retour de devenir un élément fixe du décor : passé
  ce plafond, il s'éteint une fois, quitte à se rallumer si le martelage continue. Un signal
  toujours visible cesse d'être lu comme un signal.

**Durées — au jugé, aucune n'a été essayée en jeu, à confirmer par le game-tester :**

| Paramètre | Valeur proposée | Rapport au tick (125 ms) |
|---|---|---|
| Durée d'affichage du pictogramme (par déclenchement) | 250 ms | 2 ticks |
| Plafond de prolongation continue (pictogramme) | 500 ms | 4 ticks |
| Durée d'affichage du texte de pause | 1,5 s après le dernier appui refusé | non lié au tick — la simulation est figée en pause, la contrainte des 125 ms ne s'y applique pas |

⚠ **Le motif de refus n'a pas une source unique** — c'est le piège de cette API, et il découle
directement du §4.2 du GDD : le demi-tour ne peut pas être jugé à l'appui, seulement au tick, contre
la direction réellement appliquée. Il n'appartient donc **pas** à `ResultatEmpilage` (qui ne connaît
que `RefuseeDoublon`, `RefuseeFilePleine`, `RefuseeJeuEnPause`) mais à `ResultatTick.DemiTourRefuse`.
Une UI qui n'écouterait que `Empiler()` n'afficherait **jamais** le refus de demi-tour — le cas que
le §3 impose pourtant de rendre visible.

```csharp
// Deux points d'appel, deux moments :
//  - après FileEntrees.Empiler(), pour tout résultat qui n'est pas Acceptee ;
//  - après FileEntrees.Tick(), quand ResultatTick.DemiTourRefuse est vrai.
public interface IRetourEntreeRefusee
{
    void Notifier(MotifRefus motif, Direction directionRefusee);
}
```

`MotifRefus` est une énumération **propre à la couche de retour**, alimentée depuis les deux
sources ; sa forme exacte est laissée au développeur, tant qu'elle distingue les trois traitements
ci-dessous. Ne pas ajouter le demi-tour à `ResultatEmpilage` pour uniformiser : ce serait déclarer
qu'un demi-tour peut être refusé à l'empilage, exactement l'erreur que le contre-exemple Nord/Sud du
§4.2 interdit.

- L'implémentation filtre `RefuseeDoublon` en tout premier — aucun rendu, retour immédiat (§5.3).
- Le **demi-tour refusé** (source : tick) et `RefuseeFilePleine` (source : empilage) routent vers le
  composant qui pilote le pictogramme ancré à la tête (position = case tête courante + direction
  refusée), avec la logique d'échéance ci-dessus.
- `RefuseeJeuEnPause` route vers le composant qui pilote la ligne de texte sur l'écran pause.
- `FileEntrees` reste une classe pure sans dépendance moteur (§4.2 du GDD) : c'est au
  `MonoBehaviour` de gameplay d'appeler `Notifier`, jamais à `FileEntrees` de connaître l'UI.

### 5.6 Ce que le jeu a démenti (constaté le 2026-08-27)

> ⚠ **Le chevron d'un demi-tour tombe TOUJOURS sur le corps du serpent.** Ce n'est pas un cas
> limite : un demi-tour vise, par définition, la case d'où le serpent vient — donc la case occupée
> par son deuxième segment. Le pictogramme, ancré « au bord de la case tête, côté de la direction
> refusée » (§5.4), se superpose donc systématiquement au corps. En niveaux de gris, gris clair sur
> gris moyen, il devient difficile à lire au moment précis où il doit l'être.
>
> Le brief a été écrit sans que ce cas soit visualisé ; la capture `docs/verif_refus_demitour.png`
> le montre. Le pictogramme **fonctionne** (position, orientation, échéance) — c'est son contraste
> qui échoue. Trois pistes, non tranchées : contour sombre autour du pictogramme, décalage
> perpendiculaire à la direction refusée, ou une couleur d'alerte réservée que la palette (§1)
> devra prévoir. **À arbitrer par le directeur artistique**, et ce sera plus facile une fois la
> palette posée.

> **La barre d'interdiction est perpendiculaire à l'axe du chevron, pas diagonale.** Écart assumé au
> §5.4, décidé à l'implémentation : à 45°, la barre tombe exactement parallèle à l'une des deux
> branches du chevron et se lit comme une troisième branche. La grammaire « sens interdit » d'origine
> est conservée, seule l'inclinaison change.

### 5.7 Ce qui reste à confirmer séparément

- **Palette** : ce brief ne fixe aucun code hexa. Le pictogramme et le texte de pause devront
  référencer `UiPalette` (§1) une fois posée ; en attendant, construire en niveaux de gris /
  silhouettes.
- **Police** du texte de pause : aucune contrainte de glyphe hors ASCII n'est nécessaire ici
  (aucune flèche dans le message retenu), donc pas de risque de repli WebGL — à vérifier quand même
  une fois la police choisie (§2).
- **Ancrage écran de la case tête** : ce brief suppose l'existence d'une conversion grille → écran
  déjà utilisée pour dessiner le serpent ; le composant du pictogramme doit la réutiliser, pas en
  recalculer une nouvelle.
- Toutes les durées de §5.5 sont des points de départ, pas des valeurs mesurées.

### 5.8 Interdits

- Jamais de caractère flèche Unicode (`← → ↑ ↓`) dans un composant `Text` — perte silencieuse
  garantie en WebGL (`docs/PITFALLS_UNITY.md`). Tout symbole directionnel est un **sprite**.
- Jamais de retour pour `RefuseeDoublon` (§5.3).
- Jamais une information portée par la seule couleur.
- Jamais un clignotement en boucle (stroboscope) — une seule enveloppe fondu-entrée/fondu-sortie
  par déclenchement.
- Jamais un retour qui dépasse son plafond de prolongation continue sans s'éteindre au moins une
  fois.
- Jamais de couleur en dur dans le code du pictogramme ou du texte : référencer `UiPalette` dès
  qu'elle existe.
- Jamais construit par clic dans l'éditeur — la scène est un artefact régénéré par
  `SceneBuilder.Build()` ; tout ce qui précède se construit par code.

## 6. Historique des décisions

<!-- Une entrée par brief tranché, dans l'ordre chronologique. Garder les variantes écartées et
     leur raison plutôt que les effacer — voir la convention du GDD. -->

- **2026-08-28 — Score et record du bandeau (GDD §4.5) : placement posé PAR DÉFAUT, brief toujours
  ouvert.** Aucun brief n'existait et le lot ne pouvait pas attendre : score à gauche du bandeau en
  texte principal, record à droite en texte secondaire, récapitulatif entre le titre et la relance
  sur l'écran de fin. Tout est en gris (§5.6). ⚠ **Ce n'est pas une décision artistique tranchée** —
  c'est un développeur qui a choisi faute de directeur artistique, exactement ce que le §1 vide
  finit toujours par produire. À reprendre avec la palette et la typographie.

- **2026-08-27 — Retour d'une entrée refusée (§5).** Variante A **retenue et validée par
  l'auteur** (pictogramme ancré à la tête pour `DemiTourRefuse`/`RefuseeFilePleine`, texte sur
  l'écran pause pour
  `RefuseeJeuEnPause`, aucun retour pour `RefuseeDoublon`). Variante B (contour de case) écartée
  pour perte d'information directionnelle. Variante C (un seul retour incluant le doublon) écartée
  pour risque de bruit.
