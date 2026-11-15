![The Farmer was Refactored — a straw-hatted drone, floating code windows, and a field of golden crops](docs/assets/the-farmer-was-refactored.png)

# The Farmer was Refactored

**Grow crops. Write TypeScript. Refactor the farm.**

An experimental language-system overhaul for **The Farmer Was Replaced**.
The goal: play in TypeScript, with an editor, execution controls, and in-game
lessons that teach the new language while preserving the farming mechanics.

[Progress & plans](PROJECT_STATUS.md) · [Development guide](docs/development.md) · [TypeScript example](examples/two-flips.ts)

> 🌱 **Still growing.** This is a development prototype, not a playable release.
> You can run TypeScript, harvest grass, and patrol neighboring tiles through
> opt-in development settings. The editor overhaul is unfinished, and there is no mod-store package.

## A little taste of the harvest

The source-input diagnostic can compile this and make the drone flip twice:

```typescript
// @refactored
const flipCount: number = 2;

for (let index = 0; index < flipCount; index++) {
  doAFlip();
}
```

Two flips. Strong types. One very enthusiastic drone.

Public APIs use TypeScript-style camelCase: `doAFlip()`,
[`canHarvest(): boolean`](docs/design/can-harvest.md), and
[`harvest(): boolean`](docs/design/harvest.md). Both query results and successful
harvesting are verified in-game; a failed harvest remains desktop-tested only.
[Typed movement](docs/design/typed-movement.md), such as `move("north")`, is
verified in-game for all four successful directions and invalid-input recovery.
The `// @refactored` first line currently selects TypeScript execution; without
it, the game still uses its native language. It is a temporary diagnostic marker,
not part of TypeScript or the intended finished experience. With source mode
enabled, a single `doAFlip();` really does mean one flip. An opt-in
[all-window TypeScript setting](docs/design/typescript-default-mode.md) removes
the marker requirement after a restart; existing Python code is not converted.

TypeScript windows suppress Python parser errors and show compiler and runtime
error popups. Suggestions use camelCase APIs and TypeScript keywords; hovering a
farm API suggestion shows its signature and example. Syntax highlighting is still
Python-based, and completion is a small static vocabulary rather than semantic
IntelliSense. Runtime errors do not yet identify their exact source location. See the
[source-input test instructions](docs/design/typescript-source-input.md) for
setup, supported behavior, and limits; this is not a general-purpose farming mod yet.

The [First Program lesson](docs/in-game/first-program.md) teaches your first
harvest, continuous polling, and a two-tile patrol. Its rendering, patrol behavior,
Stop, and save/reload are verified in-game. Other documentation remains untranslated.

## What's sprouted?

| Piece | Where it stands |
| --- | --- |
| Plugin startup | Verified in the isolated development game |
| JavaScript → native actions | Two flips and Stop verified in-game |
| TypeScript checking | Strict type checking and source-map emission tested |
| Embedded compiler | TypeScript in V8 verified under Unity/Mono |
| Compiled TypeScript → Jint → native actions | Completion and Stop verified in-game |
| Actual code-window source | One/three flips, repeated Run, type-error rejection, recovery, and Stop verified |
| Editor errors, suggestions & suggestion help | Verified; highlighting and semantic completion unfinished |
| First Program lesson | Harvesting and two-tile patrol verified, including save/reload |
| Continuous loops | Native calls renew the computation budget; Stop and runaway protection verified |
| Full farming API, modules & drones | Planned |
| Playable release | Not yet |

The current test environment is **CachyOS Linux running the Windows game through
Proton**. Testing directly on Windows and macOS is still pending.
The working diagnostics do not yet establish full gameplay or timing parity.

## Pull on your development boots

For a fresh checkout, start with the [development guide](docs/development.md).
It covers the local SDK, game references, building, and an isolated game copy so
development stays separate from the normal installation.

Want to try just the compiler? With Node.js and npm installed:

```bash
npm ci --ignore-scripts --cache .tools/npm-cache
npm run test:compiler
```

That runs the compiler and lesson-example tests without launching the game. Once the .NET/game
development prerequisites are set up, the
[TypeScript-to-Jint check](docs/design/typescript-jint-pipeline.md) tests the emitted
JavaScript against the staged runtime.

Node is a **development tool** used to prepare the compiler bundle. The verified
in-game diagnostic hosts TypeScript in ClearScript/V8 and executes the emitted
JavaScript in Jint; the game does not launch Node. Release packaging and the final
runtime choice remain open. Jint 4.18.0 fixes the previously observed labeled-loop
issue; regression checks and in-game loop tests pass. See
[project status](PROJECT_STATUS.md) for remaining correctness work.

## Find your patch of the field

| Directory | What's inside |
| --- | --- |
| [src/Plugin](src/Plugin) | C# game integration and native action diagnostics |
| [src/Runtime](src/Runtime) | JavaScript runtime and command transport experiments |
| [src/V8Probe](src/V8Probe) | Embedded compiler and source-input diagnostics |
| [src/Compiler](src/Compiler) | TypeScript checking and JavaScript emission |
| [examples](examples) | TypeScript examples for the compiler prototype |
| [tests](tests) | Compiler, runtime, and integration checks |
| [docs/design](docs/design) | Architecture findings and diagnostic instructions |
| [docs/in-game](docs/in-game) | Embedded TypeScript lesson and future reference material |
| [packaging](packaging) | Future release packaging |

## Help tend the project

Small, focused changes are welcome. We aim for **200–400 changed lines per PR**
when practical, and keep verification evidence separate from future plans.

Read [AGENTS.md](AGENTS.md) for working conventions and
[PROJECT_STATUS.md](PROJECT_STATUS.md) for the current handoff, decisions, bugs,
and next steps. Keep game binaries, saves, credentials, and local tools out of Git.

*The crops can stay. The snake_case is getting composted.*
