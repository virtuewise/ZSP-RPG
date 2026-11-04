#!/usr/bin/env python3
"""Static integrity checks for the ZSP Unity project and mirrored runtime content."""

from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXPECTED_UNITY = "6000.6.8f1"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def main() -> None:
    version_file = ROOT / "ProjectSettings" / "ProjectVersion.txt"
    if not version_file.is_file():
        fail("ProjectSettings/ProjectVersion.txt is missing")

    version_line = next(
        (line for line in version_file.read_text(encoding="utf-8").splitlines()
         if line.startswith("m_EditorVersion:")),
        "",
    )
    actual_version = version_line.partition(":")[2].strip()
    if actual_version != EXPECTED_UNITY:
        fail(f"Unity editor version is {actual_version!r}; expected {EXPECTED_UNITY}")

    resources = ROOT / "Assets" / "Resources" / "Content"
    streaming = ROOT / "Assets" / "StreamingAssets" / "Content"
    if not resources.is_dir() or not streaming.is_dir():
        fail("Resources/Content or StreamingAssets/Content directory is missing")

    resource_json = {p.relative_to(resources).as_posix(): p for p in resources.rglob("*.json")}
    streaming_json = {p.relative_to(streaming).as_posix(): p for p in streaming.rglob("*.json")}
    if not resource_json:
        fail("No JSON content files found under Assets/Resources/Content")
    if resource_json.keys() != streaming_json.keys():
        missing_runtime = sorted(resource_json.keys() - streaming_json.keys())
        missing_resource = sorted(streaming_json.keys() - resource_json.keys())
        fail(f"Content mirror file set differs; missing in StreamingAssets={missing_runtime}; "
             f"missing in Resources={missing_resource}")

    for relative, resource_path in sorted(resource_json.items()):
        runtime_path = streaming_json[relative]
        try:
            json.loads(resource_path.read_text(encoding="utf-8"))
            json.loads(runtime_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as exc:
            fail(f"Invalid JSON in {relative}: {exc}")
        if resource_path.read_bytes() != runtime_path.read_bytes():
            fail(f"Resources and StreamingAssets copies differ: {relative}")

    mission_dir = resources / "missions"
    mission_files = sorted(mission_dir.glob("*.json"))
    mission_ids: set[str] = set()
    for path in mission_files:
        payload = json.loads(path.read_text(encoding="utf-8"))
        for mission in payload.get("missions", []):
            mission_id = mission.get("id")
            if not mission_id or mission_id in mission_ids:
                fail(f"Missing or duplicate mission id in {path.relative_to(ROOT)}: {mission_id!r}")
            mission_ids.add(mission_id)
            beats = mission.get("beats", [])
            beat_ids = [beat.get("id") for beat in beats]
            if not beats or any(not beat_id for beat_id in beat_ids):
                fail(f"Mission {mission_id} has missing or empty beat IDs")
            if len(beat_ids) != len(set(beat_ids)):
                fail(f"Mission {mission_id} has duplicate beat IDs")
            if mission.get("start") not in set(beat_ids):
                fail(f"Mission {mission_id} start beat does not exist")

    expected_missions = {"M1", "M2", "M3"}
    if mission_ids != expected_missions:
        fail(f"Expected mission set {sorted(expected_missions)}, found {sorted(mission_ids)}")

    print(f"PASS: Unity {EXPECTED_UNITY}; {len(resource_json)} mirrored JSON files valid; "
          f"{len(mission_ids)} missions and their beat identifiers validated.")


if __name__ == "__main__":
    main()
