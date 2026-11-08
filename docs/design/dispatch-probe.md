# Worker-to-main-thread dispatch probe

A single-slot mailbox transfers integer requests/results between the JavaScript worker and its owner thread. No Jint values, engine objects, or Unity objects cross this boundary. This is a diagnostic transport, not a complete game-action scheduler.

## Behavior and checks

- JavaScript invokes two synchronous-looking host calls; the worker waits while the main thread pumps one short handler per update. Returned results sum to 43.
- The Unity handler reads `Time.frameCount` on the owner thread and returns the input plus one. It does not change the farm.
- A deliberate handler exception is returned to the waiting worker and checked there.
- A queued request is cancelled before claiming; its handler must never run.
- Desktop tests also reject pumping from a non-owner thread, waiting from the owner, and adding a second outstanding request.

The request state atomically distinguishes queued, claimed, and cancelled-before-claim. Cancellation releases the worker. If the main thread has already claimed the request, its action may still run; cancellation cannot roll back an executed action. Host handlers must be short and nonblocking because they execute on the main thread.

`DispatchProbe.Tick` polls completed tasks and pumps the mailbox without waiting. Disposal cancels outstanding work and disposes the token source only after the current worker completes. The diagnostic has a ten-second watchdog. Frame updates are only a dispatch mechanism, not game tick accounting.

## Validation

Run `bash scripts/test-runtime.sh`. Desktop and Unity/Mono checks passed on 2026-11-07. The user launched the isolated game on CachyOS/Proton; their screenshot and agent inspection of the fresh BepInEx log confirm all three dispatch checks and `Dispatch probe: ALL PASS`. Other platforms and actual farming actions remain untested. Existing runtime and stepping checks remain unchanged and pass.

With the development game closed, deploy and enable `RunDispatchProbe = true` under `[Diagnostics]` in the local BepInEx plugin config. Launch the isolated game, wait for the menu, close it, and inspect:

```bash
rg 'Dispatch probe' .tools/game/BepInEx/LogOutput.log
```

Expect three PASS checks and `Dispatch probe: ALL PASS` in the fresh log. The setting defaults to false; turn it off after verification. Earlier diagnostics may remain disabled.

## Remaining work

Integrate actual game action/timing APIs only after inspecting their semantics. Long actions need completion scheduling, rather than blocking an Update handler. Multi-drone fairness, shutdown races during real actions, error/source mapping, and pause-aware budgets remain open. These diagnostic probes do not yet compose into a player script runner.
