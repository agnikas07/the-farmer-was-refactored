# The Farmer was Refactored

A planned TypeScript language-system overhaul for **The Farmer Was Replaced**, including the in-game editor, execution controls, documentation, and tutorials.

## Status

The startup plugin loads successfully in the isolated development game. TypeScript execution and editor integration are not implemented yet.

Startup has been verified on CachyOS Linux using the Windows game through Proton. Windows and macOS are intended targets but have not been tested.

## Development

- Git and Node.js/npm are available on the development machine.
- The local .NET SDK lives in `.tools/dotnet` (not tracked).
- Run `source scripts/dev-env.sh` from the repository root to expose the local SDK in your shell.
- Run `dotnet --info` to verify the SDK.

The plugin targets .NET Standard 2.1 and BepInEx 5.4.23.6. The SDK version is not the plugin runtime requirement.

See [the development guide](docs/development.md) for setup, building, launching, and checking the startup log.

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
