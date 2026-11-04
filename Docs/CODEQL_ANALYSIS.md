# CodeQL C# Analysis — ZSP

## Purpose

This document records the repository's CodeQL configuration, the observed C# database-quality diagnostics, and the controlled path for improving analysis without destabilising the Unity project.

## Project baseline

- Unity Editor: **6000.6.8f1** (authoritative project version in `ProjectSettings/ProjectVersion.txt`).
- Language analysed: C#.
- Current CodeQL build mode: `none`.
- Current workflow: `.github/workflows/codeql.yml`.
- Current source inventory at review: 30 C# files under `Assets/Scripts/`.
- The repository does not currently track `.sln`, `.csproj`, or `.asmdef` files.
- The package manifest contains Unity built-in modules and UGUI; it does not define a conventional standalone .NET application build.

Unity normally generates IDE project files as part of editor project generation. Their absence from source control is not, by itself, a project defect. It does mean a generic hosted .NET build cannot be assumed to reproduce Unity's compilation context.

## Observed CodeQL diagnostics

The scan associated with commit `1a1da29d42a87931acc1a34b30ba818da7a0463` reported:

| Diagnostic | Observed result | Interpretation |
|---|---:|---|
| Calls with resolved call targets | 72% (target: 85%) | Some call relationships were not resolved in the extracted database. |
| Expressions with known types | 85% (target: 85%) | At the stated threshold. |
| Extraction mode | `none` | C# source was scanned without a build observed by CodeQL. |
| Improved incremental analysis | Skipped after a previous resource-related failure | Separate runner/cache resource concern; not a source-code finding. |

The workflow completed successfully. These diagnostics describe analysis coverage and resource behaviour; they are not proof of a vulnerability and are not evidence that the source is vulnerability-free.

## Why build mode is not changed yet

A switch to `autobuild` is not justified until a supported build entry point exists on the runner. The current repository has no committed solution or project file, and the C# scripts are Unity project scripts. A generic `dotnet build` may not reproduce Unity's compiler references, generated assemblies, editor APIs, or package resolution.

A Unity-aware manual build would require a reproducible Unity 6 editor environment and valid licensing/activation on the runner. Those prerequisites have not yet been established. Do not downgrade the project editor version or commit machine-specific generated project files merely to silence a CodeQL diagnostic.

## Controlled improvement plan

1. **Preserve the baseline scan.** Keep `build-mode: none` active so all repository C# files continue to be analysed.
2. **Establish a Unity CI build.** Separately provision Unity `6000.6.8f1`, Android Build Support if required by the validation target, package resolution, and secure licensing. Keep license material in GitHub Actions secrets; never commit it.
3. **Capture a reproducible compile.** Generate Unity project context on the runner and validate compilation using the same editor version as the project. Record editor logs and build outputs.
4. **Evaluate CodeQL build extraction on a branch.** Test `autobuild` only if a supported project/solution build is discoverable; otherwise test `manual` with the actual Unity compilation command. Do not assume either mode works before the run completes.
5. **Compare before promotion.** Compare scan completion, extracted source coverage, call-target resolution, known expression types, and alert changes against the `none` baseline. Promote a new mode only if it is repeatable and improves analysis without dropping source coverage.
6. **Treat incremental analysis separately.** Revisit it when the CodeQL version or runner disk capacity changes. Do not delete caches as a routine fix; cache invalidation is a diagnostic experiment, not a source-code remediation.

## Operational guardrails

- Keep CodeQL and Unity compilation as separate checks: a successful security scan does not prove the Unity project compiles, and a successful Unity build does not prove the code is secure.
- Review actual Code Scanning alerts independently. The quality diagnostics alone do not disclose whether actionable alerts exist.
- Keep the Unity version pinned to `6000.6.8f1`.
- Never store Unity licence files, passwords, tokens, or signing credentials in the repository.
- Do not suppress or exclude project C# files merely to improve the reported percentages.
