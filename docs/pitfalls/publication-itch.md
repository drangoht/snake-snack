# Pièges — Publication (itch.io)


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
