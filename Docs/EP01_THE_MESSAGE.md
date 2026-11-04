# Mission 001 — EPISODE 01: THE MESSAGE · Development Specification

**Slice:** Episode 01 · **Playtime:** 8–12 min (full 45–75 min episode later) · **Engine:** Unity 6000.6.8f1

> Faithful continuation of `ZSP_SEASON_01_STORY_BIBLE.md`. Nothing here depicts real people or institutions.

## Premise
Emmanuel Mhara vanished with the Black Ledger. Six words on a dead phone: *"They have activated the protocol. Run."* Tinashe Mukasa wakes to a Meridian sweep of Avondale and a locker that is no longer empty.

## Cast
- **Tinashe Mukasa** (player) — former elite Directorate operative; Silent Gate veteran; disgraced.
- **Amara Chikowore** — Directorate analyst; Tinashe's former handler/contact; hiding what she saw in Silent Gate.
- **Emmanuel Mhara** — missing deputy director; the sender; dead or worse.
- **Lovemore Chigumba** — terminus porter/witness.
- **Victor Nhari** — present only as a silhouette in terminus CCTV; unnamed in mission text.
- **Meridian Strategic Group** — enemy faction; two "cleaners" in the garage.

## Level geography
- **Avondale safehouse** (flat above a garage) — opening.
- **Mbare bus terminus / market** — flashpoint.
- **Avondale** — exfiltration to M2.

## Beat map (implemented)
| Beat | Type | Purpose |
|---|---|---|
| M1_BRIEF | dialogue | Amara radio briefing; mission frame; `{OIS_CALLSIGN}` voiced |
| M1_AMARA_ANSWERS | dialogue | Optional; confirms Ama knows more than she says |
| M1_APPROACH | route | Avondale front_door vs back_alley; SIS entry conditions apply |
| M1_SWEEP | combat | Two Meridian cleaners in the garage; evade or fight |
| M1_LOVEMORE | choice | 03:40 terminus; stop the cleaner / observe / walk away; witness fate |
| M1_CLOCK | dialogue | Amara's deadline; SIS contacts surface; moving to Chitungwiza |
| M1_END | mission_end | Transition to Episode 02 |

## SIS wiring
- `approach` events (`front_door`=1, `back_alley`=2) feed `entry_counts`.
- `witness_spared`/`witness_killed` Lovemore feeds M2 `informant`/`liability` branch.
- `trust_contact` Amara events feed her exposure; she may be compromised before Episode 02.
- Meridian `heat` rises with loud approaches; it reinforces corridors in M2.

## OIS wiring
- `{OIS_CALLSIGN}` in Amara's briefing and M3/interference lines.
- Dossier: episodes completed becomes 1/12 after commit; `STYLE`, reputation and witnesses update.
- Default: **Tinashe Mukasa / SHADOW-07**.

## Acceptance criteria (Mission 001, Slice)
1. A player can run the whole episode without developer input.
2. Choices produce visible differences in M2 (Lovemore informant vs dead or silent; Amara trusted vs exposed).
3. Same choices twice → bit-identical replay digest.
4. Any path, any skill, reaches M1_END.
5. No commando/branded screens visible.
6. Save/load mid-mission restores SIS and identity.

## Cinematic notes (for production pass)
- Opening: rain on a truck windscreen; a radio crackle; a checkpoint that shouldn't exist.
- M1_SWEEP: no music until the garage clears; then a single piano note.
- M1_LOVEMORE: one long take; the porter does not speak until the cleaner folds.
- Transition: cut to black on the word "run." Hold. Fade to Episode 02 title.
