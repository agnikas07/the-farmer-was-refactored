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
- The Jint 4.17.0 feasibility probe is merged in PR #3: JavaScript evaluation, host callback, statement budget, running-loop cancellation, and fresh-engine recovery pass on desktop .NET. All four checks also passed in the development game under Unity/Mono on CachyOS/Proton (2026-11-07). No player-facing runtime, editor replacement, or documentation replacement exists yet. A development-only TypeScript compiler prototype now performs semantic checking and emits JavaScript/source maps; it is not connected to the game.
- `src/Compiler` contains the compiler prototype and `examples` has a typed two-flip example. `docs/in-game` and `packaging` remain placeholders. `src/Runtime` and `tests/RuntimeProbe` now contain the diagnostic.
- No public playable release or Thunderstore package exists.
- Native Windows and macOS testing is pending. Current validation covers startup and bounded runtime, stepping, dispatch, read-only simulation observation, the fixed native two-flip adapter, and JavaScript-to-native completion/Stop diagnostics, and Windows x64 V8 loading under Proton.

## Decisions and proposals

### Agreed direction

- Player language: TypeScript. Earlier Rust-like language and transpiler ideas were superseded.
- Public API functions, variables, and properties use camelCase (canHarvest, doAFlip); types use PascalCase. Native snake_case names stay inside adapters and native diagnostic instructions. New TypeScript documentation/examples must follow these conventions.
- Architecture direction: compile TypeScript to JavaScript and run it in an embedded runtime, with C# game integration.
- Use the official TypeScript compiler and actual semantic type checking, not type stripping alone.
- Replace in-game teaching material as part of the feature work, not as a final polish step.
- Preserve a working normal installation and use an isolated development game/prefix.
- Use feature branches and reviewable PRs. Do not merge without user authorization.

### Proposed, not yet selected or proven

- Jint 4.17.0 is pinned for the feasibility probe, not yet selected as the production runtime.
- Compiler hosting/bundling is undecided. Prefer a release that does not require players to install Node.js separately.
- Synchronous-looking farm commands, global beginner API, shared API metadata for declarations/docs, and compiler instrumentation are design proposals.
- Beyond the agreed naming convention/examples, API coverage, module model, execution scheduler, computation tick accounting, and drone state transfer remain undecided.
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

The runtime probe subsequently passed all four checks in-game on 2026-11-07; the user supplied the result and the agent confirmed the fresh on-disk log, including `Runtime probe: ALL PASS` and chainloader completion.

This is not evidence of TypeScript functionality, gameplay parity, or other-platform support. An old log is not evidence that a new build loaded.

## Next work, in small increments

1. Dispatch feasibility is verified in Unity/Mono and PR #5 is merged. JavaScript round-trip, host error propagation, and queued cancellation pass in-game; owner-thread and capacity guards also pass on desktop. See [dispatch instructions](docs/design/dispatch-probe.md).
2. Static game-action inspection is complete: [findings](docs/design/game-action-integration.md). Critical correction: native simulation runs on its own thread; PR #5 does not authorize unsynchronized farm mutations in Unity Update. A read-only simulation-thread/identity/timing probe is now implemented on `feat/simulation-observation-probe`; [manual validation](docs/design/simulation-probe.md) passed in Unity/Mono. The next branch, `feat/native-action-adapter`, adds a fixed two-flip custom Node using native dependency checks, built-in binding, and scheduling; [manual validation](docs/design/native-action-probe.md) passed in Unity/Mono. A per-run [action mailbox](docs/design/action-mailbox.md) now passes desktop Jint tests with camelCase doAFlip, separate claim/completion, cancellation, identity, and failure handling. Native wiring is now implemented in the opt-in [JavaScript action probe](docs/design/javascript-action-probe.md); Unity/Mono normal completion and manual Stop checks passed. Its bounded scheduler wait suspends world timers while waiting for the worker, so production computation timing remains undecided. Nested stacks, drone isolation, pause-aware budgets, and source mapping remain open.
3. [TypeScript compiler prototype](docs/design/typescript-compiler.md) passes 13 desktop tests: strict semantic checking, original-source diagnostics, camelCase doAFlip, JavaScript emission, and source-map contents. Seven [TypeScript-to-Jint integration cases](docs/design/typescript-jint-pipeline.md) now pass against staged runtime DLLs on desktop. The first [embedded compiler host probe](docs/design/compiler-host-feasibility.md) loads TypeScript in Jint but fails library-reference discovery (Node 9, Jint 0). The [minimal reproduction](docs/design/loop-control-reproduction.md) isolates incorrect ordinary continue behavior inside labeled while/do loops; an isolated 4.16.4 comparison also fails. The [ClearScript/V8 comparison](docs/design/v8-compiler-host.md) now passes library discovery, loop control, semantic checks, output execution, and recovery on Linux x64 desktop. A separate opt-in [V8 loader probe](docs/design/v8-game-loader-probe.md) passes native evaluation, labeled-loop control, disposal, and fresh-engine creation in Unity/Mono under Proton; in-game TypeScript compiler hosting is next; Jint COMP-001 remains open. Runtime stack remapping and UI integration remain pending.
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
| COMP-001 | Open | TypeScript 6.0.3 finds nine ES2020 library references under Node, zero under Jint 4.17.0; compilation reports missing global types. | Reduced to ordinary continue inside labeled while/do loops. Both 4.17.0 and isolated 4.16.4 fail; scratch scanner rewrite restores compilation, but no workaround ships. Compare another host or verified upstream fix. | Reproduced on desktop .NET; not tested on Unity/Mono. |
| DEV-005 | Fixed invocation | `dotnet build --locked-mode` failed with an unknown switch. | Use `dotnet build -p:RestoreLockedMode=true` (or `dotnet restore --locked-mode`). | Locked build passed. |

No open plugin behavior bug has been established by the limited startup test. Runtime scheduling and platform compatibility remain untested risks, not fixed bugs.

For each new issue, assign a stable ID and record reproduction/evidence, status, fix/workaround, and verification or remaining uncertainty. Never label a workaround as a root-cause fix.

## Current compiler-host investigation

- 2026-11-07: PR #17 merged. ClearScript/V8 7.5.1.1 passes the isolated Linux x64 compiler-host probe; observed load about 0.3 s and tiny compilations about 0.1–0.3 s. It is a candidate only, with native packaging costs; full compiler hosting in Unity/Proton remains unverified. The desktop comparison did not change the main plugin. Branch `feat/v8-game-loader-probe` adds a separate Windows x64 loader diagnostic; locked netstandard2.1 build passes with zero warnings/errors. PR #18 passes manual Unity/Proton verification: screenshot and fresh on-disk log confirm native evaluation/loop control on worker 10 versus Unity 1, disposal/fresh-engine PASS, and ALL PASS. Its local Enabled setting is now false; all main-plugin diagnostics remain disabled. PR #18 is ready for review, not merged. Next validate the actual TypeScript compiler bundle in-game. See [instructions](docs/design/v8-game-loader-probe.md).

- 2026-11-07: PR #13 merged. README banner/readability work is separate in PR #14. Branch `test/embedded-compiler-host` preserves an exploratory compiler-host check: TypeScript loads in about 1.1 s, but standard-library reference discovery differs from Node, so this path is not selected. PRs #14 and #15 are merged. The follow-up loop-control reproduction intentionally exits nonzero on three cases; Node passes all six. Existing regression suites remain passing. No game deployment; all diagnostics remain disabled.

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

- 2026-11-07: Runtime feasibility change on `feat/javascript-runtime-probe`. Desktop and Unity/Mono checks pass using the staged netstandard DLLs. PR #3 merged. The local opt-in diagnostic has been disabled after verification. Check GitHub for current PR state.

- 2026-11-07: `feat/runtime-stepping-probe` adds a diagnostic worker-thread checkpoint gate and nonblocking Unity polling. Desktop and Unity/Mono checks pass. User screenshot and the fresh on-disk log confirm all four checks and `Stepping probe: ALL PASS` on 2026-11-07. The local diagnostic is now disabled. PR #4 merged. No game actions or tick scheduler are implemented.

- 2026-11-07: `feat/main-thread-dispatch-probe` adds a single-slot request/result transport and an opt-in Update-driven diagnostic. Desktop and Unity/Mono checks pass. The user screenshot and fresh on-disk log confirm all three checks and `Dispatch probe: ALL PASS` on 2026-11-07. PR #5 is merged. The local dispatch diagnostic is now disabled, as are previous diagnostics. No farming actions are called.

- 2026-11-07: PR #6 merged. `docs/game-action-integration` records build-specific Core.dll inspection, native action accounting, and thread/lock ownership. No gameplay code changed; extracted source remains ignored under `work/inspection`. Runtime confirmation of the proposed observation hook is pending.

- 2026-11-07: PR #7 merged. `feat/simulation-observation-probe` adds a checksum-gated Harmony observation hook, primitive snapshots, bounded output, and cleanup. PR #8 verified in-game: user screenshot and fresh on-disk log confirm the ALL PASS marker, simulation thread 16 versus Unity thread 1, and advancing idle/running time for generation 1 (operation duration 2,500,000 ns). Release build and existing desktop diagnostics pass. PR #8 merged. The local simulation diagnostic is now disabled, as are other diagnostics. Safe action injection and timing parity remain unproven.

- 2026-11-07: PR #8 merge confirmed. Native action adapter diagnostic builds and existing desktop checks pass; verified in-game and RunNativeActionProbe now disabled (other diagnostics also off). It replaces only a marked one-flip program with a two-flip custom node. User screenshot and fresh on-disk log confirm two completed flips on simulation thread 16, each charged 400 operations, and ALL PASS. PR #9 merged. Next: design nonblocking worker request delivery without bypassing native action completion or adding simulation/worker lock waits.

- 2026-11-07: PR #9 merge confirmed. Branch `feat/action-command-transport` adds a per-run action mailbox and desktop Jint sequencing/lifecycle tests. Release build has zero warnings/errors; the full desktop suite passes. No game deployment/test is needed for this unwired transport change. All local in-game diagnostics remain disabled. User explicitly requested idiomatic TypeScript naming; recorded in AGENTS.md and design docs.

- 2026-11-07: PR #10 merged. Branch `feat/javascript-native-actions` connects a fixed camelCase JavaScript two-flip script to the native adapter. It adds a validated loop-boundary yield, bounded native monitor wait, and native Stop worker disposal. Build/full desktop suite pass. Normal-run screenshot confirms scheduler yield, two doAFlip completions at 400 operations each, ALL PASS, and worker released. Stop-test screenshot and fresh on-disk log confirm worker released with no completed-call/ALL PASS markers; the earlier normal log was overwritten, so normal-run evidence is screenshot-only. PR #11 merged. RunNativeActionProbe is now disabled, as are other probes. Broader cancellation races, world reset, timing parity, and pause/step remain unverified.

- 2026-11-07: PR #11 merge confirmed. Branch `feat/typescript-compiler-prototype` adds an isolated virtual CompilerHost using pinned TypeScript 6.0.3, a Node development host, doAFlip declarations, and a typed example. All 13 compiler tests pass on Node 26.11.1. No game deployment or changes to runtime behavior; in-game diagnostics remain disabled.

- 2026-11-07: PR #12 merge confirmed. Branch `test/typescript-jint-pipeline` adds fresh compile-to-Jint integration fixtures for loops, enum/generic/optional access output, runtime exceptions, statement budgets, compile rejection, and fresh-engine recovery. All seven pipeline cases, 13 compiler tests, and the existing runtime suite pass. No plugin behavior change or game deployment; Unity/Mono execution of compiler output and compiler hosting remain pending.

- 2026-11-07: README refresh adds the user-supplied banner unchanged, a typed example, milestone table, and clearer contributor navigation. All README file links and the copied image hash were checked. This is a documentation-only change; compiler-host investigation continues separately.

- 2026-11-07: Branch `test/loop-control-reproduction` isolates COMP-001 without TypeScript dependencies. Six shared Node/Jint cases expose three failures. A scratch-only explicit-continue scanner experiment restored references and semantic compilation (about 5–6 s per tiny input); no compiler patch, dependency change, or game deployment is included. Next compare an alternate compiler host or verified upstream runtime fix.
