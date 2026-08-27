# Guide — l'équipe d'agents de Snake Snack

Comment sont organisés les agents et les skills du projet, et quand invoquer lequel.

## Les 9 agents (`.claude/agents/`)

| Agent | Quand l'invoquer | Modèle |
|---|---|---|
| **`developpeur`** | Code, architecture, build, tests | opus |
| **`game-designer`** | Design, équilibrage, valeurs de tuning, scope | opus |
| **`game-tester`** | Après toute implémentation majeure — joue et documente | opus |
| **`release-manager`** | Publier une version de bout en bout + rédiger le devlog | sonnet |
| **`directeur-artistique`** | Identité visuelle, cohérence, briefs graphiques | sonnet |
| **`graphiste`** | Sprites, VFX, icônes — via les générateurs Python | sonnet |
| **`musicien`** | Musique, SFX, mixage, pipeline audio | sonnet |
| **`story-teller`** | Textes en jeu, noms, descriptions, localisation | sonnet |
| **`marketing`** | Page itch, pitch, briefs de captures | sonnet |

## Les 4 skills (`.claude/skills/`)

- **`/carte-projet`** — index du code : où vit tel système, écran, donnée, outil, plus les
  checklists de câblage. **À invoquer avant toute exploration** plutôt que Glob/Grep à froid.
- **`/verifier-en-jeu`** — construire, lancer, injecter de vraies entrées, capturer. À invoquer
  chaque fois qu'on s'apprête à écrire « ça devrait marcher ».
- **`/rediger-le-gdd`** — remplir `docs/GDD.md` section par section, par entretien, dans l'ordre
  où les décisions se prennent réellement. À invoquer au démarrage, et dès qu'une section est restée
  vide alors qu'on s'apprête à coder le système qu'elle devrait décrire.
- **`/publier-itch`** — la procédure de publication en version courte.

## Comment un chantier s'enchaîne

```
constat (session jouée ou mesure)
   → game-designer  : diagnostic + règle proposée, reportée dans le GDD
   → developpeur    : implémentation + tests (logique pure dans Assets/Scripts/Rules/)
   → mesure         : le banc, si le sujet est chiffrable
   → game-tester    : ce que la mesure ne peut pas dire — le ressenti
   → release-manager: publication + devlog
```

**L'ordre compte.** Le raccourci « implémenter puis mesurer après coup » coûte plusieurs
allers-retours : sur un projet précédent, un cran de difficulté a été publié sans avoir jamais été
joué, et le testeur n'a rien senti.

## Les trois règles apprises à la dure

1. **Une partie isolée ne tranche rien.** La variance entre deux parties peut atteindre un facteur
   2,4 avant même que le réglage testé n'agisse. Un verdict d'équilibrage se prend au **banc
   apparié**, sur le test des signes.
2. **Le banc ne dit pas ce qui se *sent*.** Il mesure la pression que le contenu exerce, pas
   l'expérience. Les deux se sont déjà contredits — le testeur avait raison.
3. **Quand un correctif ne déplace pas la métrique, suspecte l'instrument.** Continuer à doser est la
   manière la plus coûteuse de se tromper.

## Documentation — qui répond à quoi

| Question | Document |
|---|---|
| Phase courante, conventions | `CLAUDE.md` (chargé automatiquement) |
| *Pourquoi* le jeu est réglé ainsi | `docs/GDD.md` — le remplir : `/rediger-le-gdd` |
| *Où* se trouve quoi | skill `/carte-projet` |
| Quels pièges guettent | `docs/PITFALLS_UNITY.md` |
| Ce qui a été testé | `docs/TEST_REPORT.md` |
| Ce qui est réellement sorti | `docs/DEVLOG.md` |
| Publier | `docs/RELEASE.md` + `/publier-itch` |

## Faire évoluer un agent

Si un agent prend systématiquement une mauvaise décision sur un point, **enrichis son fichier `.md`**
— c'est le mécanisme prévu pour capitaliser l'expérience, et c'est moins coûteux que de le corriger
à chaque session. Les fichiers `.claude/` sont versionnés au même titre que le code.

⚠ **Un agent qui décrit un état périmé du projet est pire qu'un agent absent** : il donne des
instructions fausses avec autorité. Quand une phase se termine, relis les agents qu'elle concerne.

## Le LLM local (optionnel)

Si un serveur MCP `local-llm` est enregistré (LM Studio), il permet d'interroger un fichier **trop
gros pour être lu** : il lit le fichier **chez lui** et ne renvoie que la réponse. Mesuré sur un
projet précédent : **83 000 tokens lus en local → 675 renvoyés**.

⚠ Trois garde-fous, appris par la mesure :
1. **C'est lent** (~6-7 min pour 290 Ko) : lancer l'appel **avant** ce qu'on allait faire.
2. **`max_tokens` trop bas tronque la réponse sans lever d'erreur.** Viser 1500-2500.
3. **Bon sur du texte, à proscrire sur des chiffres et sur du code à éditer.** S'il existe un outil
   déterministe, il gagne. Et pour localiser, `Grep` est instantané et exact.

⚠ Un agent déclare une liste `tools:` **fermée** : s'il n'y déclare pas l'outil MCP, il ne *peut
pas* l'appeler, quelle que soit la consigne écrite ailleurs. *Une capacité qu'on documente sans la
câbler n'existe pas.*
