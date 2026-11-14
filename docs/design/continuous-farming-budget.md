# Continuous farming budget

Source-input runs allow 10,000 Jint statement checks before the next completed
farm call. A returned false value counts as completion: polling canHarvest must
not exhaust a lifetime statement counter while waiting for grass. The counter
resets on the JavaScript worker after native scheduling finishes the action cost,
never when a request is merely queued or claimed. Failed/canceled calls do not
reset it.

Source mode no longer has the fixed 30-second watchdog. Stop still cancels the
worker and releases a pending mailbox request. The existing 8 MB allocation and
64-level recursion limits remain per run. These are prototype guardrails, not
game-operation timing or an assurance of indefinite execution. Fixed feasibility
probes retain their original 100-statement / 30-second limits.

A computation-only loop fails with an explicit computation-limit message in the
BepInEx log. Runtime-error popups remain a separate follow-up.

## Validation

Desktop checks cover 12,000 completed false queries, exhaustion after those calls
in the same execution, computation-only exhaustion and fresh execution recovery,
failed/canceled requests not renewing the budget, and Stop during a farm loop.
Existing mailbox, movement, stepping, cancellation, and dispatch checks pass.

Unity/Mono checks passed on CachyOS/Proton: user confirmed the requested
40-second harvest/Stop check and same-session one-flip recovery. The game log
confirmed repeated harvests and worker release; user-pasted output confirmed
computation-limit termination and worker release. Duration is user-reported.

To repeat with source mode and all-window TypeScript enabled, restart and run:

```typescript
while (true) {
  if (canHarvest()) {
    harvest();
  }
}
```

Let it run for at least 40 seconds, checking for repeated harvests, then press
Stop. Confirm worker release and no statement-limit/watchdog termination.
Next, test a computation-only loop and recovery with a one-flip program. These
manual checks do not establish world-reset races, final timing parity, or
unlimited-duration memory behavior.
