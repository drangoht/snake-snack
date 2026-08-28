# Pièges — Build web (WebGL)


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

