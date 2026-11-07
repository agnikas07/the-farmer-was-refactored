# Project status and handoff

Last updated: 2026-11-07 (America/New_York).

This is the canonical handoff for a new agent or session. Read it alongside `AGENTS.md` before starting work. It records verified facts, decisions, plans, and issues; the code and current GitHub state take precedence if this snapshot becomes stale.

## Product and user intent

**The Farmer was Refactored** replaces The Farmer Was Replaced's Python-like programming experience with TypeScript.

The goal is a complete language-system replacement: editor, execution, debugging, code windows/modules, API, progression, tutorials, and in-game documentation. Farming mechanics should be preserved. Documentation must teach playing in TypeScript, not merely translate API names.

Development is on CachyOS Linux. Windows, Linux, and macOS are intended release targets. Linux currently runs the Windows game under Proton; native Linux support is not established.

The user wants the agent to take initiative and perform authorized setup/implementation, with short progress updates and manageable steps. Keep PRs as small as practical, ideally 200–400 changed lines; smaller focused changes are welcome. Avoid delivering the entire long-term plan in every reply.

## Current snapshot

- Startup milestone complete: C# plugin `0.1.0` loads through BepInEx in the isolated development game.
- [PR #1: startup plugin and development workflow](https://github.com/agnikas07/the-farmer-was-refactored/pull/1) merged on 2026-11-07. It contained 189 additions and 4 deletions.
- A Jint 4.17.0 feasibility probe is implemented on `feat/javascript-runtime-probe`: JavaScript evaluation, host callback, statement budget, running-loop cancellation, and fresh-engine recovery pass on desktop .NET. Unity/Mono testing is pending. No player-facing runtime, TypeScript compiler, game-action bindings, editor replacement, or documentation replacement exists yet.
- `src/Compiler`, `docs/in-game`, `examples`, and `packaging` are placeholders. `src/Runtime` and `tests/RuntimeProbe` now contain the diagnostic.
- No public playable release or Thunderstore package exists.
- Windows and macOS testing is pending. Current validation proves startup only.

## Decisions and proposals

### Agreed direction

- Player language: TypeScript. Earlier Rust-like language and transpiler ideas were superseded.
- Architecture direction: compile TypeScript to JavaScript and run it in an embedded runtime, with C# game integration.
- Use the official TypeScript compiler and actual semantic type checking, not type stripping alone.
- Replace in-game teaching material as part of the feature work, not as a final polish step.
- Preserve a working normal installation and use an isolated development game/prefix.
- Use feature branches and reviewable PRs. Do not merge without user authorization.

### Proposed, not yet selected or proven

- Jint 4.17.0 is pinned for the feasibility probe, not yet selected as the production runtime.
- Compiler hosting/bundling is undecided. Prefer a release that does not require players to install Node.js separately.
- Synchronous-looking farm commands, global beginner API, shared API metadata for declarations/docs, and compiler instrumentation are design proposals.
- Exact API names, module model, execution scheduler, computation tick accounting, and drone state transfer remain undecided.
- TypeScript support does not automatically provide Node.js, browser APIs, or arbitrary npm package compatibility.

## Environment and local state

Repository: https://github.com/agnikas07/the-farmer-was-refactored

Local checkout: `/mnt/StorageDrive/Code Projects/the-farmer-was-refactored`.

- Original game: `/mnt/StorageDrive/SteamLibrary/steamapps/common/The Farmer Was Replaced`.
- Steam build inspected: `26462009`; Unity `6000.0.43f1`, Mono backend.
- Loader: official BepInEx Windows x64 `5.4.23.6`.
- Plugin framework: `netstandard2.1`; SDK pinned separately to `10.0.403` in `global.json`.
- Plugin ID: `io.github.agnikas07.thefarmerwasrefactored`.
- Git, Node.js, npm, and authenticated GitHub CLI were available at setup. Verify access in a new session; never store tokens here.
- SDK: `.tools/dotnet`; use `source scripts/dev-env.sh` to configure it.
- Isolated game and loader: `.tools/game`.
- Separate Proton prefix/saves: `.tools/compatdata` (created by the launcher).
- Original save backup: `work/save-backups/initial`.
- Built plugin: `artifacts/plugin/TheFarmerWasRefactored.dll`.
- Local tools, game binaries, saves, logs, and build outputs are ignored, and are not restored by cloning GitHub.

See [development instructions](docs/development.md) for fresh-checkout prerequisites and overrides, and [environment evidence](docs/design/environment.md) for loader provenance.

## Verification evidence

2026-11-07:

- Release build passed with zero warnings/errors, including a build with `-p:RestoreLockedMode=true`.
- Bash syntax checks and Git whitespace checks passed.
- User launched `bash scripts/launch-dev.sh`, waited for the game, closed it, and checked the log.
- The agent subsequently read `.tools/game/BepInEx/LogOutput.log` and confirmed these markers:

```text
Loading [The Farmer was Refactored 0.1.0]
The Farmer was Refactored loaded.
Plugin 0.1.0; game 0.1; Unity 6000.0.43f1; platform WindowsPlayer.
Chainloader startup complete
```

`WindowsPlayer` is expected under Proton. The reported game version `0.1` comes from Unity's application metadata; use the Steam build ID when identifying the installed build.

This is not evidence of TypeScript functionality, gameplay parity, or other-platform support. An old log is not evidence that a new build loaded.

## Next work, in small increments

1. Run the opt-in runtime probe inside Unity/Mono and inspect its fresh log. Desktop checks pass; game compatibility remains pending. See [probe instructions](docs/design/runtime-probe.md).
2. Prove an execution approach for pause/resume, stepping, simulation timing, and main-thread-safe game actions before committing to a runtime architecture. Investigate drone isolation and source mapping as explicit requirements.
3. Prototype TypeScript checking/compilation and error/source mapping independently of the UI. Decide how to bundle the compiler.
4. Deliver one playable slice: TypeScript code window, a small typed farm API, run/stop controls, saved source, and a rewritten first lesson.
5. Expand API, progression, modules, drones, and teaching material together; validate broader behavior and platforms before packaging a release.

These are sequencing intentions, not completed work or blanket permission to merge/publish. Do not attempt the entire runtime/editor/docs replacement in one PR.

## Issues and fixes

| ID | State | Symptom / cause | Fix or next action | Verification |
| --- | --- | --- | --- | --- |
| DEV-001 | Fixed | Initial `netstandard2.0` build conflicted with Unity's .NET Standard 2.1 references. | Target `netstandard2.1`. | Clean build and actual plugin startup. |
| DEV-002 | Fixed | Compiler could not resolve `MonoBehaviour` through BepInEx. | Reference both local `UnityEngine.dll` and `UnityEngine.CoreModule.dll`; do not copy game references into plugin output. | Clean build and actual plugin startup. |
| DEV-003 | Workaround | Sandboxed .NET build could not create a mutex under `/run/user/1000`. | Use a private `.tools/runtime` directory with mode 700 and set `XDG_RUNTIME_DIR` for the build command only. | Build passed; commands in development guide. |
| DEV-004 | Tooling limitation | A file-level write permission for Proton's `dist.lock` caused sandbox mount failures, then prevented shell commands in that turn. | Avoid requesting file-level access to that lock. The user successfully ran the launch script in their normal terminal. Request only necessary directory permissions if automated testing is retried. | User launch verified; sandbox launch itself remains unverified. |
| DEV-005 | Fixed invocation | `dotnet build --locked-mode` failed with an unknown switch. | Use `dotnet build -p:RestoreLockedMode=true` (or `dotnet restore --locked-mode`). | Locked build passed. |

No open plugin behavior bug has been established by the limited startup test. Runtime scheduling and platform compatibility remain untested risks, not fixed bugs.

For each new issue, assign a stable ID and record reproduction/evidence, status, fix/workaround, and verification or remaining uncertainty. Never label a workaround as a root-cause fix.

## Resume checklist

1. Read `AGENTS.md` and this file; inspect applicable instructions before editing.
2. Check `git status`, branch/remotes, and current PR states. Preserve any uncommitted work. Fetch before creating a new branch from `origin/main`.
3. Check local prerequisites; request filesystem/network access only as needed. Permission grants may expire between sessions.
4. Choose one small next outcome and state it briefly to the user.
5. Run checks appropriate to that change; distinguish automated checks from manual game verification.
6. Update this file with new decisions, evidence, bugs/fixes, next steps, and PR state. Exclude secrets and large raw logs.

## Handoff history

- 2026-11-07: Repository and local toolchain established. Startup plugin verified on CachyOS/Proton; PR #1 merged.
- 2026-11-07: Added this handoff record at the user's request. PR #2 merged; the status file is now part of the handoff workflow.

- 2026-11-07: Runtime feasibility change on `feat/javascript-runtime-probe`. Desktop checks pass using the staged netstandard DLLs; opt-in game validation remains pending. Check GitHub for the current PR state.
