# ZSP Release Readiness and Verification Record

**Project:** Zimbabwe: The Shadow Protocol (ZSP)  
**Authoritative editor:** Unity 6000.6.8f1  
**Target:** Android first; iOS later  
**Status:** Prototype and repository validation automated; Unity editor/device validation remains outstanding.

## Verified in GitHub Actions

| Area | Evidence | Result | Scope / limitation |
|---|---|---|---|
| CodeQL C# extraction | CodeQL main-branch run 41851065355 | Passed | Source analysis completed with build-mode `none`; this is not a Unity compile. |
| Python reference prototypes | Python main-branch run 41851066207 | 22 tests passed | SIS, OIS and adaptive mission-chain reference suite. |
| Project content integrity | Python main-branch run 41851066207 | Passed | Unity version pin, JSON parsing, Resources/StreamingAssets byte parity, M1–M3 mission IDs and start/beat IDs. |
| Unity editor compilation | Not available in hosted workflow | Not verified | Requires Unity Editor 6000.6.8f1 and a configured licensed runner or local editor. |
| Android build and device performance | Not available in hosted workflow | Not verified | Requires Android Build Support, a signing setup and physical-device profiling. |

## Local Unity 6 acceptance checklist

**Confirmed completed:** Unity Editor **6000.6.8f1** is installed and the project opens in that editor. This confirms the editor/project-open step only; it does not confirm a clean compile, scene execution, or Android build.

Run the remaining checks on the machine with Unity **6000.6.8f1** and Android Build Support installed.

- [ ] Allow package resolution to finish and confirm the Console has no compile errors.
- [ ] Run the ZSP project setup commands documented in the root README.
- [ ] Open and play the Bootstrap scene.
- [ ] Verify environment bundle loading; the optional local bundle is not part of this public repository.
- [ ] Verify player movement, camera, weapon, stealth, melee, health and HUD interactions.
- [ ] Verify OIS registration, save/load, dossier display and dialogue token substitution.
- [ ] Play Episodes M1, M2 and M3 through the actual Unity scenes, not only the Python reference.
- [ ] Verify SIS state changes reach enemy spawning, contact state, dialogue variants and mission transitions.
- [ ] Build an Android development APK and test on at least one affordable/low-memory device and one mid-range device.
- [ ] Profile sustained frame rate, memory, loading, thermal behaviour, battery use and crash-free mission completion.
- [ ] Test fresh install, save migration, app pause/resume and low-storage behaviour.

## Release and rights gates

- [ ] Complete a provenance/licence audit for every third-party model, animation, sound, font and plugin.
- [ ] Keep all assets derived from the third-party decompiled APK excluded from the repository and release build.
- [ ] Verify original art and audio rights, music rights, voice talent consent and localisation approvals.
- [ ] Confirm privacy disclosures and data collection behaviour before store submission.
- [ ] Configure Android package signing and protect signing credentials in a secure secret store.
- [ ] Confirm target SDK, store policy compliance, age rating and content descriptors at release time.
- [ ] Complete security review of actual Code Scanning alerts; a successful scan is not proof that no vulnerabilities exist.

## Repository metadata correction

The live repository metadata was checked on 4 November 2026 and already reports Unity **6000.6.8f1**. The repository description is current; no owner action is outstanding for this item.

## Working rule

Do not mark the project release-ready until the local Unity editor, Android device, security-alert and third-party-rights gates above have been completed and recorded. A green Python/CodeQL workflow is necessary, not sufficient.
