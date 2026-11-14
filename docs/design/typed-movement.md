# Typed movement

`move(direction: Direction): boolean` accepts the string literals `"north"`,
`"east"`, `"south"`, and `"west"`. Direction is a TypeScript type, not a runtime
object. For example: `const heading: Direction = "east"; move(heading);`.
Misspelled directions and wrong argument types are compile errors. The runtime
also rejects non-string values and unknown directions before queuing a command,
including values that bypass TypeScript checking with `any`.

A ticket carries the immutable direction string across the existing mailbox.
The simulation adapter explicitly maps it to a native PyGridDirection and calls
the native move binding, preserving dependency/unlock checks. No game objects
cross into Jint. Completion returns the native boolean only after the native
scheduler applies movement and its costs. The game owns wrapping, blocked moves,
hat behavior, and world-specific movement rules. Stop cannot undo an already
applied movement. This does not resolve computation timing or COMP-001.

## Validation

Desktop checks cover all four ticket directions and both boolean results,
completion gating, stale tickets, cancellation before/after claim, and rejection
of invalid runtime arguments without a pending ticket. Compiler tests cover the
Direction union, return type, argument count, and conditional behavior. Native
direction mapping, costs, wrapping, and blocked/locked movement need game tests.

## Manual checks (pending)

With movement unlocked, restart with source mode and all-window mode enabled:

```typescript
if (move("north")) {
  doAFlip();
}
```

Expect native north movement and a flip if the move succeeds. On a one-tile farm,
wrapping may leave the drone on the same tile; use the `move(north)=...` log to
confirm completion. If movement is locked, keep that as a separate unlock check,
not a movement success. Do not change progression just to make a test pass.

Follow up with the other directions on a farm where position changes can be
observed, a blocked move when available, invalid direction compile rejection,
and Stop/recovery. Record unavailable scenarios explicitly. This increment adds
movement; the traversal lesson follows separately after the API is verified.

Manual progress (2026-11-14): user unlocked the first farm expansion and confirms
north movement plus one flip. Fresh log confirms move(north)=true at 200 operations
and worker release. Other directions and blocked/locked cases remain pending.
A separate continuous-harvest attempt hit the existing 100-statement runtime cap;
tracked as RUN-001, not a movement failure.

Manual follow-up: user confirms the development save loads as 4×4 and east,
south, and west movement each perform the expected move plus a flip. Fresh log
confirms those directions return true at 200 operations. Screenshot confirms
TS2345 highlights "up"; the log confirms rejection before actions and subsequent
east movement plus flip with worker release. Together with the earlier north
check, all direction success paths are verified. Blocked/locked moves, explicit
edge wrapping, and movement-specific Stop remain unverified in-game; false
results and cancellation are desktop-tested. Local diagnostics are disabled.
