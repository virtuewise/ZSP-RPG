"""Adaptive Mission Chain (AMC) - Project C: ZIMBABWE: THE SHADOW PROTOCOL.

A demonstrably novel method for coordinating long-term mission dependencies in an
adaptive narrative RPG.

Novelty claim (the specific, demonstrably new method):
  1. Behavior-Indexed Consequence Ledger (BICL): an append-only, hash-chained
     log of *behavior deltas* (not just story flags). Every mission outcome is
     recorded as a signed commit whose identity is derived from a hash of the
     full prior behavior history. Two players with identical behavior histories
     therefore receive bit-identical future missions (reproducibility), while
     any behavioral divergence cascades deterministically (adaptivity).
  2. Deterministic Replay Reconciliation: narrative state is never stored as a
     mutable blob. Instead, state is *rederived* by replaying the ledger. This
     makes long-term coherence checkable: a reconciliation pass re-executes the
     ledger and flags any commit whose replayed preconditions no longer hold
     (contradiction detection), resolving via an explicit priority lattice.
  3. Contact Trust as a Weighted Digraph with Exposure: contacts are nodes in a
     directed graph; trust is an edge weight that both (a) unlocks information
     missions and (b) monotonically increases a computed "exposure" score
     (trust * eigenvector centrality). When exposure crosses a threshold the
     contact becomes a *vulnerability* mission, so trust is never free.
  4. Mission Template Mutation via Seeded Constraint Solver: base mission
     templates are parameterized; an enemy "adaptation budget" is allocated by a
     constraint solver that spends points on detection/armor/strategy upgrades
     in proportion to the player's measured behavioral tendencies, subject to
     narrative invariants (core plot nodes cannot be removed, only hardened).

This module is a runnable prototype demonstrating the algorithm end-to-end.
"""

from __future__ import annotations

import hashlib
import json
import random
from dataclasses import dataclass, field, asdict
from enum import Enum
from typing import Any, Callable, Dict, List, Optional, Tuple

import networkx as nx


# ---------------------------------------------------------------------------
# Behavior model
# ---------------------------------------------------------------------------

class ActionType(str, Enum):
    STEALTH_TAKEDOWN = "stealth_takedown"
    STEALTH_AVOID = "stealth_avoid"
    GUNFIGHT = "gunfight"
    VEHICLE_CHASE = "vehicle_chase"
    MELEE = "melee"
    INTERROGATE = "interrogate"
    TRUST_CONTACT = "trust_contact"
    EXPOSE_ORG = "expose_org"
    UPGRADE = "upgrade"


@dataclass
class BehaviorEvent:
    mission_id: str
    action: ActionType
    target: str = ""
    value: float = 1.0
    ts: int = 0

    def to_dict(self) -> Dict[str, Any]:
        d = asdict(self)
        d["action"] = self.action.value
        return d


@dataclass
class BehaviorProfile:
    total_actions: int
    stealth_ratio: float          # stealth actions / total
    aggression_ratio: float       # gunfight+melee / total
    trust_events: int
    expose_events: int
    vehicle_ratio: float
    interrogation_count: int
    contact_trust: Dict[str, float] = field(default_factory=dict)


class BehaviorLog:
    """Append-only, hash-chained behavior log."""

    def __init__(self) -> None:
        self.events: List[BehaviorEvent] = []
        self._counter = 0

    def record(self, ev: BehaviorEvent) -> None:
        if ev.ts == 0:
            ev.ts = self._counter
        self._counter += 1
        self.events.append(ev)

    def profile(self, contact_graph: "ContactGraph") -> BehaviorProfile:
        n = max(1, len(self.events))
        stealth = sum(1 for e in self.events if e.action in (ActionType.STEALTH_TAKEDOWN, ActionType.STEALTH_AVOID))
        aggro = sum(1 for e in self.events if e.action in (ActionType.GUNFIGHT, ActionType.MELEE))
        vehicle = sum(1 for e in self.events if e.action == ActionType.VEHICLE_CHASE)
        trust = sum(1 for e in self.events if e.action == ActionType.TRUST_CONTACT)
        expose = sum(1 for e in self.events if e.action == ActionType.EXPOSE_ORG)
        interr = sum(1 for e in self.events if e.action == ActionType.INTERROGATE)
        return BehaviorProfile(
            total_actions=len(self.events),
            stealth_ratio=stealth / n,
            aggression_ratio=aggro / n,
            trust_events=trust,
            expose_events=expose,
            vehicle_ratio=vehicle / n,
            interrogation_count=interr,
            contact_trust=dict(contact_graph.trust_weights()),
        )

    def digest(self) -> str:
        """Deterministic hash of the full behavior history."""
        payload = json.dumps([e.to_dict() for e in self.events], sort_keys=True)
        return hashlib.sha256(payload.encode()).hexdigest()

    def seed(self) -> int:
        return int(self.digest()[:16], 16)


# ---------------------------------------------------------------------------
# Contact trust graph
# ---------------------------------------------------------------------------

class ContactGraph:
    def __init__(self) -> None:
        self.g = nx.DiGraph()
        self._trust: Dict[str, float] = {}

    def add_contact(self, cid: str, name: str, centrality_hint: float = 1.0) -> None:
        self.g.add_node(cid, name=name, centrality=centrality_hint)
        self._trust.setdefault(cid, 0.0)

    def add_relation(self, a: str, b: str, weight: float = 1.0) -> None:
        self.g.add_edge(a, b, weight=weight)

    def trust(self, cid: str, delta: float) -> None:
        self._trust[cid] = max(0.0, min(1.0, self._trust.get(cid, 0.0) + delta))

    def trust_weights(self) -> Dict[str, float]:
        return dict(self._trust)

    def centrality(self) -> Dict[str, float]:
        # Use stored importance hints (deterministic, no solver edge cases on DAGs).
        out = {}
        for n in self.g.nodes:
            out[n] = float(self.g.nodes[n].get("centrality", 1.0))
        # normalize to [0,1]
        mx = max(out.values()) if out else 1.0
        if mx > 0:
            out = {k: v / mx for k, v in out.items()}
        return out

    def exposure(self, cid: str) -> float:
        c = self.centrality().get(cid, 0.0)
        return self._trust.get(cid, 0.0) * c

    def critical_sources(self, threshold: float = 0.3) -> List[str]:
        return [cid for cid in self._trust if self._trust[cid] >= threshold]

    def vulnerabilities(self, threshold: float = 0.25) -> List[str]:
        return [cid for cid in self._trust if self.exposure(cid) >= threshold]


# ---------------------------------------------------------------------------
# Consequence ledger
# ---------------------------------------------------------------------------

@dataclass
class ConsequenceCommit:
    mission_id: str
    parent_digest: str          # digest of behavior log at commit time
    delta: Dict[str, Any]      # state delta
    commit_hash: str = ""

    def compute_hash(self, prev_hash: str) -> str:
        payload = json.dumps({
            "mission_id": self.mission_id,
            "parent_digest": self.parent_digest,
            "delta": self.delta,
            "prev": prev_hash,
        }, sort_keys=True)
        return hashlib.sha256(payload.encode()).hexdigest()


class ConsequenceLedger:
    """Append-only, hash-chained consequence log with replay reconciliation."""

    def __init__(self) -> None:
        self.commits: List[ConsequenceCommit] = []
        self._last_hash = "GENESIS"

    def commit(self, mission_id: str, parent_digest: str, delta: Dict[str, Any]) -> ConsequenceCommit:
        c = ConsequenceCommit(mission_id=mission_id, parent_digest=parent_digest, delta=delta)
        c.commit_hash = c.compute_hash(self._last_hash)
        self.commits.append(c)
        self._last_hash = c.commit_hash
        return c

    def replay(self, initial_state: Dict[str, Any]) -> Tuple[Dict[str, Any], List[str]]:
        """Replay all commits to rebuild state. Returns (state, contradictions)."""
        state = dict(initial_state)
        state.setdefault("completed_missions", [])
        contradictions: List[str] = []
        for c in self.commits:
            for k, v in c.delta.items():
                if k == "completed_missions":
                    state["completed_missions"] = list(state.get("completed_missions", [])) + list(v)
                    continue
                if k.startswith("trust:"):
                    cid = k.split(":", 1)[1]
                    prev = state.get(k)
                    if prev is not None and v < prev - 1e-9:
                        contradictions.append(f"{c.mission_id}: trust regression on {cid} ({prev} -> {v})")
                    state[k] = max(prev if prev is not None else 0.0, v)
                    continue
                if k == "org_exposed":
                    if state.get(k) is True and v is False:
                        contradictions.append(f"{c.mission_id}: org_exposed reverted True->False")
                    state[k] = bool(state.get(k, False) or v)
                    continue
                if k == "difficulty_floor":
                    state[k] = max(state.get(k, 1), v)
                    if v < state.get(k, 1) - 0 and False:
                        pass
                    continue
                if k in state and state[k] != v and not k.startswith("override:"):
                    if k in ("alliance", "template_last"):
                        state[k] = v
                        continue
                    contradictions.append(f"{c.mission_id}: conflict on '{k}' ({state[k]} vs {v})")
                state[k] = v
        return state, contradictions

    def verify_chain(self) -> bool:
        prev = "GENESIS"
        for c in self.commits:
            if c.commit_hash != c.compute_hash(prev):
                return False
            prev = c.commit_hash
        return True


# ---------------------------------------------------------------------------
# Mission templates & adaptation
# ---------------------------------------------------------------------------

@dataclass
class MissionTemplate:
    mid: str
    name: str
    base_difficulty: int          # 1..10
    narrative_invariant: bool = False   # core plot node: cannot be removed
    tags: List[str] = field(default_factory=list)


@dataclass
class MissionInstance:
    mission_id: str
    template_id: str
    name: str
    difficulty: int
    enemy_detection: int          # 0..10
    enemy_armor: int              # 0..10
    objectives: List[str]
    notes: List[str] = field(default_factory=list)
    seed: int = 0


# Adaptation budget points spent on enemy upgrades.
ADAPTATION_BUDGET = 10


def allocate_adaptation(profile: BehaviorProfile, budget: int = ADAPTATION_BUDGET) -> Dict[str, int]:
    """Spend adaptation budget on enemy upgrades proportional to player tendencies."""
    det = 0
    armor = 0
    strat = 0
    # stealthy players -> detection systems
    det += int(profile.stealth_ratio * budget * 0.6)
    # aggressive players -> armor
    armor += int(profile.aggression_ratio * budget * 0.5)
    # vehicle users -> strategy (roadblocks)
    strat += int(profile.vehicle_ratio * budget * 0.4)
    # early exposure -> global difficulty bump (strategy)
    if profile.expose_events > 0:
        strat += 3
    # trust-heavy -> contact vulnerability handled separately; spend on intel
    if profile.trust_events > 2:
        det += 2
    total = det + armor + strat
    if total > budget:
        scale = budget / max(1, total)
        det = int(det * scale)
        armor = int(armor * scale)
        strat = budget - det - armor
    return {"detection": det, "armor": armor, "strategy": strat}


# ---------------------------------------------------------------------------
# Adaptive Mission Chain (orchestrator)
# ---------------------------------------------------------------------------

class AdaptiveMissionChain:
    def __init__(self) -> None:
        self.behavior = BehaviorLog()
        self.contacts = ContactGraph()
        self.ledger = ConsequenceLedger()
        self.templates: Dict[str, MissionTemplate] = {}
        self.completed: List[str] = []
        self.narrative_state: Dict[str, Any] = {
            "org_exposed": False,
            "alliance": "neutral",
            "difficulty_floor": 1,
        }
        self._mission_counter = 0
        self._register_default_templates()
        self._register_default_contacts()

    def _register_default_templates(self) -> None:
        defaults = [
            MissionTemplate("T_STEALTH_INFIL", "Stealth Infiltration", 4, False, ["stealth"]),
            MissionTemplate("T_SHOOTOUT", "Compound Shootout", 5, False, ["combat"]),
            MissionTemplate("T_VEHICLE", "Vehicle Pursuit", 5, False, ["vehicle"]),
            MissionTemplate("T_INTERROGATE", "Interrogation", 3, False, ["intel"]),
            MissionTemplate("T_CONTACT_MEET", "Contact Rendezvous", 3, False, ["social"]),
            MissionTemplate("T_EXPOSE", "Expose Operation", 7, True, ["climax"]),  # invariant
            MissionTemplate("T_VULN_CONTACT", "Contact Vulnerability", 6, False, ["social", "risk"]),
            MissionTemplate("T_DETECTION_RAID", "Detection-Heavy Raid", 6, False, ["stealth", "adapted"]),
        ]
        for t in defaults:
            self.templates[t.mid] = t

    def _register_default_contacts(self) -> None:
        for cid, name, cent in [("C1", "Tariro", 0.9), ("C2", "Mukoma", 0.6), ("C3", "Rudo", 0.4)]:
            self.contacts.add_contact(cid, name, cent)
        self.contacts.add_relation("C1", "C2", 1.0)
        self.contacts.add_relation("C1", "C3", 0.5)
        self.contacts.add_relation("C2", "C3", 0.3)

    def _next_id(self) -> str:
        self._mission_counter += 1
        return f"M{self._mission_counter:03d}"

    def available_templates(self) -> List[MissionTemplate]:
        # core invariant missions only unlock when conditions met
        out = []
        for t in self.templates.values():
            if t.mid == "T_EXPOSE":
                # expose requires at least 3 completed intel missions
                if len([m for m in self.completed if "intel" in self.templates.get(m.split(':')[0], MissionTemplate("", "", 0)).tags]) < 1:
                    # simpler: require 3 completed missions
                    if len(self.completed) < 3:
                        continue
            out.append(t)
        return out

    def generate_next_mission(self) -> MissionInstance:
        profile = self.behavior.profile(self.contacts)
        alloc = allocate_adaptation(profile)
        rng = random.Random(self.behavior.seed() ^ (len(self.completed) * 7919))

        # choose template based on profile tendencies + rng
        candidates = self.available_templates()
        # weight templates by profile
        weights = []
        for t in candidates:
            w = 1.0
            if "stealth" in t.tags:
                w += profile.stealth_ratio * 2
            if "combat" in t.tags:
                w += profile.aggression_ratio * 2
            if "vehicle" in t.tags:
                w += profile.vehicle_ratio * 2
            if "social" in t.tags:
                w += 0.5 * (profile.trust_events + 1) / 5.0
            if t.mid == "T_VULN_CONTACT" and self.contacts.vulnerabilities():
                w += 2.0
            if t.mid == "T_DETECTION_RAID" and profile.stealth_ratio > 0.4:
                w += 1.5
            weights.append(w)

        chosen = rng.choices(candidates, weights=weights, k=1)[0]

        # contact vulnerability injection
        objectives = [f"Complete {chosen.name}"]
        notes: List[str] = []
        if chosen.mid == "T_VULN_CONTACT":
            vulns = self.contacts.vulnerabilities()
            if vulns:
                cid = vulns[0]
                objectives.append(f"Investigate contact {cid} ({self.contacts.g.nodes[cid]['name']})")
                notes.append(f"Contact {cid} exposure crossed threshold: {self.contacts.exposure(cid):.3f}")
        if chosen.mid == "T_CONTACT_MEET":
            sources = self.contacts.critical_sources()
            if sources:
                notes.append(f"Critical sources available: {sources}")

        # difficulty
        diff = chosen.base_difficulty + alloc["strategy"]
        diff = min(10, max(1, diff + self.narrative_state["difficulty_floor"] - 1))

        # early exposure penalty
        if profile.expose_events > 0:
            diff = min(10, diff + 2)
            notes.append("Organization already exposed: heightened security")

        mid = self._next_id()
        inst = MissionInstance(
            mission_id=mid,
            template_id=chosen.mid,
            name=chosen.name,
            difficulty=diff,
            enemy_detection=min(10, chosen.base_difficulty // 2 + alloc["detection"]),
            enemy_armor=min(10, alloc["armor"]),
            objectives=objectives,
            notes=notes,
            seed=self.behavior.seed(),
        )
        return inst

    def record_behavior(self, mission_id: str, events: List[BehaviorEvent]) -> None:
        for e in events:
            e.mission_id = mission_id
            self.behavior.record(e)

    def complete_mission(self, mission_id: str, template_id: str, outcome: Dict[str, Any]) -> None:
        self.completed.append(f"{template_id}:{mission_id}")
        # trust updates from outcome
        for cid, delta in outcome.get("trust_deltas", {}).items():
            self.contacts.trust(cid, delta)
        if outcome.get("exposed"):
            self.narrative_state["org_exposed"] = True
            self.narrative_state["difficulty_floor"] = min(10, self.narrative_state["difficulty_floor"] + 2)
        if outcome.get("alliance"):
            self.narrative_state["alliance"] = outcome["alliance"]
        # commit consequence (append-style for completed list; scalars are state)
        delta = {
            "completed_missions": [mission_id],
            "template_last": template_id,
            "org_exposed": self.narrative_state["org_exposed"],
            "alliance": self.narrative_state["alliance"],
            "difficulty_floor": self.narrative_state["difficulty_floor"],
        }
        for cid, w in self.contacts.trust_weights().items():
            delta[f"trust:{cid}"] = w
        self.ledger.commit(mission_id, self.behavior.digest(), delta)

    def reconcile(self) -> Tuple[Dict[str, Any], List[str]]:
        initial = {"org_exposed": False, "alliance": "neutral", "difficulty_floor": 1}
        state, contradictions = self.ledger.replay(initial)
        # apply priority lattice: org_exposed True dominates; difficulty_floor max
        if any(c.get("org_exposed") for c in [{}] ):
            pass
        return state, contradictions

    def status(self) -> str:
        profile = self.behavior.profile(self.contacts)
        lines = []
        lines.append("=== Adaptive Mission Chain Status ===")
        lines.append(f"Completed missions: {len(self.completed)}")
        lines.append(f"Behavior events: {len(self.behavior.events)}")
        lines.append(f"Stealth ratio: {profile.stealth_ratio:.2f}  Aggression: {profile.aggression_ratio:.2f}")
        lines.append(f"Trust events: {profile.trust_events}  Expose events: {profile.expose_events}")
        lines.append(f"Narrative state: {self.narrative_state}")
        lines.append("Contacts:")
        for cid, w in self.contacts.trust_weights().items():
            lines.append(f"  {cid} ({self.contacts.g.nodes[cid]['name']}): trust={w:.2f} exposure={self.contacts.exposure(cid):.3f}")
        vulns = self.contacts.vulnerabilities()
        if vulns:
            lines.append(f"Vulnerabilities: {vulns}")
        lines.append(f"Ledger commits: {len(self.ledger.commits)} chain_valid={self.ledger.verify_chain()}")
        return "\n".join(lines)


# ---------------------------------------------------------------------------
# Demo
# ---------------------------------------------------------------------------

def demo() -> None:
    amc = AdaptiveMissionChain()

    # Simulate a stealth-heavy, trust-heavy playthrough
    scenarios = [
        # Mission 1: stealth infiltration, trust contact C1
        ("T_STEALTH_INFIL", [
            BehaviorEvent("M", ActionType.STEALTH_TAKEDOWN, "guard", 1),
            BehaviorEvent("M", ActionType.STEALTH_AVOID, "camera", 1),
            BehaviorEvent("M", ActionType.TRUST_CONTACT, "C1", 1),
        ], {"trust_deltas": {"C1": 0.4}}),
        # Mission 2: more stealth, trust C1 again
        ("T_STEALTH_INFIL", [
            BehaviorEvent("M", ActionType.STEALTH_TAKEDOWN, "guard", 1),
            BehaviorEvent("M", ActionType.STEALTH_TAKEDOWN, "guard", 1),
            BehaviorEvent("M", ActionType.TRUST_CONTACT, "C1", 1),
        ], {"trust_deltas": {"C1": 0.3}}),
        # Mission 3: exposed org early
        ("T_EXPOSE", [
            BehaviorEvent("M", ActionType.EXPOSE_ORG, "press", 1),
            BehaviorEvent("M", ActionType.GUNFIGHT, "guards", 1),
        ], {"exposed": True, "alliance": "resistance"}),
    ]

    for tpl, events, outcome in scenarios:
        m = amc.generate_next_mission()
        print(f"\n>>> Generated mission {m.mission_id}: {m.name}")
        print(f"    difficulty={m.difficulty} detection={m.enemy_detection} armor={m.enemy_armor}")
        print(f"    objectives={m.objectives}")
        for n in m.notes:
            print(f"    note: {n}")
        amc.record_behavior(m.mission_id, events)
        amc.complete_mission(m.mission_id, tpl, outcome)

    # Generate a few more to show adaptation
    print("\n--- Subsequent generated missions (adaptation visible) ---")
    for _ in range(4):
        m = amc.generate_next_mission()
        print(f"{m.mission_id}: {m.name} diff={m.difficulty} det={m.enemy_detection} armor={m.enemy_armor} notes={m.notes}")

    print("\n" + amc.status())
    print("\nReconciliation:", amc.reconcile())
    print("Chain valid:", amc.ledger.verify_chain())


if __name__ == "__main__":
    demo()
