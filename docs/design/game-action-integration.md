# Game action and simulation integration findings

Status: static inspection complete; adapter design is proposed, not implemented or runtime-verified.

## Evidence and reproduction

Inspected the local development copy of Steam build `26462009`, Unity `6000.0.43f1`, on 2026-11-07. Gameplay/interpreter code is in `Core.dll`; `Assembly-CSharp.dll` is not the main gameplay assembly in this build.

`Core.dll` SHA-256: `f0efda7c138de8028b1769c121a4b5c17b6bb538aeae5a67d1881e7c4ebd7d25`.

Inspection tool: ILSpy CLI `11.2.0.10050`, installed from NuGet into `.tools/ilspy`. With the local SDK configured, run `bash scripts/inspect-game.sh` to reproduce the selected type inspection and checksum. Output remains in ignored `work/inspection`; do not publish game binaries or decompiled source. This document summarizes behavior and names integration points without copying implementation bodies.

These findings describe this specific build, not a supported public mod API. Recheck signatures and behavior after game updates.

## Critical correction: the simulation has its own thread

`MainSim.Start` starts a background thread running `MainSim.SimulationLoop`. That loop calls `Simulation.RunNextStep` with a goal time and the shared simulation lock.

`MainSim.Update` runs on Unity's main thread. Under the same lock, it advances the goal time from frame delta and `TimeFactor`, exchanges presentation/state information, and pulses the simulation thread.

Therefore, PR #5 proved request/result transport and safe main-thread access to a Unity property. It did **not** prove that gameplay mutations should run in our plugin's `Update` method. A call from Unity's main thread can still race the simulation if it bypasses its synchronization.

Use three distinct roles in the proposed design:

- JavaScript worker: owns the Jint engine and script state.
- Simulation execution path: owns ordered gameplay effects, time, and operation accounting.
- Unity main thread: owns editor controls and presentation; it must not wait for JavaScript or simulation request completion.

Core effect code routes sounds/effects through `ISimRunner` and render data. Do not replace that path with direct worker calls into Unity.

## Native execution path

| Stage | Verified symbols | Responsibility |
| --- | --- | --- |
| User starts code | `CodeWindow.PressExecuteOrStop`, private `StartExecution`, `Parse` | Stops the existing run, parses code, and starts a native execution. |
| Execution creation | `MainSim.StartMainExecution(CodeWindow, Node, bool)` | Locks simulation state, updates editor mode, constructs `Execution`, and starts it. |
| Scheduling | `Simulation.RunNextStep`, `Execution.Execute` | Orders timers/execution, advances simulation time, checks paused/stopped state. |
| Built-in call | `CallNode.Execute`, `PyFunction.binding`, `BuiltinFunctions` | Records unlock dependencies and stages side effects/arguments. |
| Effect application | private `Execution.ApplySideEffect(int)` | Applies drone/world effects, stores return values, and charges operations. |
| Per-drone state | `ProgramState` | Tracks pending effect, result, operation count, dependencies, and execution stack. |
| Stop/cleanup | `MainSim.StopMainExecution`, `Simulation.StopProgramExecution` | Stops execution, resets speed/step state, removes spawned drones, and closes logging. |

`Execution` is a concrete native-interpreter class constructed with a `Node`; it is not an existing generic JavaScript scheduler interface. Merely calling `StartProgramExecution` with a new object is not an adapter solution.

## Timing and accounting

- `Simulation.actionOps` and `operationOps` are instance values. Constructor defaults are 200 and 1; do not assume every simulation uses those defaults.
- `Simulation.OpDuration` begins at 0.0025 seconds. `ChangeExecutionSpeed` adjusts it using the speed factor and nanosecond rounding.
- `MainSim.TimeFactor` scales the simulation's goal-time advance. It is distinct from execution speed and operation cost.
- `Execution.Execute` advances per-drone work, processes queued unlock dependencies with `Farm.AssertUnlocked`, applies eligible effects under the simulation lock, and updates global progress.
- Effects can mutate the world before the operation cost has elapsed. The next script work is gated by native operation scheduling; do not repeat the mutation when completing its delay.
- `ProgramState.ConsumeOps` and the execution loop contribute to power consumption. Charging a wall-clock delay alone misses this bookkeeping.
- `Execution.NextExecutionTime`, `Simulation.CurrentTime`, and simulation timers control readiness. `Task.Delay`, Unity frame counts, and debugger checkpoints are not substitutes for this clock.
- `get_tick_count` returns the calling state's operation-count delta from its starting count, not a Unity frame counter.
- Speed changes can reschedule ongoing digging work. A single fixed duration captured for every action is not yet justified.

## First-action behavior

| Player command | Verified effect path | Return / cost considerations |
| --- | --- | --- |
| `can_harvest()` | Built-in stages `SideEffect.CanHarvest`; effect calls `Drone.CanHarvest` | Boolean; charges `sim.operationOps`. |
| `harvest()` | Built-in stages `SideEffect.Harvest`; effect calls `Drone.Harvest` | Boolean; successful branch charges `sim.actionOps`, false branch `sim.operationOps`. `Drone.Harvest` returns false if no entity exists. |
| `move(direction)` | Built-in validates direction, stages `SideEffect.Move`; effect calls `Drone.Move` | Boolean plus an operation cost returned by `Move`; blocked movement begins with `sim.operationOps`. |
| `get_pos_x/y()` | Effect path reads the current drone position | Useful first read-only observations; retain the native call's unlock and operation rules when exposed to players. |

Movement consults hats and ground controllers, handles wrapping/restrictions, and invokes entity callbacks. Its returned cost may differ from the default action cost. Avoid implementing movement by assigning coordinates or hard-coding 200 ticks.

Harvest collects items and triggers animation/sound paths. A successful dispatch is not proof that the full action delay, power usage, progression checks, or stop behavior were preserved.

Unlock checks occur in the native call/execution pipeline, not solely inside the low-level drone methods. An adapter that calls those methods directly must not silently grant locked functions.

## Locks, pause, and lifecycle

`MainSim.sim` and `MainSim.lockSimulation` are private. `Execution.lockExecution` also coordinates native program state. Any reflection/patch strategy must be narrow, version-checked, and tested; this inspection does not justify assuming a public safe accessor exists.

`Simulation.RunNextStep` can use `Monitor.Wait` when paused or caught up to the goal time. Calling it from Unity's main thread could block the UI; do not use it as a convenience pump.

The execution path uses both execution and simulation locks at different stages, while stop operations can acquire execution state under the simulation lock. Do not add a lock-order reversal. Never wait on a script callback or hold a lock across a cross-thread reply.

`Simulation.Paused`, native step mode, and the Jint diagnostic checkpoint gate are separate mechanisms. A future pause implementation must define how they interact, including ongoing actions and timers.

World resets and simulation/leaderboard transitions can replace `MainSim`'s active simulation. Requests need a run/simulation generation identity so a delayed response cannot mutate a replacement world. Native stop cleanup must be preserved, not bypassed by cancelling only the Jint token.

## Proposed next PR: read-only ownership probe

Before writing a farming binding:

1. Resolve and validate the required type/member shapes against the running build. Disable the diagnostic with a clear error on mismatch.
2. Add a narrowly scoped observation hook on the normal simulation execution path. Do not call `RunNextStep` yourself or hold its lock across a wait.
3. Observe simulation-thread identity, simulation identity, current time, and native execution presence. Copy primitive snapshots to the main thread for logging.
4. Verify observations while idle and while a short built-in program runs in the isolated save. Do not mutate crops, inventory, operation counts, or pause state from the probe.
5. Confirm no hook takes ownership of unrelated UI simulations or persists after plugin teardown. Use the evidence to select the command scheduling hook.

The exact hook and snapshot synchronization remain implementation questions. This is a plan for a runtime test, not a claim that the static inspection proved thread safety.

## Following implementation decisions

Prefer reusing the native side-effect/accounting path if a small adapter can preserve its invariants. Alternative: a dedicated scheduler that reproduces them explicitly; that requires more tests and maintenance. Neither approach is selected yet.

A future command lifecycle needs accepted, applied, waiting-for-simulated-completion, completed, cancelled, and failed distinctions. Cancellation before application should prevent the effect; cancellation afterward cannot undo it. Return values must not accidentally allow the next command to skip the action's remaining cost.

Test native-versus-mod behavior for successful/failed actions, locked functions, speed changes, power usage, pause/stop, and world replacement before expanding the API. Keep first integration to one drone and one command, with explicit limits.

JavaScript computation cost is still a product/design decision. Do not map every Jint debugger callback to one native tick without specifying and testing the semantics.
