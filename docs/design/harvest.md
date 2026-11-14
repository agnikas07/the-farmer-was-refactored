# Harvesting from TypeScript

`harvest(): boolean` uses the existing native harvest binding and scheduler.
It returns true when the native game harvests an entity and false when no entity
exists. True is not a guarantee of a ripe crop or a particular yield: call
`canHarvest()` first when readiness matters. The adapter does not invent a
readiness check or implement harvesting directly.

The native scheduler applies actionOps on success and operationOps on failure.
The mailbox releases the worker only after scheduler completion, carrying the
native boolean. Dependencies/unlocks, inventory collection, and field changes
remain owned by the game. Logs include harvest result, charged operations, and
separate harvest/query/flip totals. Stop can prevent an unclaimed action but
cannot undo a harvest already applied by the game.

## Validation

Automated checks cover both boolean results controlling a flip, harvest followed
by a query, completion gating, stale tickets, cancellation before/after claim,
TypeScript return/argument typing, and embedded V8-to-Jint execution with a stub
field. These do not establish native field behavior or costs. Manual testing is
pending on the isolated Unity/Mono game under CachyOS/Proton; other platforms
remain untested.

## First manual check

Restart with source mode and all-window TypeScript enabled. Over mature grass:

```typescript
if (canHarvest()) {
  if (harvest()) {
    doAFlip();
  }
}
```

Expect grass collection and one flip. Check the log for canHarvest=true,
harvest=true, native costs, and worker release.

Next try `harvest(); if (!canHarvest()) doAFlip();` to revisit the deferred false
query case. Grass may regrow before the next query: inspect the result log rather
than assuming a particular result. A stable empty tile, when available, is needed
to establish harvest=false. Do not count regrowth as an adapter failure or claim
unobserved false cases passed. Type errors should still reject before actions.

The next separate increment replaces a small introductory in-game lesson with
TypeScript examples. This PR adds no lesson UI, movement, or wider farming APIs.
