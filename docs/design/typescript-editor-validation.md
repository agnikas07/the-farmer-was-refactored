# Keep Python validation out of TypeScript windows

While source-input mode is enabled, code beginning with the exact first line
`// @refactored` uses the TypeScript execution path. Previously the editor still
parsed that text as Python when committing edits, showing errors around `//`.

The source-input diagnostic now installs two additional checksum-gated patches:

- CodeWindow.Parse skips Python syntax-tree creation for marked windows. Native
  text/undo bookkeeping still runs. The previous Python program, parse exception,
  and parsed functions are cleared under the native parse lock.
- CodeWindow.Update closes native error indicators for marked windows on Unity's
  thread, including a popup already open when the marker was added.

The same marker predicate selects execution and validation. Unmarked windows use
the original parser. The dirty flag remains set for marked windows so removing
the marker forces a fresh native parse rather than reusing a cached program.
All patches are removed when the diagnostic is disposed.

This is not TypeScript highlighting or inline diagnostics. Compiler errors still
appear in BepInEx logs after Run. Native error popups are suppressed for marked
windows, including native runtime error UI; consult the log. TypeScript windows
cannot serve as native Python import modules. The marker requirement remains.

## Manual checks

With the game closed, deploy using `bash scripts/deploy-v8-probe.sh` and enable
Enabled plus RunSourceInputProbe in the V8 diagnostic config. Other modes stay off.
Launch with `bash scripts/launch-dev.sh`.

1. In a normal farm, enter `// @refactored` followed by `doAFlip();` on the next
   line. Click outside the editor, hover the marker, then Run. Expect no Python
   error popup and exactly one flip. The log should report one completed call.
2. Replace the window with invalid native Python (`if`) and commit the edit.
   Expect the native error indicator. Add the TypeScript marker above it and
   commit again; expect the Python indicator to close. This does not make `if`
   valid TypeScript: Run should report a compiler error in the log, with no flip.
3. Restore the valid marked one-flip program and Run. Expect recovery.

Inspect `rg 'TypeScript source probe' .tools/game/BepInEx/LogOutput.log` after closing.
Disable source-input mode after verification.

Locked Unity build: zero warnings/errors. Existing desktop runtime, stepping,
mailbox, and dispatch checks pass. On 2026-11-13 the user confirmed no Python popup and one flip, native error
restoration after removing the marker, and popup removal plus successful Run
after restoring valid marked TypeScript. The fresh BepInEx log confirms one
completed call at 400 operations and worker release. Desktop checks do not test
the native editor patches. All local diagnostic flags were disabled afterward.
