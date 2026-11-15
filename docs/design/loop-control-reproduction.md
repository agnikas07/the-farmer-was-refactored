# Compiler-host mismatch reduced to loop control

The reference-discovery failure in COMP-001 reduces to ordinary continue inside
a labeled while or do loop. No TypeScript compiler, library declarations, game,
or CLR interop is needed to trigger it.

```javascript
(function () {
  let count = 0;
  scan: while (count < 2) {
    count++;
    continue;
  }
  return count;
})();
```

Expected: 2. Node 26.11.1 returns 2. Staged Jint 4.17.0 netstandard2.1 returns
undefined, skipping the function's return. Naming the target explicitly with
continue scan returns 2. The fault is not a missing library file or a declaration
loading configuration problem.

## Reproduction

```bash
XDG_RUNTIME_DIR="$PWD/.tools/runtime" bash scripts/probe-loop-control.sh
```

One JSON fixture supplies both hosts. Node checks all six cases first; the staged
Jint runner then prints every result and exits nonzero if any differ from 2.
The same six cases now also run in the normal test-runtime.sh regression suite.

| Case | Node | Jint 4.17.0 |
| --- | --- | --- |
| Ordinary while + continue | 2 | 2 |
| Labeled while + continue | 2 | undefined |
| Labeled while, switch, continue | 2 | undefined |
| Labeled for + continue | 2 | 2 |
| Labeled do + continue | 2 | undefined |
| Labeled while + explicit continue target | 2 | 2 |

An isolated build against the official Jint 4.16.4 netstandard2.1 assembly and
Acornima 1.7.0 produced the same six results and Node=9/Jint=0 compiler reference
counts. No repository dependency or deployed DLL was downgraded.

## Connection to TypeScript

The pinned compiler's comment scanner uses a labeled while loop with ordinary
continue statements. A scratch-only experiment replaced those statements within
iterateCommentRanges with explicit continue scan targets. Nested loops in that
function contain no continue statements, so their targets were not altered.

That experiment restored all nine library references, compiled and executed the
two-flip typed program, and rejected the invalid-type input. It confirms that
this control-flow mismatch causes the observed reference-discovery failure.
It does not prove the internal engine implementation cause or full compiler
correctness. No patched compiler source or transformation ships in this PR.

Compiler load was about 1.17 seconds; the tiny valid compilation took 6.44 seconds,
and semantic rejection took 4.92 seconds in that desktop run. These are exploratory
single-machine measurements, not a general performance benchmark.

## Fix and validation

[Jint 4.18.0](https://github.com/sebastienros/jint/releases/tag/v4.18.0)
backports the labeled while/do continue fix and updates Acornima to 1.9.1.
All three direct Jint references and their dependency locks now use 4.18.0.
No source rewriting or compiler-host change is involved: V8 still compiles
TypeScript, and Jint executes the emitted player program.

Before upgrading, the staged 4.17.0 assembly reproduced the three failures above.
After upgrading, all six cases return 2. Runtime/mailbox/movement/stepping,
computation-budget, runtime-popup, cancellation, dispatch, embedded compiler,
compiler-worker and V8-to-Jint desktop checks pass. Both locked plugin builds
pass without warnings/errors. The upgrade also contains other upstream fixes;
these focused tests do not certify every JavaScript feature.

Native Unity/Mono checks passed on CachyOS/Proton: the user confirms one flip
after both labeled while and do/while, followed by normal patrol/harvesting and
clean Stop without an error. To repeat the while check, restart and run:

```typescript
let count = 0;
scan: while (count < 2) {
  count++;
  continue;
}
if (count === 2) {
  doAFlip();
}
```

Expect one flip: this verifies execution reaches the code after the loop.
Follow with the do/while variant, then check normal farming and Stop. The
original TypeScript-under-Jint compiler experiment has not been rerun; the
production development path continues to use V8 for compilation.
