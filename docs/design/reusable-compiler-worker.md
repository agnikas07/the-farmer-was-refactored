# Reuse the TypeScript compiler on one worker

The latency baseline showed about 390–405 ms per repeated Run spent reading the
bundle and loading the compiler. Source-input mode now starts one background
compiler thread when enabled. That thread reads the bundle once, creates V8,
loads TypeScript and its virtual library inputs, and owns the engine until shutdown.

Each Run submits an immutable source string to a bounded queue (one waiting request
plus one active request). Every request still creates a fresh TypeScript program
and performs semantic checking; emitted output is not cached. Changed source and
compiler errors cannot reuse a previous successful result. Each action run still
creates a fresh Jint engine. Only the action worker waits for compilation, never
Unity or the simulation thread.

Stop cancels that run's wait immediately. Canceled queued requests are skipped;
results of already active canceled compilations are discarded. V8 compilation
itself is not interrupted. Shutdown stops accepting requests, cancels waiting
callers, and lets the owner finish any active compilation before disposing V8.
There is no thread join on Unity. A full queue reports a retryable busy error.
Initialization failure is reported and rejects subsequent requests until restart.

The bundle/API inputs are fixed for the plugin lifetime: restart after rebuilding
or changing them. This is compiler reuse, not a production incremental language
service. Compilation still pauses simulated time via the diagnostic scheduler.

## Timing comparison

`TypeScript compiler worker: ready` reports initialization once per launch.
Per-run timings now include `compiler_request_started`, after any queue/initialization
wait, and `emit_finished`. The old per-run bundle/compiler load and disposal stages
are absent in source mode because that work belongs to the shared worker lifetime.
Continue comparing source capture to first native binding; native action duration
is separate. The first Run may still pay Jint initialization costs.

With source mode enabled, launch and run the same marked one-flip program three
times in one session, waiting for completion each time. Close and inspect:

```bash
rg 'TypeScript (source (timing|probe)|compiler worker)' .tools/game/BepInEx/LogOutput.log
```

Compare against the previous 1010.28 / 676.99 / 676.91 ms capture-to-binding baseline.
Also verify changing source to three calls, rejection/recovery after a type error,
and Stop still work before marking the PR ready. Disable source mode after testing.

Desktop tests pass for reuse, changed action counts, source-declaration isolation,
semantic rejection/recovery, cancellation while the owner is occupied, worker-thread
ownership, disposal, and initialization failure. Existing runtime/compiler suites
pass. In-game timing, changed-source, error/recovery, and Stop/recovery checks passed. Other diagnostic modes retain their fresh-engine compiler behavior.


## First in-game measurements (2026-11-13)

User-pasted output and fresh BepInEx log confirm one compiler initialization
(thread 8), three one-flip runs at 400 operations, and action-worker release.

| Capture to native binding return | Fresh compiler baseline | Reused compiler |
| --- | ---: | ---: |
| Run 1 | 1010.28 ms | 426.34 ms |
| Run 2 | 676.99 ms | 150.35 ms |
| Run 3 | 676.91 ms | 133.55 ms |

Repeated-run delay fell approximately 78–80% in this small sample. Compiler
checking/emission took 279.96 / 145.26 / 128.40 ms. Initial compiler loading
now happens before Run; this comparison does not measure total game startup.
The native action interval remained about one second. These observations are
not statistical guarantees or first-rendered-frame measurements. Action-worker
release does not prove compiler-thread shutdown; that lifetime remains distinct.
The subsequent pasted logs confirm three completed calls at 400 operations each,
TS2322 rejection on line 3 with no completed actions followed by successful
one-call recovery, and Stop followed by successful one-call recovery in the same
session. The final Stop/recovery result is also confirmed in the fresh on-disk log;
earlier runs were overwritten and are preserved as user-pasted evidence.
PR #26 is ready for review. All local diagnostic flags are now disabled.
Compiler-owner shutdown is covered by desktop tests; the game log only proves
action-worker release, not compiler-owner disposal during game exit.
