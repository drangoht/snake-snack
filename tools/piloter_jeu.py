"""Lance le build Windows, lui injecte de vraies entrées, et capture sa fenêtre.

Pourquoi cet outil existe
-------------------------
« Ça compile » ne prouve rien sur un jeu. Un mapping clavier inversé, un personnage collé à un mur,
une balle qui sort du cadre, un menu qui ne réagit pas : aucun de ces défauts n'apparaît à la
compilation, et tous se voient en trente secondes sur une capture du jeu qui tourne.

Ce script permet de le faire **sans ouvrir l'éditeur et sans main humaine** — donc dans une boucle
d'agent. Chaque garde-fou qu'il contient correspond à une conclusion fausse qui a déjà été tirée.

Usage
-----
    py tools/piloter_jeu.py --lancer --attendre 4 --capture docs/verif.png
    py tools/piloter_jeu.py --touches "entree,bas,bas,entree" --capture docs/menu.png
    py tools/piloter_jeu.py --maintenir droite --duree 1.2 --capture docs/deplacement.png
    py tools/piloter_jeu.py --fermer

Les pièges déjà payés (ne pas les redécouvrir)
----------------------------------------------
1. **Le focus est LE point de blocage.** `SetForegroundWindow` seul échoue depuis un shell non
   interactif : la fenêtre reste en arrière-plan et Unity ne reçoit alors **aucune touche**. Ce qui
   marche : injecter un vrai clic dans la fenêtre, ce qui lui vaut le premier plan légitimement.
   Toujours vérifier `GetForegroundWindow() == hwnd` avant de conclure quoi que ce soit.
2. **`keybd_event` doit porter le CODE DE BALAYAGE**, pas seulement le code virtuel : le système
   d'entrée d'Unity lit le raw input.
3. **Les flèches exigent `KEYEVENTF_EXTENDEDKEY`** : sans lui, leur scan code est celui du pavé
   numérique et la touche est perdue en silence.
4. **La toute première touche après le lancement se perd** (le jeu vient de prendre le focus) :
   ce script amorce donc toujours par une touche pour rien.
5. **Le splash screen Unity dure ~2 s** : capturer avant, c'est capturer un logo.
6. **Le pare-feu Windows ouvre une alerte modale au premier lancement de CHAQUE nouveau chemin
   d'exe.** Elle vole le focus et grise la fenêtre. La fermer (`Get-Process PickerHost`) puis
   relancer, ou toujours rebâtir au même chemin.
7. **Ne pas coder en dur la position des éléments visés.** Un menu déplacé fait tomber les clics
   dans le vide — sans erreur, juste une capture qui montre autre chose que prévu.
8. **Les réglages sont persistants (PlayerPrefs).** Piloter une option par N appuis sur Droite
   donne un résultat *relatif* à la partie précédente : revenir à une extrémité connue d'abord.
"""

from __future__ import annotations

import argparse
import ctypes
import ctypes.wintypes as wt
import pathlib
import subprocess
import sys
import time

EXE = pathlib.Path(__file__).resolve().parent.parent / "Build" / "Windows" / "SnakeSnack.exe"
TITRE = "Snake Snack"

user32 = ctypes.windll.user32
user32.SetProcessDPIAware()

# --- Table des touches -------------------------------------------------------------
# (code virtuel, code de balayage, touche étendue ?). Les noms sont en français parce que c'est
# ainsi qu'on décrit un scénario de test ; les scan codes, eux, sont ceux d'un clavier QWERTY —
# voir la note AZERTY plus bas.
TOUCHES = {
    "entree":  (0x0D, 0x1C, False),
    "espace":  (0x20, 0x39, False),
    "echap":   (0x1B, 0x01, False),
    "tab":     (0x09, 0x0F, False),
    "gauche":  (0x25, 0x4B, True),
    "haut":    (0x26, 0x48, True),
    "droite":  (0x27, 0x4D, True),
    "bas":     (0x28, 0x50, True),
}
# Les lettres : scan codes des positions QWERTY.
# ⚠ SUR UN CLAVIER AZERTY, `Key.A` d'Unity tombe sous la touche marquée Q, `Key.W` sous Z, etc.
# Unity désigne toujours une POSITION PHYSIQUE, jamais le caractère imprimé. Les lettres dont la
# position diffère entre AZERTY et QWERTY (A, Q, Z, W, M) sont donc à proscrire pour un raccourci
# global : préférer Tab, R, les chiffres ou les flèches.
for _lettre, _vk, _sc in [
    ("a", 0x41, 0x1E), ("d", 0x44, 0x20), ("e", 0x45, 0x12), ("q", 0x51, 0x10),
    ("r", 0x52, 0x13), ("s", 0x53, 0x1F), ("w", 0x57, 0x11), ("z", 0x5A, 0x2C),
]:
    TOUCHES[_lettre] = (_vk, _sc, False)

KEYEVENTF_EXTENDEDKEY = 0x0001
KEYEVENTF_KEYUP = 0x0002
MOUSEEVENTF_LEFTDOWN = 0x0002
MOUSEEVENTF_LEFTUP = 0x0004


# --- Fenêtre -----------------------------------------------------------------------

def trouver_fenetre() -> int | None:
    """Retourne le handle de la fenêtre du jeu, ou None."""
    trouve = []

    @ctypes.WINFUNCTYPE(wt.BOOL, wt.HWND, wt.LPARAM)
    def rappel(hwnd, _):
        if not user32.IsWindowVisible(hwnd):
            return True
        longueur = user32.GetWindowTextLengthW(hwnd)
        if longueur == 0:
            return True
        tampon = ctypes.create_unicode_buffer(longueur + 1)
        user32.GetWindowTextW(hwnd, tampon, longueur + 1)
        if TITRE.lower() in tampon.value.lower():
            trouve.append(hwnd)
            return False
        return True

    user32.EnumWindows(rappel, 0)
    return trouve[0] if trouve else None


def attendre_fenetre(delai: float = 30.0) -> int:
    """Attend que la fenêtre du jeu apparaisse. Lève si elle ne vient pas."""
    fin = time.time() + delai
    while time.time() < fin:
        hwnd = trouver_fenetre()
        if hwnd:
            return hwnd
        time.sleep(0.3)
    raise RuntimeError(
        f"Fenetre « {TITRE} » introuvable apres {delai:.0f} s. "
        "Le jeu a-t-il plante au demarrage ? Lire le -logFile du player."
    )


def rectangle(hwnd: int) -> tuple[int, int, int, int]:
    rect = wt.RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(rect))
    return rect.left, rect.top, rect.right, rect.bottom


def donner_le_focus(hwnd: int) -> bool:
    """
    Met la fenêtre au premier plan — par un VRAI clic en son centre.

    `SetForegroundWindow` seul est refusé à un processus non interactif : Windows se contente de
    faire clignoter la barre des tâches. Un clic, lui, donne le focus légitimement. Le curseur est
    remis là où il était.
    """
    HWND_TOPMOST, SWP_NOMOVE, SWP_NOSIZE = -1, 0x0002, 0x0001
    user32.SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE)
    user32.SetForegroundWindow(hwnd)

    if user32.GetForegroundWindow() == hwnd:
        return True

    gauche, haut, droite, bas = rectangle(hwnd)
    ancien = wt.POINT()
    user32.GetCursorPos(ctypes.byref(ancien))

    user32.SetCursorPos((gauche + droite) // 2, (haut + bas) // 2)
    time.sleep(0.05)
    user32.mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0)
    time.sleep(0.03)
    user32.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)
    time.sleep(0.25)

    user32.SetCursorPos(ancien.x, ancien.y)
    return user32.GetForegroundWindow() == hwnd


# --- Entrées -----------------------------------------------------------------------

def _envoyer(nom: str, relacher: bool) -> None:
    if nom not in TOUCHES:
        raise SystemExit(f"Touche inconnue : « {nom} ». Connues : {', '.join(sorted(TOUCHES))}")
    vk, scan, etendue = TOUCHES[nom]
    drapeaux = KEYEVENTF_EXTENDEDKEY if etendue else 0
    if relacher:
        drapeaux |= KEYEVENTF_KEYUP
    user32.keybd_event(vk, scan, drapeaux, 0)


def appuyer(nom: str, duree: float = 0.06) -> None:
    """Un appui, maintenu `duree` seconde(s) puis relâché."""
    _envoyer(nom, False)
    time.sleep(duree)
    _envoyer(nom, True)


def amorcer() -> None:
    """
    Envoie une touche pour rien.

    La toute première touche après une prise de focus se perd systématiquement — le jeu vient de
    reprendre la main et son système d'entrée n'a pas encore resynchronisé ses périphériques. Sans
    cette amorce, le premier appui d'un scénario disparaît et le résultat semble aléatoire.
    """
    appuyer("bas")
    time.sleep(0.15)
    appuyer("haut")
    time.sleep(0.15)


# --- Capture -----------------------------------------------------------------------

def capturer(hwnd: int, destination: pathlib.Path) -> None:
    """
    Capture la FENÊTRE du jeu, jamais l'écran entier.

    Cadrer sur la fenêtre évite deux erreurs de lecture : un fond d'écran pris pour du décor, et
    des mesures de pixels faussées par ce qui déborde du jeu.
    """
    try:
        from PIL import ImageGrab
    except ImportError:
        raise SystemExit(
            "Pillow est requis pour la capture : py -m pip install pillow"
        )

    if not donner_le_focus(hwnd):
        print("!! La fenetre n'a PAS le focus : la capture montrera peut-etre autre chose.",
              file=sys.stderr)

    time.sleep(0.2)
    image = ImageGrab.grab(bbox=rectangle(hwnd), all_screens=True)
    destination.parent.mkdir(parents=True, exist_ok=True)
    image.save(destination)
    print(f"Capture : {destination} ({image.width} x {image.height})")


# --- Cycle de vie ------------------------------------------------------------------

def lancer() -> int:
    """
    Lance le jeu en fenêtré. Le plein écran rend la capture et la reprise de focus hasardeuses.
    """
    if not EXE.exists():
        raise SystemExit(
            f"Build absent : {EXE}\n"
            "Construire d'abord : powershell -File tools/build.ps1 "
            "-executeMethod SnakeSnack.EditorTools.BuildTools.RebuildEverything -logFile <log>"
        )

    journal = EXE.parent / "player.log"
    processus = subprocess.Popen([
        str(EXE),
        "-screen-width", "1280",
        "-screen-height", "720",
        "-screen-fullscreen", "0",
        "-logFile", str(journal),
    ])
    print(f"Lance (pid {processus.pid}) - journal : {journal}")
    return processus.pid


def fermer() -> None:
    hwnd = trouver_fenetre()
    if not hwnd:
        print("Aucune fenetre du jeu.")
        return
    user32.PostMessageW(hwnd, 0x0010, 0, 0)  # WM_CLOSE
    print("Fermeture demandee.")


def main() -> int:
    parseur = argparse.ArgumentParser(description="Pilote le build Windows du jeu.")
    parseur.add_argument("--lancer", action="store_true", help="lance l'executable en fenetre")
    parseur.add_argument("--attendre", type=float, default=4.0,
                         help="secondes avant d'agir (le splash Unity dure ~2 s)")
    parseur.add_argument("--touches", default="",
                         help="suite d'appuis separes par des virgules (ex. « entree,bas,entree »)")
    parseur.add_argument("--maintenir", default="", help="une touche maintenue")
    parseur.add_argument("--duree", type=float, default=0.9, help="duree du maintien, en secondes")
    parseur.add_argument("--capture", default="", help="chemin du PNG a ecrire")
    parseur.add_argument("--fermer", action="store_true", help="ferme la fenetre du jeu")
    args = parseur.parse_args()

    if args.fermer:
        fermer()
        return 0

    if args.lancer:
        lancer()

    hwnd = attendre_fenetre()
    time.sleep(args.attendre)

    if not donner_le_focus(hwnd):
        print("!! Impossible de donner le focus au jeu : les touches injectees seront perdues.",
              file=sys.stderr)
        print("   Verifier qu'aucune boite de dialogue (pare-feu Windows) ne le recouvre.",
              file=sys.stderr)
        return 1

    if args.touches or args.maintenir:
        amorcer()

    for nom in [t.strip() for t in args.touches.split(",") if t.strip()]:
        appuyer(nom)
        time.sleep(0.25)

    if args.maintenir:
        _envoyer(args.maintenir, False)
        time.sleep(args.duree)
        _envoyer(args.maintenir, True)

    if args.capture:
        capturer(hwnd, pathlib.Path(args.capture))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
