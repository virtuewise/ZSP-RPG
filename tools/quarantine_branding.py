"""Quarantine Commando-branded and third-party-watermarked extracted assets.

Run from the workspace root:
    python tools/quarantine_branding.py
"""

import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "ZSP_CLEAN" / "Assets" / "Extracted"
DST = ROOT / "ZSP_CLEAN" / "_Quarantine" / "CommandoBranding"
DST.mkdir(parents=True, exist_ok=True)

PATTERN = re.compile(
    r"p3dm|Commando|SpiritApps|Unity_Logo|splashTexture_|screen_3840|AD_ICON_|ad_\[|M_Logo|Modren|CF-Button|CF-gamepad",
    re.I,
)

moved = []
for path in SRC.rglob("*"):
    if not path.is_file():
        continue
    if PATTERN.search(path.name):
        rel = path.relative_to(SRC)
        destination = DST / rel
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(str(path), str(destination))
        moved.append((rel, destination.stat().st_size))

index = DST / "QUARANTINE_INDEX.csv"
with index.open("w", encoding="utf-8") as handle:
    handle.write("relative_path,size_bytes\n")
    for rel, size in sorted(moved):
        handle.write(f"{rel},{size}\n")

print(f"moved {len(moved)} files to {DST}")
for rel, _ in sorted(moved):
    print(rel)
