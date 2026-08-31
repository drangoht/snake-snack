"""Launches the Windows build, injects real inputs into it, and captures its window.

Why this tool exists
--------------------
"It compiles" proves nothing about a game. An inverted key mapping, a character stuck to a wall, a
ball leaving the frame, a menu that does not react: none of those defects shows at compile time, and
all of them show in thirty seconds on a screenshot of the running game.

This script does it **without opening the editor and without a human hand** — so inside an agent
loop. Every guard rail it contains matches a false conclusion that has already been drawn.

Usage
-----
    py tools/drive_game.py --launch --wait 4 --capture docs/check.png
    py tools/drive_game.py --keys "enter,down,down,enter" --capture docs/menu.png
    py tools/drive_game.py --hold right --duration 1.2 --capture docs/movement.png
    py tools/drive_game.py --close

Traps already paid for (do not rediscover them)
-----------------------------------------------
1. **Focus is THE blocking point.** `SetForegroundWindow` alone fails from a non-interactive shell:
   the window stays in the background and Unity then receives **no key at all**. What works:
   injecting a real click into the window, which earns it the foreground legitimately. Always check
   `GetForegroundWindow() == hwnd` before concluding anything.
   ⚠ **From a background agent session, even the real click fails**: the process has never "received
   user input" and Windows refuses it the foreground. `give_focus` then lifts the foreground lock,
   primes itself with an ALT, and attaches to the target window's input queue — that is the path
   that works.
2. **`keybd_event` must carry the SCAN CODE**, not only the virtual code: Unity's input system reads
   raw input.
3. **Arrow keys require `KEYEVENTF_EXTENDEDKEY`**: without it, their scan code is the numeric
   keypad's and the key is lost silently.
4. **The very first key after launch is lost** (the game has just taken focus): this script therefore
   always primes with a key for nothing.
5. **The Unity splash screen lasts ~2 s**: capturing before that is capturing a logo.
6. **The Windows firewall opens a modal alert on the first launch of EACH new exe path.** It steals
   focus and greys the window. Close it (`Get-Process PickerHost`) then relaunch, or always rebuild
   to the same path.
7. **Do not hard-code the position of the elements aimed at.** A moved menu makes clicks land in the
   void — with no error, just a screenshot showing something other than expected.
8. **Settings are persistent (PlayerPrefs).** Driving an option with N presses of Right gives a
   result *relative* to the previous session: go back to a known extreme first.
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
TITLE = "Snake Snack"

user32 = ctypes.windll.user32
user32.SetProcessDPIAware()

# --- Key table ---------------------------------------------------------------------
# (virtual code, scan code, extended key?). The scan codes are those of a QWERTY keyboard — see the
# physical-layout note further down.
KEYS = {
    "enter":     (0x0D, 0x1C, False),
    "space":     (0x20, 0x39, False),
    "escape":    (0x1B, 0x01, False),
    "tab":       (0x09, 0x0F, False),
    "backspace": (0x08, 0x0E, False),  # Backspace: the menu from the pause screen (GDD §4.6)
    "left":      (0x25, 0x4B, True),
    "up":        (0x26, 0x48, True),
    "right":     (0x27, 0x4D, True),
    "down":      (0x28, 0x50, True),
}
# The letters: scan codes of the QWERTY positions.
# ⚠ ON AN AZERTY KEYBOARD, Unity's `Key.A` sits under the key printed Q, `Key.W` under Z, and so on.
# Unity always names a PHYSICAL POSITION, never the printed character. The letters whose position
# differs between AZERTY and QWERTY (A, Q, Z, W, M) are therefore to be avoided for a global
# shortcut: prefer Tab, R, the digits or the arrows.
for _letter, _vk, _sc in [
    ("a", 0x41, 0x1E), ("d", 0x44, 0x20), ("e", 0x45, 0x12), ("q", 0x51, 0x10),
    ("r", 0x52, 0x13), ("s", 0x53, 0x1F), ("w", 0x57, 0x11), ("z", 0x5A, 0x2C),
    # ⚠ "m" is the QWERTY POSITION of M, which on an AZERTY keyboard is the key printed ",".
    # The mute shortcut is resolved by PRINTED CHARACTER in the game
    # (`SnakeGame.ReadMuteKey`), so on an AZERTY machine this entry is NOT the one that mutes:
    # "m_azerty" below is. Both are here precisely so a test can tell the two apart.
    ("m", 0x4D, 0x32),
    ("m_azerty", 0xBA, 0x27),
]:
    KEYS[_letter] = (_vk, _sc, False)

KEYEVENTF_EXTENDEDKEY = 0x0001
KEYEVENTF_KEYUP = 0x0002
MOUSEEVENTF_LEFTDOWN = 0x0002
MOUSEEVENTF_LEFTUP = 0x0004

# Regaining the foreground from a non-interactive process — see give_focus().
SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001
SPIF_SENDCHANGE = 0x0002
SW_RESTORE = 9
VK_MENU = 0x12   # ALT
SCAN_ALT = 0x38


# --- Window ------------------------------------------------------------------------

def find_window() -> int | None:
    """Returns the handle of the game window, or None."""
    found = []

    @ctypes.WINFUNCTYPE(wt.BOOL, wt.HWND, wt.LPARAM)
    def callback(hwnd, _):
        if not user32.IsWindowVisible(hwnd):
            return True
        length = user32.GetWindowTextLengthW(hwnd)
        if length == 0:
            return True
        buffer = ctypes.create_unicode_buffer(length + 1)
        user32.GetWindowTextW(hwnd, buffer, length + 1)
        if TITLE.lower() in buffer.value.lower():
            found.append(hwnd)
            return False
        return True

    user32.EnumWindows(callback, 0)
    return found[0] if found else None


def wait_for_window(timeout: float = 30.0) -> int:
    """Waits for the game window to appear. Raises if it does not come."""
    deadline = time.time() + timeout
    while time.time() < deadline:
        hwnd = find_window()
        if hwnd:
            return hwnd
        time.sleep(0.3)
    raise RuntimeError(
        f"Window \"{TITLE}\" not found after {timeout:.0f} s. "
        "Did the game crash at startup? Read the player's -logFile."
    )


def rectangle(hwnd: int) -> tuple[int, int, int, int]:
    rect = wt.RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(rect))
    return rect.left, rect.top, rect.right, rect.bottom


def _lift_foreground_lock() -> None:
    """
    Cancels the delay during which Windows refuses to let a process steal the foreground.

    Without it, `SetForegroundWindow` "succeeds" (it returns TRUE) but merely flashes the taskbar:
    the window does not have focus, Unity receives nothing, and the test lies.
    """
    user32.SystemParametersInfoW(
        SPI_SETFOREGROUNDLOCKTIMEOUT, 0, ctypes.c_void_p(0), SPIF_SENDCHANGE)


def _prime_with_alt() -> None:
    """
    An ALT press-and-release, sent to nobody in particular.

    Windows only grants the right to come to the foreground to a process that has "received the last
    input". This press manufactures that input: it is the pass, not a command sent to the game. ALT
    rather than another key because it is bound to nothing in the game — a gameplay key would produce
    a phantom action here, before the scenario has even started.
    """
    user32.keybd_event(VK_MENU, SCAN_ALT, 0, 0)
    time.sleep(0.02)
    user32.keybd_event(VK_MENU, SCAN_ALT, KEYEVENTF_KEYUP, 0)
    time.sleep(0.02)


def _foreground_by_attaching(hwnd: int) -> bool:
    """
    Attaches to the target window's input queue, just long enough to give it focus.

    Attached, both threads share the same input state: from Windows's point of view, it is the window
    itself asking for the foreground, and the request is granted. Detaching sits in a `finally` —
    staying attached would tie this script's fate to the game's.
    """
    kernel32 = ctypes.windll.kernel32
    target_thread = user32.GetWindowThreadProcessId(hwnd, None)
    current_thread = kernel32.GetCurrentThreadId()

    attached = target_thread != current_thread and bool(
        user32.AttachThreadInput(current_thread, target_thread, True))
    try:
        user32.ShowWindow(hwnd, SW_RESTORE)
        user32.BringWindowToTop(hwnd)
        user32.SetForegroundWindow(hwnd)
        user32.SetActiveWindow(hwnd)
        user32.SetFocus(hwnd)
    finally:
        if attached:
            user32.AttachThreadInput(current_thread, target_thread, False)

    time.sleep(0.15)
    return user32.GetForegroundWindow() == hwnd


def _foreground_by_clicking(hwnd: int) -> bool:
    """A REAL click at the centre of the window. The cursor is put back where it was."""
    left, top, right, bottom = rectangle(hwnd)
    previous = wt.POINT()
    user32.GetCursorPos(ctypes.byref(previous))

    user32.SetCursorPos((left + right) // 2, (top + bottom) // 2)
    time.sleep(0.05)
    user32.mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0)
    time.sleep(0.03)
    user32.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)
    time.sleep(0.25)

    user32.SetCursorPos(previous.x, previous.y)
    return user32.GetForegroundWindow() == hwnd


def give_focus(hwnd: int) -> bool:
    """
    Brings the window to the foreground, by three increasingly insistent means.

    ⚠ **Focus is THE blocking point of this whole script.** Without focus, Unity receives no key and
    no mouse movement: the scenario runs to completion, the screenshot comes out, and it shows a game
    that received nothing. Hence the check of `GetForegroundWindow()` after each attempt, and the
    boolean return the caller must read.

    The order comes from what was observed on 2026-08-27 (`docs/pitfalls/tests-driving.md`):

    1. `SetWindowPos` to TOPMOST then `SetForegroundWindow` — enough when the shell is interactive.
    2. Foreground lock lifted, ALT priming, then input-queue attachment. It is the only path that
       works **from a background agent session**, where even a real click fails: the calling process
       has then never "received user input", and Windows refuses it the foreground whatever it does.
    3. The real click, as a last resort — it remains the most legitimate means in Windows's eyes, but
       it has the drawback of sending a click to the game.
    """
    HWND_TOPMOST, SWP_NOMOVE, SWP_NOSIZE = -1, 0x0002, 0x0001
    user32.SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE)
    user32.SetForegroundWindow(hwnd)

    if user32.GetForegroundWindow() == hwnd:
        return True

    _lift_foreground_lock()
    _prime_with_alt()

    if _foreground_by_attaching(hwnd):
        return True

    return _foreground_by_clicking(hwnd)


# --- Inputs ------------------------------------------------------------------------

def _send(name: str, release: bool) -> None:
    if name not in KEYS:
        raise SystemExit(f"Unknown key: \"{name}\". Known: {', '.join(sorted(KEYS))}")
    vk, scan, extended = KEYS[name]
    flags = KEYEVENTF_EXTENDEDKEY if extended else 0
    if release:
        flags |= KEYEVENTF_KEYUP
    user32.keybd_event(vk, scan, flags, 0)


def press(name: str, duration: float = 0.06) -> None:
    """One press, held for `duration` second(s) then released."""
    _send(name, False)
    time.sleep(duration)
    _send(name, True)


def prime() -> None:
    """
    Sends a key for nothing.

    The very first key after taking focus is lost systematically — the game has just regained control
    and its input system has not resynchronised its devices yet. Without this priming, the first press
    of a scenario disappears and the result looks random.

    ⚠ **The priming key must be one the game ignores.** It used to be Down then Up: in Snake Snack,
    where the game starts on the first applicable direction (GDD §4.1), that priming started the game
    and sent the snake south before the scenario had begun. The scenario then appeared to start from
    the initial pose while the snake had already moved — a discrepancy that raises nothing and skews
    every reading of a screenshot. Tab is bound to no command.
    """
    press("tab")
    time.sleep(0.15)
    press("tab")
    time.sleep(0.15)


# --- Capture -----------------------------------------------------------------------

# Recording width of the screenshots. A screenshot is READ by an agent, and an image costs in
# proportion to its area: 1280x720 costs ~1200 tokens, 960x540 ~700, for the same information (a menu
# is showing, the snake is in the right place, the score has moved).
# Pass --full-resolution when a pixel-level detail must be judged (aliasing, fine alignment).
CAPTURE_WIDTH = 960


def capture(hwnd: int, destination: pathlib.Path, width: int = CAPTURE_WIDTH) -> None:
    """
    Captures the game WINDOW, never the whole screen.

    Framing on the window avoids two reading errors: a desktop wallpaper mistaken for scenery, and
    pixel measurements skewed by whatever spills outside the game.
    """
    try:
        from PIL import Image, ImageGrab
    except ImportError:
        raise SystemExit(
            "Pillow is required for capturing: py -m pip install pillow"
        )

    if not give_focus(hwnd):
        print("!! The window does NOT have focus: the capture may show something else.",
              file=sys.stderr)

    time.sleep(0.2)
    image = ImageGrab.grab(bbox=rectangle(hwnd), all_screens=True)
    raw = (image.width, image.height)

    if width and image.width > width:
        height = round(image.height * width / image.width)
        image = image.resize((width, height), Image.LANCZOS)

    destination.parent.mkdir(parents=True, exist_ok=True)
    image.save(destination)
    if (image.width, image.height) != raw:
        print(f"Capture: {destination} ({image.width} x {image.height}, "
              f"reduced from {raw[0]} x {raw[1]})")
    else:
        print(f"Capture: {destination} ({image.width} x {image.height})")


# --- Lifecycle ---------------------------------------------------------------------

def launch() -> int:
    """
    Launches the game windowed. Full screen makes capturing and regaining focus unreliable.
    """
    if not EXE.exists():
        raise SystemExit(
            f"Build missing: {EXE}\n"
            "Build first: powershell -File tools/build.ps1"
        )

    log = EXE.parent / "player.log"
    process = subprocess.Popen([
        str(EXE),
        "-screen-width", "1280",
        "-screen-height", "720",
        "-screen-fullscreen", "0",
        "-logFile", str(log),
    ])
    print(f"Launched (pid {process.pid}) - log: {log}")
    return process.pid


def close() -> None:
    hwnd = find_window()
    if not hwnd:
        print("No game window.")
        return
    user32.PostMessageW(hwnd, 0x0010, 0, 0)  # WM_CLOSE
    print("Close requested.")


def main() -> int:
    parser = argparse.ArgumentParser(description="Drives the Windows build of the game.")
    parser.add_argument("--launch", action="store_true", help="launches the executable windowed")
    parser.add_argument("--wait", type=float, default=4.0,
                        help="seconds before acting (the Unity splash lasts ~2 s)")
    parser.add_argument("--keys", default="",
                        help="comma-separated sequence of presses (e.g. \"enter,down,enter\")")
    parser.add_argument("--hold", default="", help="a single held key")
    parser.add_argument("--duration", type=float, default=0.9, help="hold duration, in seconds")
    parser.add_argument("--capture", default="", help="path of the PNG to write")
    parser.add_argument("--full-resolution", action="store_true", dest="full_resolution",
                        help="do not shrink the capture (costly to read: ask for it only to judge a "
                             "pixel-level detail)")
    parser.add_argument("--close", action="store_true", help="closes the game window")
    args = parser.parse_args()

    if args.close:
        close()
        return 0

    if args.launch:
        launch()

    hwnd = wait_for_window()
    time.sleep(args.wait)

    if not give_focus(hwnd):
        print("!! Cannot give focus to the game: the injected keys will be lost.",
              file=sys.stderr)
        print("   Check that no dialog box (Windows firewall) is covering it.",
              file=sys.stderr)
        return 1

    if args.keys or args.hold:
        prime()

    for name in [k.strip() for k in args.keys.split(",") if k.strip()]:
        press(name)
        time.sleep(0.25)

    if args.hold:
        _send(args.hold, False)
        time.sleep(args.duration)
        _send(args.hold, True)

    if args.capture:
        capture(hwnd, pathlib.Path(args.capture),
                width=0 if args.full_resolution else CAPTURE_WIDTH)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
