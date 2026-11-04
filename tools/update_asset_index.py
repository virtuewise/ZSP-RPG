"""Remove quarantined rows from ASSET_INDEX.csv and report the new totals."""

import csv
from collections import Counter
from pathlib import Path

root = Path(__file__).resolve().parent.parent / "ZSP_CLEAN"
index = root / "ASSET_INDEX.csv"
rows = list(csv.DictReader(index.open(encoding="utf-8")))
kept = []
removed = []
for row in rows:
    candidate = root / "Assets" / "Extracted" / row["path"]
    if candidate.exists():
        kept.append(row)
    else:
        removed.append(row)
with index.open("w", encoding="utf-8", newline="") as handle:
    writer = csv.DictWriter(handle, fieldnames=["type", "name", "path", "bytes"])
    writer.writeheader()
    writer.writerows(kept)
print(f"kept {len(kept)} rows; removed {len(removed)} quarantined rows")
for asset_type, count in Counter(row["type"] for row in kept).most_common():
    print(f"  {asset_type}: {count}")
