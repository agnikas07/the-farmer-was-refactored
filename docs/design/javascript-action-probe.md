# JavaScript-to-native action diagnostic

The existing RunNativeActionProbe switch now enables this successor to the fixed
two-flip probe. It replaces only this marked program, on ordinary Run:

```python
# refactored-javascript-probe
doAFlip()
```

This is a native-parser-compatible trigger. The actual worker script is fixed
JavaScript: doAFlip(); doAFlip();. There is no TypeScript compiler/editor yet.
The public binding is camelCase; the native adapter uses do_a_flip internally.

## Scheduling and lifecycle

A per-execution mailbox is created on the simulation thread. The worker owns Jint.
The custom node claims commands, uses the native dependency/binding path, and
acknowledges completion only after native action scheduling resumes the iterator.

When no command is available, the node marks a stopping point so ProgramState
returns rather than repeatedly advancing its iterator. A checksum-gated Harmony
transpiler inserts an early return at the outer Execution.Execute loop head,
outside both native locks/exception regions. The expected unique IL pattern is
validated; mismatches reject installation. Unrelated executions use the original
loop. No instruction bodies from the game are distributed.

The next RunNextStep call parks on the native simulation monitor for at most 10 ms
while awaiting worker input. Monitor.Wait releases the simulation lock; Unity's
normal pulse or the timeout wakes it. No Task wait/join occurs on Unity or the
simulation thread. No arbitrary operation charges or clock writes are added.
Unlike a production scheduler, this diagnostic temporarily suspends world timers
while the worker is unavailable. It does not establish computation-time parity;
catch-up behavior and pause/step semantics need further design.

Execution.StopExecution disposes the matching mailbox and cancels Jint. This runs
inside native cleanup, including normal termination. Plugin destruction also
cancels; neither path joins the worker. A continuation observes faults and disposes
the token source after exit. A 30-second watchdog bounds diagnostics, including a
worker stuck waiting on a native error; it is not an action timing mechanism.
One run is accepted per launch. No tickets transfer to replacement worlds.

## Check

1. Launch the isolated game with RunNativeActionProbe enabled; other probes off.
2. Enter the normal farm, paste the trigger above, and press ordinary Run.
3. Expect two flips and normal termination. Close and inspect:
   `rg 'JavaScript action probe' .tools/game/BepInEx/LogOutput.log`.
4. Require two doAFlip completion lines (400 operations each at default speed),
   `two doAFlip calls completed ALL PASS`, and `worker released`.
   A scheduler-yield line records an observed gap waiting for JavaScript.
5. For Stop verification, restart, run the same trigger, and press Stop during the
   first flip. Require worker released and no second completed action/ALL PASS.
   An already applied flip may finish animating; cancellation cannot undo it.
6. Disable the diagnostic after verification.

Release build and full desktop suite pass. New native hooks, worker sequencing,
and Stop behavior await Unity/Mono verification. The desktop suite validates the
mailbox and Jint independently, not this game patch. Broader world-reset, speed,
step-mode, multiple-drone, and mutation parity testing remain pending.
