# ZSP Operative Identity System (OIS)

**Status:** Canon design · Implemented in Python (`Prototype/ois.py`) and Unity (`Assets/Scripts/Narrative/OperativeIdentity.cs`, `UI/DossierUI.cs`).

## Principle
Tinashe Mukasa is fixed. The player personalises how the campaign *identifies* them, never who Tinashe is.

## Fields
| Field | Default | Validation | Shown in |
|---|---|---|---|
| Display name | `Tinashe Mukasa` | ≤ 28 chars; letters/spaces/apostrophes/hyphens/periods; homoglyph-normalised; block-list | Menus, mission records, campaign profile, save slots |
| Callsign | `SHADOW-07` | ≤ 12 chars; `[A-Za-z0-9- ]`; upper cased; block-list | Intercepts, enemy intel files, briefings, debriefs, SIS audit, Victor's transmission |
| Nickname | empty | ≤ 14 chars; Unicode letters/numbers, spaces, apostrophes, hyphens and periods only; rich-text markup rejected | Selected conversational barks (text only; never VO) |
| Campaign profile | auto | read-only | Dossier screen |

## Validation rules
- Unicode NFKC normalisation; zero-width/format characters stripped; whitespace collapsed.
- Empty input falls back to defaults — never fails silently.
- Blocked terms (indicative list, production must ship a maintained locale list) rejected on all fields.
- Validation errors are shown to the player; they are never ignored or auto-"corrected" silently.

## Tokens
`{OIS_DISPLAY}`, `{OIS_CALLSIGN}`, `{OIS_NICKNAME}`, `{OIS_FULL}` — replaced at render time by the dialogue/UI layer. Raw tokens never ship in player-facing text.

## Injection points
- Mission premise, objectives, dialogue lines, choice labels, combat/chase headers, debrief text, save/load records.
- SIS audit panel (in-world: "FIELD REPORT") and end-of-Season card.

## Dossier
`DossierUI` (F8) shows: callsign, display name, campaign, protagonist, episodes completed, SIS-derived style (`STYLE: QUIET / LOUD`), mercy profile, reputation (`UNKNOWN / KNOWN / HUNTED`), epilogue status. This is the SIS audit made diegetic.

## Persistence
Saved in `GameSaveRecord.identity`. Loaded saves restore identity; old saves fall back to defaults.

## Hard rules
1. Tinashe's face and voice are fixed in Season One.
2. No custom string is ever spoken by VO.
3. Reputation affects dialogue and dossier text only — never balance.
4. OIS is presentation over the SIS ledger; it hashes behaviour, not names.

## Not yet implemented
- `IdentityRegistrar.cs` registration component exists and validates/binds the chosen identity. The first-launch UI scene/panel, serialized field references, and end-to-end scene wiring still need to be connected and tested.
- Localised block-lists per shipping locale.
- Pronunciation of callsign for VO (if desired later).
