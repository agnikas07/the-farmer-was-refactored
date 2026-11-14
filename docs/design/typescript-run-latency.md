# Run-to-action latency measurements

Source mode now emits `TypeScript source timing` records alongside its existing
behavior logs. Each run has a process-local ID and one monotonic timestamp origin,
captured on Unity's Run path immediately after reading the source. Elapsed times
are measured at the event, not when Unity later drains the log queue. Concurrent
records may appear out of timestamp order; compare elapsedMs within the same ID.

| Stage | Meaning |
| --- | --- |
| source_captured | Timing origin, before stopping the previous native run |
| simulation_entered | Custom node begins on the simulation thread |
| worker_started | Action worker begins, before compilation |
| bundle_loaded | Bundle read and JSON parsing finished |
| compiler_loaded | Fresh V8 engine, compiler, adapter and library inputs loaded |
| emit_finished | Compile/evaluate/JSON serialization returned, including rejection |
| compiler_disposed | Successful output parsed and compiler disposed |
| player_ready | Fresh Jint engine and callback ready to execute output |
| first_request | Player enters its first doAFlip callback |
| first_claim | Simulation claims the first mailbox request |
| first_binding_returned | Native binding returned its scheduling cost |
| first_action_completed | Native scheduling resumed and mailbox completion succeeded |
| worker_released | Cleanup continuation observes worker exit |

Subtract adjacent elapsed values to estimate phase durations. The native binding
is a useful execution milestone, not a measurement of the first rendered frame.
Action completion includes game-controlled action duration. This is wall-clock
time, not simulated time or operation accounting. Logging adds some overhead.
Rejected/stopped programs legitimately omit later milestones; no events are
fabricated to make a trace look complete. No player source is included in timing logs.

## Capture a baseline

With source mode enabled, launch the development game and run this three times in
the same session, waiting for each flip and normal completion before pressing Run:

```typescript
// @refactored
doAFlip();
```

Close the game and inspect:

```bash
rg 'TypeScript source (timing|probe)' .tools/game/BepInEx/LogOutput.log
```

Compare the first run with runs two and three. Each should still complete exactly
one call at 400 operations and release the worker. The compiler still starts fresh
on each run; no caching or optimization is included here. Measurements will guide
whether compiler reuse, bundle caching, or another phase deserves the next change.

Build/runtime/compiler regression checks pass. In-game baseline captured on
2026-11-13 under Unity/Mono on CachyOS/Proton, confirmed by user-pasted output
and the fresh BepInEx log. All three runs completed one call at 400 operations
and released their worker.

| Phase (milliseconds, differences between milestones) | Run 1 | Run 2 | Run 3 |
| --- | ---: | ---: | ---: |
| Capture to worker start | 27.42 | 0.68 | 0.55 |
| Bundle read/parse | 248.74 | 180.71 | 179.91 |
| Compiler setup | 353.57 | 224.29 | 210.01 |
| Type checking/emission/serialization | 281.06 | 262.28 | 270.24 |
| Result parsing/compiler disposal | 10.26 | 6.92 | 7.21 |
| Capture to native binding return | 1010.28 | 676.99 | 676.91 |

The approximately 990 ms from binding return to action completion is the native
action interval, distinct from startup latency. These are three observations,
not a statistical performance guarantee or first-rendered-frame measurement.
Repeated-run startup is dominated by bundle/compiler work. Next investigate
bundle/compiler reuse with explicit worker ownership; unchanged-source output
caching is another candidate, but must not reuse stale compiler/API inputs.
No optimization has shipped in this PR. Local diagnostics are now disabled.
