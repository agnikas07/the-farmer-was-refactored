# Project status and handoff

Last updated: 2026-11-14 (America/New_York).

This is the canonical handoff for a new agent or session. Read it alongside `AGENTS.md` before starting work. It records verified facts, decisions, plans, and issues; the code and current GitHub state take precedence if this snapshot becomes stale.

## Product and user intent

**The Farmer was Refactored** replaces The Farmer Was Replaced's Python-like programming experience with TypeScript.

The goal is a complete language-system replacement: editor, execution, debugging, code windows/modules, API, progression, tutorials, and in-game documentation. Farming mechanics should be preserved. Documentation must teach playing in TypeScript, not merely translate API names.

Development is on CachyOS Linux. Windows, Linux, and macOS are intended release targets. Linux currently runs the Windows game under Proton; native Linux support is not established.

The user wants the agent to take initiative and perform authorized setup/implementation, with short progress updates and manageable steps. Keep PRs as small as practical, ideally 200–400 changed lines; smaller focused changes are welcome. Avoid delivering the entire long-term plan in every reply.

## Current snapshot

- Startup milestone complete: C# plugin `0.1.0` loads through BepInEx in the isolated development game.
- [PR #1: startup plugin and development workflow](https://github.com/agnikas07/the-farmer-was-refactored/pull/1) merged on 2026-11-07. It contained 189 additions and 4 deletions.
- The Jint 4.17.0 feasibility probe is merged in PR #3: JavaScript evaluation, host callback, statement budget, running-loop cancellation, and fresh-engine recovery pass on desktop .NET. All four checks also passed in the development game under Unity/Mono on CachyOS/Proton (2026-11-07). The current development path connects embedded TypeScript compilation to Jint and native actions, with compiler popups and optional marker-free execution. Full editor and documentation replacement remain unfinished.
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

## Roadmap

Agreed sequence: foundation → first playable lesson → reliable language system →
full gameplay coverage → release. Each step stays in a focused PR; historical
implementation evidence remains below and in `docs/design`.

### 1. Foundation — demonstrated, still development-only

Embedded TypeScript compilation, Jint execution, native action scheduling,
Run/Stop recovery, compiler popups, compiler reuse, and optional marker-free
execution are verified under Unity/Mono on CachyOS/Proton. This does not establish
production timing parity or complete language correctness.

### 2. First playable lesson — current milestone

1. Add `canHarvest(): boolean`, carrying native query results back to TypeScript.
2. Add `harvest()` with native return values, costs, and unlock behavior.
3. Replace a small introductory in-game lesson and example using both functions.
4. Add typed directions and `move()`, then extend the lesson to farm traversal.
5. Verify the whole slice: Run, Stop, correction, save, reload, and repetition.

The canHarvest and harvest increments are merged (#29–30); the current PR
implements step 3, the first lesson. No move binding yet. Each subsequent step gets its own review and appropriate game testing.

### 3. Runtime correctness checkpoint — before broad API expansion

Resolve COMP-001 or choose a verified alternative runtime. Decide computation
costs and simulation timing, resource budgets, pause/step behavior, and lifecycle
handling (world changes, closing windows, cancellation races). Preserve native
unlock rules. Tests must distinguish native action cost from JavaScript compute
cost; current worker waiting freezes world time and is not a final design.

### 4. Complete language and gameplay replacement

- Editor: TypeScript highlighting, indentation, completion, runtime errors with
  original source locations, and execution indicators.
- Source: verified persistence, modules/imports, and handling existing Python code.
- Gameplay: planting, inventory, entities/items, progression, advanced crops,
  multiple drones, and simulations, expanding typed APIs and lessons together.
- Teaching: replace tutorials, API documentation, tooltips, and examples with
  TypeScript material throughout the game.

### 5. Release readiness

Package dependencies and installation/update workflows; establish compatibility
checks and supported game versions. Test target platforms before claiming support.
No public package exists; native Windows and macOS remain untested.

These milestones are plans, not completed work or permission to merge/publish.

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

- 2026-11-07: PR #17 merged. ClearScript/V8 7.5.1.1 passes the isolated Linux x64 compiler-host probe; observed load about 0.3 s and tiny compilations about 0.1–0.3 s. It is a candidate only, with native packaging costs; full compiler hosting in Unity/Proton remains unverified. The desktop comparison did not change the main plugin. Branch `feat/v8-game-loader-probe` adds a separate Windows x64 loader diagnostic; locked netstandard2.1 build passes with zero warnings/errors. PR #18 passes manual Unity/Proton verification: screenshot and fresh on-disk log confirm native evaluation/loop control on worker 10 versus Unity 1, disposal/fresh-engine PASS, and ALL PASS. Its local Enabled setting is now false; all main-plugin diagnostics remain disabled. PR #18 is merged. Branch `feat/v8-game-compiler-probe` adds the [in-game compiler diagnostic](docs/design/v8-game-compiler-probe.md). The shared checks pass on Linux x64 desktop, and the locked Unity build has zero warnings/errors. The generated compiler bundle and notices are deployed locally; Enabled and RunCompilerProbe were enabled for manual testing. Screenshot and fresh BepInEx log confirm all compiler checks pass in Unity/Mono on CachyOS/Proton: load 353 ms, typed loop 342 ms, semantic rejection 172 ms, snake_case rejection 151 ms, module rejection 0 ms, recovery 113 ms. PR #19 is merged. Both diagnostic settings are now false; main-plugin diagnostics remain disabled. Branch `feat/v8-to-jint-game-probe` adds the [V8-to-Jint pipeline diagnostic](docs/design/v8-to-jint-game-probe.md). Shared V8/Jint desktop checks and locked builds pass. PR #20 verified under Unity/Mono on CachyOS/Proton: user screenshot and fresh BepInEx log confirm all seven checks and ALL PASS. Load 317 ms; typed loop 495 ms; semantic rejection 141 ms; snake_case rejection 148 ms; module rejection 0 ms; recovery 100 ms; enum/generic/optional access 159 ms. Valid cases include the insufficient-statement-budget check. All three local diagnostic settings are now false. PR #20 is merged. Next connect compiled output to the native action mailbox after this boundary is verified. See [instructions](docs/design/v8-game-loader-probe.md).

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

- 2026-11-07: Branch `feat/typescript-native-actions` connects a fixed V8-compiled TypeScript loop to the existing Jint/native action mailbox. [Manual instructions](docs/design/typescript-native-actions.md). Compiler/preflight finishes before the main plugin is armed on Unity Update; only the new marked trigger runs actions. Builds and desktop runtime/mailbox/compiler checks pass; normal completion and Stop checks pass in Unity/Mono on CachyOS/Proton. Normal-run screenshot confirms two completions at 400 operations each, ALL PASS, and worker released. Stop screenshot and fresh on-disk log confirm worker released with no completed-action or ALL PASS markers. The normal log was overwritten by the Stop run; normal evidence is screenshot-only. All local diagnostic switches are now false. PR #21 is merged. Next: connect actual source input to compilation, retaining clear diagnostics and execution lifecycle boundaries.

- 2026-11-07: Branch `feat/typescript-source-input` adds opt-in actual source capture from code windows marked `// @refactored`, bypassing the native parser on ordinary Run. Compilation happens on the action worker; no user-code preflight executes. Desktop source/compiler/runtime checks and builds pass. [Manual validation](docs/design/typescript-source-input.md) passed under Unity/Mono on CachyOS/Proton: screenshots confirm 1/3 calls at 400 operations each, repeated Run, TS2322 rejection on line 3 with no action completions, and correction followed by one successful call in the same session. Stop screenshot and fresh on-disk log confirm compilation followed by worker released without completed calls. Earlier runs are screenshot-only because later launches overwrote their logs. All local diagnostic flags are now false. PR #22 is merged. Native highlighting/errors remain a known UI limitation.

- 2026-11-07: README refreshed on `docs/readme-source-input-progress` to reflect verified embedded compilation and actual code-window input. Documents the temporary marker, Python editor indicators, development-only availability, and unresolved runtime/packaging choices. Documentation-only change; relative links and whitespace checked.

- 2026-11-13: PR #23 merge confirmed. Branch `fix/typescript-python-diagnostics` adds scoped Python-parser bypass and stale popup cleanup for marked TypeScript windows. [Validation instructions](docs/design/typescript-editor-validation.md). Locked build and desktop runtime suites pass; user confirmed popup suppression, marker removal restoring native errors, marker re-addition clearing the error, and successful one-flip execution. Fresh on-disk log confirms compilation, one completed call at 400 operations, and worker released. PR #24 is merged. All diagnostic flags are now false locally. Marker-free mode, TypeScript highlighting, and inline TypeScript diagnostics are still future work.

- 2026-11-13: User reports noticeable Run-to-first-action delay. The current source path reads the compiler bundle and creates/loads a fresh V8 compiler for every Run before Jint execution. This is an expected contributor, not a measured latency breakdown. Next measure compilation and first-action timing, then consider compiler reuse on a dedicated worker and safe caching; preserve error rejection, source snapshots, Stop, and engine thread ownership.

- 2026-11-13: Branch `test/typescript-run-latency` adds per-run monotonic timing across source capture, compilation, Jint startup, mailbox delivery, native action completion, and worker release. [Baseline instructions](docs/design/typescript-run-latency.md). Builds and runtime/compiler suites pass; User-pasted output and fresh BepInEx log confirm three successful one-flip runs at 400 operations each. Capture-to-native-binding latency: 1010.28 / 676.99 / 676.91 ms. Repeated runs spend roughly 180 ms reading/parsing the bundle, 210–224 ms loading the compiler, and 262–270 ms checking/emitting source. This isolates compiler work as the main startup cost; the subsequent ~990 ms native action interval is separate. PR #25 is merged. All diagnostic flags are now false locally. Next investigate compiler/bundle reuse with explicit worker ownership; no optimization or timing-parity claim yet.

- 2026-11-13: Branch `perf/reuse-typescript-compiler` adds a dedicated compiler owner thread in source mode with one pending request, fresh semantic programs per source, cancellation-aware waiting, and owner-thread V8 disposal. [Design/test instructions](docs/design/reusable-compiler-worker.md). Desktop reuse/isolation/error/cancellation/lifecycle tests and existing suites pass. In-game timing comparison and lifecycle checks remain pending. Enabled and RunSourceInputProbe are true locally; other flags are false. Bundle/API changes require restart; no output cache or measured speedup claim.

- 2026-11-13: PR #26 timing comparison verified by user-pasted output and fresh log: capture-to-binding 426.34 / 150.35 / 133.55 ms versus baseline 1010.28 / 676.99 / 676.91 ms. Repeated-run delay reduced ~78–80% in this three-run sample; initialization moved before Run. One compiler-ready marker and three correct one-flip completions/worker releases. Changed-source, semantic rejection/recovery, and Stop checks still pending; PR remains draft and source mode enabled.

- 2026-11-13: PR #26 manual checks complete: user-pasted logs confirm three-call source changes, semantic rejection with zero completed actions and same-session recovery, and Stop followed by successful one-call recovery. Fresh disk log verifies the final Stop/recovery; earlier logs were overwritten. All successful actions charge 400 operations. PR #26 is merged. All local diagnostic flags are disabled. Compiler-owner shutdown is desktop-tested but not established by the game action-worker-release markers.

- 2026-11-13: Branch `feat/typescript-editor-errors` adds first-error TypeScript popups on Run, structured spans, and source/run-scoped delivery on Unity. [Manual instructions](docs/design/typescript-editor-errors.md). Locked builds and Node/runtime/V8 suites pass, including CRLF/Unicode span tests. Native popup placement, correction clearing, and stale-result behavior remain unverified. Enabled and RunSourceInputProbe are true locally; other modes are off. Runtime exceptions remain log-only.

- 2026-11-13: PR #27 manual popup/recovery checks pass under Unity/Mono on CachyOS/Proton: screenshot confirms TS2322 at `count`; user confirms correction clears the popup and executes one flip. Removing the marker restores native Python errors (screenshot); restoring marked valid code clears them and executes one flip. Fresh disk log confirms rejection without completed actions and both successful one-call runs at 400 operations, with worker release. README updated. Rapid edit/new-Run and window-close delivery races remain manually unverified. All local diagnostic flags are now disabled; PR prepared for review, not merged.

- 2026-11-13: PR #27 merge confirmed at f63915e. Branch `feat/typescript-default-mode` adds opt-in `Language.TypeScriptForAllWindows` to source mode: Run and editor parsing share the same selection, without changing saved source. Default remains marker-based; restart required. [Design and manual checks](docs/design/typescript-default-mode.md). Locked builds passed with zero warnings/errors; runtime, stepping, mailbox, and dispatch regression suites pass. Deployed locally with Enabled, RunSourceInputProbe, and TypeScriptForAllWindows true; other diagnostics remain off. Manual game verification pending.

- PR #28 manual progress: user confirms unmarked one-flip execution without Python errors, semantic rejection without actions, correction clearing the error and executing one flip, and compatibility with marked scripts. TypeScriptForAllWindows is now false locally; Enabled and RunSourceInputProbe remain true for the pending restart-to-marker-mode check. PR remains draft.

- PR #28 manual validation complete: user confirms restart with TypeScriptForAllWindows false restores native Python errors, then marked TypeScript clears the error and performs one flip. Fresh disk log confirms marker-based startup, one completed action at 400 operations, and worker release; earlier all-window checks are user-reported because restart overwrote their log. All local diagnostic flags and TypeScriptForAllWindows are now false. Ready for review; not merged.

- PR #28 merge confirmed at 8367d36. Branch `feat/can-harvest` begins the agreed first playable lesson with boolean query transport and native canHarvest scheduling. Roadmap reorganized into milestones. Locked builds pass; automated and manual checks recorded in [query design](docs/design/can-harvest.md).

- canHarvest validation: 14 compiler tests, V8/compiler-worker suites, and runtime/mailbox/stepping/dispatch suites pass, including true/false conditional execution and query cancellation. Both locked plugin builds have zero warnings/errors. Local game deployment has Enabled, RunSourceInputProbe, and TypeScriptForAllWindows true for manual testing. Query field results, native cost, and unlock rejection remain unverified in-game.

- PR #29 manual evidence: user confirms mature-grass query followed by one flip without harvesting; fresh log confirms true at 1 operation, one flip (599 operations in this session), and worker release. Native flip cost varies with operation duration. User has only one grass tile; in-game false case is deferred to harvest, with false transport/branching covered on desktop. Locked-query rejection remains untested. Local diagnostics and all-window mode disabled; PR ready for review, not merged. Next implement harvest and revisit the false case.

- PR #29 merge confirmed at ba53c17. Branch `feat/harvest` adds harvest(): boolean through the native scheduler, retaining native success/failure costs and completion ordering. [Design/manual check](docs/design/harvest.md). Builds pass; manual success/false results, field/inventory effects, and costs remain pending. Next is the first rewritten in-game lesson.

- harvest automated validation: both locked plugin builds pass with zero warnings/errors; all 15 compiler tests, runtime/mailbox/stepping/dispatch, and V8/compiler-worker suites pass. Embedded farm sequence uses a stub field, not native game validation. Deployed locally; Enabled, RunSourceInputProbe, and TypeScriptForAllWindows true for the pending manual check.

- 2026-11-14: PR #30 manual checks pass for collection/conditional flip and post-harvest canHarvest=false. Fresh log confirms harvest=true costs 200 operations, query costs 1, and worker release. Two successive harvests both returned true with zero flips; native harvest=false/failure cost remain untested (desktop false transport/branching pass). This closes PR #29’s deferred false-query check. README/design evidence updated; local diagnostics and all-window mode disabled. PR prepared for review, not merged. Next: first rewritten in-game lesson.

- 2026-11-14: PR #30 merge confirmed at 37bf9fb. Branch `feat/first-typescript-lesson` replaces only the First Program docs page while source mode is active, using an embedded original English lesson with mode-aware examples. [Design/validation](docs/design/first-typescript-lesson.md). Native rendering, links, and mode restoration pending. No original game files are modified.

- First lesson validation: both locked plugin builds pass with zero warnings/errors; all 17 compiler tests pass, including every lesson code block in marker/all-window modes and true/false branching. Deployed with Enabled, RunSourceInputProbe, and TypeScriptForAllWindows true. English lesson rendering, navigation, and disabled-mode restoration remain pending; no native visual verification claimed.

- PR #31 manual progress: screenshot confirms title, introductory text, flip/harvest examples, and marker-free instructions render. User confirms home → First Program navigation and one flip from the first lesson example. TypeScriptForAllWindows now false locally for the pending marker-mode restart check; source mode remains enabled. Disabled-mode restoration remains pending.

- PR #31 marker-mode check: screenshot confirms marker insertion and explanatory text; user confirms the full marked example clears the expected native error on previously unmarked saved code and runs one flip. All local diagnostic flags now disabled (TypeScriptForAllWindows already false) for the pending original-page restoration check after restart.

- 2026-11-14: PR #31 restoration check passed: screenshots show the original First Program page after disabling source mode and restarting. Manual checks now cover visible lesson layout in both modes, home navigation, first-example execution, and original-page restoration. Full-page layout across languages/window sizes and the tech-tree link were not separately verified. Local diagnostics remain disabled. PR prepared for review, not merged; next planned feature is typed directions and move().
