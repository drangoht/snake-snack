# Pièges Unity — ce qui ne lève aucune erreur

**Ce fichier est l'actif le plus précieux du dépôt.** Il n'est pas une liste de bonnes pratiques :
chaque entrée correspond à un défaut réellement rencontré, qui **n'a produit ni erreur de
compilation, ni exception, ni avertissement** — seulement un jeu qui se comporte mal. C'est
exactement la catégorie de bug qu'on met des heures à trouver et trente secondes à corriger.

> **Le consulter avant de coder dans le domaine concerné, et y ajouter tout nouveau piège découvert,
> dans le même commit.**

Les entrées marquées **[hérité]** viennent de projets précédents (Chimera Protocol, Smily Volley) :
elles n'ont pas encore été revérifiées ici, mais elles ont chacune coûté au moins une régression.

---

## Assets et import

**⚠ Ne JAMAIS ignorer les `.meta` dans `.gitignore`.** Unity y stocke le **GUID** de chaque asset.
Un `.meta` manquant fait perdre toutes les références qui pointaient vers l'asset : scripts détachés
de leurs GameObjects, sprites vidés. Le `.gitignore` du projet ne contient aucune règle `*.meta`, et
c'est délibéré.

**⚠ `Art/` et `Resources/` ne se valent pas — et se tromper ne lève rien.** [hérité]
`Resources/` est chargé **par chemin** (`Resources.Load<Sprite>("Ui/bouton")`) et embarqué **en
entier** dans le binaire, même ce qui n'est jamais utilisé. `Art/` est consommé par **référence de
GUID**. Écrire un asset dans le mauvais des deux : le générateur annonce « écrit », et le jeu affiche
l'ancienne image. Tenir une table de destination (`tools/unity_paths.py`) et y faire référence.

**⚠ Un fichier écrit dans `Assets/` n'existe pas tant qu'Unity ne l'a pas réimporté.** Un build en
batchmode s'en charge, mais un éditeur ouvert peut servir l'ancienne version depuis sa base d'assets.
Sur un fichier **nouveau** et ignoré par git, `AssetDatabase.ImportAsset` seul ne suffit pas : il
faut d'abord un `AssetDatabase.Refresh()` pour que la base le découvre.

## Rendu (URP 2D)

**⚠ Unity range le pipeline actif dans `QualitySettings`, NIVEAU PAR NIVEAU.** [hérité]
Renseigner seulement `GraphicsSettings.defaultRenderPipeline` laisse les autres niveaux en Built-in :
le jeu change de pipeline dès que le joueur change de qualité. `RenderPipelineSetup.Apply()` boucle
sur tous les niveaux — c'est pour ça.

**⚠ Sous le Renderer 2D, un sprite sans `Light2D` globale est rendu NOIR.** [hérité]
Les sprites prennent `Sprite-Lit-Default` : sans une lumière globale dans la scène, tout le décor est
noir, sans la moindre erreur en console. `SceneBuilder.BuildGlobalLight()` en pose une.

## Polices et texte

**⚠ Le repli d'Unity sur les glyphes manquants n'existe QUE sur le bureau.** [hérité]
Avec une police dynamique, `Text` (uGUI) va chercher dans les **polices du système** ce que la police
ne contient pas : des flèches `← → ↑ ↓` sortent correctement sous Windows avec une police qui n'en
contient **aucune**. Un navigateur n'offre aucune police système : le build **WebGL les perd en
silence** — pas de carré blanc, pas d'avertissement, le texte se referme simplement sur le vide.
Constaté sur Smily Volley : bandeaux d'aide amputés, indicateurs de défilement invisibles.

Le repli déclaré à l'import (`fallbackFontReferences` → `LegacyRuntime.ttf`, posé par script sur le
`TrueTypeFontImporter`) **n'y change rien** : essayé, rebâti, les flèches restaient absentes.

**Ce qui marche** : n'écrire que des caractères que la police contient (« Haut/Bas » plutôt que
« ↑ ↓ ») et **dessiner les symboles en sprite**. Vérifier la table `cmap` avant de faire confiance —
un script Python de 20 lignes la lit et répond oui ou non. Et le vérifier **dans le navigateur**,
pas au raisonnement.

**Polices libres** : prendre le `.ttf` **et son `OFL.txt`** dans le dépôt `google/fonts` (SIL OFL) :
`https://raw.githubusercontent.com/google/fonts/main/ofl/<famille>/<Fichier>.ttf`.
⚠ Beaucoup de familles n'existent plus qu'en **version variable** (`Fredoka[wdth,wght].ttf`) :
lister le dossier avant de deviner l'URL
(`https://api.github.com/repos/google/fonts/contents/ofl/<famille>`). ⚠ L'API
`fonts.googleapis.com/css` rend une URL dont le fichier **n'est pas un TTF valide** (signature
`f89b`) : un vrai TTF commence par `00 01 00 00`, et un fichier de 39 Ko contenant du HTML est une
page 404 déguisée.

## Entrées

**⚠⚠ `ProjectSettings.asset` peut livrer `activeInputHandler: 0` — l'ANCIEN Input Manager.** Dans ce
mode, le package Input System est désactivé : **`Keyboard.current` vaut `null`**, tout code d'entrée
sort par sa garde, et le jeu tourne parfaitement — il ne répond simplement à aucune touche. Aucune
erreur, aucun avertissement, rien dans le journal du player. Constaté le 2026-08-27 : le serpent
s'affichait, le HUD s'affichait, et rien ne bougeait ; le premier soupçon est tombé à tort sur
l'injection de touches, puis sur le rendu du pictogramme.

Valeurs : `0` = ancien Input Manager, `1` = package Input System, `2` = les deux. Le projet exige
`1` (CLAUDE.md : « Input System, jamais l'ancien Input Manager »).

```powershell
Select-String "activeInputHandler" ProjectSettings\ProjectSettings.asset   # doit rendre 1
```

⚠ Corollaire de méthode : **une touche sans effet et une touche jamais reçue produisent la même
capture d'écran**. Avant de conclure qu'une règle ne s'affiche pas, prouver qu'une entrée *quelconque*
atteint le jeu — ici, une direction valide qui met le serpent en marche.

**⚠ `KeyCode` et `Key` désignent une POSITION sur un clavier QWERTY**, jamais le caractère imprimé.
Sur un clavier AZERTY, `Key.A` / `Key.D` / `Key.W` placent les commandes sous les touches marquées
**Q / D / Z**. C'est le résultat voulu, pas un bug. Corollaire : proscrire `A`, `Q`, `Z`, `W`, `M`
pour les raccourcis globaux — préférer `Tab`, `R`, les chiffres ou les flèches, dont la position est
commune aux deux dispositions. **Ce piège n'a été découvert qu'en injectant de vraies touches.**

**⚠ `InputSystemUIInputModule` et non `StandaloneInputModule`.** Avec le package Input System actif,
l'ancien module ne reçoit rien : l'UI cesse simplement de répondre, sans erreur.

**⚠ La toute première touche après une prise de focus se perd**, sur le build Windows comme dans le
navigateur. Toujours en envoyer une pour rien avant de mesurer quoi que ce soit.

## Interface

**⚠ Le HUD peut recouvrir une modale.** L'ordre de tri des canevas est le seul arbitre : deux
canevas au même `sortingOrder` s'empilent dans l'ordre de la hiérarchie, qui n'est pas stable quand
la scène est régénérée par code. Donner un `sortingOrder` explicite à chaque canevas.

**⚠ Un piège de focus se voit seulement à la manette.** Une liste dont le focus ne peut plus sortir
se navigue parfaitement à la souris. Tester chaque écran **au clavier et à la manette**.

**⚠ Invisible se lit inexistant.** [hérité] Une capacité qui n'annonce pas sa touche n'existe pas
pour le joueur : sur un projet précédent, un dash a été joué une partie entière sans que le testeur
sache qu'une touche existait. Un effet passif sans indicateur est cru inactif. C'est un bug
d'ergonomie, pas un détail de présentation.

## Build

**⚠ Lancer Unity par l'opérateur `&` en PowerShell rend la main IMMÉDIATEMENT sans rien faire.**
[hérité] Pas de log, `$LASTEXITCODE` vide, et le script poursuit comme si tout allait bien. Utiliser
`Start-Process -Wait`. *Un lancement qui échoue en silence est pire qu'un lancement qui échoue.*

**⚠ Un code retour nul ne distingue pas « construit » de « rien à faire ».** Exiger une **phrase de
réussite explicite** dans le journal (c'est ce que fait `tools/build.ps1`).

**⚠ Pire : Unity quitte avec le code retour 0 alors que le build a ÉCHOUÉ.** Constaté sur un build
Windows dont le journal dit `Build Finished, Result: Failure` (6 erreurs) puis, trente lignes plus
bas, `Exiting batchmode successfully now!` et un code 0. Un script qui se fie au code retour empaquette
et publie un dossier de build incomplet **sans que rien ne l'avertisse**. La phrase de réussite dans
le journal est le seul signal fiable.

**⚠ La DATE d'un artefact de build ne prouve rien** : Unity construit de façon incrémentale, un
fichier identique n'est **pas réécrit**. Un horodatage antérieur au build est normal. Le premier
garde-fou de fraîcheur écrit sur cette base échouait sur des builds parfaitement valides.

**⚠ Les métadonnées Windows d'un `.exe` Unity décrivent le MOTEUR** (« 6000.5.6f1 »), pas le jeu. Un
contrôle qui compare la version de release à ces métadonnées échoue toujours.

**⚠ Seule la version EMBARQUÉE tranche**, parce qu'elle est posée juste avant le build. D'où
`build_stamp.json`, écrit **par** le build : il ne peut pas annoncer une version que le build n'a pas
posée. **Une release a déjà expédié le binaire de la version précédente sans qu'aucune erreur ne soit
levée** — c'est ce contrôle qui l'empêche.

**⚠ Un tampon de build écrit par le script de PUBLICATION survit à sa release.** [hérité] Écrit
seulement au moment de publier, le fichier reste ensuite en place, et tout build local ultérieur
affiche le SHA de la dernière release. *Un garde-fou de fraîcheur qui se trompe est pire que pas de
garde-fou, puisqu'on lui fait confiance.* Il est donc posé par le **build** (`BuildTools.StampGitSha`)
et ignoré par git — c'est un artefact, pas une source.

**⚠ Le build en ligne de commande échoue si l'éditeur Unity est ouvert** (« another Unity instance is
running »). Vérifier `Get-Process Unity` ou `Temp\UnityLockfile` — et **ne jamais tuer l'éditeur** :
attendre, ou travailler sur une copie de `Assets` + `Packages` + `ProjectSettings`.

**⚠ Le premier build d'une plateforme réimporte tous les assets** (plusieurs dizaines de minutes) ;
les suivants sont rapides. Prévoir le timeout en conséquence.

**⚠ La scène régénérée produit un diff énorme et vide de sens.** `SceneBuilder` renumérote tous les
`fileID` : des milliers de lignes ajoutées et autant de retirées pour une scène identique. L'écarter
(`git checkout --`) **sauf si `SceneBuilder.cs` a changé**. Sans l'exclusion correspondante dans
`BuildTools.HasLocalChanges`, tout build se déclarerait issu d'un arbre modifié.

## Build web (WebGL)

**⚠ WebGL est la seule plateforme dont le stripping par défaut est le plus agressif.** [hérité]
L'Input System résout ses couches de contrôle **par réflexion** : au niveau élevé, le jeu démarre
normalement et **ne répond plus au clavier**. Poser `ManagedStrippingLevel.Low`.

**⚠ Le cache du navigateur mélange deux builds.** [hérité] Les fichiers de sortie WebGL portent
toujours le même nom d'un build à l'autre. Le navigateur peut donc associer le `.data` d'un build au
`.wasm` d'un autre. Le symptôme n'est **pas** « version périmée », c'est :

```
Chargement impossible : RuntimeError: memory access out of bounds
  at wasm://wasm/0b2ac7ce:wasm-function[97296]:0x1712ca9
  ... trois cents lignes d'offsets, pas un seul nom de méthode ...
```

Une heure a été perdue à chercher ça **dans le code du jeu**. La parade est double :
1. un identifiant de build injecté dans les URL de la page (`BuildTools.StampWebCacheBuster`) ;
2. ⚠ **la page hôte elle-même ne doit jamais être cachée** — elle est la seule à porter cet
   identifiant. Cachée, elle continue de désigner les fichiers de l'ancien build : *un mécanisme
   d'invalidation transporté par une ressource cachable s'auto-annule.* Les balises `http-equiv` ne
   suffisent pas (Chrome les ignore pour le document principal) : il faut de vraies en-têtes HTTP,
   d'où `tools/serve_web.py`.

**⚠ Un serveur local mono-thread bloque le démarrage du jeu.** [hérité] `socketserver.TCPServer`
traite une requête à la fois ; le navigateur garde ses connexions ouvertes et un jeu qui précharge
ses `StreamingAssets` en parallèle bloque ses propres requêtes. Le jeu reste sur sa barre de
démarrage — qui semble même reculer — **sans aucune erreur**, ni côté navigateur, ni côté serveur.

**⚠ Le nom du canal itch décide si le fichier est JOUABLE dans le navigateur.** [hérité] `html5`
(ou `html`, ou `web`) est reconnu comme tel ; tout autre nom produit une archive à télécharger, qui
s'installe parfaitement et ne se joue pas. Rien ne le signale. Prérequis côté itch, à faire une
fois : *Kind of project* = **HTML**, et le fichier coché « played in the browser ».

**⚠ Le `devicePixelRatio` mobile est le réglage de performance le plus rentable.** [hérité] Un
téléphone récent annonce 3 : Unity rend alors **neuf fois** plus de pixels que la dalle logique n'en
montre, sur un GPU dix fois plus faible qu'une carte de bureau. La cadence s'effondre sans qu'aucune
erreur ne le dise. Forcer `config.devicePixelRatio = 1` sur mobile.

**⚠ Le manifeste de version n'appartient qu'à la cible téléchargeable.** [hérité] Un joueur web est
toujours à jour (la page sert le build courant). Pousser le manifeste depuis une release web
annoncerait à tous les joueurs Windows une mise à jour qui n'existe pas.

**⚠ Unity dépose un dossier `Data/` (code Burst) à la RACINE du projet** lors d'un build WebGL, hors
de tout dossier de build. Artefact — ignoré par git.

## Tactile et mobile

**⚠ La moitié du portage mobile vit dans `index.html`, pas dans Unity.** [hérité] Le zoom, le
défilement, le geste de retour depuis le bord, l'appui long qui ouvre un menu système, la barre d'URL
qui mange le bas de l'écran (donc les commandes qui s'y trouvent) : Unity ne peut rien contre ce qui
se passe **avant** lui. Aucun de ces défauts ne se voit dans l'éditeur, aucun ne lève d'erreur, et
chacun rend le jeu injouable au doigt. Le gabarit du projet les traite tous — ne pas les défaire.

**⚠ `maxTouchPoints` est le seul test fiable pour détecter un mobile** : la chaîne d'agent
utilisateur ment (mode bureau d'un téléphone, iPad qui se déclare Mac).

**⚠ Utiliser `dvh` et non `vh`** pour la hauteur du canevas : `vh` ignore la barre d'URL qui se
rétracte, et le bas du jeu se retrouve caché derrière elle.

**⚠ Chrome de bureau ne fournit AUCUN `Touchscreen`.** [hérité] `Touchscreen.current` reste `null`
et tout code tactile sort immédiatement. Dispatcher de vrais `TouchEvent` en JS ne sert à rien —
l'événement se propage, mais le moteur n'a pas de périphérique où le ranger, et **aucune erreur** ne
le dit. Seul un mode `?touch` (qui appelle `TouchSimulation.Enable()`) rend le tactile testable.

## Boucle de jeu et pas de temps

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

## Logique pure et tests hors moteur (`Rules/` + `dotnet test`)

**⚠ `dotnet test` compile `Rules/` dans un contexte PLUS PERMISSIF qu'Unity : le vert ne prouve
pas que le build passera.** `tests/SnakeSnack.Tests.csproj` cible `net8.0` avec
`ImplicitUsings=enable` et `Nullable=enable` ; Unity 6000.5 compile le même fichier en C# 9,
`netstandard2.1`, sans usings implicites et avec le contexte nullable désactivé. Trois façons
d'être au vert et de casser ensuite, aucune détectée par le runner :

- un `using System;` oublié (fourni implicitement côté test) → **CS0246 côté Unity** ;
- une annotation `object?` / `string?` sans `#nullable enable` en tête de fichier → **CS8632 côté
  Unity**. C'est un *avertissement*, donc le build « réussit » — et la consigne du projet est zéro
  avertissement nouveau. `Assets/Scripts/Rules/Case.cs` porte donc la directive en première ligne ;
- toute syntaxe C# 10+ (namespace à portée de fichier, `record struct`) → **erreur côté Unity**,
  alors qu'elle est parfaitement légale dans les fichiers de `tests/`, eux compilés en net8.0.

**La parade coûte dix secondes** et évite d'attendre un build Unity : compiler `Rules/` dans un
projet jetable **hors du dépôt** (`$TEMP`), avec `EnableDefaultCompileItems=false`, un
`<Compile Include="...\Assets\Scripts\Rules\*.cs" />`, `TargetFramework=netstandard2.1`,
`LangVersion=9.0`, `Nullable=disable`, `ImplicitUsings=disable`, `TreatWarningsAsErrors=true`.
⚠ Le poser **dans** le dépôt le ferait ramasser par Unity comme un asset.

**⚠ Le glob du csproj n'est PAS récursif.** `..\Assets\Scripts\Rules\*.cs` ne descend pas dans les
sous-dossiers : un fichier de règles rangé dans `Rules/Deplacement/` n'entre **pas** dans l'assembly
de test. Rien ne le signale — `dotnet test` reste vert, avec une règle simplement jamais éprouvée,
pendant qu'Unity la compile et que le jeu s'en sert. Garder `Rules/` **plat**, ou passer le glob à
`**\*.cs` en connaissance de cause.

**⚠ Un script neuf n'a pas de `.meta` tant qu'Unity ne l'a pas importé.** Les cinq fichiers de
`Rules/` écrits le 2026-08-27 n'ont reçu leur GUID qu'au `tools/build.ps1` suivant. Commiter des
scripts **sans** leur `.meta` fait perdre toute référence future qui pointerait dessus : lancer un
build avant de commiter un fichier neuf de `Assets/`.

## Tests headless et pilotage

Voir le skill **`/verifier-en-jeu`** pour la procédure complète. Les pièges, en résumé :

- **Le focus est LE point de blocage** : hors focus, Unity ne reçoit aucune touche et aucun mouvement
  de souris — le test ment en silence. `SetForegroundWindow` seul échoue depuis un shell non
  interactif ; seul un **vrai clic** donne le focus légitimement.
- ⚠ **Même le vrai clic échoue depuis une session d'agent en arrière-plan** (constaté le
  2026-08-27 : `tools/piloter_jeu.py` refusait de partir, « Impossible de donner le focus »). Le
  contournement qui marche, dans cet ordre : `SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0)`,
  un appui **ALT** enfoncé-relâché — qui fait entrer le processus dans la fenêtre d'autorisation de
  Windows —, puis `AttachThreadInput` vers le thread de la fenêtre cible avant
  `SetForegroundWindow`. Vérifier ensuite `GetForegroundWindow()`, sans quoi tout le reste ment.
- ⚠ **`PrintWindow` tronque la capture dès que Windows applique une mise à l'échelle DPI** : il rend
  en pixels logiques pendant que le jeu dessine en pixels physiques. On obtient le coin supérieur
  gauche de la fenêtre, agrandi — ce qui ressemble à un problème de cadrage du jeu. Capturer l'écran
  (`CopyFromScreen`) et recadrer sur `GetWindowRect`, après `SetProcessDPIAware()`.
- **`keybd_event` doit porter le code de balayage** (Unity lit le raw input), et **les flèches
  exigent `KEYEVENTF_EXTENDEDKEY`** — sans lui, leur scan code est celui du pavé numérique et la
  touche est perdue en silence.
- **`SetCursorPos` ne met rien dans le flux d'entrée** : utiliser `SendInput` avec
  `MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE`, coordonnées normalisées sur 0..65535.
- **Un appui instantané ne teste que `wasPressedThisFrame`** : tout ce qui demande un maintien exige
  un vrai maintien. Conclure « les flèches ne marchent pas » sur un appui instantané est faux.
- **Le splash Unity dure ~2 s**, et **le pare-feu Windows ouvre une alerte modale au premier
  lancement de chaque nouveau chemin d'exe** — elle vole le focus et grise la fenêtre.
- **Ne pas coder en dur la position des éléments visés** : une refonte les déplace, et les clics
  tombent dans le vide sans aucune erreur.
- **Les PlayerPrefs sont persistants** : piloter une option par N appuis donne un résultat *relatif*
  à la session précédente.
- **Les seuils d'analyse de pixels** : deux conclusions fausses de suite (un centroïde contaminé par
  un élément de décor, un comptage de pixels clairs qui comptait le texte du HUD). Cadrer hors HUD,
  exclure chaque élément connu par sa teinte, **puis regarder l'image**.
- **Ce qui est de la pure géométrie ne se prouve pas en jouant** : un fichier jetable dans
  `Assets/Editor` appelé par `-executeMethod` logge les bornes à deux pixels près. C'est ce qui a
  rattrapé une zone trois fois trop large dont la formule se relisait parfaitement.

## PowerShell (scripts de build et de release)

**⚠ Ne JAMAIS tester `$?` après un exécutable natif en PowerShell 5.1.** `git`, Unity et Butler
écrivent leur progression sur **stderr même quand tout va bien**, ce qui met `$?` à `$false` alors
que le code retour vaut 0. Le script de release annonçait « git push échoue » à **chaque release
réussie**. Seul `$LASTEXITCODE` fait foi.

**⚠ `$ErrorActionPreference = 'Stop'` est un piège dans un script de build**, pour la même raison :
la moindre ligne de progression sur stderr avorte le script.

**⚠ Un script de release qu'on ne peut essayer qu'en publiant ne se teste jamais qu'en production.**
D'où `-DryRun`, qui va jusqu'au dossier de distribution et s'arrête avant tout effet visible.

## Audio

**⚠ Une entrée absente de la table de correspondance est MUETTE.** [hérité] Quatorze armes l'ont été
sans que rien ne le dise. Écrire un audit qui compare la liste du contenu à la table des sons.

**⚠ Le navigateur ne laisse aucun son démarrer avant un geste de l'utilisateur.** Unity ouvre son
contexte audio suspendu : sans le réveil posé dans le gabarit WebGL, la musique ne se déclenche qu'au
hasard d'un clic.

**⚠ Un log de `PlayOneShot` prouve une intention, pas un son.** Pour prouver que l'audio sort du
mixeur : `AudioListener.GetOutputData(buffer, 0)` et logger le RMS.

## Publication (itch.io)

**⚠ Le bouton « Save » cliqué par référence d'élément n'enregistre pas.** [hérité] La page remonte
simplement en haut, sans erreur ni bandeau, et la page publique garde l'ancien texte. Attendre
l'apparition du bandeau `.global_flash` « Saved » — c'est le seul signe qui distingue un envoi d'un
défilement.

**⚠ La page publique est servie depuis un cache.** [hérité] La relire aussitôt après un
enregistrement *réussi* la montre inchangée. Un paramètre d'URL quelconque (`?v=2`) suffit à
trancher ; sans lui on conclut à un échec qui n'a pas eu lieu, et on réédite pour rien.

**⚠ L'éditeur de texte d'itch est un Redactor.** [hérité] Le contenu vit dans `.redactor-layer`
(contenteditable), doublé d'un `textarea` caché. Écrire dans la couche ne synchronise pas toujours le
textarea — **sur le formulaire de devlog, jamais**. Un devlog envoyé sans écrire les deux part avec
un titre correct et un **corps vide**.

**⚠ Un `<select>` d'itch n'a qu'une option dans le DOM** : ce sont des widgets Selectize. Passer par
`element.selectize.setValue(...)`, jamais par un clic — qui ouvre un menu natif et **gèle les
captures d'écran**.

**⚠ Un devlog non coché « Published » reste en brouillon sans rien dire.**

**⚠ Trois réglages décisifs de la page ne sont dans AUCUN fichier du dépôt** [hérité] et ne se voient
donc jamais en relisant le code : la case **Mobile friendly** (elle seule décide de ce qu'itch
propose à un visiteur sur téléphone), l'onglet **Classification** (dont le décompte de joueurs), et
l'**orientation** déclarée. Ils étaient tous les trois faux jusqu'à la version 1.1.0 de Smily Volley.

**⚠ L'iframe d'itch.io est cross-origin** (`html-classic.itch.zone`) : ni clic ni touche injectés n'y
entrent. Pour éprouver le build **publié**, ouvrir l'URL de l'iframe directement dans un onglet.
