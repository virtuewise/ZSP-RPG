"""Playable reference slice for the Shadow Intelligence System.

Run:
    python sis_game.py --demo
    python sis_game.py --interactive

The Python slice is deliberately data-driven: it reads the same JSON content
that the Unity build reads, so the three missions can be authored and tested
without opening the editor.
"""

from __future__ import annotations

import argparse
import json
import random
from pathlib import Path
from typing import Any, Dict, List, Optional, Sequence

from sis_v1 import (
    Content,
    ContentError,
    ShadowIntelligenceSystem,
    SISError,
    requirement_met,
    stable_int,
)
from ois import OperativeIdentity, tokenize, DEFAULT_DISPLAY, DEFAULT_CALLSIGN, DEFAULT_NICKNAME

BASE_STATS = {
    "stealth": 4,
    "combat": 4,
    "empathy": 4,
    "intimidation": 4,
    "tech": 4,
    "driving": 4,
}

DEFAULT_DEMO_SCRIPT = {
    "M1": {
        "M1_BRIEF": ["accept"],
        "M1_APPROACH": ["service_roof"],
        "M1_YARD": ["office", "loading_bay"],
        "M1_TENDAI": ["empathy"],
        "M1_FATE": ["spare"],
        "M1_ARCHIVE": ["take"],
        "M1_EXFIL": ["evade"],
        "M1_DEBRIEF": ["warn"],
    },
    "M2": {
        "M2_OPEN": ["ask_nyasha"],
        "M2_NYASHA": ["thanks"],
        "M2_PARTNER": ["tariro"],
        "M2_ENTRY": ["service_corridor"],
        "M2_SURVEILLANCE": ["locker", "cafe"],
        "M2_FINGERS": ["empathy"],
        "M2_FATE": ["spare"],
        "M2_LEDGER": ["publish"],
        "M2_END": ["continue"],
    },
    "M3": {
        "M3_OPEN": ["ask_witness"],
        "M3_WITNESS": ["listen"],
        "M3_APPROACH": ["east_service"],
        "M3_INFILTRATION": ["stealth"],
        "M3_CONFRONTATION": ["expose_press"],
        "M3_EPILOGUE": ["finish"],
    },
}


class Game:
    def __init__(
        self,
        content: Content,
        sis: Optional[ShadowIntelligenceSystem] = None,
        verbose: bool = True,
        identity: Optional[OperativeIdentity] = None,
    ):
        self.content = content
        self.sis = sis or ShadowIntelligenceSystem(content)
        self.verbose = verbose
        self.identity = identity or OperativeIdentity()
        self.player: Dict[str, Any] = {
            "name": "Kuda Moyo",
            "health": 10,
            "maxHealth": 10,
            "credits": 0,
            "stats": dict(BASE_STATS),
        }
        self.mission_index = 0
        self.ending: Optional[str] = None
        self.current_mission_id = ""
        self.next_mission = ""
        self.conditions_by_mission: Dict[str, Dict[str, Any]] = {}
        self.log: List[str] = []

    # -- output -------------------------------------------------------------

    def say(self, text: str = "") -> None:
        text = tokenize(text, self.identity)
        self.log.append(text)
        if self.verbose:
            print(text)

    def _speaker(self, speaker_id: str) -> str:
        character = self.content.characters.get(speaker_id)
        return character["name"] if character else speaker_id.replace("_", " ").title()

    # -- checks -------------------------------------------------------------

    def _roll(self, stat_name: str, difficulty: int, beat_id: str, choice_id: str, bonus: int = 0) -> bool:
        stat = int(self.player["stats"].get(stat_name, 0))
        seed = self.sis.seed(self.current_mission_id) ^ stable_int(beat_id) ^ stable_int(choice_id)
        roll = random.Random(seed).randint(1, 6)
        total = stat + roll + bonus
        self.say(f"  [check: {stat_name} {stat}+{roll} vs {difficulty}] -> {'success' if total >= difficulty else 'failure'}")
        return total >= difficulty

    # -- effect plumbing ----------------------------------------------------

    def _apply_effects(self, effects: Optional[Dict[str, Any]]) -> None:
        if not effects:
            return
        self.sis.apply_effects(effects, mission_id=self.current_mission_id)
        if "credits" in effects:
            self.player["credits"] = max(0, int(self.player["credits"]) + int(effects["credits"]))
        if "health" in effects:
            self.player["health"] = max(
                0,
                min(int(self.player["maxHealth"]), int(self.player["health"]) + int(effects["health"])),
            )
        if effects.get("ending"):
            self.ending = effects["ending"]
        self._summarize_effects(effects)

    def _summarize_effects(self, effects: Dict[str, Any]) -> None:
        bits: List[str] = []
        for item in effects.get("trust", []):
            bits.append(f"trust {item.get('contact')} {item.get('delta'):+}")
        for item in effects.get("factions", []):
            parts = []
            if "influence" in item:
                parts.append(f"influence {item['influence']:+}")
            if "heat" in item:
                parts.append(f"heat {item['heat']:+}")
            if item.get("exposed"):
                parts.append("EXPOSED")
            if item.get("alliance"):
                parts.append(f"alliance={item['alliance']}")
            bits.append(f"{item.get('id')} " + ", ".join(parts))
        for iid in effects.get("intel", []):
            bits.append(f"intel +{iid}")
        for flag in effects.get("flags", []):
            bits.append(f"flag +{flag}")
        for item in effects.get("witnesses", []):
            bits.append(f"witness {item.get('id')}={item.get('status')}")
        for item in effects.get("keyCharacters", []):
            bits.append(f"keychar {item.get('id')}={item.get('status')}")
        if "credits" in effects:
            bits.append(f"credits {effects['credits']:+}")
        if "health" in effects:
            bits.append(f"health {effects['health']:+}")
        if effects.get("ending"):
            bits.append(f"ending={effects['ending']}")
        if bits:
            self.say("  [SIS] " + "; ".join(bits))

    def _apply_conditional_effects(self, choice: Dict[str, Any]) -> None:
        for conditional in choice.get("conditionalEffects", []):
            if all(requirement_met(req, self.sis) for req in conditional.get("requirements", [])):
                self._apply_effects(conditional.get("effects", {}))

    # -- choice helpers -----------------------------------------------------

    def _available(self, choices: Sequence[Dict[str, Any]]) -> List[Dict[str, Any]]:
        return [
            choice
            for choice in choices
            if all(requirement_met(req, self.sis) for req in choice.get("requirements", []))
        ]

    def _display_choices(self, choices: Sequence[Dict[str, Any]], title: str = "Choices") -> None:
        self.say(f"  {title}:")
        for index, choice in enumerate(choices, start=1):
            locked = not all(requirement_met(req, self.sis) for req in choice.get("requirements", []))
            marker = "LOCKED" if locked else f"{index}"
            self.say(f"    [{marker}] {choice.get('text', choice.get('id'))}")

    def _pick(
        self,
        choices: Sequence[Dict[str, Any]],
        beat_id: str,
        script: Dict[str, List[str]],
        interactive: bool,
        title: str = "Choices",
    ) -> Dict[str, Any]:
        available = self._available(choices)
        if not available:
            raise SISError(f"no available choices for {beat_id}")
        if interactive:
            self._display_choices(choices, title)
            while True:
                raw = input("  > ").strip()
                if raw.isdigit():
                    index = int(raw) - 1
                    if 0 <= index < len(choices):
                        choice = choices[index]
                        if choice in available:
                            return choice
                        self.say("  That option is locked.")
                        continue
                self.say("  Enter the number of an available option.")
        wanted = script.get(beat_id, [])
        for choice_id in wanted:
            for choice in available:
                if choice.get("id") == choice_id:
                    return choice
        return available[0]

    # -- beat execution -----------------------------------------------------

    def execute_beat(
        self,
        beat: Dict[str, Any],
        conditions: Dict[str, Any],
        script: Dict[str, List[str]],
        interactive: bool,
    ) -> str:
        beat_type = beat.get("type")
        if beat_type in ("dialogue", "choice", "combat", "chase"):
            return self._execute_choice_beat(beat, script, interactive, conditions)
        if beat_type == "route":
            return self._execute_route(beat, conditions, script, interactive)
        if beat_type == "investigation":
            return self._execute_investigation(beat, conditions, script, interactive)
        if beat_type == "ending":
            return self._execute_ending(beat, script, interactive)
        if beat_type == "mission_end":
            self.say(self._variant_text(beat))
            self.next_mission = beat.get("nextMission", "") or ""
            return "__MISSION_END__"
        raise SISError(f"unsupported beat type {beat_type!r}")

    def _variant_text(self, beat: Dict[str, Any]) -> str:
        for variant in beat.get("variants", []):
            if all(requirement_met(req, self.sis) for req in variant.get("requirements", [])):
                return variant.get("text", "")
        return beat.get("text", "")

    def _execute_choice_beat(
        self,
        beat: Dict[str, Any],
        script: Dict[str, List[str]],
        interactive: bool,
        conditions: Dict[str, Any],
    ) -> str:
        if beat.get("type") == "combat":
            enemy = conditions["enemy"]
            self.say(
                f"  [enemy profile: detection {enemy['detection']}, armour {enemy['armor']}, "
                f"strategy {enemy['strategy']}, reinforcements {enemy['reinforcements']}]"
            )
        speaker = beat.get("speaker")
        if speaker:
            self.say(f"{self._speaker(speaker)}:")
        self.say(self._variant_text(beat))
        choice = self._pick(beat.get("choices", []), beat["id"], script, interactive)
        self.say(f"  > {choice.get('text', choice.get('id'))}")

        effects = choice.get("effects", {})
        self._apply_effects(effects)
        self._apply_conditional_effects(choice)

        next_beat = choice.get("next")
        check = choice.get("check")
        if check:
            success = self._roll(
                check["stat"],
                int(check["difficulty"]),
                beat["id"],
                choice.get("id", ""),
                int(check.get("bonus", 0)),
            )
            branch = choice.get("success" if success else "failure", {})
            if branch:
                if branch.get("text"):
                    self.say(branch["text"])
                self._apply_effects(branch.get("effects", {}))
                self._apply_conditional_effects(branch)
                next_beat = branch.get("next", next_beat)
        return next_beat or beat.get("next") or "__MISSION_END__"

    def _execute_route(
        self,
        beat: Dict[str, Any],
        conditions: Dict[str, Any],
        script: Dict[str, List[str]],
        interactive: bool,
    ) -> str:
        self.say(self._variant_text(beat))
        route_rows = []
        for route_id in beat.get("routes", []):
            row = conditions["entry"].get(route_id)
            if row is None:
                continue
            route_rows.append(row)
            status = "available" if row["available"] else "LOCKED"
            reason = f" - {row['reason']}" if row["reason"] else ""
            self.say(f"    [{status}] {row['name']} (difficulty {row['difficulty']}){reason}")
        available = [row for row in route_rows if row["available"]]
        if not available:
            raise SISError(f"no available routes for {beat['id']}")
        chosen = None
        if interactive:
            while chosen is None:
                raw = input("  route > ").strip()
                if raw.isdigit():
                    index = int(raw) - 1
                    if 0 <= index < len(route_rows) and route_rows[index]["available"]:
                        chosen = route_rows[index]
                    else:
                        self.say("  That route is locked.")
        else:
            wanted = script.get(beat["id"], [])
            for route_id in wanted:
                for row in available:
                    if row["id"] == route_id:
                        chosen = row
                        break
                if chosen:
                    break
            chosen = chosen or available[0]
        self.say(f"  > {chosen['name']}")

        kind_value = {"front": 1, "side": 2, "social": 3, "stealth": 4, "insider": 5}.get(chosen["kind"], 2)
        self.sis.record(self.current_mission_id, "approach", chosen["id"], kind_value)
        self.sis.apply_effects({"flags": [f"route_{chosen['id']}"]}, mission_id=self.current_mission_id, infer_behavior=False)
        if chosen["kind"] == "front":
            self.sis.apply_effects(
                {"factions": [{"id": "nightjar", "heat": 1}]},
                mission_id=self.current_mission_id,
                infer_behavior=False,
            )
        if chosen["id"] == "worker_bribe":
            self._apply_effects({"trust": [{"contact": "mukoma", "delta": 0.05}],
                                 "behavior": [{"action": "trust_contact", "target": "mukoma", "value": 1}]})
        elif chosen["id"] == "hill_path":
            self._apply_effects({"trust": [{"contact": "mukoma", "delta": 0.1}],
                                 "behavior": [{"action": "trust_contact", "target": "mukoma", "value": 1}]})
        elif chosen["id"] == "press_convoy":
            self._apply_effects({"trust": [{"contact": "tariro", "delta": 0.1}],
                                 "behavior": [{"action": "trust_contact", "target": "tariro", "value": 1}]})
        return beat.get("next", "__MISSION_END__")

    def _execute_investigation(
        self,
        beat: Dict[str, Any],
        conditions: Dict[str, Any],
        script: Dict[str, List[str]],
        interactive: bool,
    ) -> str:
        self.say(self._variant_text(beat))
        remaining = list(beat.get("areas", []))
        searches = int(beat.get("maxSearches", 1))
        wanted = list(script.get(beat["id"], []))
        detection = int(conditions["enemy"]["detection"])
        for _ in range(searches):
            if not remaining:
                break
            self.say("  Search options:")
            for area in remaining:
                difficulty = int(area.get("difficulty", 1)) + detection // 3
                self.say(f"    - {area['name']} ({area['stat']} difficulty {difficulty})")
            chosen = None
            if interactive:
                while chosen is None:
                    raw = input("  search > ").strip()
                    if raw.isdigit():
                        index = int(raw) - 1
                        if 0 <= index < len(remaining):
                            chosen = remaining[index]
                remaining.remove(chosen)
            else:
                for area_id in wanted:
                    for area in remaining:
                        if area["id"] == area_id:
                            chosen = area
                            break
                    if chosen:
                        break
                if chosen is None:
                    chosen = remaining[0]
                remaining.remove(chosen)
                if wanted and chosen["id"] in wanted:
                    wanted.remove(chosen["id"])
            self.say(f"  > {chosen['name']}")
            self.say(chosen.get("text", ""))
            difficulty = int(chosen.get("difficulty", 1)) + detection // 3
            success = self._roll(chosen["stat"], difficulty, beat["id"], chosen["id"])
            self.sis.record(self.current_mission_id, "investigate", chosen["id"], 1)
            if success:
                intel = chosen.get("intel", [])
                if intel:
                    names = [self.content.intel[i]["name"] for i in intel if i in self.content.intel]
                    self.say("  You recover: " + ", ".join(names))
                self._apply_effects(chosen.get("effects", {}))
                if intel:
                    self.sis.apply_effects({"intel": intel}, mission_id=self.current_mission_id, infer_behavior=False)
            else:
                self.say("  You get what you can, but not cleanly.")
                self._apply_effects(chosen.get("failureEffects", {}))
        return beat.get("next", "__MISSION_END__")

    def _execute_ending(
        self,
        beat: Dict[str, Any],
        script: Dict[str, List[str]],
        interactive: bool,
    ) -> str:
        self.say(self._variant_text(beat))
        epilogue = self.content.epilogues.get(self.ending or "")
        if epilogue:
            self.say("")
            self.say(f"EPILOGUE - {epilogue['title']}")
            self.say(epilogue["text"])
            self.say("")
        choice = self._pick(beat.get("choices", []), beat["id"], script, interactive, "Ending")
        self.say(f"  > {choice.get('text', choice.get('id'))}")
        self._apply_effects(choice.get("effects", {}))
        return choice.get("next", "__MISSION_END__")

    # -- mission loop -------------------------------------------------------

    def run_mission(
        self,
        mission_id: str,
        script: Optional[Dict[str, List[str]]] = None,
        interactive: bool = False,
    ) -> str:
        script = script or {}
        mission = self.content.mission(mission_id)
        conditions = self.sis.resolve_mission(mission_id)
        self.conditions_by_mission[mission_id] = conditions
        self.current_mission_id = mission_id
        self.next_mission = ""

        self.say("")
        self.say("=" * 72)
        self.say(f"MISSION {mission.get('order')}: {mission.get('title')}")
        self.say(f"{conditions['location']}")
        self.say("=" * 72)
        self.say(mission.get("premise", ""))
        self.say("")
        self.say("Objectives:")
        for objective in conditions["objectives"]:
            self.say(f"  - {objective}")
        enemy = conditions["enemy"]
        self.say(
            f"Enemy profile: detection {enemy['detection']}, armour {enemy['armor']}, "
            f"strategy {enemy['strategy']}, reinforcements {enemy['reinforcements']} "
            f"(adaptation {enemy['totalAdaptation']}/{10})"
        )
        if conditions["witnesses"]:
            self.say("Witnesses in play:")
            for witness in conditions["witnesses"]:
                self.say(f"  - {witness['name']}: {witness['role']} ({witness['effect']})")
        if conditions["contacts"]:
            self.say("Contacts:")
            for contact in conditions["contacts"]:
                risk = "EXPOSED" if contact["compromised"] else "secure"
                self.say(f"  - {contact['name']}: trust {contact['trust']:.2f}, exposure {contact['exposure']:.2f} ({risk})")
        if conditions["notes"]:
            self.say("SIS notes:")
            for note in conditions["notes"]:
                self.say(f"  - {note}")
        self.say("")

        beat_id = mission["start"]
        safety = 0
        while True:
            safety += 1
            if safety > 200:
                raise SISError(f"mission {mission_id} exceeded beat safety limit")
            beat = self.content.beat(mission_id, beat_id)
            next_beat = self.execute_beat(beat, conditions, script, interactive)
            if next_beat == "__MISSION_END__":
                break
            if not next_beat:
                break
            beat_id = next_beat

        self.sis.commit_mission(mission_id, ending=self.ending if mission_id == "M3" else None)
        chain_ok, chain_error = self.sis.ledger.verify()
        verified, contradictions = self.sis.verify()
        self.say("")
        self.say(
            f"[SIS] {mission_id} committed. ledger height {len(self.sis.ledger.commits)}; "
            f"chain {'OK' if chain_ok else 'BROKEN'}; replay {'OK' if verified else 'CONTRADICTIONS'}"
        )
        if chain_error:
            self.say(f"[SIS] chain error: {chain_error}")
        for contradiction in contradictions:
            self.say(f"[SIS] contradiction: {contradiction}")
        self.mission_index += 1
        return self.next_mission

    def _next_uncompleted(self) -> str:
        completed = set(self.sis.state.get("completed_missions", []))
        for mission_id in ("M1", "M2", "M3"):
            if mission_id not in completed:
                return mission_id
        return ""

    def run_all(
        self,
        script: Optional[Dict[str, Dict[str, List[str]]]] = None,
        interactive: bool = False,
    ) -> Dict[str, Any]:
        script = script or {}
        mission_id = self.next_mission or self._next_uncompleted()
        while mission_id:
            mission_id = self.run_mission(mission_id, script.get(mission_id, {}), interactive)
        return self.summary()

    # -- persistence --------------------------------------------------------

    def summary(self) -> Dict[str, Any]:
        verified, contradictions = self.sis.verify()
        return {
            "ending": self.ending,
            "identity": self.identity.to_dict(),
            "health": self.player["health"],
            "credits": self.player["credits"],
            "missionIndex": self.mission_index,
            "behaviorDigest": self.sis.behavior.digest(),
            "ledgerHeight": len(self.sis.ledger.commits),
            "verified": verified,
            "contradictions": contradictions,
            "sis": self.sis.world_snapshot(),
            "conditions": {
                mid: {
                    "enemy": cond["enemy"],
                    "entry": {rid: {"available": row["available"], "difficulty": row["difficulty"], "reason": row["reason"]}
                              for rid, row in cond["entry"].items()},
                    "objectives": cond["objectives"],
                    "witnesses": cond["witnesses"],
                    "contacts": cond["contacts"],
                }
                for mid, cond in self.conditions_by_mission.items()
            },
        }

    def save(self, path: str | Path) -> None:
        payload = {
            "version": 1,
            "player": self.player,
            "identity": self.identity.to_dict(),
            "missionIndex": self.mission_index,
            "ending": self.ending,
            "currentMissionId": self.current_mission_id,
            "nextMission": self.next_mission,
            "sis": self.sis.to_dict(),
        }
        Path(path).write_text(json.dumps(payload, indent=2, ensure_ascii=False), encoding="utf-8")

    @classmethod
    def load(cls, path: str | Path, content: Content, verbose: bool = True) -> "Game":
        payload = json.loads(Path(path).read_text(encoding="utf-8"))
        sis = ShadowIntelligenceSystem.from_dict(content, payload["sis"])
        game = cls(content, sis=sis, verbose=verbose)
        game.player = payload.get("player", game.player)
        game.identity = OperativeIdentity.from_dict(payload.get("identity"))
        game.mission_index = int(payload.get("missionIndex", 0))
        game.ending = payload.get("ending")
        game.current_mission_id = payload.get("currentMissionId", "")
        game.next_mission = payload.get("nextMission", "")
        return game


def demo(verbose: bool = True) -> Dict[str, Any]:
    content = Content.load()
    game = Game(content, verbose=verbose)
    return game.run_all(DEFAULT_DEMO_SCRIPT, interactive=False)


def main() -> int:
    parser = argparse.ArgumentParser(description="Shadow Intelligence System reference slice")
    parser.add_argument("--interactive", action="store_true", help="play the three missions interactively")
    parser.add_argument("--demo", action="store_true", help="run the deterministic demo path")
    parser.add_argument("--content", default=None, help="path to the Content directory")
    parser.add_argument("--save", default=None, help="save the final state to a JSON file")
    parser.add_argument("--load", default=None, help="load a saved state and continue")
    parser.add_argument("--json", action="store_true", help="print the final summary as JSON")
    parser.add_argument("--display-name", default=None, help="display name used in the dossier (max 28)")
    parser.add_argument("--callsign", default=None, help="operational callsign (max 12)")
    parser.add_argument("--nickname", default=None, help="optional nickname (max 14)")
    args = parser.parse_args()

    identity = OperativeIdentity(
        display_name=args.display_name or DEFAULT_DISPLAY,
        callsign=args.callsign or DEFAULT_CALLSIGN,
        nickname=args.nickname or DEFAULT_NICKNAME,
    )

    content = Content.load(args.content) if args.content else Content.load()
    if args.load:
        game = Game.load(args.load, content, verbose=not args.json)
    else:
        game = Game(content, verbose=not args.json, identity=identity)

    if args.interactive:
        summary = game.run_all(interactive=True)
    else:
        summary = game.run_all(DEFAULT_DEMO_SCRIPT, interactive=False)

    if args.save:
        game.save(args.save)
    if args.json:
        print(json.dumps(summary, indent=2, ensure_ascii=False))
    else:
        print("")
        print("FINAL SUMMARY")
        print(f"  callsign: {summary['identity']['callsign']}  display: {summary['identity']['displayName']}")
        print(f"  ending: {summary['ending']}")
        print(f"  health: {summary['health']}  credits: {summary['credits']}")
        print(f"  ledger height: {summary['ledgerHeight']}")
        print(f"  behaviour digest: {summary['behaviorDigest']}")
        print(f"  replay verified: {summary['verified']}")
        if summary["contradictions"]:
            print("  contradictions:")
            for item in summary["contradictions"]:
                print(f"    - {item}")
        print("  key consequences:")
        sis = summary["sis"]
        print(f"    witnesses: { {k: v['status'] for k, v in sis['witnesses'].items()} }")
        print(f"    contacts: { {k: (v['trust'], v['compromised']) for k, v in sis['contacts'].items()} }")
        print(f"    flags: {', '.join(sis['flags'][:18])}{'...' if len(sis['flags']) > 18 else ''}")
        print(f"    intel: {', '.join(sis['intel'])}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
