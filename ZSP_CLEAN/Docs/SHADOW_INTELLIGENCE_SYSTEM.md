# Shadow Intelligence System (SIS) — v1 Specification

**ZIMBABWE: THE SHADOW PROTOCOL** · Season 1

## What SIS is

SIS is the system that makes the game behave like a suspicious intelligence service rather
than a script. It watches **how** the player plays, remembers it across missions, and
changes only the *conditions* of the story — never the story itself. It is deterministic,
auditable, and bounded.

## What SIS is not

- It is not a branching-dialogue tree.
- It is not a difficulty slider.
- It is not a procedural quest generator.
- It is not a replacement for authored narrative.
- It is not a real-time pacing director.

## The five guarantees

1. **Determinism.** The same behaviour history produces the same future content. No hidden
   rolls that cannot be reproduced from the log.
2. **Auditability.** Every change SIS makes carries a reason: "Nightjar reinforced the
   front gate because you used it 4 times."
3. **Boundedness.** Adaptation is a 0–10 budget per mission. The plot cannot be consumed.
4. **Coherence.** Narrative state is rederived by replaying the ledger; contradictions are
   detected, not papered over.
5. **Persistence.** Witnesses, key characters, contacts and factions carry their fate
   across missions.

---

## 1. Data model

```
SISState
 ├── trust[contact_id]              float 0..1   (latest write; may increase or decrease)
 ├── faction[id].influence          int 0..10   (latest write)
 ├── faction[id].heat               int 0..10   (latest write)
 ├── faction[id].exposed            bool         (monotonic False → True)
 ├── faction[id].alliance           string       (latest write)
 ├── witness[id].status             unknown|spared|detained|captured|killed
 ├── witness[id].cooperative        bool         (monotonic False → True)
 ├── keychar[id]                    unknown|alive|dead|missing
 ├── intel[id]                      bool         (monotonic False → True)
 ├── flag[name]                     bool         (monotonic False → True)
 ├── ending[mission]                string       (set once)
 └── completed_missions[]           ordered list
```

Behaviour stream: `BehaviorEvent = { mission_id, action, target, value, seq }`.

`behavior_digest = SHA256(canonical_json(events))`

## 2. The Behaviour-Indexed Consequence Ledger (BICL)

Each mission outcome is committed as:

```
commit = {
  missionId,
  behaviorDigest,
  delta,             // key/value consequences, e.g. "witness:tendai:status":"spared"
  prevHash,          // previous commit's hash, or GENESIS
  commitHash = SHA256(canonical(missionId|behaviorDigest|delta|prevHash))
}
```

Properties:

- Append-only. A later mission cannot retroactively edit an earlier consequence.
- Hash-chained. Tampering with any commit invalidates the chain.
- Bound to behaviour. A behaviour change changes the digest and therefore the seed,
  producing a deterministic cascade of future content.

## 3. Deterministic Replay Reconciliation

Runtime state is a convenience. The ledger is the truth.

`initial_state(content)` → replay each commit with `apply_delta` → compare with the
in-memory state. Any divergence (excluding pending mid-mission keys) is reported.

Replay **detects contradictions**:

- a witness both spared and killed;
- a faction no longer exposed after being exposed;
- a key character both dead and alive;
- a mission whose required ending was set differently;
- a completed-missions list that does not match the ledger.

SIS resolves nothing by guessing. It reports contradictions and halts the commit.

## 4. The Contact Exposure Graph

Each contact has:

- `trust` (0..1) — changes with dialogue and choices;
- `centrality` (0..1) — how load-bearing the contact is in the player's network;
- `exposure = trust × centrality`;
- `trust_events` — how many times the player has leaned on this contact.

A contact is **compromised** when:

```
exposure ≥ threshold(contact)   AND   trust_events ≥ min_events(contact)
```

Thresholds are per contact and are defined in `COMPROMISE_RULES` in the reference
implementation. Roughly: Nyasha needs sustained reliance, Tariro and Mukoma are exposed
faster.

Being compromised is a *mission condition*, not a failure:

- M2: the safehouse is a burned shell; the player can still try the Avondale route and is
  denied help.
- M3: Nyasha is captured and must be rescued; Tariro is targeted and must be protected;
  Mukoma is watched and his routes shrink.

Player-facing text: "They know you talk to her."

## 5. The Witness / Key-Character Registry

Witnesses (`tendai`, `fingers`, `mhofu`) persist:

| Status | Effect in later missions |
|---|---|
| `spared` + `cooperative` | Appears as an informant/whistleblower; gives intel; lowers detection. |
| `spared` + not cooperative | Appears as a liability; raises heat and detection. |
| `captured` | Objective "Recover …" is added; mission may include a rescue. |
| `killed` | Testimony unavailable; dependent missions route around the hole. |

Key characters (e.g. `mhofu`) follow the same rule. Deleting a key character always has a
consequence, never an inconvenience:

- Fingers dead → no `fingers_testimony`; the Expose ending requires the published ledger.
- Fingers + Tendai dead → "Hack the estate terminal for the core file" is added to M3.
- Mhofu dead → the `mhofu_keycard` route in M3 is locked.
- `gumbo` dead → ending `burn` has a named corpse; ending `control` harder (no patron).

## 6. The Adaptation Budget

Each mission starts with a fixed base enemy profile. SIS derives an **adaptation vector**
from the behaviour profile and spends it against a cap of **10 points**:

| Signal | Spend |
|---|---|
| `stealth_ratio × 4` | detection |
| `aggression_ratio × 4` | armour |
| `vehicle_ratio × 3` | strategy |
| front-door pattern (entries ≥ 2, front_ratio ≥ 0.6) | strategy +2, reinforcements +1 |
| witnesses spared | strategy +1 |
| exposed contact | detection +1 |
| nightjar heat ≥ 4 | detection +1, armour +1 |
| key character killed | strategy +1 |

The vector is scaled to fit inside the cap. It is applied to:

- `detection` — how fast sensors fill;
- `armour` — how hard enemies are;
- `strategy` — how coordinated their response is;
- `reinforcements` — how many extra units respond.

It is **never** applied to: story beats, the confrontation, the protocol reveal, the
availability of an ending, or the fact that the mission ends where the script says.

## 7. The Five Named Player Behaviours

| Player behaviour | SIS mechanism |
|---|---|
| "Repeatedly enter buildings through the front door" | `entry_counts` + `front_ratio`; `front` route gets +2 difficulty and the "reinforced entrance" reason; cameras also improve. |
| "Relied heavily on one contact" | Contact exposure graph → `contact_compromised` → M2/M3 variants and objectives. |
| "Left witnesses alive" | Witness registry → informant/liability/captive appearances; may raise heat. |
| "Exposed organisations" | `expose_org` → faction `exposed` (monotonic), `heat` +2, enemy profile up, civic-front influence up. |
| "Eliminated key characters" | `keychar`/`witness` registry → fallback objectives; dependent endings re-resolve; narrative invariants re-validate. |

## 8. Narrative invariants

Each mission declares `invariants`. For Season 1:

- **M1** — archive resolved; Nightjar contact made.
- **M2** — ledger resolved; Fingers contacted.
- **M3** — confrontation happens; the protocol is revealed; at least one ending is available.

SIS validates these at load and at resolution. `M3` with no endings available is a build
failure, not a runtime shrug.

## 9. Save / load

A save stores the full SIS dictionary: state, behaviour list, ledger, pending mid-mission
delta, player health/credits/stats, current mission, ending. On load, the ledger is
replayed and verified; any contradiction is surfaced to the player as a "save is damaged or
from an incompatible version" error.

## 10. Verification

The reference implementation (`ZSP_CLEAN/Prototype`) ships with 14 tests covering:

- content validation and requirement parsing;
- deterministic replay of the three-mission slice;
- front-door reinforcement (difficulty ≥ 8 on the M3 main gate);
- witness reappearance (informant lowers detection; liability raises heat);
- contact exposure and capture/protection objectives;
- key-character elimination → fallback terminal objective;
- heat-based reinforcement profile;
- failed-check paths still reach an ending;
- save → load → continue == continuous run (identical behaviour digest);
- ledger hash-chain verification and replay contradiction detection;
- M3 narrative invariants under every scripted ending.

## 11. Unity integration

`Assets/Scripts/Narrative/StoryContent.cs` loads the same JSON content the Python slice
consumes (`Assets/StreamingAssets/Content/`). `ShadowIntelligenceSystem.cs` is a C# port of
the reference engine. `MissionDirector.cs` resolves a mission, applies SIS conditions to
`StealthSystem`/`HUDController`, and raises events for spawning. `SISSaveSystem.cs` handles
`Application.persistentDataPath/zsp_save.json`. `SISDemo.cs` runs the whole three-mission
slice in Play Mode without a scene build.

## 12. Player-facing voice

The SIS is never named in the game. The player hears:

- "They know you came in through the front."
- "Tendai talked."
- "You made her too useful to trust."
- "There are more of them now."

The audit lines exist for debugging and for a post-mission debrief.

## 13. Known limits

- Not a substitute for writers.
- Does not handle contradictions inside a mission if the player edits the save mid-mission.
- Contact exposure thresholds are authored and must be tuned by playtest.
- The adaptation budget is a pacing tool, not a difficulty curve; story-facing difficulty
  remains the writer's problem.
- SIS is not, by itself, evidence of patentability.
