---
name: verifier-en-jeu
description: Construire Snake Snack en ligne de commande, le lancer, lui injecter de vraies entrées et capturer l'écran — pour constater qu'un changement fonctionne réellement au lieu de conclure qu'il compile. À invoquer après toute modification de gameplay, d'UI, de rendu ou de commandes, et chaque fois qu'on s'apprête à écrire « ça devrait marcher ».
---

# Vérifier en lançant le jeu — Snake Snack

> **« Ça compile » ne prouve rien sur un jeu.** Un mapping clavier inversé, un personnage collé à un
> mur, un menu qui ne réagit pas, un décor rendu entièrement noir : aucun de ces défauts n'apparaît à
> la compilation, et tous se voient en trente secondes sur une capture du jeu qui tourne.

Tout se pilote **sans ouvrir l'éditeur**.

## 1. Construire

```powershell
& "tools/build.ps1"              # Windows -> Build\Windows\SnakeSnack.exe
& "tools/build.ps1" -Target web  # Web     -> Build\Web
& "tools/build.ps1" -Lancer      # ... puis lance le jeu et capture l'écran
```

Le build active URP, régénère `Assets/Scenes/Game.unity` depuis `SceneBuilder`, compile, et écrit
son journal dans `Logs\build-<cible>.log`.

⚠ **Ne pas invoquer `Unity.exe` directement.** Son chemin n'est pas le même d'une machine à l'autre
(`Program Files`, ou n'importe quel disque via le *secondary install path* du Hub) ; écrit en dur,
il donne « Unity.exe : Le terme «Unity.exe» n'est pas reconnu comme nom d'applet de commande ».
`build.ps1` le résout, le mémorise, et couvre les trois pièges ci-dessous.

⚠ **Le build échoue si l'éditeur Unity est ouvert** (« another Unity instance is running ») :
`build.ps1` refuse alors de partir. **Ne jamais tuer l'éditeur** — soit attendre, soit travailler
sur une copie de `Assets` + `Packages` + `ProjectSettings` dans le scratchpad.

⚠ En PowerShell, lancer Unity par l'opérateur `&` rend la main **immédiatement sans rien faire** :
il faut `Start-Process -Wait`.

⚠ Le **premier** build importe tout le projet (plusieurs dizaines de minutes) et génère `Library/`
et `ProjectSettings/` — rien à ouvrir dans Unity Hub avant. Les suivants sont rapides.

## 2. Lancer, agir, capturer

```
py tools/piloter_jeu.py --lancer --attendre 4 --capture docs/verif.png
py tools/piloter_jeu.py --touches "entree,bas,bas,entree" --capture docs/menu.png
py tools/piloter_jeu.py --maintenir droite --duree 1.2 --capture docs/deplacement.png
py tools/piloter_jeu.py --fermer
```

Le script lance l'exe **en fenêtré** (le plein écran rend la capture et le focus hasardeux), lui
donne le focus par un vrai clic, amorce par une touche pour rien, puis agit.

⚠ **Une capture se paie à la lecture** (~700 jetons l'unité, et une boucle de vérification en
enchaîne dix). Deux réflexes :
- **Capturer beaucoup, n'en ouvrir que ce qui tranche.** Les PNG restent sur le disque : on les
  relit à la demande, on ne les fait pas tous défiler pour constater que le menu s'affiche.
- Les captures sont réduites à 960 px de large par le script — assez pour juger d'une position, d'un
  état d'écran ou d'un texte. `--pleine-resolution` seulement pour un détail au pixel (crénelage,
  alignement fin), et alors une seule.

## Les huit pièges — chacun a déjà produit une conclusion fausse

1. **Le focus est LE point de blocage.** Hors focus, Unity ne reçoit **aucune** touche et aucun
   mouvement de souris : le test ment en silence. `SetForegroundWindow` seul échoue depuis un shell
   non interactif — seul un **vrai clic** dans la fenêtre donne le focus légitimement. Toujours
   vérifier `GetForegroundWindow() == hwnd` avant de conclure quoi que ce soit.
2. **La toute première touche après le lancement se perd.** Amorcer par un aller-retour.
3. **Les touches injectées doivent porter le CODE DE BALAYAGE** (le système d'entrée d'Unity lit le
   raw input, pas le code virtuel), et **les flèches exigent en plus `KEYEVENTF_EXTENDEDKEY`** :
   sans lui, leur scan code est celui du pavé numérique et la touche disparaît sans erreur.
4. **`SetCursorPos` ne suffit pas pour la souris** : il déplace le curseur à l'écran sans rien mettre
   dans le flux d'entrée. Utiliser `SendInput` avec `MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE`,
   coordonnées normalisées sur 0..65535, par petits pas.
5. **Un appui instantané ne teste que `wasPressedThisFrame`.** Tout ce qui demande un maintien
   (déplacement, navigation continue) exige `--maintenir`. Conclure « les flèches ne marchent pas »
   sur un appui instantané est faux : c'est l'outil, pas le jeu.
6. **Le splash Unity dure ~2 s** : ignorer les premières images.
7. **Le pare-feu Windows ouvre une alerte modale au premier lancement de CHAQUE nouveau chemin
   d'exe.** Elle vole le focus et grise la fenêtre. La fermer (`Get-Process PickerHost`) puis
   relancer, ou toujours rebâtir au même chemin.
8. **Les réglages sont persistants (PlayerPrefs).** Piloter une option par N appuis donne un résultat
   *relatif* à la session précédente : revenir à une extrémité connue, puis **relire la valeur à
   l'écran**.

⚠ **Ne pas coder en dur la position des éléments visés.** Une refonte qui déplace un bouton fait
tomber les clics dans le vide — sans erreur, juste une capture qui montre autre chose que prévu.
Relire la position sur une capture avant de rejouer un ancien script.

## Quand l'œil ne suffit pas

- **Analyse de pixels** pour ce qui est trop rapide ou trop fin (« la balle sort-elle du cadre ? »).
  ⚠ Cadrer la zone balayée **hors HUD** et exclure chaque élément connu par sa teinte : deux fois de
  suite, un seuil trop large a mené à une conclusion fausse (un centroïde contaminé par le décor,
  puis un comptage de pixels clairs qui comptait le texte blanc du HUD). **Puis regarder l'image.**
- **Fenêtre de capture assez longue** : 1,5 s tombe souvent entièrement dans une pause. Balayer 20 s
  et plus, en analysant à la volée plutôt qu'en gardant les bitmaps (90 images ≈ 350 Mo).
- **Provoquer le cas** : une mécanique qui ne s'active qu'à la demande ne se mesure pas au hasard.
  Comparer trois colonnes — avant, après en jeu passif, après en jeu provoqué. C'est la colonne
  « passif » qui prouve qu'on n'a rien cassé.
- **Ce qui est de la pure géométrie ne se prouve pas en jouant** : écrire un fichier **jetable** dans
  `Assets/Editor`, l'appeler par `-executeMethod`, logger les points encadrant la borne à deux pixels
  près — puis le supprimer. C'est ce qui a rattrapé une zone de rejet trois fois trop large, dont la
  formule se relisait pourtant parfaitement.
- **Pour l'audio, mesurer la sortie** (`AudioListener.GetOutputData` + RMS), pas les appels.
- **Lire le `-logFile` du player** à la fin : c'est là que sortent les exceptions d'exécution.

## Vérifier la version web

```
py tools/serve_web.py            # http://localhost:8080, SANS cache navigateur
```
⚠ Ne pas utiliser `python -m http.server` : après un rebuild, le navigateur associe le `.data` d'un
build au `.wasm` d'un autre, et le jeu meurt sur un `RuntimeError: memory access out of bounds` de
trois cents lignes d'offsets, qui ne ressemble en rien à un problème de cache.

Depuis Chrome (skill `claude-in-chrome`) :
- **un appui instantané ne déclenche que `wasPressedThisFrame`** — pour un maintien, dispatcher
  soi-même l'événement (Unity ne filtre pas `isTrusted`) :
  ```js
  const c = document.querySelector('canvas');
  const o = {key:'a', code:'KeyA', keyCode:65, which:65, bubbles:true, cancelable:true};
  c.dispatchEvent(new KeyboardEvent('keydown', o));
  await new Promise(r => setTimeout(r, 900));
  c.dispatchEvent(new KeyboardEvent('keyup', o));
  ```
- **Chrome de bureau ne fournit AUCUN `Touchscreen`** : `Touchscreen.current` reste `null` et tout
  code tactile sort immédiatement, sans erreur. Seul **`?touch`** (qui active `TouchSimulation`) rend
  le tactile testable — et il répond alors aux **vrais** clics, pas aux clics synthétiques. Pour
  prouver qu'un bouton de déplacement répond, `left_click_drag` d'un point à un autre **du même
  bouton** : le maintien dure assez longtemps pour produire un déplacement visible.
- **Les `PointerEvent` synthétiques n'atteignent pas uGUI** (contrairement aux `KeyboardEvent`).
- **L'iframe d'itch.io est cross-origin** : rien n'y entre. Ouvrir l'URL de l'iframe directement dans
  un onglet (`document.querySelector('iframe').src`) — là, tout redevient pilotable.

## ⚠ Clavier AZERTY

`KeyCode` (ancien Input Manager) comme `Key` (Input System) désignent une **position physique sur un
clavier QWERTY**, jamais le caractère imprimé. `Key.A` / `Key.D` / `Key.W` placent les commandes sous
les touches marquées **Q / D / Z** d'un clavier français — c'est le résultat voulu, pas un bug.
Proscrire `A`, `Q`, `Z`, `W`, `M` pour les raccourcis globaux ; préférer `Tab`, `R`, les chiffres ou
les flèches.
