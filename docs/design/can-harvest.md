# First farm query: canHarvest

`canHarvest(): boolean` reports whether the entity under the drone is ready to
harvest. It does not harvest anything. Only `doAFlip` and `canHarvest` are bound
in this increment; `harvest` and the first rewritten lesson are separate steps.

The worker sends a CanHarvest ticket through the existing per-run mailbox. The
simulation adapter calls the native builtin, records its dependency, and yields
to the native scheduler. That scheduler evaluates the query, applies the native
operation cost, and provides a PyBool. The adapter validates completion and sends
only a CLR boolean back to Jint. No game object is exposed to the worker. Native
unlock checks are retained; testing requires a save with the query unlocked.

Both false and true are real results, not success flags. The same ticket identity,
owner-thread, capacity, cancellation, and completion gates apply as for flips.
Stop cancels a waiting query. Action logs distinguish query results from flips;
source completion reports separate counts. This does not resolve computation
costs, world-time freezing while waiting, or COMP-001.

## Validation

Locked plugin builds, 14 compiler tests, and runtime/mailbox suites pass. Desktop tests cover boolean-controlled Jint branches,
false/true result transport, delayed completion, stale tickets, query cancellation,
and strict TypeScript return/argument checks with no snake_case alias. The game
adapter still requires manual verification; desktop stubs cannot prove native
unlock handling, costs, or field state.

## Manual checks (pending)

With source mode enabled and canHarvest unlocked, stand over a mature crop:

```typescript
if (canHarvest()) {
  doAFlip();
}
```

Expect one flip, the crop unchanged, and a log entry `canHarvest=true` with the
native operation cost (one operation in the inspected normal simulation).
On an empty or unready tile, the same code should report false and perform no flip.
Use a stable empty tile if growth could change the result during startup.
Also verify query failure/type errors do not cause later flips, and ordinary
one-flip scripts still work. Native Windows/macOS are not yet tested.
