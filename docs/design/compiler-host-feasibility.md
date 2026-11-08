# Embedded compiler host: first feasibility result

## Outcome

TypeScript 6.0.3 loads inside staged Jint 4.17.0 on desktop .NET, but the compiler
is not yet usable there. The same lib.es2020.d.ts source yields nine
libReferenceDirectives under Node and zero under Jint. Compilation then reports
missing global types such as Array and String.

This is an observed host compatibility mismatch, not a diagnosed Jint bug.
The underlying cause remains unknown. Do not ship this path, suppress its errors,
or claim semantic checking works merely because the compiler script loaded.

## Reproduce

After installing the locked npm dependencies and local .NET/game development
prerequisites, run:

```bash
XDG_RUNTIME_DIR="$PWD/.tools/runtime" bash scripts/probe-compiler-host.sh
```

Current expected result: **nonzero exit**, with:

```text
Compiler host: library references Node=9, Jint=0
Compiler host incompatible: standard-library reference discovery differs.
```

The probe is deliberately separate from the passing regression suite.
Its failure is retained as evidence rather than converted into a passing check.
If reference discovery starts matching, it goes on to require successful typed
compilation, execution of the emitted two-flip program, a source map, and rejection
of an invalid type with no executable output.

## Method and measurements

A Node preparation script copies the pinned compiler text, virtual compiler
adapter, declaration files, and API into a temporary JSON fixture. It records the
Node reference count as a baseline. The adapter's import/export wrappers are
removed for script loading; its compiler logic is unchanged.

The C# process loads those texts into Jint; it never starts Node and exposes no
filesystem or CLR APIs to the compiler. The fixture is local test data, not a
release bundle. Copyright/license text in the compiler and declarations is
preserved in the temporary fixture. Third-party sources are not committed.

On CachyOS, loading took approximately 1.1 seconds across the inspected runs.
This is one-machine exploratory timing, not a benchmark or player startup budget.
The engine has a 30-second execution timeout, statement/allocation/recursion
limits, and an outer 90-second process timeout. No Unity/Mono test was attempted.

The earlier unguarded compilation returned missing-global-type diagnostics;
the committed probe now catches the reference mismatch before compilation.
That avoids obscuring the earliest observed incompatibility with downstream
errors. No workaround has been applied to the compiler or runtime.

## Next decision

Reduce reference/comment scanning to a minimal reproduction before attributing a
root cause. If it requires substantial engine changes, compare another embeddable
JavaScript host against the same semantic checks. Native dependencies would need
Windows/macOS/Linux packaging and Unity compatibility evidence.

This result does not invalidate Jint's tested execution of generated player code.
Compiler hosting and player-code execution may use different engines. The release
goal remains that players do not need to install Node separately.
