# Ce qui a été écarté, et pourquoi

La liste la plus utile du design : elle évite de rouvrir dix fois le même débat. Sortie de
`docs/GDD.md` (§7) parce qu'on ne la consulte que pour rouvrir une décision — le sommaire y garde
la liste des sujets tranchés, de quoi savoir s'il faut ouvrir ce fichier.

⚠ Une conclusion réfutée se **garde et se marque** comme telle plutôt que se réécrire : le
raisonnement qui a mené à l'erreur évite de refaire deux fois le même détour.

<!-- La liste la plus utile du document : elle évite de rouvrir dix fois le même débat. -->

> **Bords téléportants (le serpent ressort par le côté opposé).** Écarté pour la 0.1, décidé au
> design, **pas encore contredit par une partie** : une grille close se lit entièrement d'un coup
> d'œil, alors qu'un bord traversant demande de simuler mentalement une continuité invisible. Surtout,
> il rend certaines morts non imputables (« il est ressorti où ? »), ce que le pilier de la §2
> interdit. À rouvrir si les premières parties montrent une mortalité précoce contre les murs.

> **Snacks à effets distincts, bonus temporaires.** Écartés au pitch (§1). Ils déplacent la décision
> « par où passer » vers « atteindre le bon objet », et la mort cesse d'être attribuable à un virage.
> Le jeu retenu est le Snake canonique : l'enjeu est la sensation, pas l'ajout de mécaniques.

> **Cadence qui accélère avec la longueur (le Snake Nokia).** Écartée pour la 0.1, **décidée au
> design, aucune partie jouée** : c'est un multiplicateur, pas une règle nommée — le joueur ne peut
> pas la lire avant de lancer. Elle s'empile sur une difficulté qui monte déjà seule (§4.1), elle
> brouille l'attribution de la mort (§2 : mal planifié, ou dépassé par la cadence ?), et elle rend le
> tick — l'unité de mesure — variable, donc deux parties incomparables au banc. À rouvrir **une fois
> le banc apparié disponible**, pas avant : c'est précisément le genre de réglage qu'une partie
> isolée ne tranche pas.

> **File d'entrées de profondeur 1 (une seule direction retenue).** Écartée au design. Elle perd la
> seconde moitié de toute chicane tapée en moins d'un tick, c'est-à-dire qu'elle punit le joueur qui
> joue *plus vite* que la cadence, et la perte est invisible (§3). C'est l'origine habituelle du
> « ce Snake rate mes virages ». Voir §4.2.

> **Grille 32 × 18 remplissant le 16:9 sans marges.** Écartée au design : dimensions paires, donc pas
> de case centrale exacte (§4.3) ; 576 cases au lieu de 315, soit une partie type qui double de durée
> pour la même décision répétée ; et plus aucune marge où poser le score sans le superposer à l'aire
> de jeu. À rouvrir si les premières parties se révèlent trop courtes ou trop serrées.

> **Retour de refus : variantes écartées.** Le contour de case épaissi (ne dit pas *quelle*
> direction a été refusée) et le retour unique incluant le doublon (bruit à chaque tick d'un joueur
> qui va tout droit) — détail et raisons dans `docs/art/historique.md`, qui tient l'historique des
> décisions visuelles comme cette section tient celles de design.

> **Tirage de la pomme par rejet (« tirer une case au hasard, retirer tant qu'elle est occupée »).**
> Écarté au design, **aucune partie jouée**. Le coût du tirage croît avec le remplissage et n'a
> **aucune borne** : sur une grille presque pleine le jeu se fige, sans exception ni log — un défaut
> qui n'apparaît qu'en toute fin de partie longue, donc jamais pendant les tests. L'énumération
> (§4.4) coûte au plus 315 cases, toujours. À rouvrir **seulement** si un profilage WebGL montre que
> l'énumération pèse, et alors en hybride **borné** (N rejets puis repli sur l'énumération), jamais
> en rejet nu. <!-- à mesurer : coût réel du parcours, en WebGL -->

> **Contraindre l'apparition de la pomme (distance minimale à la tête, interdiction dans le
> prolongement immédiat).** Écarté au design. Une pomme ne peut ni bloquer ni tuer, et manger n'est
> jamais obligatoire (§4.4) : aucune position ne rend une mort non imputable, la contrainte ne
> protégerait donc de rien. Elle retirerait seulement des tirages *favorables* et changerait le
> nombre de cases éligibles, rendant chaque banc plus lourd à décrire. À rouvrir si le
> `game-tester` rapporte que les pommes offertes à une ou deux cases dévaluent le score — c'est du
> ressenti, aucun banc ne le tranche.

> **Plusieurs pommes simultanées.** Écartées au design. Une contrainte se juge à ce qu'elle *donne*
> : deux pommes ne raccourcissent pas seulement le trajet, elles offrent une **cible de secours**
> quand la première devient inatteignable — le joueur y gagne plus qu'il n'y perd. Elles diluent
> aussi la décision « par où passer », qui est le verbe du §1. À rouvrir si le `TEST_REPORT` montre
> que le trajet entre deux pommes est ressenti comme du temps mort.

> **Pomme à durée de vie limitée (elle disparaît et réapparaît ailleurs).** Écartée au design. C'est
> un aléa hostile : le joueur s'engage dans un couloir pour une cible qui s'évapore, et la mort qui
> suit n'est plus imputable à son virage mais à un minuteur qu'il ne contrôle pas (§2). C'est aussi
> un mur de patience déguisé. Non rouverte pour la 0.1.

> **`UnityEngine.Random` ou `System.Random` pour le tirage de la pomme.** Écartés au design. Le
> premier est un état global partagé, indisponible dans `Rules/`. Le second **ne garantit pas** la
> même suite d'un runtime à l'autre : un banc apparié dont les pommes diffèrent entre `dotnet test`,
> le build bureau et le build WebGL ne compare plus rien, et l'écart serait attribué au réglage
> testé. À rouvrir si .NET publie un contrat de stabilité de séquence — pas avant.

> **Score pondéré (bonus de rapidité, points liés au temps ou à la longueur).** Écarté au design.
> Il ajoute une pression de temps que rien n'affiche, et fait basculer l'explication de la défaite
> de « j'aurais dû passer par la droite » (§2) vers « j'ai été trop lent ». La longueur, elle, vaut
> déjà `3 + score` : ce serait le même nombre affiché deux fois. À rouvrir si le score brut se
> révèle ne donner aucune raison de relancer une fois le record posé.

> **Manette et tactile.** *Reportés, pas écartés* — voir §3. Chaque périphérique est un chemin de
> plus à rejouer à chaque build, pour un jeu web joué au clavier. À rouvrir sur retour de joueurs
> mobiles.

⚠ Quand une de ces conclusions est réfutée par une partie réelle, **la garder et la marquer comme
telle** plutôt que la réécrire.
