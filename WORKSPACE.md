# Workspace map

| Path | What it is | Keep? |
|---|---|---|
| `ZSP_SEASON_01_STORY_BIBLE.md` | **Official Season One bible** (Tinashe Mukasa; 12 episodes; The Black Ledger; Adaptive Enemy Doctrine). | **Yes — canon.** |
| `ZSP_CLEAN/` | **The clean Unity 6000.6.8f1 project.** Original brand art, quarantined Commando/SDK assets, new gameplay scripts, Season 1 content (interim) and SIS. | **Yes — build here.** |
| `ZSP_CLEAN/StrippedSource/` | The only thing kept from the old APK: `data.unity3d` + `REMOVED_ITEMS.txt`. | Yes (source of truth for art). |
| `ZSP_CLEAN/_Quarantine/Ads/`, `ZSP_CLEAN/_Quarantine/CommandoBranding/` | Ad creatives and franchise/SDK/watermarked assets removed from the usable library. | No — reference only. |
| `ZIMBABWE THE SHADOW PROTOCOL/` | Original decompiled APK (ads, Firebase, old IL2CPP code, Android shell). Untouched reference. | Safe to delete once `ZSP_CLEAN` is verified. |
| `adaptive_mission_chain.py`, `test_amc.py`, `ADAPTIVE_MISSION_CHAIN_DESIGN.md` | Original Python prototype of the adaptive mission system. | Copied into `ZSP_CLEAN/Prototype` and `ZSP_CLEAN/Docs`; root copies can be deleted. |
| `inventory.py`, `characters.py`, `probe_*.py`, `extract_*.py` | APK analysis/extraction scripts. | Keep until extraction is final; then archive. |
| `inventory_out.txt`, `ASSET_INVENTORY.md` | Original APK inventory notes. | Archive/reference. |
| `Zimbabwe RPG Invention Ideas.html` | Seed conversation. | Keep as design history. |

## The short version

1. Read **`ZSP_SEASON_01_STORY_BIBLE.md`** first — it is the canon.
2. Open **`ZSP_CLEAN/`** in **Unity 6000.6.8f1** (Android Build Support).
3. Use the **ZSP** menu to create the bootstrap scene, player rig, SIS demo and vertical-slice scene.
4. Run the Python SIS prototype: `cd ZSP_CLEAN\Prototype; python -m pytest test_sis_v1.py -q`.
5. The interim "Courier's Warning" missions are superseded by **Episode 01: The Message**; the engine is unchanged.
