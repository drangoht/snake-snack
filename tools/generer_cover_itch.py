"""
Génère la couverture 630 × 500 de la page itch.io.
==================================================

Pourquoi un générateur plutôt qu'une capture recadrée : la cover est la **seule image que voient les
visiteurs qui n'ouvrent pas la page** (`docs/ITCH_STORE_PAGE.md`), et elle est lue en vignette, à
côté de dizaines d'autres. Une capture d'écran recadrée y perd son titre ou son sujet — le menu du
jeu pose le texte à gauche et le serpent à droite, un ratio 16:9 qu'aucun recadrage en 1.26 ne
conserve entier.

⚠ **La palette n'est pas recopiée ici**, comme dans `generer_illustration_serpent.py` : elle est LUE
dans `Assets/Scripts/UI/UiPalette.cs`. Une cover aux anciennes couleurs après une retouche de la
palette ne se verrait jamais — personne ne rouvre une image publiée il y a trois versions.

⚠ **L'illustration n'est pas redessinée non plus** : on réemploie le PNG déjà produit pour le menu,
pour que la vignette et le premier écran du jeu montrent exactement le même serpent. Un visiteur qui
ne retrouve pas l'image de la vignette en lançant le jeu a le sentiment d'avoir cliqué sur autre
chose.

Usage
-----
    py tools/generer_cover_itch.py            # écrit docs/itch/cover.png

⚠ Dépendance : `Pillow` (`py -m pip install Pillow`). Le script le dit s'il manque.

⚠ Cette image ne va PAS dans `Assets/` : elle n'est jamais chargée par le jeu, seulement téléversée
sur itch.io à la main. L'y poser l'embarquerait dans le binaire pour rien.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError:
    sys.exit("Pillow manquant : py -m pip install Pillow")

RACINE = Path(__file__).resolve().parent.parent

SOURCE_PALETTE = RACINE / "Assets" / "Scripts" / "UI" / "UiPalette.cs"
SOURCE_ILLUSTRATION = RACINE / "Assets" / "Resources" / "Illustrations" / "serpent-menu.png"
POLICE_TITRE = RACINE / "Assets" / "Resources" / "Polices" / "Nunito-ExtraBold.ttf"
POLICE_TEXTE = RACINE / "Assets" / "Resources" / "Polices" / "Nunito-SemiBold.ttf"

CIBLE = RACINE / "docs" / "itch" / "cover.png"

# ⚠ 630 × 500 n'est pas un choix : c'est le format qu'itch.io attend pour une cover. Toute autre
# dimension est redimensionnée par le site, et un texte fin en ressort illisible.
LARGEUR = 630
HAUTEUR = 500

# Suréchantillonnage, comme pour l'illustration : tout est dessiné en grand puis réduit en LANCZOS.
FACTEUR = 3


def lire_palette() -> dict[str, tuple[int, int, int]]:
    """Extrait les rôles de couleur de `UiPalette.cs` (mêmes règles que le générateur du serpent)."""
    if not SOURCE_PALETTE.exists():
        raise SystemExit(f"Palette introuvable : {SOURCE_PALETTE}")

    motif = re.compile(
        r"public static readonly Color (\w+) = Octets\("
        r"0x([0-9A-Fa-f]{2}), 0x([0-9A-Fa-f]{2}), 0x([0-9A-Fa-f]{2})\);"
    )
    palette = {
        nom: (int(r, 16), int(v, 16), int(b, 16))
        for nom, r, v, b in motif.findall(SOURCE_PALETTE.read_text(encoding="utf-8"))
    }

    requis = ("Fond", "TexteHud", "TexteSecondaire", "Pomme")
    manquants = [nom for nom in requis if nom not in palette]
    if manquants:
        raise SystemExit(
            "Rôles absents de UiPalette.cs : " + ", ".join(manquants)
            + " — le générateur ne devine aucune couleur, corriger l'un ou l'autre."
        )
    return palette


def composer(palette: dict[str, tuple[int, int, int]]) -> Image.Image:
    largeur, hauteur = LARGEUR * FACTEUR, HAUTEUR * FACTEUR
    image = Image.new("RGB", (largeur, hauteur), palette["Fond"])

    # L'illustration d'abord, dans les deux tiers bas : elle porte le regard, le titre respire
    # au-dessus. ⚠ Elle tient ENTIÈRE dans le cadre — une spirale rognée par le bord se lit comme
    # une image mal cadrée, pas comme un parti pris.
    if not SOURCE_ILLUSTRATION.exists():
        raise SystemExit(
            f"Illustration introuvable : {SOURCE_ILLUSTRATION}\n"
            "Lancer d'abord : py tools/generer_illustration_serpent.py"
        )
    serpent = Image.open(SOURCE_ILLUSTRATION).convert("RGBA")
    cote = int(hauteur * 0.66)
    serpent = serpent.resize((cote, cote), Image.LANCZOS)
    image.paste(serpent, ((largeur - cote) // 2, hauteur - cote - int(hauteur * 0.03)), serpent)

    dessin = ImageDraw.Draw(image)

    # Le titre, en haut, en capitales : c'est le seul élément qui doit rester lisible réduit à une
    # vignette de 150 px de large dans une grille de jeux.
    titre = ImageFont.truetype(str(POLICE_TITRE), int(hauteur * 0.135))
    _texte_centre(dessin, largeur, int(hauteur * 0.10), "SNAKE SNACK", titre, palette["TexteHud"])

    # La tagline : la phrase du GDD §1, pas un slogan inventé pour l'occasion.
    tagline = ImageFont.truetype(str(POLICE_TEXTE), int(hauteur * 0.048))
    _texte_centre(
        dessin, largeur, int(hauteur * 0.235),
        "Il s'allonge à chaque bouchée.", tagline, palette["TexteSecondaire"],
    )

    return image.resize((LARGEUR, HAUTEUR), Image.LANCZOS)


def _texte_centre(dessin, largeur: int, y: int, texte: str, police, couleur) -> None:
    """Centre horizontalement, `y` étant le haut du texte."""
    gauche, haut, droite, _ = dessin.textbbox((0, 0), texte, font=police)
    dessin.text(((largeur - (droite - gauche)) // 2 - gauche, y - haut), texte, font=police, fill=couleur)


def main() -> int:
    palette = lire_palette()
    for police in (POLICE_TITRE, POLICE_TEXTE):
        if not police.exists():
            raise SystemExit(
                f"Police introuvable : {police}\nLancer d'abord : py tools/generer_polices.py"
            )

    CIBLE.parent.mkdir(parents=True, exist_ok=True)
    composer(palette).save(CIBLE)
    print(f"Cover ecrite : {CIBLE} ({LARGEUR}x{HAUTEUR})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
