# Cleanup Report — ZIMBABWE: THE SHADOW PROTOCOL

## Decision
Keep the **reusable Unity assets** and throw away the **mobile FPS shell**
(ads, Firebase, analytics, billing, Android/Java/Kotlin glue, old IL2CPP code).

The original APK folder is untouched. Two clean outputs were produced:

1. **`ZSP_CLEAN/`** — a fresh Unity 6000.6.8f1 project (Unity 6) with the assets,
   the original asset bundle, new gameplay scripts, original brand art and the Season 1 content database.
2. **`ZSP_CLEAN/StrippedSource/`** — the absolute minimum asset source:
   `assets/bin/Data/data.unity3d` only, plus `REMOVED_ITEMS.txt`.

## Kept (inside ZSP_CLEAN/Assets)
| Folder | Contents |
|---|---|
| `StreamingAssets/zsp_assets.unity3d` | Original 44 MB Unity bundle: scenes, prefabs, rigs, animations, audio, materials |
| `Extracted/Meshes` | 333 `.obj` meshes (environments, weapons, vehicles, characters) |
| `Extracted/Textures` | 652 usable `.png` textures (43 branded/SDK/watermarked files quarantined outside Assets; 20 ad creatives already quarantined) |
| `Extracted/Materials` | 271 material/shader property dumps (11 p3dm.ru dumps quarantined) |
| `Extracted/Animations` | 186 animation clip metadata dumps |
| `Extracted/Controllers` | 49 animator controller dumps |
| `Extracted/Shaders` | 77 shader dumps as `.shader.txt` (reference only; not valid ShaderLab) |
| `Extracted/Audio` | 226 `.fsb` audio banks |
| `Extracted/Scenes` | 7 scene hierarchy manifests (SDK splash scene excluded) |
| `Scripts` | New controllers, camera, weapons, stealth, vehicles, mission chain, HUD |

Counts: 333 meshes, 652 usable textures, 271 materials, 186 animation clips,
49 animator controllers, 77 shader reference dumps, 226 audio clips, 7 scenes.

**Original brand art** now lives in `Assets/Art/` (emblem, logo, splash, app icon,
crosshair, HUD bars, nightjar weave) — generated from scratch, no extracted input.
See `Docs/ASSET_REPLACEMENT.md`.

Extraction gaps: 5 textures could not be decoded by UnityPy, and 10 of the 87
shaders failed to parse. Everything else from the bundle was exported.

## Removed / excluded
- **Ads:** AppLovin, Unity Ads, Pangle/ByteDance, InMobi, Mintegral, Liftoff,
  Gadsme, `audience_network.dex`, ad-viewer assets.
- **Ad creative textures (20):** other-game banners, "ADVERTISEMENT HERE",
  "AD_FILLER", "Ad_Tag", "No_Ads" IAP art — moved to
  `_Quarantine/Ads/`, outside the Unity project, not part of the usable library.
- **Firebase:** App, Analytics, Crashlytics, Messaging, RemoteConfig,
  `libFirebase*.so`, `libcrashlytics*.so`.
- **Analytics/tracking:** SolarEngine, APM insight, datastore, buffer.
- **Billing:** Google Play Billing, `billing.properties`.
- **Old Android shell:** `classes*.dex`, `AndroidManifest.xml`, `resources.arsc`,
  `META-INF/`, `res/`, `okhttp3/`, `org/`, `kotlin/`, `google/`.
- **Old runtime:** `libil2cpp.so`, `libunity.so`, `libmain.so`, `global-metadata.dat`.
- **SDK config:** all `.properties`, `.proto`, `builddatas.json`,
  `google-services-desktop.json`, `UnityServicesProjectConfiguration.json`.

Full machine-readable list: `StrippedSource/REMOVED_ITEMS.txt`.

## Branding and licensing pass — 2026-11-04

54 additional files were removed from `Assets/Extracted/` and moved to
`_Quarantine/CommandoBranding/` (index: `QUARANTINE_INDEX.csv`):

- All `p3dm.ru_*` textures and material property dumps (third-party, watermarked, licence unclear).
- All `CF-Button-*`, `CF-gamepad-icon` (branded UI kit).
- `SpiritApps-Splash_[1].png`, `M_Logo.png`, `Modren_Commando.png` (franchise branding).
- `Splash_Screen_Unity_Logo.png`, `splashTexture_*` (Unity splash screens).
- `AD_ICON_[1].png`, `ad_[3].png`, `screen_3840x2160_*` (missed ad creatives/store screenshots).

`ASSET_INDEX.csv` regenerated to 1,570 usable rows. `Assets/` now contains
no Commando, Spirit Apps, SDK-splash, p3dm.ru or ad assets. The original bundle is
retained only as internal reference and will not ship.

## New gameplay foundation
`Assets/Scripts` contains:
- `Core/AssetBundleLoader.cs` — loads the clean asset bundle (desktop + Android).
- `Core/GameBootstrap.cs` — boot sequence and optional environment streaming.
- `Player/ThirdPersonController.cs` — walk/sprint/crouch/jump/stealth noise.
- `Player/ThirdPersonCamera.cs` — orbit, collision, aim zoom.
- `Player/WeaponSystem.cs` — hitscan, spread, recoil, reload, weapon switching.
- `Player/StealthSystem.cs` + `EnemySensor` — detection model.
- `Player/MeleeSystem.cs` — short-range strike / stealth takedown.
- `Player/InterrogationSystem.cs` — raycast interrogation + trust.
- `Player/ProgressionSystem.cs` — credits/XP and damage/armor/stealth/reload upgrades.
- `Player/EnemyHealth.cs` — damage receiver for enemies and destructible props.
- `Vehicles/VehicleController.cs` — WheelCollider jeep/truck driving.
- `Narrative/MissionChain.cs` — legacy C# port of the Adaptive Mission Chain.
- `Narrative/StoryContent.cs` — loads `StreamingAssets/Content/*.json` (Season 1 data).
- `Narrative/ShadowIntelligenceSystem.cs` — SIS port (BICL, replay, contact exposure, adaptation budget).
- `Narrative/SISSaveSystem.cs` — JSON save/load in `Application.persistentDataPath`.
- `Narrative/MissionDirector.cs` — resolves missions, applies SIS conditions to HUD/stealth, raises enemy spawns.
- `Narrative/ShadowIntelligenceDemo.cs` — Play-Mode console demo of the three-mission slice.
- `AI/EnemyAI.cs`, `AI/EnemySpawner.cs`, `Player/PlayerHealth.cs` — patrol/chase/attack and damage.
- `UI/HUDController.cs` — ammo, suspicion, objective, SIS contact/witness readout.
- `UI/DialogueUI.cs`, `UI/SISPanel.cs` — dialogue choices and SIS debug panel.
- `Editor/AssetLibraryWindow.cs` — browse the extracted library.
- `Editor/ProjectSetup.cs` — one-click bootstrap scene, player rig, SIS demo scene.

## What is still missing for the actual game
- Zimbabwean environments (Harare CBD, Mbare, Avondale, Msasa, Kaguvi Hill estate, later Nyanga and Great Zimbabwe).
- Licensed character art and rigs; Zimbabwean civilians, faction NPCs, world dressing.
- Cinematic direction, voiced dialogue (or authored radio/text VO), score and ambience.
- FSB audio conversion to WAV (or runtime AudioClip loading from the bundle).
- Mobile input mapping, HUD polish, save-slot UI, settings.
- Formal patent novelty/FTO opinion before SIS is presented as proprietary.
