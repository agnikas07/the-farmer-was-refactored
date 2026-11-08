# TypeScript output in Jint

The integration suite compiles fresh TypeScript through the development compiler
and executes the emitted JavaScript in the exact staged Jint 4.17.0 netstandard
DLL referenced by the desktop runtime test project. It does not use Node's
JavaScript evaluator for these execution checks.

## Run

Install locked compiler dependencies first with
`npm ci --ignore-scripts --cache .tools/npm-cache`. Then run:

```bash
XDG_RUNTIME_DIR="$PWD/.tools/runtime" bash scripts/test-compiler-runtime.sh
```

The script runs the compiler unit suite, generates fixtures in a unique ignored
temporary directory, builds the staged plugin/runtime, and runs the compiled-code
checks. Errors stop the script. A failed generation cannot reuse a previous
successful fixture; the temporary directory is removed when the script exits.
The runtime runner's outer timeout bounds a hung test process.

Each successful fixture gets a fresh Jint engine with statement, time, memory,
and recursion limits. doAFlip is a counting stub, not a real farming action.
The runner never executes a rejected compiler result.

## Verified cases

| Input | Expected result |
| --- | --- |
| Typed two-flip example | Two camelCase host calls |
| Enum, generic function, optional access, nullish fallback, array callback | Three host calls |
| One flip followed by a thrown error | One call; later call never executes |
| Infinite loop | Statement budget exception |
| A valid action followed by a semantic type error | Entire input rejected; no executable output |
| Native snake_case function spelling | Compiler rejection |
| Valid typed program after failure cases | Fresh engine executes one call |

All seven integration cases and all 13 compiler unit tests pass on desktop .NET
10 / Node 26.11.1 on CachyOS. The existing runtime suite also passes.

## Limits and next work

This verifies a small set of compiler output against Jint, not all TypeScript or
ES2020 features. Source maps accompany the fixtures, but runtime error positions
are not yet mapped back to TypeScript. The compiler itself still runs under Node
for development; the release compiler host remains undecided.

No plugin behavior changes or in-game deployment are included. Unity/Mono
execution of compiled fixtures and connection to the native action adapter are
not established by this desktop test. Select and test compiler hosting/bundling
before claiming that players can type TypeScript in the game.
