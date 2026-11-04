"""Re-key documentation references to the official Season One canon."""

from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TARGETS = [
    ROOT / "Docs" / "PRIOR_ART.md",
    ROOT / "Docs" / "SHADOW_INTELLIGENCE_SYSTEM.md",
    ROOT / "Docs" / "VERTICAL_SLICE.md",
    ROOT / "Docs" / "ASSET_REPLACEMENT.md",
    ROOT / "Docs" / "ROADMAP.md",
    ROOT / "README.md",
    ROOT / "ZSP_SEASON_01_STORY_BIBLE.md",
]

REPS = [
    ("Nightjar", "Meridian"),
    ("nightjar", "meridian"),
    ("Kuda Moyo", "Tinashe Mukasa"),
    ("Nyasha Chikore", "Amara Chikowore"),
    ("Nyasha", "Amara"),
    ("Tariro Mupfumi", "Tafadzwa Dube"),
    ("Tariro", "Tafadzwa"),
    ("Tendai Chuma", "Lovemore Chigumba"),
    ("Tendai", "Lovemore"),
    ("Kuda", "Tinashe"),
    ("The Courier's Warning", "Episode 01: The Message"),
    ("14 tests", "22 tests"),
    ("# 14 passed", "# 22 passed"),
    ("`fingers_testimony`", "`tafadzwa_tip`"),
    ("you left Tendai alive", "you left Lovemore alive"),
]

for path in TARGETS:
    text = path.read_text(encoding="utf-8")
    for old, new in REPS:
        text = text.replace(old, new)
    path.write_text(text, encoding="utf-8")
print("docs re-keyed")
