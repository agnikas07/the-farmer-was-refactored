# Native action adapter feasibility

This opt-in diagnostic substitutes a fixed custom Node for one marked program.
It tests whether a mod-owned command source can use native execution scheduling.
It does not yet execute TypeScript or accept JavaScript worker requests.

## Scope

- Installation checks the inspected Core.dll SHA-256.
- A prefix on MainSim.StartMainExecution replaces only the exact marked source
  below (CRLF and outer whitespace are normalized), with step mode off.
- The original start method still constructs Execution, sets editor state, and
  starts the scheduler. Ordinary programs keep their parsed syntax tree.
- The custom iterator requires a MainSim runner, normal-farm leaderboard type,
  one state, drone zero, and a thread different from Unity's startup thread.
- Each command queues the native do_a_flip dependency and calls its built-in
  binding. The native execution loop checks unlocks, applies the side effect,
  advances operation accounting, and decides when to resume the iterator.
- Completion requires a cleared effect, a native None result, a positive operation
  charge, and unchanged thread identity. It reports each charge and a final marker.
- No direct Drone calls, private ApplySideEffect invocation, worker waits,
  hand-written action delays, or simulated-clock writes are used.
- The probe replaces at most one matching start per launch, bounding its output.
  Native Stop retains ownership of cleanup. Disposal removes only our patch and
  prevents the iterator from issuing further commands.

The native flip uses AddAndConsumeOps: its charge is not treated like a harvesting
power charge. This probe deliberately reuses that behavior. Do not generalize a
flip pass into proof of harvesting power parity or JavaScript computation costs.

Core.dll and Utils.dll are local compile references with copying disabled.
Neither is shipped in artifacts. Direct native type references mean a future game
update may fail type loading before the checksum check; this is build-specific
development tooling, not a stable compatibility layer.

## Manual check

1. Deploy with the game closed. Enable RunNativeActionProbe in Diagnostics.
2. Launch the isolated game and enter the normal development farm.
3. Put exactly this in a code window and press ordinary Run:

   ```python
   # refactored-action-probe
   do_a_flip()
   ```

4. Expect two flips, then normal program termination. No loop unlock is needed.
5. Close the game and run:
   `rg 'Native action probe' .tools/game/BepInEx/LogOutput.log`
6. Require two completion lines and
   `Native action probe: two native actions completed ALL PASS`.
   At default speed, each flip should report chargedOps=400.
7. Disable the diagnostic after verification. Restart to retry: it is one-shot.

The marked source is a diagnostic trigger, not new player syntax. If Run stops
early, a dependency fails, or the world changes, absence of ALL PASS is not success.
The ordinary Stop button remains available. Broader pause/stop/reset and speed
parity tests are follow-up work; this first test only establishes sequential action
completion through the custom node.

## Validation

Release build: zero warnings/errors. Existing desktop runtime, stepping, mailbox,
and dispatch checks pass. Native custom-node verification passed on CachyOS/Proton
on 2026-11-07. The user screenshot and fresh on-disk log confirm selection of the
custom node, two completed flips on simulation thread 16, chargedOps=400 for each,
and the final ALL PASS marker. The local diagnostic is now disabled.
This does not establish a production scheduler, multiple-drone
support, source mapping, or a safe asynchronous worker request lifecycle.
