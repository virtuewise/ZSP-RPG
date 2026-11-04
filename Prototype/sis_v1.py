"""Shadow Intelligence System (SIS) v1 - core reference implementation.

This module is the executable specification for the system described in
Docs/SHADOW_INTELLIGENCE_SYSTEM.md. It deliberately has no third-party
dependencies so it can run beside the Unity project and inside CI.

The SIS is a consequence engine for a long-form, authored story:

* Every mission records a behaviour event stream.
* Mission outcomes are committed to an append-only, hash-chained ledger.
* State is re-derived by deterministic replay, so contradictions are detectable.
* Contacts have a trust/exposure graph. Repeated reliance exposes them.
* Witnesses and key characters persist across missions.
* Authored missions have fixed narrative invariants, but their *conditions*
  (entry security, enemy profile, witness reappearances, available routes,
  dialogue variants) are resolved from the consequence state.
"""

from __future__ import annotations

import copy
import hashlib
import json
import os
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Sequence, Tuple

DEFAULT_CONTENT_DIR = Path(__file__).resolve().parent.parent / "Assets" / "StreamingAssets" / "Content"

KIND_BY_VALUE = {1: "front", 2: "side", 3: "social", 4: "stealth", 5: "insider"}

COMPROMISE_RULES = {
    "amara": (0.55, 3),
    "tafadzwa": (0.40, 2),
    "muponda": (0.50, 2),
    "rudo": (0.60, 3),
}
DEFAULT_COMPROMISE_RULE = (0.55, 2)

ADAPTATION_BUDGET = 10


class SISError(RuntimeError):
    """Raised when the SIS or its content is internally inconsistent."""


class ContentError(SISError):
    """Raised when content references are invalid."""


def canonical(obj: Any) -> str:
    return json.dumps(obj, sort_keys=True, separators=(",", ":"), ensure_ascii=True)


def sha256_hex(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def stable_int(text: str, hex_chars: int = 16) -> int:
    return int(sha256_hex(text)[:hex_chars], 16)


def clamp(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))


def parse_requirement(req: str) -> Tuple[str, List[str]]:
    """Parse a requirement string. Raises ContentError for malformed input."""
    if not isinstance(req, str) or not req:
        raise ContentError("empty requirement")
    if req.startswith("any:"):
        parts = [p for p in req[4:].split(",") if p]
        if not parts:
            raise ContentError("empty any: requirement")
        for p in parts:
            parse_requirement(p)
        return "any", parts
    kind, sep, rest = req.partition(":")
    if not sep:
        raise ContentError(f"requirement missing ':' -> {req!r}")
    if kind in ("flag", "intel", "route", "contact_available", "contact_compromised"):
        if not rest:
            raise ContentError(f"requirement missing value -> {req!r}")
        return kind, [rest]
    if kind in ("trust", "witness", "keychar", "faction"):
        parts = rest.split(":")
        if len(parts) < 2:
            raise ContentError(f"requirement missing fields -> {req!r}")
        return kind, parts
    raise ContentError(f"unknown requirement kind -> {req!r}")


def requirement_met(req: str, sis: "ShadowIntelligenceSystem") -> bool:
    kind, parts = parse_requirement(req)
    if kind == "any":
        return any(requirement_met(p, sis) for p in parts)
    if kind == "flag":
        return sis.has_flag(parts[0])
    if kind == "intel":
        return sis.has_intel(parts[0])
    if kind == "route":
        return sis.has_flag(f"route_{parts[0]}")
    if kind == "contact_available":
        return sis.contact_available(parts[0])
    if kind == "contact_compromised":
        return sis.is_compromised(parts[0])
    if kind == "trust":
        return sis.trust(parts[0]) >= float(parts[1])
    if kind == "witness":
        return sis.witness_status(parts[0]) == parts[1]
    if kind == "keychar":
        return sis.keychar_status(parts[0]) == parts[1]
    if kind == "faction":
        faction_id, field_name, expected = parts[0], parts[1], ":".join(parts[2:])
        value = sis.faction(faction_id, field_name)
        if isinstance(value, bool):
            return str(value).lower() == expected.lower()
        if isinstance(value, (int, float)):
            try:
                return float(value) >= float(expected)
            except ValueError:
                return str(value) == expected
        return str(value) == expected
    raise ContentError(f"unhandled requirement -> {req!r}")


# ---------------------------------------------------------------------------
# Content
# ---------------------------------------------------------------------------


class Content:
    """Merged, validated story content."""

    def __init__(self, data: Dict[str, Any]):
        self.data = data
        self.characters = {c["id"]: c for c in data.get("characters", [])}
        self.factions = {f["id"]: f for f in data.get("factions", [])}
        self.contacts = {c["id"]: c for c in data.get("contacts", [])}
        self.locations = {l["id"]: l for l in data.get("locations", [])}
        self.intel = {i["id"]: i for i in data.get("intel", [])}
        self.witnesses = {w["id"]: w for w in data.get("witnesses", [])}
        self.epilogues = {e["id"]: e for e in data.get("epilogues", [])}
        self.missions = {m["id"]: m for m in data.get("missions", [])}
        self.mission_order = [m["id"] for m in sorted(data.get("missions", []), key=lambda m: m.get("order", 0))]
        self._beats: Dict[str, Dict[str, Dict[str, Any]]] = {}
        for mission in data.get("missions", []):
            self._beats[mission["id"]] = {b["id"]: b for b in mission.get("beats", [])}

    @classmethod
    def load(cls, content_dir: Optional[os.PathLike | str] = None) -> "Content":
        root = Path(content_dir) if content_dir else DEFAULT_CONTENT_DIR
        if not root.exists():
            raise ContentError(f"content directory not found: {root}")
        merged: Dict[str, Any] = {"schemaVersion": 1}
        for name in ("season.json", "locations.json", "codex.json"):
            path = root / name
            if not path.exists():
                raise ContentError(f"missing content file: {path}")
            payload = json.loads(path.read_text(encoding="utf-8"))
            for key, value in payload.items():
                if key == "schemaVersion":
                    merged["schemaVersion"] = max(int(merged.get("schemaVersion", 1)), int(value))
                    continue
                if isinstance(value, list):
                    merged.setdefault(key, []).extend(value)
                else:
                    merged[key] = value
        missions: List[Dict[str, Any]] = []
        mission_dir = root / "missions"
        if mission_dir.exists():
            for path in sorted(mission_dir.glob("*.json")):
                payload = json.loads(path.read_text(encoding="utf-8"))
                missions.extend(payload.get("missions", []))
        merged["missions"] = missions
        content = cls(merged)
        content.validate()
        return content

    def mission(self, mission_id: str) -> Dict[str, Any]:
        if mission_id not in self.missions:
            raise ContentError(f"unknown mission {mission_id!r}")
        return self.missions[mission_id]

    def beat(self, mission_id: str, beat_id: str) -> Dict[str, Any]:
        try:
            return self._beats[mission_id][beat_id]
        except KeyError as exc:
            raise ContentError(f"unknown beat {beat_id!r} in {mission_id!r}") from exc

    def location_for(self, mission_id: str) -> Dict[str, Any]:
        mission = self.mission(mission_id)
        loc_id = mission.get("location")
        if loc_id not in self.locations:
            raise ContentError(f"mission {mission_id} references unknown location {loc_id!r}")
        return self.locations[loc_id]

    # -- validation ---------------------------------------------------------

    def validate(self) -> None:
        required = ("schemaVersion", "season", "characters", "factions", "contacts", "locations", "intel", "witnesses", "missions", "epilogues")
        missing = [k for k in required if k not in self.data]
        if missing:
            raise ContentError(f"missing top-level keys: {', '.join(missing)}")
        if not self.missions:
            raise ContentError("no missions defined")
        for mid in ("M1", "M2", "M3"):
            if mid not in self.missions:
                raise ContentError(f"vertical slice requires mission {mid}")
        for mission in self.missions.values():
            self._validate_mission(mission)
        if not self.missions["M3"].get("endings"):
            raise ContentError("M3 must define at least one ending")
        for ending in self.missions["M3"]["endings"]:
            if ending not in self.epilogues:
                raise ContentError(f"missing epilogue {ending!r}")

    def _validate_mission(self, mission: Dict[str, Any]) -> None:
        mid = mission.get("id", "<unknown>")
        for key in ("id", "order", "title", "location", "start", "beats", "invariants"):
            if key not in mission:
                raise ContentError(f"mission {mid} missing {key}")
        if mission["location"] not in self.locations:
            raise ContentError(f"mission {mid} has unknown location {mission['location']!r}")
        for cid in mission.get("contacts", []):
            if cid not in self.contacts:
                raise ContentError(f"mission {mid} has unknown contact {cid!r}")
        for wid in mission.get("witnesses", []):
            if wid not in self.witnesses:
                raise ContentError(f"mission {mid} has unknown witness {wid!r}")
        for kid in mission.get("keyCharacters", []):
            if kid not in self.characters:
                raise ContentError(f"mission {mid} has unknown key character {kid!r}")
        beats = {b.get("id"): b for b in mission["beats"]}
        if mission["start"] not in beats:
            raise ContentError(f"mission {mid} start beat {mission['start']!r} not found")
        if not any(b.get("type") == "mission_end" for b in beats.values()):
            raise ContentError(f"mission {mid} has no mission_end beat")
        route_ids = {r["id"] for r in self.locations[mission["location"]].get("routes", [])}
        for beat in beats.values():
            self._validate_beat(mid, beat, beats, route_ids)
        next_mission = mission.get("nextMission")
        # nextMission lives on the mission_end beat, not the mission, but accept both.
        if next_mission and next_mission not in self.missions:
            raise ContentError(f"mission {mid} points to unknown next mission {next_mission!r}")

    def _validate_beat(self, mid: str, beat: Dict[str, Any], beats: Dict[str, Any], route_ids: set) -> None:
        bid = beat.get("id", "<unknown>")
        btype = beat.get("type")
        if btype not in ("dialogue", "choice", "route", "investigation", "combat", "chase", "ending", "mission_end"):
            raise ContentError(f"beat {bid} has unknown type {btype!r}")
        if btype in ("dialogue", "choice", "combat", "chase", "ending"):
            choices = beat.get("choices", [])
            if not choices:
                raise ContentError(f"beat {bid} has no choices")
            for choice in choices:
                self._validate_choice(mid, bid, choice, beats)
        if btype == "route":
            routes = beat.get("routes", [])
            if not routes:
                raise ContentError(f"route beat {bid} has no routes")
            for rid in routes:
                if rid not in route_ids:
                    raise ContentError(f"route beat {bid} references unknown route {rid!r}")
            if beat.get("next") not in beats:
                raise ContentError(f"route beat {bid} has invalid next {beat.get('next')!r}")
        if btype == "investigation":
            if not beat.get("areas"):
                raise ContentError(f"investigation beat {bid} has no areas")
            for area in beat["areas"]:
                for iid in area.get("intel", []):
                    if iid not in self.intel:
                        raise ContentError(f"area {area.get('id')} has unknown intel {iid!r}")
                self._validate_effects(mid, area.get("effects", {}))
                self._validate_effects(mid, area.get("failureEffects", {}))
            if beat.get("next") not in beats:
                raise ContentError(f"investigation beat {bid} has invalid next {beat.get('next')!r}")
        if btype == "ending":
            if "text" not in beat:
                raise ContentError(f"ending beat {bid} has no text")
        if btype == "mission_end":
            next_mission = beat.get("nextMission", "")
            if next_mission not in ("", None) and next_mission not in self.missions:
                raise ContentError(f"mission_end beat {bid} points to unknown mission {next_mission!r}")

    def _validate_choice(self, mid: str, bid: str, choice: Dict[str, Any], beats: Dict[str, Any]) -> None:
        cid = choice.get("id", "<unknown>")
        self._validate_effects(mid, choice.get("effects", {}))
        self._validate_effects(mid, choice.get("failureEffects", {}))
        if choice.get("next") is not None and choice["next"] not in beats:
            raise ContentError(f"choice {bid}/{cid} has invalid next {choice['next']!r}")
        for req in choice.get("requirements", []):
            try:
                parse_requirement(req)
            except ContentError as exc:
                raise ContentError(f"choice {bid}/{cid}: {exc}") from exc
        for branch in ("success", "failure"):
            payload = choice.get(branch)
            if not payload:
                continue
            self._validate_effects(mid, payload.get("effects", {}))
            if payload.get("next") is not None and payload["next"] not in beats:
                raise ContentError(f"choice {bid}/{cid}/{branch} has invalid next {payload['next']!r}")
        for conditional in choice.get("conditionalEffects", []):
            for req in conditional.get("requirements", []):
                try:
                    parse_requirement(req)
                except ContentError as exc:
                    raise ContentError(f"choice {bid}/{cid}: {exc}") from exc
            self._validate_effects(mid, conditional.get("effects", {}))

    def _validate_effects(self, mid: str, effects: Dict[str, Any]) -> None:
        if not isinstance(effects, dict):
            raise ContentError(f"effects in {mid} must be an object")
        for item in effects.get("trust", []):
            if item.get("contact") not in self.contacts:
                raise ContentError(f"trust effect references unknown contact {item.get('contact')!r}")
        for item in effects.get("factions", []):
            if item.get("id") not in self.factions:
                raise ContentError(f"faction effect references unknown faction {item.get('id')!r}")
        for iid in effects.get("intel", []):
            if iid not in self.intel:
                raise ContentError(f"intel effect references unknown intel {iid!r}")
        for item in effects.get("witnesses", []):
            if item.get("id") not in self.witnesses:
                raise ContentError(f"witness effect references unknown witness {item.get('id')!r}")
        for item in effects.get("keyCharacters", []):
            if item.get("id") not in self.characters:
                raise ContentError(f"key character effect references unknown character {item.get('id')!r}")


# ---------------------------------------------------------------------------
# Behaviour
# ---------------------------------------------------------------------------


@dataclass
class BehaviorEvent:
    mission_id: str
    action: str
    target: str = ""
    value: float = 1.0
    seq: int = 0

    def to_dict(self) -> Dict[str, Any]:
        return {
            "missionId": self.mission_id,
            "action": self.action,
            "target": self.target,
            "value": self.value,
            "seq": self.seq,
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "BehaviorEvent":
        return cls(
            mission_id=data.get("missionId", ""),
            action=data.get("action", ""),
            target=data.get("target", ""),
            value=float(data.get("value", 1.0)),
            seq=int(data.get("seq", 0)),
        )


class BehaviorLog:
    def __init__(self, events: Optional[List[BehaviorEvent]] = None):
        self.events: List[BehaviorEvent] = list(events or [])

    def record(self, mission_id: str, action: str, target: str = "", value: float = 1.0) -> BehaviorEvent:
        event = BehaviorEvent(mission_id, action, target, float(value), len(self.events))
        self.events.append(event)
        return event

    def digest(self) -> str:
        return sha256_hex(canonical([e.to_dict() for e in self.events]))

    def profile(self) -> "BehaviorProfile":
        return BehaviorProfile.from_events(self.events)

    def count(self, action: str) -> int:
        return sum(1 for e in self.events if e.action == action)

    def count_target(self, action: str, target: str) -> int:
        return sum(1 for e in self.events if e.action == action and e.target == target)

    def to_list(self) -> List[Dict[str, Any]]:
        return [e.to_dict() for e in self.events]

    @classmethod
    def from_list(cls, data: List[Dict[str, Any]]) -> "BehaviorLog":
        return cls([BehaviorEvent.from_dict(d) for d in data])


@dataclass
class BehaviorProfile:
    total_actions: int = 0
    counts: Dict[str, int] = field(default_factory=dict)
    entry_counts: Dict[str, int] = field(default_factory=dict)
    stealth_ratio: float = 0.0
    aggression_ratio: float = 0.0
    vehicle_ratio: float = 0.0
    front_ratio: float = 0.0
    entries: int = 0
    trust_events: int = 0
    expose_events: int = 0
    interrogations: int = 0
    witness_spared: int = 0
    witness_killed: int = 0
    keychar_killed: int = 0

    @classmethod
    def from_events(cls, events: Sequence[BehaviorEvent]) -> "BehaviorProfile":
        counts: Dict[str, int] = {}
        entry_counts: Dict[str, int] = {}
        for event in events:
            counts[event.action] = counts.get(event.action, 0) + 1
            if event.action == "approach":
                kind = KIND_BY_VALUE.get(int(event.value), "unknown")
                entry_counts[kind] = entry_counts.get(kind, 0) + 1
        total = max(1, len(events))
        entries = sum(entry_counts.values())
        stealth_actions = (
            counts.get("stealth_takedown", 0)
            + counts.get("stealth_avoid", 0)
            + entry_counts.get("stealth", 0)
            + entry_counts.get("insider", 0)
        )
        aggression_actions = (
            counts.get("gunfight", 0)
            + counts.get("melee", 0)
            + counts.get("key_character_killed", 0)
        )
        return cls(
            total_actions=len(events),
            counts=counts,
            entry_counts=entry_counts,
            stealth_ratio=clamp(stealth_actions / total, 0.0, 1.0),
            aggression_ratio=clamp(aggression_actions / total, 0.0, 1.0),
            vehicle_ratio=clamp(counts.get("vehicle_chase", 0) / total, 0.0, 1.0),
            front_ratio=(entry_counts.get("front", 0) / entries) if entries else 0.0,
            entries=entries,
            trust_events=counts.get("trust_contact", 0),
            expose_events=counts.get("expose_org", 0),
            interrogations=counts.get("interrogate", 0),
            witness_spared=counts.get("witness_spared", 0),
            witness_killed=counts.get("witness_killed", 0),
            keychar_killed=counts.get("key_character_killed", 0),
        )

    def to_dict(self) -> Dict[str, Any]:
        return {
            "totalActions": self.total_actions,
            "counts": dict(self.counts),
            "entryCounts": dict(self.entry_counts),
            "stealthRatio": self.stealth_ratio,
            "aggressionRatio": self.aggression_ratio,
            "vehicleRatio": self.vehicle_ratio,
            "frontRatio": self.front_ratio,
            "entries": self.entries,
            "trustEvents": self.trust_events,
            "exposeEvents": self.expose_events,
            "interrogations": self.interrogations,
            "witnessSpared": self.witness_spared,
            "witnessKilled": self.witness_killed,
            "keycharKilled": self.keychar_killed,
        }


# ---------------------------------------------------------------------------
# Ledger
# ---------------------------------------------------------------------------


class ConsequenceLedger:
    """Append-only, hash-chained consequence ledger."""

    def __init__(self, commits: Optional[List[Dict[str, Any]]] = None):
        self.commits: List[Dict[str, Any]] = list(commits or [])

    def commit(self, mission_id: str, behavior_digest: str, delta: Dict[str, Any]) -> Dict[str, Any]:
        prev_hash = self.commits[-1]["commitHash"] if self.commits else "GENESIS"
        body = {
            "missionId": mission_id,
            "behaviorDigest": behavior_digest,
            "delta": delta,
            "prevHash": prev_hash,
        }
        commit = dict(body)
        commit["commitHash"] = sha256_hex(canonical(body))
        self.commits.append(commit)
        return commit

    def verify(self) -> Tuple[bool, str]:
        prev_hash = "GENESIS"
        for index, commit in enumerate(self.commits):
            body = {
                "missionId": commit.get("missionId", ""),
                "behaviorDigest": commit.get("behaviorDigest", ""),
                "delta": commit.get("delta", {}),
                "prevHash": commit.get("prevHash", ""),
            }
            if commit.get("prevHash") != prev_hash:
                return False, f"commit {index}: prevHash mismatch"
            if commit.get("commitHash") != sha256_hex(canonical(body)):
                return False, f"commit {index}: commitHash mismatch"
            prev_hash = commit["commitHash"]
        return True, ""

    def replay(self, initial_state: Dict[str, Any]) -> Tuple[Dict[str, Any], List[str]]:
        state = copy.deepcopy(initial_state)
        contradictions: List[str] = []
        for commit in self.commits:
            apply_delta(state, commit.get("delta", {}), contradictions, commit.get("missionId", ""))
        return state, contradictions

    def to_list(self) -> List[Dict[str, Any]]:
        return copy.deepcopy(self.commits)

    @classmethod
    def from_list(cls, data: List[Dict[str, Any]]) -> "ConsequenceLedger":
        return cls(data)


def apply_delta(state: Dict[str, Any], delta: Dict[str, Any], contradictions: List[str], mission_id: str) -> None:
    for key, value in delta.items():
        if key == "completed_missions":
            completed = state.setdefault("completed_missions", [])
            for mission in value:
                if mission not in completed:
                    completed.append(mission)
            continue
        if key.startswith("trust:"):
            state[key] = clamp(float(value), 0.0, 1.0)
            continue
        if key.startswith("faction:"):
            if key.endswith(":influence"):
                state[key] = int(clamp(float(value), 0, 10))
            elif key.endswith(":heat"):
                state[key] = int(clamp(float(value), 0, 10))
            elif key.endswith(":exposed"):
                old = bool(state.get(key, False))
                new = bool(value)
                if old and not new:
                    contradictions.append(f"{mission_id}: faction exposure cannot be reversed ({key})")
                state[key] = old or new
            elif key.endswith(":alliance"):
                state[key] = str(value)
            else:
                state[key] = value
            continue
        if key.startswith("witness:") and key.endswith(":status"):
            old = state.get(key, "unknown")
            if old not in ("unknown", value):
                contradictions.append(f"{mission_id}: witness fate contradiction {key}: {old} -> {value}")
            else:
                state[key] = value
            continue
        if key.startswith("witness:") and key.endswith(":cooperative"):
            state[key] = bool(state.get(key, False) or value)
            continue
        if key.startswith("keychar:"):
            old = state.get(key, "unknown")
            if old not in ("unknown", value):
                contradictions.append(f"{mission_id}: key character contradiction {key}: {old} -> {value}")
            else:
                state[key] = value
            continue
        if key.startswith("intel:"):
            state[key] = bool(state.get(key, False) or value)
            continue
        if key.startswith("flag:"):
            state[key] = bool(state.get(key, False) or value)
            continue
        if key.startswith("ending:"):
            old = state.get(key, "")
            if old and old != value:
                contradictions.append(f"{mission_id}: ending contradiction {key}: {old} -> {value}")
            else:
                state[key] = value
            continue
        state[key] = value


def initial_state(content: Content) -> Dict[str, Any]:
    state: Dict[str, Any] = {}
    for contact in content.contacts.values():
        state[f"trust:{contact['id']}"] = float(contact.get("trust", 0.0))
    for faction in content.factions.values():
        state[f"faction:{faction['id']}:influence"] = int(faction.get("influence", 0))
        state[f"faction:{faction['id']}:heat"] = int(faction.get("heat", 0))
        state[f"faction:{faction['id']}:exposed"] = bool(faction.get("exposed", False))
        state[f"faction:{faction['id']}:alliance"] = str(faction.get("alliance", "neutral"))
    for witness in content.witnesses.values():
        state[f"witness:{witness['id']}:status"] = "unknown"
        state[f"witness:{witness['id']}:cooperative"] = False
    for character in content.characters.values():
        state[f"keychar:{character['id']}"] = "unknown"
    for intel in content.intel.values():
        state[f"intel:{intel['id']}"] = False
    for mission in content.missions.values():
        state[f"ending:{mission['id']}"] = ""
    state["completed_missions"] = []
    return state


# ---------------------------------------------------------------------------
# Shadow Intelligence System
# ---------------------------------------------------------------------------


class ShadowIntelligenceSystem:
    def __init__(
        self,
        content: Content,
        state: Optional[Dict[str, Any]] = None,
        behavior: Optional[BehaviorLog] = None,
        ledger: Optional[ConsequenceLedger] = None,
        pending: Optional[Dict[str, Any]] = None,
    ):
        self.content = content
        self.state = copy.deepcopy(state) if state is not None else initial_state(content)
        self.behavior = behavior or BehaviorLog()
        self.ledger = ledger or ConsequenceLedger()
        self.pending: Dict[str, Any] = dict(pending or {})

    # -- state helpers ------------------------------------------------------

    def trust(self, contact_id: str) -> float:
        return float(self.state.get(f"trust:{contact_id}", 0.0))

    def exposure(self, contact_id: str) -> float:
        contact = self.content.contacts.get(contact_id, {})
        return self.trust(contact_id) * float(contact.get("centrality", 0.0))

    def trust_events(self, contact_id: str) -> int:
        return self.behavior.count_target("trust_contact", contact_id)

    def is_compromised(self, contact_id: str) -> bool:
        if self.has_flag(f"contact_compromised_{contact_id}"):
            return True
        threshold, events = COMPROMISE_RULES.get(contact_id, DEFAULT_COMPROMISE_RULE)
        return self.exposure(contact_id) >= threshold and self.trust_events(contact_id) >= events

    def contact_available(self, contact_id: str) -> bool:
        contact = self.content.contacts.get(contact_id)
        if not contact or not contact.get("available", True):
            return False
        if contact_id in self.content.witnesses:
            status = self.witness_status(contact_id)
            if status in ("killed", "captured"):
                return False
        if contact_id == "amara" and self.has_flag("amara_captured"):
            return False
        if contact_id == "tafadzwa" and self.has_flag("tafadzwa_captured"):
            return False
        return self.trust(contact_id) > 0.0

    def witness_status(self, witness_id: str) -> str:
        return str(self.state.get(f"witness:{witness_id}:status", "unknown"))

    def witness_cooperative(self, witness_id: str) -> bool:
        return bool(self.state.get(f"witness:{witness_id}:cooperative", False))

    def keychar_status(self, character_id: str) -> str:
        return str(self.state.get(f"keychar:{character_id}", "unknown"))

    def has_intel(self, intel_id: str) -> bool:
        return bool(self.state.get(f"intel:{intel_id}", False))

    def has_flag(self, flag: str) -> bool:
        return bool(self.state.get(f"flag:{flag}", False))

    def faction(self, faction_id: str, field_name: str) -> Any:
        return self.state.get(f"faction:{faction_id}:{field_name}", 0)

    def record(self, mission_id: str, action: str, target: str = "", value: float = 1.0) -> BehaviorEvent:
        return self.behavior.record(mission_id, action, target, value)

    def profile(self) -> BehaviorProfile:
        return self.behavior.profile()

    def seed(self, mission_id: str) -> int:
        return stable_int(self.behavior.digest() + "|" + mission_id)

    # -- effects ------------------------------------------------------------

    def apply_effects(self, effects: Optional[Dict[str, Any]], mission_id: str = "", infer_behavior: bool = True) -> None:
        if not effects:
            return
        pending = self.pending
        for item in effects.get("trust", []):
            cid = item.get("contact")
            if cid not in self.content.contacts:
                continue
            new_value = clamp(self.trust(cid) + float(item.get("delta", 0.0)), 0.0, 1.0)
            key = f"trust:{cid}"
            self.state[key] = new_value
            pending[key] = new_value
        for item in effects.get("factions", []):
            fid = item.get("id")
            if fid not in self.content.factions:
                continue
            if "influence" in item:
                key = f"faction:{fid}:influence"
                value = int(clamp(float(self.faction(fid, "influence")) + float(item["influence"]), 0, 10))
                self.state[key] = value
                pending[key] = value
            if "heat" in item:
                key = f"faction:{fid}:heat"
                value = int(clamp(float(self.faction(fid, "heat")) + float(item["heat"]), 0, 10))
                self.state[key] = value
                pending[key] = value
            if "exposed" in item:
                key = f"faction:{fid}:exposed"
                value = bool(self.faction(fid, "exposed") or item["exposed"])
                self.state[key] = value
                pending[key] = value
            if item.get("alliance"):
                key = f"faction:{fid}:alliance"
                self.state[key] = item["alliance"]
                pending[key] = item["alliance"]
        for iid in effects.get("intel", []):
            if iid in self.content.intel:
                key = f"intel:{iid}"
                self.state[key] = True
                pending[key] = True
        for flag in effects.get("flags", []):
            key = f"flag:{flag}"
            self.state[key] = True
            pending[key] = True
        for item in effects.get("witnesses", []):
            wid = item.get("id")
            if wid not in self.content.witnesses:
                continue
            if "status" in item:
                key = f"witness:{wid}:status"
                self.state[key] = item["status"]
                pending[key] = item["status"]
            if "cooperative" in item:
                key = f"witness:{wid}:cooperative"
                value = bool(self.witness_cooperative(wid) or item["cooperative"])
                self.state[key] = value
                pending[key] = value
        for item in effects.get("keyCharacters", []):
            cid = item.get("id")
            if cid in self.content.characters and "status" in item:
                key = f"keychar:{cid}"
                self.state[key] = item["status"]
                pending[key] = item["status"]

        explicit = effects.get("behavior", [])
        if explicit:
            for event in explicit:
                self.record(
                    mission_id,
                    event.get("action", ""),
                    event.get("target", ""),
                    float(event.get("value", 1.0)),
                )
        elif infer_behavior:
            for item in effects.get("trust", []):
                self.record(mission_id, "trust_contact", item.get("contact", ""), 1)
            for item in effects.get("witnesses", []):
                if item.get("status") == "spared":
                    self.record(mission_id, "witness_spared", item.get("id", ""), 1)
                elif item.get("status") == "killed":
                    self.record(mission_id, "witness_killed", item.get("id", ""), 1)
            for item in effects.get("keyCharacters", []):
                if item.get("status") == "dead":
                    self.record(mission_id, "key_character_killed", item.get("id", ""), 1)
            for item in effects.get("factions", []):
                if item.get("exposed"):
                    self.record(mission_id, "expose_org", item.get("id", ""), 1)

    def commit_mission(self, mission_id: str, ending: Optional[str] = None) -> Dict[str, Any]:
        delta: Dict[str, Any] = dict(self.pending)
        delta["completed_missions"] = [mission_id]
        completed = self.state.setdefault("completed_missions", [])
        if mission_id not in completed:
            completed.append(mission_id)
        if ending:
            delta[f"ending:{mission_id}"] = ending
            self.state[f"ending:{mission_id}"] = ending
        commit = self.ledger.commit(mission_id, self.behavior.digest(), delta)
        self.pending.clear()
        return commit

    # -- mission resolution ------------------------------------------------

    def _apply_mission_start_effects(self, mission: Dict[str, Any]) -> None:
        mid = mission["id"]
        order = int(mission.get("order", 1))
        effects: Dict[str, Any] = {"flags": [], "intel": [], "factions": []}
        if order >= 2 and self.is_compromised("amara"):
            effects["flags"].append("amara_captured")
        if mid == "M2" and self.witness_status("lovemore") == "spared":
            if self.witness_cooperative("lovemore"):
                effects["intel"].append("cleaner_roster")
                effects["flags"].append("lovemore_informant")
            else:
                effects["flags"].append("lovemore_liability")
                if not self.has_flag("lovemore_liability_applied"):
                    effects["factions"].append({"id": "meridian", "heat": 1})
                    effects["flags"].append("lovemore_liability_applied")
        if mid == "M3":
            if self.witness_status("tafadzwa") == "spared" and self.witness_cooperative("tafadzwa"):
                effects["intel"].append("tafadzwa_tip")
                effects["flags"].append("tafadzwa_whistleblower")
            if self.witness_status("mhofu") == "spared":
                effects["intel"].append("mhofu_keycard")
                effects["flags"].append("mhofu_ally")
            if self.is_compromised("tafadzwa"):
                effects["flags"].append("tafadzwa_targeted")
        if effects["flags"] or effects["intel"] or effects["factions"]:
            self.apply_effects(effects, mission_id=mid, infer_behavior=False)

    def resolve_mission(self, mission_id: str) -> Dict[str, Any]:
        mission = self.content.mission(mission_id)
        self._apply_mission_start_effects(mission)
        location = self.content.location_for(mission_id)
        profile = self.profile()
        notes: List[str] = []

        entry: Dict[str, Any] = {}
        for route in location.get("routes", []):
            rid = route["id"]
            difficulty = int(route.get("baseDifficulty", 1))
            reasons: List[str] = []
            available = True
            if rid == "hill_path" and not (self.trust("muponda") >= 0.5 or self.has_flag("muponda_route")):
                available = False
                reasons.append("Muponda has not offered the hunter's path.")
            if rid == "press_convoy" and not (self.trust("tafadzwa") >= 0.4 or self.has_flag("ledger_published")):
                available = False
                reasons.append("Tafadzwa cannot attach you to the press convoy.")
            if rid == "mhofu_keycard" and not (self.witness_status("mhofu") == "spared" and self.has_intel("mhofu_keycard")):
                available = False
                reasons.append("Mhofu's keycard is not available.")
            if route.get("kind") == "front" and profile.entries >= 2 and profile.front_ratio >= 0.6:
                difficulty += 2
                reasons.append("Front approaches have been used repeatedly; Meridian reinforced the gate.")
            if profile.stealth_ratio >= 0.5:
                difficulty += 1
                reasons.append("Your stealth profile raised Meridian's camera budget.")
            if self.faction("meridian", "heat") >= 4 and route.get("kind") in ("front", "side"):
                difficulty += 1
                reasons.append("Meridian is on high alert.")
            if mission_id == "M3" and rid == "main_gate":
                difficulty += 1
                reasons.append("The main gate is a killing box.")
            if self.witness_status("mhofu") == "killed" and rid == "east_service":
                difficulty -= 1
                reasons.append("Mhofu's death left the east patrols disorganised.")
            entry[rid] = {
                "id": rid,
                "name": route.get("name", rid),
                "kind": route.get("kind", "unknown"),
                "available": available,
                "difficulty": max(1, difficulty),
                "reason": " ".join(reasons),
            }

        order = int(mission.get("order", 1))
        base = {
            "detection": 1 + order,
            "armor": order,
            "strategy": order,
            "reinforcements": max(0, order - 1),
        }
        adaptation = {
            "detection": int(profile.stealth_ratio * 4),
            "armor": int(profile.aggression_ratio * 4),
            "strategy": int(profile.vehicle_ratio * 3),
            "reinforcements": 0,
        }
        if profile.entries >= 2 and profile.front_ratio >= 0.6:
            adaptation["strategy"] += 2
            adaptation["reinforcements"] += 1
            notes.append("Front-door pattern: Meridian has reinforced entrances.")
        if profile.witness_spared >= 1:
            adaptation["strategy"] += 1
            notes.append("Spared witnesses: Meridian is asking questions.")
        if any(self.is_compromised(cid) for cid in self.content.contacts):
            adaptation["detection"] += 1
            notes.append("A contact relationship is exposed: Meridian is watching your network.")
        if self.faction("meridian", "heat") >= 4:
            adaptation["detection"] += 1
            adaptation["armor"] += 1
            notes.append("Meridian heat is high: heavier response.")
        if profile.keychar_killed >= 1:
            adaptation["strategy"] += 1
            notes.append("A key character is dead: Meridian has reorganised.")
        if self.has_flag("lovemore_informant"):
            adaptation["detection"] = max(0, adaptation["detection"] - 1)
            notes.append("Lovemore's patrol schedule reduces detection.")
        if self.has_flag("lovemore_liability"):
            adaptation["detection"] += 1
            notes.append("Lovemore may have warned Meridian: detection increased.")
        total_adaptation = sum(adaptation.values())
        if total_adaptation > ADAPTATION_BUDGET:
            scale = ADAPTATION_BUDGET / total_adaptation
            adaptation = {k: int(v * scale) for k, v in adaptation.items()}
        enemy = {k: int(clamp(base[k] + adaptation[k], 0, 10)) for k in base}
        enemy["adaptation"] = adaptation
        enemy["totalAdaptation"] = sum(adaptation.values())
        enemy["base"] = base

        witnesses: List[Dict[str, Any]] = []
        for wid in mission.get("witnesses", []):
            status = self.witness_status(wid)
            if status in ("unknown", "killed"):
                if status == "killed":
                    notes.append(f"{self.content.witnesses[wid]['name']} is dead.")
                continue
            role = "witness"
            effect = ""
            if wid == "lovemore":
                if self.witness_cooperative(wid):
                    role, effect = "informant", "reveals patrol routes"
                else:
                    role, effect = "liability", "may warn Meridian"
            elif wid == "tafadzwa":
                if self.witness_cooperative(wid):
                    role, effect = "whistleblower", "testimony available"
                else:
                    role, effect = "liability", "will not testify"
            elif wid == "mhofu":
                role, effect = "potential_ally", "keycard route available"
            witnesses.append({
                "id": wid,
                "name": self.content.witnesses[wid]["name"],
                "status": status,
                "role": role,
                "effect": effect,
            })
            notes.append(f"Witness {self.content.witnesses[wid]['name']}: {role} ({effect}).")

        contacts: List[Dict[str, Any]] = []
        for cid in mission.get("contacts", []):
            contact = self.content.contacts[cid]
            compromised = self.is_compromised(cid)
            available = self.contact_available(cid)
            contacts.append({
                "id": cid,
                "name": contact.get("name", cid),
                "trust": round(self.trust(cid), 3),
                "exposure": round(self.exposure(cid), 3),
                "compromised": compromised,
                "available": available,
            })
            if compromised:
                notes.append(f"Contact {contact.get('name', cid)} is exposed (trust {self.trust(cid):.2f}, exposure {self.exposure(cid):.2f}).")

        objectives = list(mission.get("objectives", []))
        if mission_id == "M3":
            if self.has_flag("amara_captured"):
                objectives.append("Protect Amara's safehouse")
            if self.has_flag("tafadzwa_targeted"):
                objectives.append("Protect Tafadzwa")
            if self.witness_status("tafadzwa") == "captured":
                objectives.append("Recover Tafadzwa from Meridian")
            if self.witness_status("lovemore") == "killed" and self.witness_status("tafadzwa") == "killed":
                objectives.append("Hack the estate terminal for the core file")

        endings = list(mission.get("endings", []))
        if mission_id == "M3" and not endings:
            raise SISError("M3 resolved with no available endings")
        if mission_id == "M3" and not mission.get("invariants"):
            raise SISError("M3 must retain its narrative invariants")

        dialogue = {
            "m2_open": "burned" if self.is_compromised("amara") else "safehouse",
            "m3_open": "captured" if self.has_flag("amara_captured") else "default",
            "m3_witness": "available" if any(w["status"] == "spared" for w in witnesses) else "none",
            "m3_confrontation": "evidence" if (self.has_flag("ledger_published") or self.has_intel("tafadzwa_tip")) else "limited",
        }

        return {
            "missionId": mission_id,
            "title": mission.get("title", mission_id),
            "location": location.get("name", mission.get("location", "")),
            "entry": entry,
            "enemy": enemy,
            "witnesses": witnesses,
            "contacts": contacts,
            "objectives": objectives,
            "endings": endings,
            "dialogue": dialogue,
            "notes": notes,
            "seed": self.seed(mission_id),
        }

    # -- reconciliation / persistence --------------------------------------

    def world_snapshot(self) -> Dict[str, Any]:
        contacts = {}
        for cid in self.content.contacts:
            contacts[cid] = {
                "trust": round(self.trust(cid), 3),
                "exposure": round(self.exposure(cid), 3),
                "compromised": self.is_compromised(cid),
                "available": self.contact_available(cid),
            }
        factions = {}
        for fid in self.content.factions:
            factions[fid] = {
                "influence": self.faction(fid, "influence"),
                "heat": self.faction(fid, "heat"),
                "exposed": bool(self.faction(fid, "exposed")),
                "alliance": self.faction(fid, "alliance"),
            }
        witnesses = {
            wid: {"status": self.witness_status(wid), "cooperative": self.witness_cooperative(wid)}
            for wid in self.content.witnesses
        }
        key_characters = {cid: self.keychar_status(cid) for cid in self.content.characters}
        intel = [iid for iid in self.content.intel if self.has_intel(iid)]
        flags = sorted(key[5:] for key, value in self.state.items() if key.startswith("flag:") and value)
        endings = {mid: self.state.get(f"ending:{mid}", "") for mid in self.content.missions if self.state.get(f"ending:{mid}", "")}
        return {
            "contacts": contacts,
            "factions": factions,
            "witnesses": witnesses,
            "keyCharacters": key_characters,
            "intel": intel,
            "flags": flags,
            "completed": list(self.state.get("completed_missions", [])),
            "endings": endings,
        }

    def reconcile(self) -> Tuple[Dict[str, Any], List[str]]:
        return self.ledger.replay(initial_state(self.content))

    def verify(self) -> Tuple[bool, List[str]]:
        chain_ok, chain_error = self.ledger.verify()
        replay_state, contradictions = self.reconcile()
        if not chain_ok:
            contradictions.insert(0, chain_error)
        # Compare committed keys to current state, ignoring pending keys.
        for key, value in replay_state.items():
            if key in self.pending:
                continue
            if key == "completed_missions":
                if sorted(value) != sorted(self.state.get(key, [])):
                    contradictions.append("completed missions differ from replay")
                continue
            if self.state.get(key) != value:
                contradictions.append(f"state drift at {key}: {self.state.get(key)!r} != replay {value!r}")
        return (not contradictions), contradictions

    # -- serialisation ------------------------------------------------------

    def to_dict(self) -> Dict[str, Any]:
        return {
            "state": copy.deepcopy(self.state),
            "behavior": self.behavior.to_list(),
            "ledger": self.ledger.to_list(),
            "pending": copy.deepcopy(self.pending),
        }

    @classmethod
    def from_dict(cls, content: Content, data: Dict[str, Any]) -> "ShadowIntelligenceSystem":
        return cls(
            content=content,
            state=data.get("state"),
            behavior=BehaviorLog.from_list(data.get("behavior", [])),
            ledger=ConsequenceLedger.from_list(data.get("ledger", [])),
            pending=data.get("pending", {}),
        )

    def save(self, path: os.PathLike | str) -> None:
        Path(path).write_text(json.dumps(self.to_dict(), indent=2, ensure_ascii=False), encoding="utf-8")

    @classmethod
    def load(cls, path: os.PathLike | str, content: Optional[Content] = None) -> "ShadowIntelligenceSystem":
        content = content or Content.load()
        data = json.loads(Path(path).read_text(encoding="utf-8"))
        return cls.from_dict(content, data)
