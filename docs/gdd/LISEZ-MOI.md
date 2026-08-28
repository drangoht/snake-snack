# docs/gdd/ — un système, un fichier

`docs/GDD.md` §4 est un **index** : une ligne par système, qui pointe ici. Le détail vit dans un
fichier par système (`deplacement.md`, `score.md`, `difficulte.md`…).

Pourquoi ce découpage : le GDD est relu avant chaque tâche de design ou d'implémentation, par le
principal **et** par chaque agent délégué. Un GDD monolithique fait payer le détail de tous les
systèmes à qui n'en touche qu'un — mesuré à 21 ko pour la seule §4 sur un Snake, soit ~5 400 jetons
rechargés à chaque tâche.

Ce que contient un fichier de système :

- **Ce qu'il fait**, en une phrase — la même que dans l'index.
- **Ses valeurs**, et surtout la mesure ou l'observation qui les justifie. Les chiffres eux-mêmes
  vivent dans `Assets/Scripts/Rules/` : ici on écrit *pourquoi* ils valent ça.
- **Ce qui a été essayé et écarté** pour ce système, avec la raison.

Plafond : ~150 lignes. Au-delà, c'est que le système en cache deux.
