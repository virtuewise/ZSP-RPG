# ZSP Roadmap — the five milestones

**ZIMBABWE: THE SHADOW PROTOCOL** · 4 November 2026

Canon: **`ZSP_SEASON_01_STORY_BIBLE.md` at the repo root.** Milestone 4 is **Episode 01 — THE MESSAGE**; the slice now ends on the Season One interlude after Episode 03.

Engine: **Unity 6000.6.8f1** (Unity 6). Android first, iOS later.

## Milestone 1 — Make the project playable
- Unity 6000.6.8f1 + Android Build Support installed locally.
- `ZSP → Create Bootstrap Scene` → streams `level3` (Mirage) / `level7` (Mountain).
- `ZSP → Create Player Rig` → third-person control, orbit camera, hitscan weapons.
- **Done (by code):** controller, camera, weapons, stealth, HUD scripts, bundle loader; generated player health/tag; vertical-slice EventSystem and identity registrar; enemy health, player targeting and line-of-sight filtering.
- **Done:** Unity 6000.6.8f1 is installed and the project opens in the editor.
- **Pending:** regenerate the checked-in Player prefab and Vertical Slice scene from the updated menu tooling; run Bootstrap/SIS Demo/Vertical Slice; verify scene and bundle streaming, gameplay systems, and 30 fps on device.

## Milestone 2 — Character creation
- Identify a licensed humanoid from the bundle; keep the rig, replace the face.
- Clothing/equipment/loadout customisation (no character modelling by the player).
- Connect aim, reload, cover, vault, takedown, death/ragdoll animations.
- **Pending:** licensed character acquisition; animation-event reassembly in Unity.

## Milestone 3 — First Harare environment
- Avondale flat + Mbare terminus (M1) ← Mirage airport/storage kit.
- Chitungwiza township (M2) ← town kit; back to Avondale (M3).
- Original textures only; branded/SDK/watermarked assets quarantined.
- Audio: weapon/footstep/ambience/VO pass.
- **Pending:** dressing pass, lighting, weather, LODs, Android profile.

## Milestone 4 — Mission 001
- "Episode 01 — THE MESSAGE" end-to-end: cinematic, briefing, Avondale approach, investigation, Lovemore encounter, exfiltration.
- Dialogue, investigation, first combat encounter, mission conclusion.
- Save/load mid-mission; deterministic replay.
- **Done (content):** Episode 01–03 fully authored; SIS tests re-keyed; OIS registrar implemented in Unity (`UI/IdentityRegistrar.cs`).
- **Pending:** wire identity registration and mission flows into the actual Unity scenes; complete in-editor runtime and Android acceptance. The Unity 6 project-open check is complete, but this is not a gameplay or build verification.

## Milestone 5 — Shadow Intelligence System
- SIS driving enemy profile, entry security, witness/key-character persistence, contact exposure, dialogue variants and ending availability.
- Deterministic replay; contradictions detected, never shown to the player.
- **Done (by reference):** Python SIS slice and 23-test full suite; Unity C# port written with beat/choice/branch transition validation.
- **Pending:** wire SIS to Unity spawn points, verify Android build.

## Delivered so far
- Clean project stripped of Commando branding/SDK/ads (no Commando assets ship).
- Original brand kit: emblem, logo, splash, icon, crosshair, HUD bars.
- Season 1 story data (canon): Tinashe, Amara, Muponda, Tafadzwa, Lovemore; Meridian / Directorate / The Ledger / Shadow Network; Avondale, Mbare terminus, Chitungwiza; Episodes 01–03.
- Python SIS prototype: deterministic replay, contact exposure, witness persistence, adaptation budget, OIS; 23 tests pass in CI.
- Static project validator: 9 mirrored JSON files, six missions, and mission/beat/choice/branch/next-mission references validated in CI.
- Docs: story bible, prior art (Nemesis patent/AI Director/DDA/storylets), vertical slice, **EP01 development spec**, **OPERATIVE_IDENTITY_SYSTEM**.

## Explicitly later
- Seasons 2+, live ops, co-op, purchases, leaderboards, analytics/IAP (never without consent).
- Formal patent counsel re: SIS.

## Verification commands

```powershell
cd Prototype
python -m pytest test_sis_v1.py -q   # 22 passed
python sis_game.py --demo            # three-mission slice
```
