# TypeScript compiler inside Unity/V8

This extends the separate V8 diagnostic after PR #18 verified native loading
under Unity/Mono on CachyOS/Proton. All checks passed in-game on 2026-11-07, confirmed by the user screenshot and fresh BepInEx log. Compiler load took 353 ms; individual compilations took 0–342 ms. The local diagnostic was disabled afterward.
The [Jint pipeline mode](v8-to-jint-game-probe.md) separately checks emitted code in Jint.
The same C# check implementation passes in the Linux x64 desktop V8 probe.

## What it checks

- Load the unmodified pinned TypeScript 6.0.3 compiler and existing virtual host.
- Discover the expected ES2020 library references.
- Compile a typed camelCase two-flip loop and preserve source-map source text.
- Execute emitted JavaScript in a separate V8 engine with a counting doAFlip stub.
- Reject a number/string assignment with TS2322 on original line 2, without output.
- Reject snake_case API calls and module syntax, without output.
- Successfully compile and execute another two-call program after those errors.

Everything runs on the existing worker. Unity Update only drains progress logs.
Each case reports elapsed milliseconds. No CLR objects are exposed to scripts,
no farm actions occur, and this does not yet connect compiler output to Jint or
the native action adapter. There is no editor or player-source integration.

## Build and run

Close the development game, then from the repository run:

```bash
npm ci
bash scripts/deploy-v8-probe.sh
```

Node is used only during development packaging to generate the trusted JSON
bundle. The game reads this file beside the diagnostic DLL; it never launches
Node. The bundle contains compiler text, standard declarations, the virtual
host adapter, and farm declarations. Generated artifacts stay out of Git;
TypeScript license and third-party notices are staged with the bundle.

In `.tools/game/BepInEx/config/io.github.agnikas07.thefarmerwasrefactored.v8probe.cfg`,
set both `Enabled = true` and `RunCompilerProbe = true` under `[Diagnostics]`.
The latter defaults to false; add it if the previous loader-only config lacks it.

```bash
bash scripts/launch-dev.sh
# Wait at the title screen for about 15 seconds, close the game, then:
rg 'V8 game compiler probe' .tools/game/BepInEx/LogOutput.log
```

Require all checks and `V8 game compiler probe: ALL PASS`. The older
`V8 loader probe: ALL PASS` only verifies loading, not compilation. If the new
marker is absent, inspect the full log for loading failures. Disable both
settings after validation.

This is a bounded set of trusted fixtures, not a production compiler service.
Shutdown is checked between cases; an active compiler call is not interrupted.
Cancellation/timeouts, memory budgets, diagnostics UI, source-map stack remapping,
and other operating systems remain follow-up work.
