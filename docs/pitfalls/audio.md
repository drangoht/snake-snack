# Pièges — Audio


**⚠ Une entrée absente de la table de correspondance est MUETTE.** [hérité] Quatorze armes l'ont été
sans que rien ne le dise. Écrire un audit qui compare la liste du contenu à la table des sons.

**⚠ Le navigateur ne laisse aucun son démarrer avant un geste de l'utilisateur.** Unity ouvre son
contexte audio suspendu : sans le réveil posé dans le gabarit WebGL, la musique ne se déclenche qu'au
hasard d'un clic.

**⚠ Un log de `PlayOneShot` prouve une intention, pas un son.** Pour prouver que l'audio sort du
mixeur : `AudioListener.GetOutputData(buffer, 0)` et logger le RMS.

