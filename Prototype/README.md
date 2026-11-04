# Prototype — Adaptive Mission Chain

This is the original Python prototype that defines the novel coordination
method used by `Assets/Scripts/Narrative/MissionChain.cs`.

- `adaptive_mission_chain.py` — Behaviour-Indexed Consequence Ledger (BICL),
  deterministic replay reconciliation, exposure-coupled contact trust graph,
  adaptation budget, seeded mission-template mutation, and demo.
- `test_amc.py` — determinism, chain integrity (zero contradictions) and
  exposure-threshold tests.

Run:

```powershell
python adaptive_mission_chain.py
python -m pytest test_amc.py -q
```

The C# port keeps the same core concepts but is intentionally smaller; use the
Python version as the reference specification when extending the Unity system.
