# TypeScript source from a code window

This diagnostic replaces the fixed program with a snapshot of the code window
when ordinary Run is pressed. It only handles windows whose first line is
exactly `// @refactored` (LF or CRLF). Unmarked windows retain native behavior.

Local inspection of the checksum-gated game build showed CodeWindow.StartExecution
calls the Python parser before MainSim.StartMainExecution. A Harmony prefix on
StartExecution bypasses that parser for marked windows, stops any previous run,
and starts the existing custom node with the captured source. No game source is
included in the repository. The compiler delegate runs on the action worker,
never on Unity or the simulation thread. Only successfully emitted JavaScript
reaches Jint. User code is not executed as a compilation preflight.

The fixed two-flip diagnostics remain available separately. Source mode has no
expected action count and logs the actual completed count. Each Run takes a new
snapshot and creates a fresh compiler/player; edits cannot alter a running script.
The native Stop path still cancels the mailbox. Cancellation is checked before
and after compilation; it prevents execution after Stop, but does not interrupt
an active V8 compilation. Compilation currently pauses simulated time through
the diagnostic scheduler wait. This is not production timing behavior.

## Manual validation

Close the game and deploy with `bash scripts/deploy-v8-probe.sh`. In the V8 probe
config set Enabled and RunSourceInputProbe true; all other diagnostic flags,
including those in the main plugin, should be false. Source mode takes precedence.

Launch with `bash scripts/launch-dev.sh`. In a normal, single-drone farm with
flips unlocked, paste this into a code window and press ordinary Run:

```typescript
// @refactored
doAFlip();
```

Expect exactly one flip and `completed 1 doAFlip calls`, then `worker released`.
Without restarting, replace it and Run again:

```typescript
// @refactored
const count: number = 3;
for (let i = 0; i < count; i++) doAFlip();
```

Expect three flips and `completed 3 doAFlip calls`. Next verify no actions occur:

```typescript
// @refactored
doAFlip();
const count: number = "two";
```

Require compilation failure with TS2322 on line 3, no action completions for that
run, and worker released. Restore the one-flip source and Run to verify recovery.
Finally run the three-flip source and Stop during the first flip; require worker
released without three completed calls. An applied animation may still finish.

After closing, inspect all runs in order:

```bash
rg 'TypeScript source probe' .tools/game/BepInEx/LogOutput.log
```

The source mode deliberately has no ALL PASS marker: completion is not an assertion
about what the user intended. Disable Enabled and RunSourceInputProbe afterward.

## Limits and evidence

Desktop tests pass for 0/1/3 calls, CRLF, semantic error line numbers, recovery,
source size rejection, and compilation without executing runtime-only exceptions.
Existing runtime/mailbox and V8 compiler suites pass; Unity build is clean.
Manual validation passed on 2026-11-07 under Unity/Mono on CachyOS/Proton:
user screenshots confirm one-call and three-call runs (400 operations per call),
repeated Run, rejection with TS2322 on line 3 and no action completions, and a
corrected one-call run in the same session after rejection. The Stop screenshot
and fresh BepInEx log confirm source compiled, scheduler yield, and worker released
without completed calls. Earlier results are screenshot-only because subsequent
launches overwrote their logs. All local diagnostic flags are now disabled.
These checks do not establish every cancellation race or supported game state.

The editor still uses Python highlighting, tokenization, and error UI; native
parse warnings may remain visible even when marked TypeScript runs successfully.
Use the BepInEx log for TypeScript diagnostics. Step mode is unsupported; use Run
and Stop only. The only farm API is doAFlip. Source is limited to 32,768 characters,
Jint to 100 statements, 8 MB allocations and recursion 64, with a 30-second run
watchdog. Modules, imports, additional APIs, source-map UI, world-reset races,
and general-purpose runtime hardening remain future work.
