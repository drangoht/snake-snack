# Pièges — Tests headless et pilotage


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
  ✔ **Versé dans `piloter_jeu.donner_le_focus` le 2026-08-27** : il enchaîne désormais les trois
  moyens, du moins insistant au plus insistant.
- ⚠ **L'amorce « une touche pour rien » doit être une touche que LE JEU IGNORE.** Elle était Bas
  puis Haut ; dans Snake Snack, où la partie démarre sur la première direction applicable (GDD
  §4.1), elle lançait la partie et envoyait le serpent vers le sud **avant** le scénario. La capture
  montrait alors un serpent ailleurs que là où le scénario le plaçait — sans erreur, et en donnant
  l'impression d'un bug de gameplay. `piloter_jeu.amorcer` amorce sur **Tab**, lié à rien.
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


**⚠ Dans un navigateur, le focus de la FENÊTRE ne suffit pas : c'est le CANEVAS qui doit l'avoir.**
Constaté le 2026-08-28 en pilotant le build web dans Chrome. `donner_le_focus()` rend `True` — la
fenêtre est bien au premier plan — et les touches partent quand même à la page, pas au jeu : rien
ne bouge, aucune erreur. Il faut un **vrai clic au centre du canevas** en plus
(`_mettre_au_premier_plan_par_clic`).
⚠ Et surtout **pas l'amorce Tab** du pilote bureau : dans un navigateur, Tab **déplace le focus**
d'un élément à l'autre, donc l'amorce fait exactement le contraire de ce qu'on lui demande. Le clic
sert alors des deux : il donne le premier plan ET le focus clavier au canevas.

**⚠ Un scénario de touches peut « réussir » en produisant un tout autre état.** `--touches
"echap,haut"` devait donner un écran de pause : la capture montrait un serpent en pleine course.
Échap ne met en pause que depuis `EnCours` — avant le premier appui le jeu est `EnAttente`, la touche
n'y fait rien, et c'est `haut` qui a lancé la partie. Aucune erreur nulle part : le script a fait son
travail, le jeu aussi, et la capture racontait autre chose que le test. **Écrire l'état attendu avant
de lancer le scénario, et le relire sur la capture** — ici, le bandeau disait « Pause » ou ne le
disait pas.

**⚠ Le clic de prise de focus de `piloter_jeu.py` est un VRAI clic dans le jeu.** Tant que le jeu
n'avait aucune interface cliquable, il était sans conséquence ; depuis le menu du 2026-08-28, il
peut activer ce qui se trouve au centre de la fenêtre. Le menu actuel y laisse un espace vide, mais
tout écran qui poserait un bouton au centre serait déclenché par l'outil de vérification lui-même.

**⚠ L'outil RESTAURE la position du curseur physique** après ce clic. Si la souris de la machine
repose au-dessus d'une entrée de menu, elle en fait la sélection courante, et le scénario clavier
qui suit valide une autre entrée que celle qu'on croyait. Écarter le curseur avant un scénario de
menu :

```
py -c "import ctypes; ctypes.windll.user32.SetCursorPos(1890, 12)"
```

**⚠ « cannot write empty image » à la capture signifie que la fenêtre a disparu**, pas que la
capture a échoué : le jeu s'est fermé (ou minimisé) pendant le scénario. Lire
`Build/Windows/player.log` — une fermeture propre s'y voit sans aucune exception, et c'est
justement ce qui la rend trompeuse.
