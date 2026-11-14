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
pass. In-game performance and lifecycle validation are pending; no measured speedup
is claimed yet. Other diagnostic modes retain their fresh-engine compiler behavior.
