"""Tests for the Shadow Intelligence System and the official Season One slice."""

from __future__ import annotations

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
    initial_state,
    parse_requirement,
    requirement_met,
)
from sis_game import BASE_STATS, DEFAULT_DEMO_SCRIPT, Game


@pytest.fixture(scope="module")
def content() -> Content:
    return Content.load()


def run_script(script, stats=None):
    content = Content.load()
    game = Game(content, verbose=False)
    if stats:
        game.player["stats"] = dict(stats)
    game.run_all(script, interactive=False)
    return game


def test_content_loads_and_validates(content: Content):
    assert set(content.missions) >= {"M1", "M2", "M3", "M4", "M5", "M6"}
    for mid, mission in content.missions.items():
        assert mission["beats"], mid
        assert mission["invariants"], mid
    assert content.epilogues["interlude"]["title"]
    assert content.locations["chitungwiza"]["routes"]
    assert content.locations["avondale_safehouse"]["routes"]
    assert content.locations["bulawayo_yard"]["routes"]
    for req in ("flag:accepted_message", "intel:mhara_phone", "any:witness:lovemore:spared,witness:tafadzwa:spared", "trust:amara:0.4"):
        parse_requirement(req)


def test_demo_path_is_deterministic():
    first = run_script(DEFAULT_DEMO_SCRIPT)
    second = run_script(DEFAULT_DEMO_SCRIPT)
    assert first.sis.behavior.digest() == second.sis.behavior.digest()
    assert first.ending == second.ending == "interlude"
    assert first.mission_index == second.mission_index == 3
    assert first.sis.world_snapshot() == second.sis.world_snapshot()
    assert first.sis.verify()[0] is True


def test_front_door_pattern_reinforces_entrances():
    script = {
        "M1": {
            "M1_BRIEF": ["who_else"],
            "M1_AMARA_ANSWERS": ["accept"],
            "M1_APPROACH": ["front_door"],
            "M1_SWEEP": ["fight"],
            "M1_LOVEMORE": ["stop"],
            "M1_CLOCK": ["go"],
        },
        "M2": {
            "M2_OPEN": ["thanks"],
            "M2_ENTRY": ["bus_rank"],
            "M2_CULVERT": ["phone", "lovemore"],
            "M2_MSG09": ["protect"],
            "M2_VAN": ["run"],
        },
    }
    game = run_script(script)
    m3 = game.sis.resolve_mission("M3")
    notes = " ".join(m3["notes"]).lower()
    assert "front-door pattern" in notes
    assert m3["enemy"]["strategy"] >= 5
    assert m3["enemy"]["reinforcements"] >= 3


def test_lovemore_informant_returns_in_m2():
    script = {
        "M1": {
            "M1_BRIEF": ["accept"],
            "M1_APPROACH": ["back_alley"],
            "M1_SWEEP": ["evade"],
            "M1_LOVEMORE": ["stop"],
            "M1_CLOCK": ["go"],
        }
    }
    game = run_script(script)
    m2 = game.sis.resolve_mission("M2")
    assert game.sis.has_intel("cleaner_roster") or any(w["role"] == "informant" for w in m2["witnesses"])
    assert game.sis.witness_status("lovemore") == "spared"
    assert game.sis.witness_cooperative("lovemore") is True


def test_lovemore_silent_raises_heat():
    script = {
        "M1": {
            "M1_BRIEF": ["silence"],
            "M1_APPROACH": ["back_alley"],
            "M1_SWEEP": ["evade"],
            "M1_LOVEMORE": ["observe"],
            "M1_CLOCK": ["go"],
        }
    }
    game = run_script(script)
    m2 = game.sis.resolve_mission("M2")
    assert game.sis.faction("meridian", "heat") >= 3
    notes = " ".join(m2["notes"]).lower()
    assert "meridian" in notes


def test_amara_exposure_sets_protect_objective_in_m3():
    script = {
        "M1": {
            "M1_BRIEF": ["who_else"],
            "M1_AMARA_ANSWERS": ["accept"],
            "M1_APPROACH": ["back_alley"],
            "M1_SWEEP": ["evade"],
            "M1_LOVEMORE": ["stop"],
            "M1_CLOCK": ["go"],
        },
        "M2": {
            "M2_OPEN": ["thanks"],
            "M2_ENTRY": ["drain"],
            "M2_CULVERT": ["phone", "lovemore"],
            "M2_MSG09": ["silent_gate"],
            "M2_MUPONDA": ["call"],
            "M2_VAN": ["run"],
        },
    }
    game = run_script(script)
    assert game.sis.is_compromised("amara")
    m3 = game.sis.resolve_mission("M3")
    assert any("Protect Amara" in objective for objective in m3["objectives"])
    amara = next(row for row in m3["contacts"] if row["id"] == "amara")
    assert amara["compromised"] is True


def test_tafadzwa_dead_still_completes_slice():
    script = {
        "M1": {
            "M1_BRIEF": ["accept"],
            "M1_APPROACH": ["back_alley"],
            "M1_SWEEP": ["evade"],
            "M1_LOVEMORE": ["stop"],
            "M1_CLOCK": ["go"],
        },
        "M2": {
            "M2_OPEN": ["thanks"],
            "M2_ENTRY": ["drain"],
            "M2_CULVERT": ["phone", "lovemore"],
            "M2_MSG09": ["silent_gate"],
            "M2_MUPONDA": ["hold"],
            "M2_VAN": ["run"],
        },
        "M3": {
            "M3_OPEN": ["brief"],
            "M3_BREACH": ["assault"],
            "M3_TAFADZWA": ["leave"],
            "M3_EPILOGUE": ["finish"],
        },
    }
    game = run_script(script)
    assert game.ending == "interlude"
    assert game.sis.witness_status("tafadzwa") == "killed"
    assert game.sis.verify()[0] is True


def test_low_stats_still_reach_the_interlude():
    stats = {name: 0 for name in BASE_STATS}
    game = run_script(DEFAULT_DEMO_SCRIPT, stats=stats)
    assert game.ending == "interlude"
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
    resumed.run_all({"M3": script["M3"]}, interactive=False)

    assert resumed.ending == continuous.ending == "interlude"
    assert resumed.sis.behavior.digest() == continuous.sis.behavior.digest()
    assert resumed.sis.verify()[0] is True


def test_ledger_replay_detects_witness_contradiction():
    content = Content.load()
    initial = initial_state(content)
    ledger = ConsequenceLedger()
    spared = {"witness:lovemore:status": "spared", "keychar:lovemore": "alive"}
    killed = {"witness:lovemore:status": "killed", "keychar:lovemore": "dead"}
    ledger.commit("M1", "digest-a", spared)
    ledger.commit("M1", "digest-b", killed)
    replayed, contradictions = ledger.replay(initial)
    assert any("lovemore" in item for item in contradictions)
    assert replayed["witness:lovemore:status"] == "spared"


def test_requirements_and_invariants():
    content = Content.load()
    game = Game(content, verbose=False)
    game.sis.state["trust:amara"] = 0.4
    game.sis.state["intel:mhara_phone"] = True
    assert requirement_met("trust:amara:0.4", game.sis)
    assert requirement_met("intel:mhara_phone", game.sis)
    assert requirement_met("any:flag:nope,intel:mhara_phone", game.sis)
    assert not requirement_met("any:flag:nope,flag:also_nope", game.sis)
    with pytest.raises(Exception):
        parse_requirement("nonsense")


def test_narrative_invariants_survive_scripted_paths():
    for choice in ("carry", "extract", "leave"):
        script = dict(DEFAULT_DEMO_SCRIPT)
        m3 = dict(script["M3"])
        m3["M3_TAFADZWA"] = [choice]
        script["M3"] = m3
        game = run_script(script)
        assert game.mission_index == 3
        assert game.sis.state["completed_missions"] == ["M1", "M2", "M3"]
        assert game.ending == "interlude"
