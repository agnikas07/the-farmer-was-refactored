# TypeScript errors in the code window

Source mode now shows the first compiler error in the game's existing error popup
after Run. TypeScript emits UTF-16 start/length spans; a structured exception
carries the readable message and span through the compiler/action workers.
The complete diagnostics JSON remains in the BepInEx log.

The game adapter queues delivery to Unity. Results must match both the current
source snapshot and the most recent Run for that window. Editing or rerunning
invalidates the previous error. Closing a window or resetting the probe discards
its deliveries. Weak per-window state avoids retaining closed windows after
pending delivery is drained. Native Python errors remain suppressed only for
marked TypeScript windows; valid native windows retain their normal behavior.

A source span is clamped and converted to the native inclusive end index. When
an error has no main-source location, it is attached to the first character.
Only compiler errors are covered; runtime exceptions and infrastructure failures
remain log-only. This is validation on Run, not background validation while typing.
The TypeScript marker and existing diagnostic execution limits remain unchanged.

## Manual check

Launch with source mode enabled, then Run:

```typescript
// @refactored
doAFlip();
const count: number = "two";
```

Expect zero flips and a popup showing `TypeScript TS2322` with the string/number
assignment explanation, anchored at `count` on line 3. The marker must not produce
a Python error. Correct the assignment to `const count: number = 2;`: editing
should clear the stale popup, and Run should complete one flip.

Then verify changing the window to invalid native Python (`if`, without the
marker) still shows the native error after committing the edit. Restore marked
valid code and Run to confirm recovery. A rapid edit/new Run should not redisplay
an error for the older source.

## Automated evidence

Locked Unity builds, the 13 Node compiler tests, runtime/mailbox tests, and V8
compiler-worker suites pass. Added assertions cover original diagnostic spans,
CRLF and LF, Unicode before an error, inclusive end conversion, and span clamping.
Manual verification on 2026-11-13 under Unity/Mono on CachyOS/Proton:

- User screenshot confirms TS2322 highlights `count` and explains the mismatch.
- User confirms editing clears the popup and corrected code performs one flip.
- Screenshot confirms removing the marker restores the native Python error for `if`.
- User confirms restoring marked valid code clears the native error and performs one flip.
- Fresh BepInEx log confirms rejection without completed actions, followed by two
  successful one-call runs at 400 operations each and worker release.

Rapid edit/new-Run delivery races and window-close races have not been manually
verified; source/version guards were reviewed, but these remain coverage limits.
Local source-mode diagnostic switches were disabled after verification.
