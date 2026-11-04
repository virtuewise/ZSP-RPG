> **SUPERSEDED (4 Nov 2026):** The milestone plan still stands, but the story content is superseded by `ZSP_SEASON_01_STORY_BIBLE.md` at the workspace root. Milestone 4 (Mission 001) will be **Episode 01: The Message**, not "The Courier's Warning". The SIS engine and tests are unaffected and will be re-pointed at the new episodes.

# Vertical Slice — Acceptance, Build Order and Status

**ZIMBABWE: THE SHADOW PROTOCOL** · Season 1

This document defines what "first playable slice" means and tracks what is built.
It replaces the old "Milestones 0–2" plan in ROADMAP.md.

## The five milestones (owner order)

### Milestone 1 — Make the project playable
- Unity 6000.6.8f1 (installed via Unity Hub 3+).
- `ZSP → Create Bootstrap Scene` → `Bootstrap.unity`.
- `ZSP → Create Player Rig` → `Player.prefab`.
- `GameBootstrap` loads `StreamingAssets/zsp_assets.unity3d` and streams `level3` (Mirage) or `level7` (Mountain).
- Player can walk/run/jump/crouch/aim/shoot; orbit camera works; HUD shows ammo/detection/objective.
- **Status:** scripts written; bundle loaded; not yet executed in-editor. Unity 6000.6.8f1 not installed on this machine.
- **Acceptance:** 30 seconds of uninterrupted third-person control on Android and Windows Editor; no console errors; save/load works.

### Milestone 2 — Character creation
- Select a suitable rigged humanoid from the bundle (`Final Men`, `EnemyBadNew3/4`, `Character001`) as the reference; **no character modelling by the player** — clothing, weapons, capability only.
- Swap the temporary extraction for an original-rig or license-cleaned CGTrader character (see Art Direction).
- Connect at minimum: locomotion, aim, reload, fire, melee, crouch, death/ragdoll, vault.
- **Acceptance:** Kuda walks, aims and fires with body/arms layered correctly; hair/clothing/equipment customisation hooks exist; no Commando-branded skins or textures.

### Milestone 3 — First Harare environment
- Convert the Mirage airport kit → **Msasa Freight Yard** (M1).
- Convert the Mirage town kit → **Eastgate / Samora Machel Avenue** (M2).
- Mountain kit → **Kaguvi Hill estate** (M3).
- Replace all branded textures with original ZSP set (`Assets/Art/`).
- Audio: convert FSB banks or use direct bundle playback; at minimum rifle, footsteps, ambient crowd, door, radio chatter.
- **Acceptance:** three playable blockouts with fictional Harare signage, original textures, no Commando/branded assets; 60 minutes of playable ambient loop at 30 fps on a mid-tier Android device.

### Milestone 4 — Mission 001
- **"The Courier's Warning"** — opening cinematic (helicopter/radio), Nyasha briefing, Msasa approach choice, investigation, Tendai encounter, archive resolution, exfiltration, debrief.
- **Acceptance:** complete playthrough; choices alter M2 conditions; save/load mid-mission rehydrates correctly; 45–75 minutes of authored content; zero developer menus visible to the player.

### Milestone 5 — Shadow Intelligence System
- SIS integrated into the mission director, enemy spawning, HUD and save system.
- **Acceptance:** given the same choices twice, the second playthrough is bit-identical; given different entry styles (front door vs service roof), M2/M3 enemy profiles differ visibly; a spared witness reappears; an exposed contact is rescued/traded for; key-character deletion produces stated fallbacks; contradictions are never shown to the player as a crash.

---

## What is already built (as of 2026-11-04)

### Python reference slice (playable now)
`Prototype/sis_game.py` — data-driven, deterministic, three-mission playthrough.

```powershell
cd Prototype
python sis_game.py --demo          # deterministic demo run
python sis_game.py --interactive   # play it
python -m pytest test_sis_v1.py -q # 14 tests
python -m pytest test_amc.py -q    # 3 AMC tests
```

Includes: content loader, BICL, replay reconciliation, contact exposure graph, witness/key-character registry, adaptation budget, three authored missions with seized endings, save/load.

### Unity C# scaffolding
- `MissionChain.cs`, `MissionChainDemo.cs` — legacy AMC prototype.
- `StoryContent.cs`, `ShadowIntelligenceSystem.cs`, `SISSaveSystem.cs`, `ShadowIntelligenceDemo.cs` — SIS port (see below).
- `MissionDirector.cs`, `EnemySpawner.cs`, `EnemyAI.cs`, `PlayerHealth.cs` — integration.
- `ZSP/Create Bootstrap Scene`, `ZSP/Create Player Rig`, `ZSP/Create SIS Demo Scene` — one-click scaffolding.

### Content and art
- `Assets/StreamingAssets/Content/` — season 1 data: characters, factions, contacts, locations (6), intel (11), witnesses (3), M1–M3 with full beats.
- `Assets/Art/` — original emblem, logo, splash, app icon, crosshair, HUD bars, nightjar weave texture.
- `_Quarantine/CommandoBranding/` (54 files) and `_Quarantine/Ads/` (20 files) — none ship.

### Docs
PRIOR_ART.md · SHADOW_INTELLIGENCE_SYSTEM.md · STORY_BIBLE.md · ASSET_REPLACEMENT.md · ROADMAP.md

---

## Acceptance criteria for the full slice (M1–M5)

1. One operative, one Harare section, one intelligence HQ, three supporting characters, one enemy organisation.
2. Three connected missions; shooting, walking, running, stealth, melee, interrogation, vehicle.
3. Dialogue with investigation; clues unlock follow-ups; witnesses persist.
4. Save/load mid-mission and between missions; deterministic replay.
5. At least one SIS consequence visible in every playthrough (entry pattern, witness, contact, faction heat).
6. No Commando/SDK/Unity-splash assets anywhere in `Assets/`.
7. Android build exports within 2 GB; 30 fps on a 2022-era mid-range phone.
8. Written fiction disclaimer shown.

## Not in the slice (deliberately)

- Full customisation economy, seasons, co-op, ranked progression, live ops, purchases.
- Character modelling by the player.
- Any depiction of a real Zimbabwean institution or individual.
- Any claim of patent novelty without counsel.

## Known blockers

- Unity 6000.6.8f1 is not installed on this machine; no project has been opened.
- No Zimbabwe-specific 3D environments exist yet; Mirage/Mountain are generic.
- FSB audio needs conversion (vgmstream/FMOD) or runtime loading.
- The original humanoid rigs are generic stock characters; the player body must be licensed or replaced.
- Dialogue VO is absent; the slice is text + barks + radio static.
