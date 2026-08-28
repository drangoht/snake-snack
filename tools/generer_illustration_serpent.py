"""
Génère l'illustration du serpent affichée par le menu principal.
================================================================

Pourquoi un générateur plutôt qu'un `.png` dessiné à la main : le brief `docs/art/menu.md` veut une
illustration **faite de la même matière que le jeu** — des carrés arrondis de la palette, posés le
long d'une spirale. Dessinée à la souris, elle serait irrejouable : personne ne saurait six mois
plus tard quelles couleurs, quel pas, quelle taille de segment ont été employés, et une retouche de
la palette (`docs/ART.md` §1) laisserait l'image en arrière sans que rien ne le signale.

⚠ **La palette n'est pas recopiée ici.** Elle est LUE dans `Assets/Scripts/UI/UiPalette.cs`, qui
reste « le seul endroit du dépôt où une couleur est écrite en dur » (CLAUDE.md). Un rôle renommé ou
supprimé fait échouer ce script avec un message explicite, au lieu de produire une image aux
anciennes couleurs.

Usage
-----
    py tools/generer_illustration_serpent.py            # écrit Assets/Resources/Illustrations/
    py tools/generer_illustration_serpent.py --apercu   # écrit aussi un aperçu sur le fond du jeu

⚠ Dépendance : `Pillow` (`py -m pip install Pillow`). Le script le dit s'il manque.

⚠ Après avoir lancé ce script, **relancer un build** (`& "tools/build.ps1"`) : un fichier écrit dans
`Assets/` n'existe pour Unity qu'une fois réimporté, et c'est le build en batchmode qui s'en charge
(`docs/pitfalls/assets-import.md`). C'est aussi lui qui produit le `.meta`, à committer avec le PNG.
"""

from __future__ import annotations

import argparse
import hashlib
import math
import pathlib
import re
import sys

try:
    from PIL import Image, ImageDraw
except ImportError:  # pragma: no cover - dépendance absente
    print("Pillow est requis : py -m pip install Pillow", file=sys.stderr)
    raise SystemExit(2)

RACINE = pathlib.Path(__file__).resolve().parent.parent

# ⚠ `Resources/` et non `Art/` : le menu est construit PAR CODE (`EcranMenu.Construire`), sans
# aucune référence sérialisée dans la scène — il ne peut donc charger l'image que par chemin,
# `Resources.Load<Sprite>("Illustrations/serpent-menu")`. Posée dans `Art/`, elle serait invisible au
# runtime et le seul symptôme serait un menu sans illustration (docs/pitfalls/assets-import.md).
DOSSIER_CIBLE = RACINE / "Assets" / "Resources" / "Illustrations"
NOM_FICHIER = "serpent-menu.png"

SOURCE_PALETTE = RACINE / "Assets" / "Scripts" / "UI" / "UiPalette.cs"

# Côté de l'image finale, en pixels. Le menu l'affiche à ~360 px de large dans le cadre de référence
# 1280×720 : exporter plus grand laisse de la marge aux écrans qui agrandissent la page itch.
COTE = 512

# Suréchantillonnage. Tout est dessiné à `COTE * FACTEUR` puis réduit en LANCZOS : c'est ce qui donne
# des bords nets sur des carrés tournés, que Pillow ne sait pas anticréner autrement.
FACTEUR = 4

# --- Géométrie du serpent -----------------------------------------------------------------
# Une spirale : la queue au centre, la tête qui sort par le haut. Les valeurs sont exprimées en
# pixels de l'image FINALE (avant suréchantillonnage), pour se relire contre le brief.

TOURS = 2.05               # nombre de tours de la spirale
RAYON_QUEUE = 30.0         # rayon au départ (queue, au centre)
RAYON_TETE = 158.0         # rayon à l'arrivée (tête, au bord)
PAS_SEGMENT = 27.0         # distance entre deux centres de segments, le long de la courbe
COTE_SEGMENT = 25.0        # côté d'un segment de corps à pleine taille
TAILLE_QUEUE = 0.48        # facteur d'échelle du segment de queue (il s'affine)
COTE_TETE = 40.0           # côté de la tête
DEGRADE_TETE = 4           # nombre de segments qui virent progressivement vers la couleur de tête

# La pomme, posée devant la tête, dans l'axe de son regard.
DISTANCE_POMME = 80.0      # distance du centre de la tête au centre de la pomme
DIAGONALE_POMME = 46.0     # diagonale du losange (c'est un carré tourné à 45°, comme en jeu)


def lire_palette() -> dict[str, tuple[int, int, int]]:
    """
    Extrait les rôles de couleur de `UiPalette.cs`.

    ⚠ Ne lit QUE la forme `Octets(0xNN, 0xNN, 0xNN)`. Les rôles écrits en `new Color(...)` — le voile
    de pause, le tampon de build — portent une transparence et ne servent pas à une illustration ;
    les ignorer silencieusement est ici volontaire, et l'appelant vérifie de toute façon que les
    rôles dont il a besoin sont présents.
    """
    if not SOURCE_PALETTE.exists():
        raise SystemExit(f"Palette introuvable : {SOURCE_PALETTE}")

    source = SOURCE_PALETTE.read_text(encoding="utf-8")
    motif = re.compile(
        r"public static readonly Color (\w+) = Octets\("
        r"0x([0-9A-Fa-f]{2}), 0x([0-9A-Fa-f]{2}), 0x([0-9A-Fa-f]{2})\);"
    )

    palette = {
        nom: (int(r, 16), int(v, 16), int(b, 16))
        for nom, r, v, b in motif.findall(source)
    }

    requis = ("CorpsSerpent", "TeteSerpent", "Pomme", "Fond", "Pictogramme")
    manquants = [nom for nom in requis if nom not in palette]
    if manquants:
        raise SystemExit(
            "Rôles absents de UiPalette.cs : " + ", ".join(manquants)
            + " — le générateur ne devine aucune couleur, corriger l'un ou l'autre."
        )

    return palette


def melange(a: tuple[int, int, int], b: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    """Interpolation linéaire entre deux couleurs de la palette (aucune couleur nouvelle n'est inventée)."""
    t = min(1.0, max(0.0, t))
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def courbe_spirale(echantillons: int = 4000) -> list[tuple[float, float]]:
    """
    La ligne médiane du corps, échantillonnée finement, en coordonnées image (y vers le bas).

    Le rayon croît en puissance 0,85 plutôt que linéairement : à croissance linéaire, les tours
    intérieurs se resserrent au point que les segments se recouvrent, et la spirale se lit comme une
    tache. L'exposant écarte les premiers tours et laisse voir le corps.
    """
    centre = COTE / 2.0
    points = []
    theta_max = TOURS * 2.0 * math.pi

    for i in range(echantillons + 1):
        avancement = i / echantillons
        theta = avancement * theta_max
        rayon = RAYON_QUEUE + (RAYON_TETE - RAYON_QUEUE) * (avancement ** 0.85)

        # -sin en y : la spirale tourne dans le sens horaire à l'écran, la tête ressort en haut.
        points.append((centre + rayon * math.cos(theta), centre - rayon * math.sin(theta)))

    return points


def poser_le_long(courbe: list[tuple[float, float]], pas: float) -> list[tuple[float, float, float]]:
    """
    Rend les positions et angles des segments, espacés d'une distance constante LE LONG de la courbe.

    ⚠ Un espacement constant en *paramètre* (un point tous les N échantillons) donnerait des segments
    serrés au centre et écartés au bord : sur une spirale, la longueur d'arc n'est pas proportionnelle
    à l'angle. C'est l'arc qu'on mesure ici.
    """
    poses = []
    parcouru = 0.0
    prochaine = 0.0  # le premier segment est posé sur le tout premier point : c'est la queue

    for i in range(1, len(courbe)):
        x0, y0 = courbe[i - 1]
        x1, y1 = courbe[i]
        longueur = math.hypot(x1 - x0, y1 - y0)
        if longueur <= 0.0:
            continue

        while parcouru + longueur >= prochaine:
            avancement = (prochaine - parcouru) / longueur
            # Angle visuel du sens de marche : y descend à l'écran, d'où le signe.
            angle = -math.degrees(math.atan2(y1 - y0, x1 - x0))
            poses.append((x0 + (x1 - x0) * avancement, y0 + (y1 - y0) * avancement, angle))
            prochaine += pas

        parcouru += longueur

    return poses


def carre_arrondi(cote: float, couleur: tuple[int, int, int], angle: float, rayon: float = 0.28) -> Image.Image:
    """Un segment : carré arrondi dessiné droit, puis tourné. Le rayon est une fraction du côté."""
    taille = max(2, int(round(cote * FACTEUR)))
    tuile = Image.new("RGBA", (taille, taille), (0, 0, 0, 0))
    dessin = ImageDraw.Draw(tuile)
    dessin.rounded_rectangle(
        (0, 0, taille - 1, taille - 1), radius=max(1, int(taille * rayon)), fill=couleur + (255,)
    )
    return tuile.rotate(angle, resample=Image.BICUBIC, expand=True)


def coller_centre(fond: Image.Image, tuile: Image.Image, x: float, y: float) -> None:
    """Colle une tuile en la centrant sur (x, y), exprimés en pixels de l'image finale."""
    cx = int(round(x * FACTEUR - tuile.width / 2.0))
    cy = int(round(y * FACTEUR - tuile.height / 2.0))
    fond.alpha_composite(tuile, (cx, cy))


def dessiner_tete(cote: float, palette: dict, angle: float) -> Image.Image:
    """
    La tête : un carré arrondi plus gros, deux yeux et une langue, dessinés **tête vers la droite**
    puis tournés d'un bloc — les yeux suivent ainsi le sens de marche sans calcul supplémentaire.
    """
    taille = int(round(cote * FACTEUR))
    marge = int(taille * 0.55)  # place pour la langue, qui dépasse devant
    tuile = Image.new("RGBA", (taille + 2 * marge, taille + 2 * marge), (0, 0, 0, 0))
    dessin = ImageDraw.Draw(tuile)

    gauche, haut = marge, marge
    droite, bas = marge + taille, marge + taille

    # La langue d'abord : elle passe SOUS la tête, seule sa pointe dépasse.
    epaisseur = max(2, int(taille * 0.09))
    milieu = haut + taille / 2.0
    avant = droite + taille * 0.42
    dessin.line([(droite - taille * 0.1, milieu), (avant, milieu)], fill=palette["Pomme"] + (255,), width=epaisseur)
    dessin.line([(avant, milieu), (avant + taille * 0.16, milieu - taille * 0.16)],
                fill=palette["Pomme"] + (255,), width=epaisseur)
    dessin.line([(avant, milieu), (avant + taille * 0.16, milieu + taille * 0.16)],
                fill=palette["Pomme"] + (255,), width=epaisseur)

    dessin.rounded_rectangle((gauche, haut, droite, bas), radius=int(taille * 0.32),
                             fill=palette["TeteSerpent"] + (255,))

    # Les yeux sont du bleu-noir du fond : le seul rôle assez sombre pour trancher sur la tête
    # claire sans introduire une couleur qui n'existe nulle part ailleurs dans le jeu.
    rayon_oeil = taille * 0.11
    for decalage in (-1, 1):
        cx = gauche + taille * 0.66
        cy = milieu + decalage * taille * 0.24
        dessin.ellipse((cx - rayon_oeil, cy - rayon_oeil, cx + rayon_oeil, cy + rayon_oeil),
                       fill=palette["Fond"] + (255,))

    return tuile.rotate(angle, resample=Image.BICUBIC, expand=True)


def dessiner_pomme(palette: dict) -> Image.Image:
    """
    La pomme : un carré tourné à 45°, exactement comme en jeu (`VuePlateau.ConstruirePomme`).

    ⚠ La forme porte l'information avant la couleur (`docs/ART.md` §4) : le losange doit rester un
    losange dans l'illustration, sans quoi le menu annonce une pomme que le jeu ne montre pas.
    """
    cote = DIAGONALE_POMME / math.sqrt(2.0)
    losange = carre_arrondi(cote, palette["Pomme"], 45.0, rayon=0.18)

    # Un éclat blanc minuscule, en haut à gauche : il donne le volume sans introduire de couleur —
    # le blanc pur est déjà un rôle de la palette (`Pictogramme`).
    eclat = Image.new("RGBA", losange.size, (0, 0, 0, 0))
    dessin = ImageDraw.Draw(eclat)
    r = losange.width * 0.065
    cx, cy = losange.width * 0.36, losange.height * 0.34
    dessin.ellipse((cx - r, cy - r, cx + r, cy + r), fill=palette["Pictogramme"] + (170,))
    return Image.alpha_composite(losange, eclat)


def composer(palette: dict) -> Image.Image:
    image = Image.new("RGBA", (COTE * FACTEUR, COTE * FACTEUR), (0, 0, 0, 0))

    courbe = courbe_spirale()
    poses = poser_le_long(courbe, PAS_SEGMENT)
    if len(poses) < 6:
        raise SystemExit("La spirale n'a produit que %d segments : revoir PAS_SEGMENT." % len(poses))

    corps = poses[:-1]
    tete_x, tete_y, tete_angle = poses[-1]

    dernier = len(corps) - 1
    for index, (x, y, angle) in enumerate(corps):
        avancement = index / dernier if dernier else 1.0

        # La queue s'affine, et les derniers segments virent vers la couleur de tête : le regard
        # trouve la tête avant d'avoir suivi le corps.
        echelle = TAILLE_QUEUE + (1.0 - TAILLE_QUEUE) * (avancement ** 0.6)
        proximite = max(0.0, (index - (dernier - DEGRADE_TETE)) / DEGRADE_TETE) if DEGRADE_TETE else 0.0
        couleur = melange(palette["CorpsSerpent"], palette["TeteSerpent"], proximite * 0.55)

        coller_centre(image, carre_arrondi(COTE_SEGMENT * echelle, couleur, angle), x, y)

    coller_centre(image, dessiner_tete(COTE_TETE, palette, tete_angle), tete_x, tete_y)

    # La pomme est posée DANS L'AXE du regard : c'est ce qui raconte la boucle de jeu en une image,
    # plutôt qu'un serpent et une pomme simplement voisins.
    radians = math.radians(tete_angle)
    pomme_x = tete_x + math.cos(radians) * DISTANCE_POMME
    pomme_y = tete_y - math.sin(radians) * DISTANCE_POMME
    coller_centre(image, dessiner_pomme(palette), pomme_x, pomme_y)

    return recadrer(image).resize((COTE, COTE), resample=Image.LANCZOS)


def recadrer(image: Image.Image) -> Image.Image:
    """
    Recentre le dessin sur un carré, d'après ce qui est réellement opaque.

    ⚠ Ce n'est pas de la cosmétique : le menu pose l'image dans un rectangle fixe, et c'est le
    CENTRE DU FICHIER qui tombe au centre de ce rectangle. Sans recadrage, la moindre retouche des
    constantes ci-dessus (un tour de plus, une pomme plus loin) décale l'illustration dans la mise en
    page du menu sans qu'on ait touché au menu — et on va corriger dans le mauvais fichier.
    """
    boite = image.getbbox()
    if boite is None:
        raise SystemExit("L'illustration est entierement transparente : rien n'a ete dessine.")

    contenu = image.crop(boite)
    cote = int(max(contenu.width, contenu.height) * 1.06)  # 3 % de marge de chaque côté
    carre = Image.new("RGBA", (cote, cote), (0, 0, 0, 0))
    carre.alpha_composite(contenu, ((cote - contenu.width) // 2, (cote - contenu.height) // 2))
    return carre


def main() -> int:
    analyseur = argparse.ArgumentParser(description="Illustration du serpent du menu principal.")
    analyseur.add_argument("--apercu", action="store_true",
                           help="écrit aussi docs/verif-illustration-menu.png, l'image posée sur le fond réel du jeu")
    arguments = analyseur.parse_args()

    palette = lire_palette()
    image = composer(palette)

    DOSSIER_CIBLE.mkdir(parents=True, exist_ok=True)
    cible = DOSSIER_CIBLE / NOM_FICHIER
    image.save(cible)

    empreinte = hashlib.sha256(cible.read_bytes()).hexdigest()[:16]
    # Messages ASCII : la console Windows est en cp1252 et un caractere accentue y leve
    # UnicodeEncodeError, ce qui ferait echouer le script APRES avoir ecrit son image.
    print(f"Ecrit : {cible.relative_to(RACINE)} ({COTE}x{COTE}, sha256 {empreinte})")

    if arguments.apercu:
        # ⚠ `docs/ART.md` §4 : tout sprite se valide sur le FOND RÉEL du jeu, jamais sur un damier.
        fond = Image.new("RGBA", image.size, palette["Fond"] + (255,))
        apercu = RACINE / "docs" / "verif-illustration-menu.png"
        Image.alpha_composite(fond, image).save(apercu)
        print(f"Apercu : {apercu.relative_to(RACINE)}")

    print("Relancer un build : Unity importera le PNG et produira son .meta, a committer avec.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
