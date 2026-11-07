# The Farmer was Refactored

A planned TypeScript language-system overhaul for **The Farmer Was Replaced**, including the in-game editor, execution controls, documentation, and tutorials.

## Status

Initial development setup. No playable mod is available yet.

Development takes place on CachyOS Linux. Windows, Linux, and macOS are intended targets; compatibility has not yet been verified.

## Development

- Git and Node.js/npm are available on the development machine.
- The local .NET SDK lives in `.tools/dotnet` (not tracked).
- Run `source scripts/dev-env.sh` from the repository root to expose the local SDK in your shell.
- Run `dotnet --info` to verify the SDK.

The plugin target framework and BepInEx version will be selected after inspecting the installed game. The SDK version is not the plugin runtime requirement.

## Structure

- `src/Plugin`: C# game integration.
- `src/Runtime`: JavaScript execution and scheduling integration.
- `src/Compiler`: TypeScript checking, compilation, and source mapping.
- `docs/in-game`: rewritten player documentation.
- `docs/design`: architecture decisions and milestones.
- `examples`: player-facing TypeScript examples.
- `tests`: compiler, runtime, and integration tests.
- `packaging`: release packaging.

Keep game assemblies, saves, credentials, and local tools out of Git. Runtime and compiler dependencies are not selected yet.
