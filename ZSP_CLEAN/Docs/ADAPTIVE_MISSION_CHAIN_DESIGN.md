# Adaptive Mission Chain (AMC) — Design Specification

**Project C: ZIMBABWE — THE SHADOW PROTOCOL** (cinematic mobile RPG)

## Problem
Adaptive enemies and branching narratives already exist. The unsolved, specific problem is
**coordinating long-term mission dependencies coherently**: a decision made in mission 3 must
still constrain mission 47 in a way that is (a) adaptive, (b) deterministic/reproducible,
(c) auditable, and (d) free of state explosion or silent contradictions.

## The Novel Method (specific and demonstrable)

### 1. Behavior-Indexed Consequence Ledger (BICL)
- Every mission outcome is recorded as an **append-only, hash-chained commit**.
- The commit's identity includes a hash of the **full prior behavior history** (the digest
  of the `BehaviorLog`), not just story flags. Thus the commit is bound to *how* the player
  played, not only *what* they chose.
- **Reproducibility invariant**: two players with byte-identical behavior logs receive
  byte-identical future missions, because the RNG seed is derived from the log digest.
- **Adaptivity**: any behavioral divergence changes the digest, cascading into different
  mission construction. No manual branching flags needed.

### 2. Deterministic Replay Reconciliation
- Narrative state is **never stored as a mutable blob**. It is re-derived by replaying the
  ledger from a genesis state.
- A reconciliation pass re-executes every commit and applies a **priority lattice**:
  - Monotonic invariants (e.g. `org_exposed`) can only go `False -> True`, never back.
  - Trust weights are monotonic non-decreasing per contact (no silent trust loss).
  - `difficulty_floor` is monotonic non-decreasing.
  - `completed_missions` accumulates.
- Any violation is reported as an explicit contradiction with mission id and key, instead
  of being silently overwritten. This is what makes long-term coherence *checkable*.

### 3. Contact Trust as Weighted Digraph with Exposure
- Contacts are nodes in a directed graph; trust is a weight on the player→contact relation.
- Each contact's **exposure** = `trust * normalized_centrality` (centrality = stored
  importance hint, normalized). Trust is never free: raising trust raises exposure.
- When exposure crosses a threshold, the contact becomes a **vulnerability mission**
  (`T_VULN_CONTACT`), converting a trusted source into a potential liability.
- Critical sources (trust above threshold) unlock information missions.
- This couples the social system to the mission system through a single, auditable score.

### 4. Mission Template Mutation via Seeded Constraint Solver
- Base mission templates are parameterized (difficulty, detection, armor, objectives).
- An **adaptation budget** (default 10 points) is allocated by a constraint solver that
  spends points on enemy upgrades in proportion to measured player tendencies:
  - stealth-heavy → detection systems
  - aggressive → armor
  - vehicle use → strategy/roadblocks
  - early exposure → +3 strategy, +2 difficulty
  - trust-heavy → +2 detection (counter-intel)
- The solver rescales to stay within budget (no runaway difficulty).
- **Narrative invariants** (core plot nodes such as `T_EXPOSE`) cannot be removed by
  adaptation — only hardened. This guarantees the story remains completable.
- The RNG is seeded from the behavior digest XOR mission count, so mutation is
  deterministic given the same history.

### 5. Mission Selection Weighting
Templates are chosen by a weighted draw where weights depend on the player's
`BehaviorProfile` (stealth/aggression/vehicle/trust ratios) plus adaptation triggers
(e.g. `T_DETECTION_RAID` gains weight when stealth ratio > 0.4; `T_VULN_CONTACT` gains
weight when vulnerabilities exist). This makes the *content* of missions track behavior,
not just their difficulty.

## Coherence Guarantees (why this works over 100+ missions)
1. State is a pure function of the ledger → no drift, no hidden mutable state.
2. Hash chain gives tamper-evidence and auditability.
3. Monotonic priority lattice prevents contradictory reversals.
4. Deterministic seeding gives reproducibility and debuggability.
5. Adaptation budget cap + narrative invariants bound difficulty and preserve completness.

## Files
- `adaptive_mission_chain.py` — runnable prototype (demo + API).
- `test_amc.py` — determinism, chain integrity, exposure tests.

## Running
```
python adaptive_mission_chain.py   # demo
python test_amc.py                 # tests
```
