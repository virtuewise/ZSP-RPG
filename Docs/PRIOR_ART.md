# Shadow Intelligence System — Prior Art Review

**Project C: ZIMBABWE — THE SHADOW PROTOCOL** · 4 November 2026

Purpose: establish the existing landscape before making any novelty claim. This is a
technical review, **not a legal patentability or freedom-to-operate opinion**. A formal
patent search and counsel review are required before filing or before public disclosure of
the SIS as proprietary technology.

---

## 1. The closest prior art

### 1.1 US Patent 10,926,179 B2 — "Nemesis characters, nemesis forts, social vendettas and followers in computer games"

- Assignee: Warner Bros. Entertainment; filed 26 March 2015; granted 23 March 2021; active.
- Estimated expiry: ~2036 (subject to maintenance fees).
- Covers, in substance, computer-game systems in which a *persistent enemy hierarchy* is
  generated, ranked and tracked, in which an enemy remembers a player's past encounters,
  takes revenge, gains power, and gains followers/forts over time.
- **Relevance:** very high. This is the canonical "the game remembers what you did" system.
- **Where it differs from SIS:**
  - Nemesis models a *social hierarchy of enemies*. It is about who the enemy is, not
    what the story requires next.
  - SIS models *mission condition synthesis* from an auditable, replayable consequence
    ledger. It does not rank enemies, does not grant enemies revenge arcs, does not
    create nemesis characters, and does not mutate forts.
  - SIS treats enemy adaptation as a small, capped *budget* applied to authored mission
    conditions (entry security, detection, armour, reinforcements), never to the
    narrative topology.
- **Risk:** any SIS feature that generates a named enemy who (a) remembers the player,
  (b) is promoted/demoted through a hierarchy, and (c) takes revenge through forts or
  followers would enter this claim space. The current design must continue to avoid it.
- **Action:** SIS will not ship a "Dark Lord promotion ladder". Witness persistence is
  limited to testifying/dead/captive/informant states directly required by the authored
  story; it does not rank enemies by vendetta.

### 1.2 Left 4 Dead — "AI Director" (Valve, 2008)

- A real-time pacing system that reads the survivors' state and the population, then
  modulates when and where enemies spawn so the game follows a dramatic arc
  (build-up, peak, release).
- **Relevance:** high for *adaptation*, low for *persistence*.
- **Where it differs:** the Director optimises second-to-second intensity inside one
  session. It does not persist across episodes, does not reconcile a ledger, does not
  bind mission identity to a cryptographic behaviour digest, and does not maintain a
  trust/exposure graph over named contacts.
- **Carry-over rule:** SIS uses the same instinct (read the player, adjust pressure) but
  applies it between missions, under a budget, with replayable consequences.

### 1.3 RimWorld — "Storytellers" (Ludeon Studios, 2013)

- Storytellers (Cassandra, Phoebe, Randy) select events/incidents from a deck, weighted
  by a threat budget and an escalation curve.
- **Relevance:** high for *budgeting adaptation pressure* and for *event selection*.
- **Where it differs:** RimWorld generates a colony simulation, not a narrative. Events
  are incidents, not mission conditions for an authored cinematic arc. SIS does not
  generate events; it resolves *authored* mission skeletons against persisted
  consequences.

### 1.4 Dynamic Difficulty Adjustment / DDA research

- Well-documented adaptive-difficulty systems adjust health, damage, spawn rates, aim
  assist and puzzle complexity based on player performance. Broadly used in commercial
  games and widely described in academic literature (e.g. Yannakakis & Togelius,
  *Experience-Driven Procedural Content Generation*, 2018; Togelius et al., *Procedural
  Content Generation in Games: A Survey*, 2011).
- **Relevance:** foundational for "adaptation budget".
- **Where it differs:** DDA is not a narrative coherence system. It has no requirement
  that a decision in mission 3 still constrain mission 47, no contradiction detection,
  no witness persistence, no mission identity derived from player behaviour.
- **Carry-over rule:** SIS may borrow the DDA principle of a capped adaptation budget,
  but applies it only to conditions, never to plot.

### 1.5 Storylets / quality-based narrative (Fallen London; Sunless Sea; Failbetter Games)

- A storylet is a content node gated by qualities (stats, story flags). The narrative
  advances by selecting the highest-quality node whose gates pass.
- **Relevance:** high for *longform adaptive narrative content structure*.
- **Where it differs:** quality-gating is still flag-based. It has no behaviour-indexed
  digest, no hash-chained ledger, no replay reconciliation, no expiry/budget, and no
  requirement that two players with identical behaviour receive identical missions.
- **Carry-over rule:** SIS missions are authored storylet containers; the SIS sits
  beneath them and guarantees consistency, not the other way round.

### 1.6 Classic branching / consequence games

- Mass Effect (save import and choice-passthrough), The Witcher 3 (quest outcome flags),
  Dragon Age, Detroit: Become Human, Cyberpunk 2077 (job availability by world state).
- **Relevance:** these prove the market for persistent decisions; none provides
  determinism, auditability, or behaviour-indexed mission identity.
- **Where it differs:** SIS's defining properties are (i) identical behaviour ⇒ identical
  future missions, (ii) any divergence ⇒ deterministic cascade, (iii) the player can audit
  why a change happened, (iv) contradictions are detected by replay rather than papered
  over.

### 1.7 Emergent simulation games

- Dwarf Fortress, RimWorld, Crusader Kings III, Kenshi.
- **Relevance:** demonstrates emergent narrative from simulation.
- **Where it differs:** these systems are not auditable deterministic ledgers; they are
  stochastic sandboxes. SIS intentionally keeps a bounded, authored story and uses
  consequences to modulate it.

### 1.8 Other notable adaptive narrative research

- The Interactive & Narrative Intelligence / CAVA line of work (guided narrative
  generation, drama management), Façade (2005) and its beat-system successors, and
  drama-manager research from the Microsoft Research / Georgia Tech tradition.
- **General finding:** drama management chooses which beat happens next. SIS does not
  choose the beat; it chooses the *conditions under which the authored beat happens*.

---

## 2. SIS in one sentence

> SIS is a **deterministic, auditable consequence engine** that binds mission identity to
> an append-only, hash-chained ledger of player behaviour, reconciles narrative state by
> replay, budgets enemy adaptation so it never consumes the plot, and persists witnesses,
> contacts and key characters across an authored story — specifically so that a decision in
> mission 3 still constrains mission 47 without state explosion.

What is **not** claimed as novel: dynamic difficulty, AI-director pacing, branching
narratives, storylets, enemy vendettas, colony storytellers.

What is claimed as novel **in combination and in mechanism**:

1. **Behaviour-Indexed Consequence Ledger (BICL).** Mission identity and the RNG seed are
   derived from a hash of the *whole* behaviour log, not from story flags.
2. **Deterministic Replay Reconciliation.** State is never the source of truth; it is
   rederived. Contradictions are detectable and reportable.
3. **Exposure-coupled Contact Graph.** Repeated reliance on a contact raises exposure; a
   fully-exposed contact becomes a mission condition (capture, rescue, protection).
4. **Witness/Key-Character Persistence Registry.** Spared, detained, captive, dead,
   cooperative, liability: these states are immutable or explicitly monotonic in the
   ledger.
5. **Capped Adaptation Budget with Non-Removable Narrative Invariants.** Adaptation may
   only change conditions (security, detection, armour, reinforcements). It may never
   remove a required confrontation, a required reveal, or an available ending.

---

## 3. Patent-risk register

| Feature | Prior-art overlap | Risk | Required control |
|---|---|---|---|
| Enemy "remembers" the player | Nemesis (US10926179B2) | Medium | Memory is expressed as mission conditions only; never as enemy rank/vendetta/fort capture. |
| Enemy leveling / promotion | Nemesis | Medium | Enemy adaptation is a 0–10 per-mission budget, capped per mission; no hierarchy. |
| Adaptive spawn pacing | AI Director; DDA | Low | Already used in industry; SIS applies it between missions, not real-time. |
| Persistent save import | Mass Effect; The Witcher 3 | None | Standard practice. |
| Auditable replay | Audit-log literature; blockchain metaphor | Low | No claim of novelty over hash chains; novelty is the *application* to narrative coherence. |
| Storylet content | Fallen London | Low | SIS is a runtime layer, not a new content format. |
| Contact exposure | Ally-betrayal in narrative games | Low | Mechanism is exposure = trust × centrality, thresholded per contact. |
| Key-character deletion | Shadow of Mordor (killing captains) | Medium | Dead key characters produce explicitly authored fallback paths; no procedural promotion. |

## 4. Required design constraints

1. No "nemesis tier", "vendetta hierarchy", "fort capture" or "enemy promotion" mechanics.
2. No real-time enemy spawn director that replaces the authored patrol layout.
3. Adaptation budget never exceeds 10 points per mission and never edits narrative slots.
4. Narrative invariants are validated at load and at mission resolution; violating them is
   a build failure, not a runtime warning.
5. Every SIS effect carries: mission id, behaviour digest, ledger entry, and a human-readable
   audit line ("Nightjar reinforced the front gate because you used it four times").
6. Player-facing text must not mention "SIS"; it mentions consequences and shadows.

## 5. What this does **not** decide

- Whether a filing is worthwhile.
- Whether the claims would survive examination.
- Whether the game infringes any active patent in any territory.
- Whether Nemesis-style mechanics, used anywhere, would infringe.

**Recommendation:** treat SIS as a strong, differentiated engineering approach with clear
commercial value; obtain a formal novelty/FTO opinion before external disclosure; do not
publish a full specification publicly until that opinion is in hand. The public-facing name
"Shadow Intelligence System" can ship regardless.

## 6. Sources consulted (public)

- US10926179B2, *Nemesis characters, nemesis forts, social vendettas and followers in computer games*, Warner Bros. Entertainment, filed 2015-03-26, granted 2021-03-23.
- Left 4 Dead — AI Director, Valve Corporation (design overview via public documentation and the Left 4 Dead article).
- Dynamic game difficulty balancing — Wikipedia overview.
- Procedural generation — Wikipedia overview.
- Emergent gameplay — Wikipedia overview.
- Artificial intelligence in video games — Wikipedia overview.
- Quest (video games) — Wikipedia overview.
- Fallen London / Sunless Sea — Failbetter Games' public description of storylets and qualities.
- Togelius et al., *Procedural Content Generation in Games: A Survey*, IEEE T-GAMIG 2011.
- Yannakakis & Togelius, *Experience-Driven Procedural Content Generation*, TKDE 2018.
- Left 4 Dead — Valve, 2008. Middle-earth: Shadow of Mordor — Monolith Productions.
- Mass Effect — BioWare; The Witcher 3 — CD Projekt Red; Detroit: Become Human — Quantic Dream.
