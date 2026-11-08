# ClearScript/V8 compiler-host comparison

## Result

ClearScript/V8 7.5.1.1 passes the first embedded compiler-host comparison on
CachyOS Linux x64 under desktop .NET 10. The official, unmodified TypeScript 6.0.3
compiler discovers all nine ES2020 library references and performs semantic
checking. All six loop-control cases pass, including the three that fail in Jint.

This is a promising candidate, not a selected production host. Nothing is deployed
to the game, and Jint remains pinned for the existing player-code diagnostics.
COMP-001 is still open for Jint; using another compiler host does not fix it.

## Run

Install the locked npm dependencies first, then:

```bash
XDG_RUNTIME_DIR="$PWD/.tools/runtime" bash scripts/probe-v8-compiler-host.sh
```

The script reuses the existing compiler fixture generator. Node prepares local
texts; the C# process then hosts the compiler directly in V8 and never starts Node.
The adapter and declaration input are the same as the Jint experiment. Only the
module import/export wrapper is removed; compiler internals are not patched.

The isolated test project pins ClearScript and the Linux x64 native package, with
a NuGet lock file and locked builds. It has no plugin project references. No
native binaries or third-party compiler sources are committed.

## Verified behavior and timing

| Check | Result |
| --- | --- |
| Library reference discovery | Nine, matching Node |
| Labeled/unlabeled loop-control fixtures | All six pass |
| Typed loop | Compiles, source map retains input, output calls stub twice |
| Semantic type error | Error 2322 on original line 2, no JS/map output |
| snake_case call | Rejected |
| Module declaration | Rejected |
| Valid compilation after errors | Succeeds in the same compiler engine |

One observed run loaded the compiler in 297 ms, compiled the typed loop in 286 ms,
rejected the semantic error in 137 ms, rejected snake_case in 133 ms, rejected a
module in under 1 ms, and compiled the recovery case in 104 ms.
Per-case timings include result validation and, for valid cases, running output
in a separate V8 engine. These are illustrative single-machine observations, not
a controlled benchmark against Jint or a promise of in-game latency.

The runner exposes no CLR objects or types to JavaScript. Input is trusted test
data; a 90-second outer process timeout bounds the experiment. Per-request
interruption, heap limits, threading, and untrusted-input handling remain future
host requirements.

## Packaging cost and next gate

The local Linux native library is about 58.9 MB uncompressed, plus about 10.8 MB
of ICU data and smaller managed dependencies. Download/package sizes differ.
This is a materially larger dependency than Jint.

ClearScript publishes separate platform packages; see its
[deployment guide](https://clearscript.clearfoundry.net/Details/Build.html).
That availability does not prove compatibility with this game's Unity/Mono build.
Our development game is a Windows process under Proton: this Linux library cannot
simply be copied into it.

Next test the Windows x64 native library and managed assembly loading in an
isolated Unity/Mono diagnostic. Keep work off the Unity thread, validate shutdown,
and preserve compiler/license notices when preparing any distributable bundle.
macOS and other release platform/architecture combinations need their own tests.
