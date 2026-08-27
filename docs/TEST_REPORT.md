# Rapport de test — Snake Snack

Fichier **cumulatif**. Chaque session ajoute une section **en tête** (le plus récent en premier),
datée, avec la version testée.

> **Ne jamais réécrire une section passée.** Si une conclusion ancienne est réfutée, ajouter la
> réfutation et **marquer l'ancienne comme telle** : le raisonnement qui a mené à l'erreur a autant
> de valeur que la correction. C'est ce fichier qui évite de re-signaler un bug connu et de refaire
> un test déjà tranché.

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
