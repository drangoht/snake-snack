# Crédits et licences tierces

Tout élément qui n'a pas été produit pour ce projet est listé ici, avec sa licence et l'attribution
qu'elle exige. ⚠ **Une entrée s'ajoute dans le commit qui introduit l'élément**, pas au moment de
publier : une licence oubliée ne se voit qu'après la mise en ligne.

## Polices

### Nunito — SIL Open Font License 1.1

- **Auteurs** : Vernon Adams, Cyreal, Jacques Le Bailly.
  `Copyright 2014 The Nunito Project Authors (https://github.com/googlefonts/nunito)`
- **Licence** : SIL Open Font License, Version 1.1. Texte intégral versé au dépôt **et embarqué dans
  le binaire** : `Assets/Resources/Polices/OFL.txt`.
- **Source** : `google/fonts`, `ofl/nunito/Nunito[wght].ttf` (fichier variable — c'est le seul que
  l'amont publie, il ne construit aucune graisse statique).
- **Modification** : instances statiques `wght=600` (SemiBold) et `wght=800` (ExtraBold) extraites
  par `fontTools.varLib.instancer`. La procédure exacte, reproductible, est
  `tools/generer_polices.py` — le sha256 du fichier amont y est épinglé.
- **Nom conservé** : l'`OFL.txt` de Nunito **ne déclare aucun Reserved Font Name** (sa ligne de
  copyright ne porte pas le suffixe `with Reserved Font Name`). La clause 5 de la SIL OFL, qui
  interdirait à une version modifiée de garder le nom, ne s'applique donc pas.
  ⚠ **Ce contrôle est à refaire pour toute autre famille** : la plupart en déclarent un, et le
  renommage devient alors obligatoire, pas optionnel.
