# Zimbabwe: The Shadow Protocol

A cinematic, mobile-first third-person espionage action RPG built on Unity 6.

**Engine:** Unity 6000.6.8f1 (Android first; iOS later). Install 6000.6.8f1 with Android Build Support before opening the project.

**This repository is the game.** The project root is `Assets/`, `Packages/`, `ProjectSettings/`.

```
.
├── ZSP_SEASON_01_STORY_BIBLE.md   ← canonical Season One story (THE BLACK LEDGER)
├── Docs/                          ← SIS design, prior art, vertical slice, roadmap, EP01 spec, OIS, current docs
├── Prototype/                     ← Python SIS prototype (playable, tested)
├── Assets/                        ← Original art, content, scripts, resources
│   ├── Art/                       ← Original emblem, logo, splash, icon, HUD bars
│   ├── Resources/Content/         ← Season content (readable by Unity and Python)
│   ├── StreamingAssets/Content/   ← Same content, StreamingAssets copy
│   └── Scripts/                   ← Player, enemy AI, Narrative/SIS, UI
├── Packages/manifest.json         ← Minimal, ad/billing/analytics-free
├── ProjectSettings/               ← 6000.6.8f1
└── tools/                         ← Original art generators
```

**Not in this repository (ever):** anything derived from the decompiled "Commando Army" APK — extracted meshes, textures, audio, materials, SDK splash screens, ad creatives, or Unity splash art. Those remain on the local machine only and are excluded by `.gitignore`.

## Run the SIS prototype today (no Unity required)

```powershell
cd Prototype
python sis_game.py --demo
python sis_game.py --interactive
python -m pip install -r ../requirements-dev.txt
python -m pytest -q   # complete prototype suite
```

## First run in the editor

1. Open `.` in Unity 6000.6.8f1 with Android Build Support.
2. **ZSP → Create Bootstrap Scene**, **ZSP → Create Player Rig**, optionally **ZSP → Create SIS Demo Scene**.
3. Press Play. The bootstrap streams the Mirage or Mountain bundle (local only — `zsp_assets.unity3d` is not in this repo).

## The two systems that make the game

- **SHADOW INTELLIGENCE — Adaptive Enemy Doctrine** (`Docs/SHADOW_INTELLIGENCE_SYSTEM.md`, `Assets/Scripts/Narrative/ShadowIntelligenceSystem.cs`, `Prototype/sis_v1.py`): the game remembers how you enter, fight, trust and spare; it reconciles the story by replay and stays inside a fixed adaptation budget.
- **Operative Identity System (OIS)** (`Docs/../ZSP_SEASON_01_STORY_BIBLE.md` §14, `Prototype/ois.py`, `Assets/Scripts/Narrative/OperativeIdentity.cs`): fixed protagonist Tinashe Mukasa; the player chooses display name, callsign and optional nickname; dossier updates from SIS.

## Read before writing

- `ZSP_SEASON_01_STORY_BIBLE.md` — canon.
- `Docs/PRIOR_ART.md` — Nemesis patent, AI Director, DDA, storylets; no novelty claim without counsel.
- `Docs/SHADOW_INTELLIGENCE_SYSTEM.md` — SIS v1 specification (deterministic, auditable, capped).
- `Docs/EP01_THE_MESSAGE.md` — Mission 001 development specification.
- `Docs/OPERATIVE_IDENTITY_SYSTEM.md` — OIS (callsign/display name/dossier) design.
- `Docs/CODEQL_ANALYSIS.md` — CodeQL C# baseline (build-mode `none` retained; controlled improvement plan; Unity 6 build prerequisites).
- `Docs/RELEASE_READINESS.md` — verified CI evidence and remaining Unity/Android/release acceptance gates (see [issue #3](https://github.com/virtuewise/ZSP-RPG/issues/3)).
- `Docs/VERTICAL_SLICE.md` — five-milestone build order and acceptance criteria.
- `Docs/ASSET_REPLACEMENT.md` — what was quarantined and what is original.

## Scenes and prefabs

The repo includes the Unity-generated scenes (`Assets/Scenes/Bootstrap.unity`, `SISDemo.unity`, `VerticalSlice.unity`), the player rig (`Assets/Prefabs/Player.prefab`) and their `.meta` files, generated deterministically by the `ZSP` menu with Unity 6000.6.8f1. If a fresh clone ever disagrees with the meta GUIDs, regenerate them via `ZSP → Create Bootstrap Scene / Player Rig / SIS Demo Scene / Vertical Slice Scene` and re-commit.

## Licence posture

Original code, art, and fiction are ours. The APK's ownership remains with its original publisher. Nothing from it ships here.
