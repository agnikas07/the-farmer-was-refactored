# First milestone: a plugin that loads — verified

Completed on 2026-11-07. The user launched the isolated game on CachyOS/Proton; the BepInEx log confirms plugin startup and chainloader completion. See [environment details](environment.md).

1. Locate the installed game and record its version, Unity backend, and managed runtime.
2. Select the matching BepInEx build and plugin target framework.
3. Back up saves and use an isolated development profile.
4. Build a plugin that logs "The Farmer was Refactored loaded."
5. Verify it in the actual game before implementing language integration.

## Following technical gate

Jint evaluation/cancellation is verified in-game (PR #3). The [stepping probe](stepping-probe.md) and [dispatch probe](dispatch-probe.md) are verified in-game. [Static inspection](game-action-integration.md) shows a separate native simulation thread; next verify ownership with a read-only hook before farming integration. Simulation timing, main-thread-safe actions, source mapping, and drone isolation remain open before committing to the execution architecture.

Use the official TypeScript compiler with semantic checking. Prototype compiler hosting separately; players should not need a separate Node.js installation for the eventual release.

Documentation replacement is part of the first playable slice, not a final polish task. Define computation costs explicitly; JavaScript operations do not automatically match the original interpreter's ticks.
