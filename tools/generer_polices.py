"""
Génère les polices statiques du jeu à partir du fichier variable amont.
=======================================================================

Pourquoi ce script existe plutôt qu'un `.ttf` déposé à la main : `docs/art/typographie.md` §2.2
retient **Nunito**, mais `google/fonts` ne la publie **qu'en fichier variable**
(`Nunito[wght].ttf`) — l'amont porte `buildStatic: false`, les graisses statiques ne sont pas en
retard de publication, elles ne sont jamais construites. L'auteur a tranché : on **instancie** deux
graisses figées depuis le variable, au lieu de changer de famille.

Une police téléchargée puis instanciée à la main serait irreproductible : personne ne saurait six
mois plus tard de quelle source, à quel poids, ni avec quelle version de `fonttools` viennent les
deux fichiers du dépôt. D'où ce générateur versionné.

Usage
-----
    py tools/generer_polices.py              # récupère, instancie, vérifie, écrit
    py tools/generer_polices.py --verifier   # ne réécrit rien, revérifie ce qui est déjà dans Assets/

⚠ Dépendance : `fonttools` (`py -m pip install fonttools`). Le script le dit s'il manque.
"""

from __future__ import annotations

import argparse
import hashlib
import io
import pathlib
import sys
import urllib.request

# --- Amont -------------------------------------------------------------------------------

# ⚠ URL listée, jamais devinée : le dossier `ofl/nunito` a été énuméré par
# `GET https://api.github.com/repos/google/fonts/contents/ofl/nunito` avant d'écrire ces deux
# lignes. Deviner une URL `static/...` rend une page 404 de 39 Ko qui ressemble à un téléchargement
# réussi — c'est le piège de `docs/pitfalls/polices-texte.md`.
URL_VARIABLE = "https://raw.githubusercontent.com/google/fonts/main/ofl/nunito/Nunito%5Bwght%5D.ttf"
URL_LICENCE = "https://raw.githubusercontent.com/google/fonts/main/ofl/nunito/OFL.txt"

# Empreinte du fichier amont au jour où ces polices ont été produites (2026-08-28). `main` bouge :
# sans cette empreinte, une régénération silencieuse un an plus tard produirait d'autres fichiers
# que ceux du dépôt, sans que rien ne le signale. Si elle ne correspond plus, c'est que l'amont a
# publié une nouvelle version : la relire, revérifier la `cmap`, PUIS mettre cette constante à jour.
EMPREINTE_AMONT = "bb55a5ca5c2042335b3991af27c4d0705d0ef41cac6164ac737fd8f2a1e85207"

# --- Ce qu'on produit --------------------------------------------------------------------

# Les deux seules graisses du jeu (`docs/art/typographie.md` §2.2). Pas de Regular : à ces tailles
# et sur un rendu WebGL redimensionné, un trait fin de police ronde disparaît avant de se lire.
GRAISSES = {
    "Nunito-SemiBold.ttf": 600,
    "Nunito-ExtraBold.ttf": 800,
}

# ⚠ `Resources/` et non `Art/` : le HUD est construit **par code** (`HudJeu.Construire`), sans
# aucune référence sérialisée — il ne peut donc charger la police que PAR CHEMIN,
# `Resources.Load<Font>("Polices/Nunito-SemiBold")`. Posée dans `Art/`, elle serait invisible au
# runtime, et le seul symptôme serait un texte qui ne se dessine pas (docs/pitfalls/assets-import.md).
DOSSIER_CIBLE = pathlib.Path("Assets/Resources/Polices")

# --- Couverture de glyphes exigée --------------------------------------------------------

# `docs/art/typographie.md` §2.2 : l'ASCII imprimable, plus TOUT le jeu d'accents français — pas
# seulement le « é » qu'utilise `TextesUi.cs` aujourd'hui, sinon le prochain texte ajouté oblige à
# rouvrir le sujet. ⚠ Un glyphe manquant se replie sur une police système AU BUREAU et disparaît
# EN SILENCE dans un navigateur : cette vérification est la seule barrière avant le build web.
ACCENTS_FRANCAIS = "àâäçéèêëîïôöùûüÀÂÄÇÉÈÊËÎÏÔÖÙÛÜ"
CARACTERES_EXIGES = "".join(chr(c) for c in range(32, 127)) + ACCENTS_FRANCAIS


def racine_depot() -> pathlib.Path:
    """Racine du dépôt, déduite de l'emplacement du script — aucun chemin en dur."""
    return pathlib.Path(__file__).resolve().parents[1]


def telecharger(url: str) -> bytes:
    with urllib.request.urlopen(url, timeout=120) as reponse:
        return reponse.read()


def verifier_signature_truetype(donnees: bytes, origine: str) -> None:
    """
    Un vrai TTF commence par `00 01 00 00`.

    Sans ce contrôle, une page d'erreur HTML de quelques dizaines de Ko passe pour un
    téléchargement réussi et n'échoue que bien plus tard, à l'import Unity.
    """
    if donnees[:4] != b"\x00\x01\x00\x00":
        raise SystemExit(
            origine + " n'est pas un TrueType : signature " + donnees[:4].hex()
            + " (un vrai TTF commence par 00010000). Page d'erreur deguisee ?"
        )


def controler_glyphes(chemin: pathlib.Path) -> None:
    """Lit la table `cmap` du fichier RÉELLEMENT produit, et refuse le moindre manque."""
    from fontTools.ttLib import TTFont

    with TTFont(chemin) as police:
        couverts = set(police.getBestCmap().keys())

    manquants = [c for c in CARACTERES_EXIGES if ord(c) not in couverts]
    if manquants:
        raise SystemExit(
            chemin.name + " : " + str(len(manquants))
            + " caractere(s) exige(s) absent(s) de la cmap : " + " ".join(manquants)
        )
    print("  cmap OK : " + str(len(CARACTERES_EXIGES)) + " caracteres exiges, tous presents ("
          + str(len(couverts)) + " au total dans la police)")


def nom_de_famille(chemin: pathlib.Path) -> str:
    """Nom de famille tel que la police le déclare — sert à prouver la conformité OFL."""
    from fontTools.ttLib import TTFont

    with TTFont(chemin) as police:
        famille = police["name"].getDebugName(1)
        sous_famille = police["name"].getDebugName(2)
    return str(famille) + " / " + str(sous_famille)


def instancier(donnees_variable: bytes, poids: int, destination: pathlib.Path) -> None:
    from fontTools.ttLib import TTFont
    from fontTools.varLib import instancer

    police = TTFont(io.BytesIO(donnees_variable))
    # `updateFontNames` réécrit la table `name` d'après la table STAT : l'instance se déclare
    # « Nunito SemiBold », pas « Nunito » avec un poids invisible. Sans ça, deux fichiers portant
    # le même nom de famille et la même sous-famille se marchent dessus dans l'éditeur.
    instancer.instantiateVariableFont(police, {"wght": poids}, inplace=True, updateFontNames=True)
    destination.parent.mkdir(parents=True, exist_ok=True)
    police.save(destination)
    police.close()


def main() -> int:
    parseur = argparse.ArgumentParser(description="Genere les polices statiques du jeu.")
    parseur.add_argument("--verifier", action="store_true",
                         help="ne regenere rien : revalide la cmap des fichiers deja presents")
    args = parseur.parse_args()

    try:
        import fontTools  # noqa: F401
    except ImportError:
        raise SystemExit("fonttools est absent. Installer avec : py -m pip install fonttools")

    cible = racine_depot() / DOSSIER_CIBLE

    if args.verifier:
        for nom in GRAISSES:
            chemin = cible / nom
            if not chemin.exists():
                raise SystemExit(str(chemin) + " est absent : lancer le script sans --verifier.")
            print(nom + " (" + nom_de_famille(chemin) + ")")
            controler_glyphes(chemin)
        return 0

    print("Recuperation de l'amont : " + URL_VARIABLE)
    variable = telecharger(URL_VARIABLE)
    verifier_signature_truetype(variable, "Le fichier amont")

    empreinte = hashlib.sha256(variable).hexdigest()
    if empreinte != EMPREINTE_AMONT:
        raise SystemExit(
            "L'amont a change (sha256 " + empreinte + " au lieu de " + EMPREINTE_AMONT + ").\n"
            "Ce n'est pas forcement une erreur : relire la nouvelle version, revalider la cmap, "
            "puis mettre EMPREINTE_AMONT a jour dans ce script."
        )
    print("  amont conforme (" + str(len(variable)) + " octets, sha256 verifie)")

    for nom, poids in GRAISSES.items():
        destination = cible / nom
        instancier(variable, poids, destination)
        print(nom + " : wght=" + str(poids) + ", " + str(destination.stat().st_size)
              + " octets (" + nom_de_famille(destination) + ")")
        controler_glyphes(destination)

    # La SIL OFL exige que la licence accompagne le Font Software, y compris redistribue dans un
    # binaire de jeu. Elle vit donc a cote des .ttf, pas seulement dans docs/CREDITS.md.
    licence = telecharger(URL_LICENCE)
    (cible / "OFL.txt").write_bytes(licence)
    print("OFL.txt : " + str(len(licence)) + " octets")

    return 0


if __name__ == "__main__":
    sys.exit(main())
