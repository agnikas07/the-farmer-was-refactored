# JavaScript runtime feasibility probe

Jint 4.17.0 is pinned as a candidate, with transitive dependencies locked. This diagnostic does not select the final scheduler or add TypeScript support.

## Checks

- Evaluate array mapping/reduction and verify a host logging callback.
- Terminate a computation-only infinite loop via a 100-statement budget.
- Start a timer from a host callback, then interrupt a running infinite loop via cancellation from another thread. A separate two-second timeout is a failure fallback, not a passing cancellation result.
- Create a fresh engine and execute successfully after cancellation.

Each probe engine has memory, recursion, and time limits. No broad CLR access or game actions are exposed. Cancellation destroys the current execution; it is not pause/resume. These cooperative limits do not preempt a blocking host callback or guarantee a hard deadline during parsing/native work.

## Desktop test

Run `bash scripts/test-runtime.sh`. It builds the plugin and runs the staged .NET Standard DLLs in a .NET 10 console harness, with a 15-second outer process deadline. This avoids accidentally testing NuGet's different net10 Jint assets. It does not prove Unity/Mono compatibility.

## In-game check

Close the development game, run `bash scripts/deploy-dev.sh`, and enable the opt-in BepInEx setting in `.tools/game/BepInEx/config/io.github.agnikas07.thefarmerwasrefactored.cfg`:

```ini
[Diagnostics]
RunRuntimeProbe = true
```

Run `bash scripts/launch-dev.sh`, wait for the menu, then close it and inspect:

```bash
rg 'Runtime probe' .tools/game/BepInEx/LogOutput.log
```

All four checks and `Runtime probe: ALL PASS` must appear in the new log. A `FAILED` entry or absent marker requires investigation. The config defaults to false; turn it off after testing. The small diagnostic runs synchronously at startup and can delay startup up to its execution limits. It is not a player script runner.

Desktop probe: passed on 2026-11-07. Unity/Mono probe: pending. The previous startup-only verification does not cover these new dependencies.

## Next gate

After Unity compatibility is verified, investigate safe pause/resume, stepping, tick accounting, and scheduling game actions on the main thread. Avoid extending this diagnostic into a production execution architecture by accident.

## References

- https://github.com/sebastienros/jint/tree/v4.17.0
- https://github.com/sebastienros/jint/blob/4.x/README.md

Before distributing binaries, include the license/notices for Jint (BSD-2-Clause), Acornima (BSD-3-Clause), and System.Runtime.CompilerServices.Unsafe (MIT); the current change stages local development binaries only.
