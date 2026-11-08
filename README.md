![The Farmer was Refactored — a straw-hatted drone, floating code windows, and a field of golden crops](docs/assets/the-farmer-was-refactored.png)

# The Farmer was Refactored

**Grow crops. Write TypeScript. Refactor the farm.**

An experimental language-system overhaul for **The Farmer Was Replaced**.
The goal: play in TypeScript, with an editor, execution controls, and in-game
lessons that teach the new language while preserving the farming mechanics.

[Progress & plans](PROJECT_STATUS.md) · [Development guide](docs/development.md) · [TypeScript example](examples/two-flips.ts)

> 🌱 **Still growing.** This is a development prototype, not a playable release.
> You cannot write TypeScript in the game's editor yet. There is no mod-store
> package to install.

## A little taste of the harvest

The compiler prototype already understands this:

```typescript
const flipCount: number = 2;

for (let index = 0; index < flipCount; index++) {
  doAFlip();
}
```

Two flips. Strong types. One very enthusiastic drone.

Public APIs use TypeScript-style camelCase: `doAFlip()`, and eventually
`canHarvest()`. The latter is a planned name, not an implemented binding.
The example runs in desktop compiler/runtime tests; the game currently uses
separate, opt-in diagnostic programs.

## What's sprouted?

| Piece | Where it stands |
| --- | --- |
| Plugin startup | Verified in the isolated development game |
| JavaScript → native actions | Two flips and Stop verified in-game |
| TypeScript checking | Strict type checking and source-map emission tested |
| Compiled TypeScript → Jint | Seven desktop integration cases passing |
| TypeScript editor & rewritten lessons | Planned |
| Full farming API, modules & drones | Planned |
| Playable release | Not yet |

The current test environment is **CachyOS Linux running the Windows game through
Proton**. Windows and macOS are intended targets, but remain untested.
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

That runs the 13 compiler tests without launching the game. Once the .NET/game
development prerequisites are set up, the
[TypeScript-to-Jint check](docs/design/typescript-jint-pipeline.md) tests the emitted
JavaScript against the staged runtime.

Node is currently a **development tool**. How the compiler ships inside the mod
is still being investigated; players should not need to install Node separately.

## Find your patch of the field

| Directory | What's inside |
| --- | --- |
| [src/Plugin](src/Plugin) | C# game integration and native action diagnostics |
| [src/Runtime](src/Runtime) | JavaScript runtime and command transport experiments |
| [src/Compiler](src/Compiler) | TypeScript checking and JavaScript emission |
| [examples](examples) | TypeScript examples for the compiler prototype |
| [tests](tests) | Compiler, runtime, and integration checks |
| [docs/design](docs/design) | Architecture findings and diagnostic instructions |
| [docs/in-game](docs/in-game) | Future player lessons and reference material |
| [packaging](packaging) | Future release packaging |

## Help tend the project

Small, focused changes are welcome. We aim for **200–400 changed lines per PR**
when practical, and keep verification evidence separate from future plans.

Read [AGENTS.md](AGENTS.md) for working conventions and
[PROJECT_STATUS.md](PROJECT_STATUS.md) for the current handoff, decisions, bugs,
and next steps. Keep game binaries, saves, credentials, and local tools out of Git.

*The crops can stay. The snake_case is getting composted.*
