# Pièges — Boucle de jeu et pas de temps


**⚠ Un plafond de rattrapage qui REPORTE le retard ne plafonne rien : il l'étale.** Le jeu
avance par ticks accumulés dans un temps résiduel (`Cadence.NombreDeTicks`). Plafonner le nombre de
ticks joués par image sans jeter le retard donne un code qui *a l'air* correct — le plafond est bien
là, il est bien respecté — et un jeu où les huit cases d'une seconde de gel passent en huit images
successives au lieu d'une. Le symptôme n'est pas « le serpent saute », c'est « le serpent part en
accéléré pendant une seconde après un hoquet », sans message ni erreur. Le reliquat rendu doit
être **la seule fraction sous-tick** (< un tick par construction) : elle garde la phase sans rien
rattraper. GDD §4.1, arbitrage du 2026-08-27.

**⚠ Corollaire côté moteur** : ce plafond suppose que **perdre le focus met le jeu en pause**
(`Application.focusChanged`). Sans cette pause, le plafond fait perdre au joueur tout le temps passé
hors de la fenêtre — il jette le retard, il ne le rend pas. La règle pure ne peut pas s'en charger :
c'est une dépendance du câblage, notée dans les remarques de `Cadence`.

**⚠ Un test de garde-fou qu'on n'a jamais vu ROUGE ne prouve rien.** Celui qui verrouille le
plafond ci-dessus (`LeRetardJeteNeRevientPasAuxImagesSuivantes`) passe tout aussi bien sur une
implémentation qui reporte le retard, si on se contente de vérifier le premier appel : c'est en
rejouant **dix images après le gel** qu'il attrape le défaut. La vérification a coûté une minute —
injecter la régression, constater l'échec, la retirer.

