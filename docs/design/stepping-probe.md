# Pause/resume and stepping feasibility

This diagnostic explores Jint 4.17.0 debugger callbacks as cooperative checkpoints. It is not the production scheduler and adds no player controls.

## Ownership and control

A dedicated background thread creates, executes, and disposes each Jint engine. A debugger callback blocks that thread at a source checkpoint until an auto-reset event releases one step. Resume allows subsequent callbacks through; pause requests a stop at the next eligible callback. A cancellation token stops both running code and a waiting checkpoint.

The controller never reads Jint objects across threads. It receives copied source-line numbers and integer callback values. The Unity integration runs the diagnostic controller in a task and polls it from a coroutine; it does not wait or join worker threads on the main thread. Unity logging stays in the coroutine. Plugin destruction requests cancellation.

## Checks

- Start paused before line 1 executes, then advance one source statement at a time.
- Preserve a variable across pauses (41), then resume to completion (42) without restarting.
- Pause an actively running computation-only loop and stop while paused.
- Stop a computation-only loop while running.

Handshakes and joins have two-second bounds; the whole diagnostic has a ten-second cancellation deadline. These bounds are diagnostic failure detection, not a production time-budget model. Blocking host/native operations cannot be forcibly interrupted by these cooperative controls.

Run `bash scripts/test-runtime.sh` for both runtime and stepping diagnostics. The staged netstandard assemblies passed on desktop .NET on 2026-11-07. Unity/Mono validation is pending.

## In-game verification

With the development game closed, deploy with `bash scripts/deploy-dev.sh`. In `.tools/game/BepInEx/config/io.github.agnikas07.thefarmerwasrefactored.cfg`, set `RunSteppingProbe = true` under `[Diagnostics]` (default false). Launch with `bash scripts/launch-dev.sh`, wait for the menu, close it, then inspect:

```bash
rg 'Stepping probe' .tools/game/BepInEx/LogOutput.log
```

Expect four PASS checks followed by `Stepping probe: ALL PASS` in the new log. Watch for freezes/errors; turn the diagnostic off after verification. The previous runtime probe can stay disabled.

## Limits and next experiment

Checkpoint frequency and debugger overhead are unmeasured; Jint stepping points are not game ticks or final TypeScript source locations. The private control helper assumes one diagnostic controller and is not a general concurrent UI control API. Multi-drone scheduling, nested call-stack cases, pause-aware runtime budgets, main-thread game-action dispatch, and TypeScript source maps require separate work.

Source reference: https://github.com/sebastienros/jint/blob/v4.17.0/Jint/Runtime/Debugger/DebugHandler.cs
