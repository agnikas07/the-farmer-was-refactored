# Runtime error popups

Source runs now queue Jint execution failures for the existing code-window error
UI. This includes the computation budget, player-thrown exceptions, and engine
resource limits. The original exception still reaches the diagnostic log.

The worker queues the error; Unity applies it only if the window, captured source,
and run version still match. Editing or starting a new run clears the error.
Stopped/canceled execution does not report a runtime popup. Compilation retains
its existing precise TypeScript diagnostic spans.

Runtime source mapping is not implemented. The marker anchors at character zero
and the message explicitly says the exact source location is unavailable.
Messages are capped at 600 characters and angle brackets are replaced to prevent
player-thrown text from being rendered as rich-text tags. Native simulation-side
failures and compiler-host infrastructure failures are outside this change.

## Validation

Both locked plugin builds and desktop runtime suites pass. New checks exercise
actual Jint computation exhaustion and thrown errors, exception preservation,
same-engine recovery, cancellation suppression, successful execution, bounded
popup text, and preservation of compiler diagnostics.

Manual Unity/Proton checks are pending:
1. Restart and run `while (true) {}`: expect a computation-limit popup.
2. Replace it with `doAFlip();`: expect the popup to clear and one flip.
3. Run `throw new Error("test failure");`: expect its message in the popup.
4. Stop a continuous farming loop: expect no runtime error.

Rapid edit/new-run and window-close delivery races remain manually unverified.
