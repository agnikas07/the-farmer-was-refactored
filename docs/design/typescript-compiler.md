# TypeScript compiler prototype

This is a development-only, single-file compiler. It uses the official JavaScript
compiler package pinned to TypeScript 6.0.3 and its createProgram diagnostics/emit
API. Pinning keeps this experiment reproducible; this is not a claim that 6.0.3
is the latest compiler or the selected release compiler.
See Microsoft's [Compiler API guide](https://github.com/microsoft/TypeScript/wiki/Using-the-Compiler-API).

## Contract

compile(source, libraries, api) accepts source text plus trusted declaration text.
Its CompilerHost sees virtual files only and captures output in memory. The Node
development host loads declarations from the pinned package and farm.d.ts.
No player-selected disk path is read by the compiler host.

The result contains ok, diagnostics, javascript, and sourceMap. Errors suppress
both output fields. Diagnostics use original source names and one-based line and
column positions. Strict semantic checking catches invalid types and arguments;
it is not merely type stripping.

The prototype emits ES2020 script code and a map with embedded original source.
Modules and dynamic imports are rejected before program construction. Source-map
emission is tested; translating Jint runtime stack positions is still pending.
Node/browser declarations and automatic @types discovery are excluded. ES2020
standard-library types are present; full runtime compatibility is not established.
Type declarations are not a security sandbox or enforcement of progression rules.

Only doAFlip(): void is declared, reflecting the tested diagnostic binding.
canHarvest is an agreed camelCase name but is not implemented or declared yet.
No snake_case alias is exposed. The typed example uses const, let, and a for loop;
loop-unlock enforcement is not part of this compiler experiment.

## Run

From the repository root:

```bash
npm ci --ignore-scripts --cache .tools/npm-cache
npm run test:compiler
```

The 13 tests cover typed output running with a stub doAFlip, semantic error
locations, syntax/argument errors, snake_case rejection, unsupported host APIs,
module rejection, source-map contents, and isolation between compilations.
They run under the local Node 26.11.1 development host, not Unity/Jint.
No game launch or deployment is needed.

The compiler adapter is JavaScript (.mjs), so it needs no bootstrap compiler;
player input and the example are TypeScript. The Node host is separate from the
virtual compiler core. Neither selects an embedded compiler engine or requires
players to install Node. Compiler bundling/performance, generated-code execution
in Jint, in-game diagnostics, and editor integration are subsequent steps.
