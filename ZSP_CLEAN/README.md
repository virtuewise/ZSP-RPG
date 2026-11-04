# ZSP_CLEAN — Zimbabwe: The Shadow Protocol (fresh project)

This folder is a **clean project for *Zimbabwe: The Shadow Protocol***, built from the
reusable, **generic, licensed** parts of the decompiled APK and then stripped of everything
that brands it "Commando Army — War Action".

**Engine:** Unity 6000.6.8f1 (Unity 6, 3 November 2026 release). If you have an older Editor, install 6000.6.8f1 with Android Build Support before opening the project.
**Platform target:** Android (iOS later).
**Asset policy:** no Commando branding, SDK splash, ad creative, or Unity splash screen ships. See `Docs/ASSET_REPLACEMENT.md`.

## What is here

| Area | Contents |
|---|---|
| `Assets/Art/` | Original emblem, logo, splash, app icon, crosshair, HUD bars, nightjar weave |
| `Assets/Extracted/` | Generic meshes, textures, materials, animation clips, audio, scene manifests |
| `Assets/Scripts/Player/` | Third-person controller, weapons, stealth, melee, stealth, interrogation, progression, health |
| `Assets/Scripts/Narrative/` | MissionChain (legacy AMC), SIS StoryContent loader, ShadowIntelligenceSystem, MissionDirector |
| `Assets/Scripts/UI/` | HUDController, DialogueUI, SISPanel |
| `Assets/Scripts/Core/` | Bundle loader, bootstrap |
| `Assets/Scripts/Editor/` | One-click scene/rig/SIS-demo builders (`ZSP` menu) |
| `Assets/StreamingAssets/Content/` | Season 1 data: characters, factions, contacts, locations, intel, witnesses, M1–M3 |
| `Prototype/` | Playable Python SIS slice + tests |
| `Docs/` | Design, story bible, prior art, vertical slice, roadmap, asset replacement |

## Counts (after branding quarantine)

- Meshes: **333** (generic; will be replaced with licensed models)
- Textures: **652** (from 695; 43 branded/SDK/watermarked removed)
- Material dumps: **271** (from 282; 11 p3dm.ru removed)
- Animation clips: **186** · Animator controllers: **49**
- FSB audio clips: **226** (need vgmstream/FMOD conversion or runtime loading)
- Scene manifests: **7** (Mirage ×3, Mountain ×2, tutorial, SDK splash excluded)
- `ASSET_INDEX.csv`: **1,570** rows

## First run

1. Install Unity 6000.6.8f1 with Android Build Support.
2. Open `ZSP_CLEAN` in Unity; accept the project upgrade prompt.
3. **ZSP → Create Bootstrap Scene** (writes `Assets/Scenes/Bootstrap.unity`).
4. **ZSP → Create Player Rig** (writes `Assets/Player/Player.prefab`).
5. **ZSP → Create SIS Demo Scene** (optional; plays the three-mission slice in the console).
6. Press Play; the bootstrap streams `level3` (Mirage) or `level7` (Mountain).

## Play the SIS prototype today (no Unity required)

```powershell
cd Prototype
python sis_game.py --demo
python sis_game.py --interactive
python -m pytest test_sis_v1.py -q   # 14 passed
```

## Story and tech

- `../ZSP_SEASON_01_STORY_BIBLE.md` — **official Season One bible** (Tinashe Mukasa, 12 episodes, The Black Ledger). The interim `Docs/STORY_BIBLE.md` is superseded.
- `../ZSP_SEASON_01_STORY_BIBLE.md` defines `SHADOW INTELLIGENCE — Adaptive Enemy Doctrine` as the player-facing name of the SIS, and **The Operative Identity System (OIS)** as the player-facing identity layer.
- OIS is implemented: `Prototype/ois.py` (Python) and `Assets/Scripts/Narrative/OperativeIdentity.cs` + `Assets/Scripts/UI/DossierUI.cs` (Unity; callsign ``SHADOW-07`` default, display name ``Tinashe Mukasa``, optional nickname; no VO of custom strings; dossier persists with the save).
- `Docs/SHADOW_INTELLIGENCE_SYSTEM.md` — SIS v1 specification (deterministic, auditable, capped).
- `Docs/PRIOR_ART.md` — Nemesis patent (US10926179B2), AI Director, DDA, storylets; risk register.
- `Docs/VERTICAL_SLICE.md` — the five-milestone build order and acceptance criteria.
- `Docs/ASSET_REPLACEMENT.md` — what was quarantined and what is original.

## Licence posture

The original APK belongs to its original publisher. Extracted assets are used as internal
reference only and will not ship. All brand art is original. The fiction is original. No one's
identity is depicted.
