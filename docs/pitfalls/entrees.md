# Pièges — Entrées


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

