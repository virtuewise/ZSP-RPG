"""Re-key Unity C# from the interim arc to the Season One canon."""

from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TARGETS = list((ROOT / "Assets" / "Scripts").rglob("*.cs"))

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
    ("mhofu", "victor"),
    ("Mhofu", "Victor"),
    ("mukoma", "muponda"),
    ("Mukoma", "Muponda"),
    ("nightjar", "meridian"),
    ("Nightjar", "Meridian"),
    ("CIVIC_FRONT", "LEDGER"),
    ("civic_front", "ledger"),
    ("Civic Front", "The Ledger"),
]

for path in TARGETS:
    text = path.read_text(encoding="utf-8")
    for old, new in REPS:
        text = text.replace(old, new)
    path.write_text(text, encoding="utf-8")
print(f"re-keyed {len(TARGETS)} files")
