# TypeScript without a marker

The V8 diagnostic has an opt-in `[Language] TypeScriptForAllWindows = true`
setting. With `Enabled` and `RunSourceInputProbe` also true, every code window
uses TypeScript for ordinary Run, parser suppression, and compiler error popups.
Restart the game after changing these settings. The default remains false:
only windows beginning with `// @refactored` select TypeScript in that mode.

Existing markers remain valid TypeScript comments. Source is passed unchanged,
so diagnostic positions still refer to the original text. No saved code is
rewritten or migrated. Existing Python programs must be translated manually
before running in all-window mode; there is no automatic language detection or
fallback to Python after a compiler error. Disabling the option and restarting
restores marker selection.

This is still the bounded source-input diagnostic: only `doAFlip()` is bound,
Step is unsupported, and highlighting remains native. Native tutorial snippets
are not yet translated. There is no playable release or platform expansion.

## Manual validation (pending)

1. Enable all three settings and restart. Run `doAFlip();` without a marker:
   expect one flip and no Python error.
2. Add `const count: number = "two";` and Run: expect TS2322 at `count` and no
   flips. Correct the value to `2`, then Run: expect one flip.
3. Confirm a marked TypeScript window still runs normally.
4. Disable TypeScriptForAllWindows and restart: unmarked `if` should show the
   native Python error, while marked TypeScript still runs.

All-window mode is installed only through the existing checksum-gated source
probe. Reset clears the selection alongside editor error state.
