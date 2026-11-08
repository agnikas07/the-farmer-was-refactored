# Read-only native simulation observation

This opt-in diagnostic uses a Harmony prefix on `Simulation.RunNextStep`. It never calls the scheduler itself, skips the original, or writes game state. It is specific to the Core.dll checksum recorded in the action-integration findings; a mismatch disables installation with an error.

## Observations and safeguards

The prefix briefly acquires the native simulation lock passed to RunNextStep, then releases it before the original can wait. It reads current simulation time, operation duration, native execution presence, and the managed thread ID. It never holds that lock across logging, a script reply, or a wait. This adds observation overhead; it is not a zero-overhead benchmark.

Only simulations whose runner has the exact MainSim type are observed, excluding UISim and other runners. A weak-key identity table assigns local generation numbers without retaining obsolete simulations. This does not yet establish which MainSim simulation is the current normal farm versus a leaderboard simulation; use the normal development farm for this test.

For one simulation identity, the probe requires two advancing time observations while idle and two while native execution is present, on a consistent thread different from the Unity thread captured at plugin startup. Snapshot logs contain only primitive values. Observation output is capped at 32 snapshots; callbacks catch errors rather than throwing into the game.

The main-thread poll logs results and removes this diagnostic's Harmony patches on completion/failure. Plugin destruction also removes the hook. No crop, inventory, speed, pause, operation-count, or script changes are made by the probe. Other mods' patches are not removed.

## Run the check

1. With the game closed, deploy using `bash scripts/deploy-dev.sh`.
2. Enable `RunSimulationProbe = true` in the local plugin config's Diagnostics section (default false).
3. Launch `bash scripts/launch-dev.sh`, enter the isolated development farm, and wait a couple of seconds with no program running.
4. Run a short built-in-language program in a code window. For a fresh save, use several consecutive `do_a_flip()` calls; this avoids requiring loop unlocks or harvesting crops. Re-run if the program is too short to observe advancing execution time.
5. Close the game and inspect `rg 'Simulation probe' .tools/game/BepInEx/LogOutput.log`.

Expected final marker:

```text
Simulation probe: distinct owner thread and advancing idle/running time ALL PASS
```

Snapshots should show both `executing=False` and `executing=True` for the same generation, with `thread` different from `unityThread`. Hook installation alone is not success. Staying at the title screen or never running code can leave the probe waiting indefinitely; it does not force game activity. Disable the setting after verification.

The user's native test program can naturally animate the drone and advance simulated time; the observer itself only reads. All activity remains in the isolated development copy/prefix.

## Validation and limits

Release build passes with zero warnings/errors. Unity/Mono verification passed on CachyOS/Proton on 2026-11-07, confirmed by the user screenshot and fresh on-disk log. Generation 1 stayed on simulation thread 16, separate from Unity thread 1; time advanced while idle and executing native code, with operation duration 2,500,000 ns. The final ALL PASS marker was present. The local diagnostic was disabled after verification. Existing runtime/stepping/dispatch desktop diagnostics remain separate checks, not proof of this Harmony hook.

A passing result confirms an observation point and thread/time behavior for this build. It does not establish a safe command-injection point, native timing parity, lock-order safety for mutations, save/reset cleanup, or cancellation during a real farming action. Those require subsequent narrowly scoped integration tests.
