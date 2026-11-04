"""One-off: re-key the Python SIS engine and demo script to the Season One canon."""

from pathlib import Path

REPS = [
    ("nightjar_patrol", "cleaner_roster"),
    ("fingers_testimony", "tafadzwa_tip"),
    ("fingers_whistleblower", "tafadzwa_whistleblower"),
    ("tendai_informant", "lovemore_informant"),
    ("tendai_liability_applied", "lovemore_liability_applied"),
    ("tendai_liability", "lovemore_liability"),
    ("nyasha", "amara"),
    ("Nyasha", "Amara"),
    ("tariro", "tafadzwa"),
    ("Tariro", "Tafadzwa"),
    ("tendai", "lovemore"),
    ("Tendai", "Lovemore"),
    ("fingers", "tafadzwa"),
    ("Fingers", "Tafadzwa"),
    ("mukoma", "muponda"),
    ("Mukoma", "Muponda"),
    ("nightjar", "meridian"),
    ("Nightjar", "Meridian"),
]

for name in ("sis_v1.py", "sis_game.py"):
    path = Path(__file__).resolve().parent.parent / "Prototype" / name
    text = path.read_text(encoding="utf-8")
    for old, new in REPS:
        text = text.replace(old, new)
    path.write_text(text, encoding="utf-8")
print("re-keyed")
