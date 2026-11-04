"""Tests for the Shadow Intelligence System and the three-mission slice."""

from __future__ import annotations

import json
import sys
from pathlib import Path

import pytest

PROTOTYPE = Path(__file__).resolve().parent
if str(PROTOTYPE) not in sys.path:
    sys.path.insert(0, str(PROTOTYPE))

from sis_v1 import (
    Content,
    ConsequenceLedger,
    ShadowIntelligenceSystem,
    apply_delta,
    initial_state,
    parse_requirement,
    requirement_met,
    stable_int,
)
from sis_game import BASE_STATS, DEFAULT_DEMO_SCRIPT, Game


@pytest.fixture(scope="module")
def content() -> Content:
    return Content.load()


def run_script(script: dict, stats: dict | None = None) -> Game:
    content = Content.load()
    game = Game(content, verbose=False)
    if stats:
        game.player["stats"] = dict(stats)
    game.run_all(script, interactive=False)
    return game


def test_content_loads_and_validates(content: Content):
    assert set(content.missions) == {"M1", "M2", "M3"}
    for mid, mission in content.missions.items():
        assert mission["beats"], mid
        assert mission["invariants"], mid
    assert content.locations["msasa_yard"]["routes"]
    assert content.epilogues["expose"]["text"]
    for req in ("flag:route_main_gate", "intel:payment_ledger", "any:witness:tendai:spared,witness:fingers:spared", "trust:tariro:0.4"):
        parse_requirement(req)


def test_demo_path_is_deterministic():
    first = run_script(DEFAULT_DEMO_SCRIPT)
    second = run_script(DEFAULT_DEMO_SCRIPT)
    assert first.sis.behavior.digest() == second.sis.behavior.digest()
    assert first.ending == second.ending == "expose"
    assert first.mission_index == second.mission_index == 3
    assert first.sis.world_snapshot() == second.sis.world_snapshot()
    assert first.sis.verify()[0] is True


def test_front_door_pattern_reinforces_entrances():
    script = {
        "M1": {
            "M1_BRIEF": ["accept"],
            "M1_APPROACH": ["front_gate"],
            "M1_YARD": ["office"],
            "M1_TENDAI": ["empathy"],
            "M1_FATE": ["kill"],
            "M1_ARCHIVE": ["take"],
            "M1_EXFIL": ["fight"],
            "M1_DEBRIEF": ["continue"],
        },
        "M2": {
            "M2_OPEN": ["listen"],
            "M2_PARTNER": ["mukoma"],
            "M2_ENTRY": ["mall_entrance"],
            "M2_SURVEILLANCE": ["locker", "street"],
            "M2_FINGERS": ["kill"],
            "M2_LEDGER": ["destroy"],
            "M2_END": ["continue"],
        },
    }
    game = run_script(script)
    m3 = game.sis.resolve_mission("M3")
    main_gate = m3["entry"]["main_gate"]
    assert main_gate["difficulty"] >= 8
    assert "reinforced" in main_gate["reason"].lower()
    assert m3["entry"]["press_convoy"]["available"] is False
    assert m3["entry"]["hill_path"]["available"] is False
    assert m3["entry"]["mhofu_keycard"]["available"] is False


def test_spared_cooperative_witness_reappears_with_intel():
    script = {
        "M1": {
            "M1_BRIEF": ["accept"],
            "M1_APPROACH": ["service_roof"],
            "M1_YARD": ["office", "loading_bay"],
            "M1_TENDAI": ["empathy"],
            "M1_FATE": ["spare"],
            "M1_ARCHIVE": ["take"],
            "M1_EXFIL": ["evade"],
            "M1_DEBRIEF": ["warn"],
        }
    }
    game = run_script(script)
    m2 = game.sis.resolve_mission("M2")
    assert game.sis.has_intel("nightjar_patrol")
    assert game.sis.has_intel("nightjar_manifest")
    assert m2["witnesses"] and m2["witnesses"][0]["role"] == "informant"
    assert m2["enemy"]["detection"] < 3 + 10  # informant reduced, not eliminated


def test_spared_silent_witness_raises_heat():
    script = {
        "M1": {
            "M1_BRIEF": ["accept"],
            "M1_APPROACH": ["front_gate"],
            "M1_YARD": ["office"],
            "M1_TENDAI": ["threat"],
            "M1_ARCHIVE": ["take"],
            "M1_EXFIL": ["evade"],
            "M1_DEBRIEF": ["continue"],
        }
    }
    game = run_script(script)
    m2 = game.sis.resolve_mission("M2")
    assert game.sis.witness_status("tendai") == "spared"
    assert game.sis.has_flag("tendai_liability")
    assert game.sis.faction("nightjar", "heat") >= 3


def test_nyasha_exposure_captures_her_before_m3():
    script = {
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
    }
    game = run_script(script)
    assert game.sis.is_compromised("nyasha")
    m3 = game.sis.resolve_mission("M3")
    assert game.sis.has_flag("nyasha_captured")
    assert "Rescue Nyasha from the estate" in m3["objectives"]
    nyasha = next(row for row in m3["contacts"] if row["id"] == "nyasha")
    assert nyasha["compromised"] is True
    assert nyasha["available"] is False


def test_tariro_exposure_sets_protect_objective():
    script = {
        "M1": {
            "M1_BRIEF": ["accept"],
            "M1_APPROACH": ["service_roof"],
            "M1_YARD": ["office"],
            "M1_TENDAI": ["empathy"],
            "M1_FATE": ["spare"],
            "M1_ARCHIVE": ["copy"],
            "M1_DEBRIEF": ["warn"],
        },
        "M2": {
            "M2_OPEN": ["listen"],
            "M2_PARTNER": ["tariro"],
            "M2_ENTRY": ["service_corridor"],
            "M2_SURVEILLANCE": ["locker", "cafe"],
            "M2_FINGERS": ["empathy"],
            "M2_FATE": ["spare"],
            "M2_LEDGER": ["publish"],
            "M2_END": ["continue"],
        },
    }
    game = run_script(script)
    assert game.sis.is_compromised("tariro")
    m3 = game.sis.resolve_mission("M3")
    assert game.sis.has_flag("tariro_targeted")
    assert "Protect Tariro" in m3["objectives"]


def test_eliminating_all_witnesses_forces_terminal_route_objective():
    script = {
        "M1": {
            "M1_BRIEF": ["accept"],
            "M1_APPROACH": ["front_gate"],
            "M1_YARD": ["office"],
            "M1_TENDAI": ["empathy"],
            "M1_FATE": ["kill"],
            "M1_ARCHIVE": ["take"],
            "M1_EXFIL": ["fight"],
            "M1_DEBRIEF": ["continue"],
        },
        "M2": {
            "M2_OPEN": ["listen"],
            "M2_PARTNER": ["solo"],
            "M2_ENTRY": ["mall_entrance"],
            "M2_SURVEILLANCE": ["locker"],
            "M2_FINGERS": ["kill"],
            "M2_LEDGER": ["destroy"],
            "M2_END": ["continue"],
        },
    }
    game = run_script(script)
    m3 = game.sis.resolve_mission("M3")
    assert "Hack the estate terminal for the core file" in m3["objectives"]


def test_high_heat_reinforces_all_approaches():
    script = {
        "M1": {
            "M1_BRIEF": ["accept"],
            "M1_APPROACH": ["front_gate"],
            "M1_YARD": ["office"],
            "M1_TENDAI": ["empathy"],
            "M1_FATE": ["kill"],
            "M1_ARCHIVE": ["take"],
            "M1_EXFIL": ["fight"],
            "M1_DEBRIEF": ["continue"],
        },
        "M2": {
            "M2_OPEN": ["listen"],
            "M2_PARTNER": ["tariro"],
            "M2_ENTRY": ["mall_entrance"],
            "M2_SURVEILLANCE": ["street"],
            "M2_FINGERS": ["kill"],
            "M2_LEDGER": ["publish"],
            "M2_END": ["continue"],
        },
    }
    game = run_script(script)
    assert game.sis.faction("nightjar", "heat") >= 4
    m3 = game.sis.resolve_mission("M3")
    assert m3["enemy"]["detection"] >= 4
    assert m3["enemy"]["reinforcements"] >= 2


def test_low_stats_still_reach_an_ending():
    stats = {name: 0 for name in BASE_STATS}
    game = run_script(DEFAULT_DEMO_SCRIPT, stats=stats)
    assert game.ending in {"burn", "expose", "control"}
    assert game.mission_index == 3
    assert game.sis.verify()[0] is True


def test_save_and_load_match_continuous_run(tmp_path: Path):
    continuous = run_script(DEFAULT_DEMO_SCRIPT)
    content = Content.load()

    script = DEFAULT_DEMO_SCRIPT
    first = Game(content, verbose=False)
    first.run_mission("M1", script["M1"], interactive=False)
    first.run_mission("M2", script["M2"], interactive=False)
    save_path = tmp_path / "zsp_save.json"
    first.save(save_path)

    resumed = Game.load(save_path, content, verbose=False)
    assert resumed.mission_index == 2
    assert resumed.next_mission == "M3"
    resumed.run_all({"M3": script["M3"]}, interactive=False)

    assert resumed.ending == continuous.ending == "expose"
    assert resumed.sis.behavior.digest() == continuous.sis.behavior.digest()
    assert resumed.sis.verify()[0] is True


def test_ledger_replay_detects_witness_contradiction():
    content = Content.load()
    initial = initial_state(content)
    ledger = ConsequenceLedger()
    delta_spared = {"witness:tendai:status": "spared", "keychar:tendai": "alive"}
    delta_killed = {"witness:tendai:status": "killed", "keychar:tendai": "dead"}
    ledger.commit("M1", "digest-a", delta_spared)
    ledger.commit("M1", "digest-b", delta_killed)
    replayed, contradictions = ledger.replay(initial)
    assert any("tendai" in item for item in contradictions)
    assert replayed["witness:tendai:status"] == "spared"


def test_requirements_and_invariants():
    content = Content.load()
    game = Game(content, verbose=False)
    game.sis.state["trust:tariro"] = 0.4
    game.sis.state["intel:payment_ledger"] = True
    assert requirement_met("trust:tariro:0.4", game.sis)
    assert requirement_met("intel:payment_ledger", game.sis)
    assert requirement_met("any:flag:nope,intel:payment_ledger", game.sis)
    assert not requirement_met("any:flag:nope,flag:also_nope", game.sis)
    with pytest.raises(Exception):
        parse_requirement("nonsense")


def test_narrative_invariants_survive_every_scripted_path():
    content = Content.load()
    for ending_choice in ("expose_press", "burn", "shoot"):
        script = dict(DEFAULT_DEMO_SCRIPT)
        m3 = dict(script["M3"])
        m3["M3_CONFRONTATION"] = [ending_choice]
        script["M3"] = m3
        game = run_script(script)
        assert game.mission_index == 3
        assert game.ending in {"expose", "control", "burn"}
        assert game.sis.state["completed_missions"] == ["M1", "M2", "M3"]
