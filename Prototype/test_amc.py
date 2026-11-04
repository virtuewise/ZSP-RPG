"""Determinism and coherence tests for Adaptive Mission Chain."""
import copy
from adaptive_mission_chain import AdaptiveMissionChain, BehaviorEvent, ActionType


def run_scenario():
    amc = AdaptiveMissionChain()
    amc.record_behavior("M001", [
        BehaviorEvent("M001", ActionType.STEALTH_TAKEDOWN, "guard"),
        BehaviorEvent("M001", ActionType.TRUST_CONTACT, "C1"),
    ])
    amc.complete_mission("M001", "T_STEALTH_INFIL", {"trust_deltas": {"C1": 0.4}})
    m = amc.generate_next_mission()
    return m.mission_id, m.name, m.difficulty, m.enemy_detection, m.seed, amc.ledger.verify_chain()


def test_determinism():
    a = run_scenario()
    b = run_scenario()
    assert a == b, (a, b)
    print("DETERMINISM OK:", a)


def test_chain_integrity():
    amc = AdaptiveMissionChain()
    amc.record_behavior("M001", [BehaviorEvent("M001", ActionType.STEALTH_TAKEDOWN, "g")])
    amc.complete_mission("M001", "T_STEALTH_INFIL", {})
    assert amc.ledger.verify_chain()
    state, contras = amc.reconcile()
    assert contras == [], contras
    print("CHAIN OK, contradictions=", contras)


def test_exposure_threshold():
    amc = AdaptiveMissionChain()
    for _ in range(2):
        amc.contacts.trust("C1", 0.4)
    assert "C1" in amc.contacts.vulnerabilities(), amc.contacts.exposure("C1")
    print("EXPOSURE OK:", amc.contacts.exposure("C1"))


if __name__ == "__main__":
    test_determinism()
    test_chain_integrity()
    test_exposure_threshold()
    print("ALL TESTS PASSED")
