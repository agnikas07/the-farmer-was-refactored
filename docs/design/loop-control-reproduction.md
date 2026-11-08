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
This is a separate exploratory probe, not part of the passing regression suite.

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

## Implications

Keep Jint 4.17.0 pinned for now, with this defect recorded. The existing generated
player-code tests still pass, but they do not certify all JavaScript control flow.
The same pattern can affect player code containing labeled while/do loops.

An upstream fix or independently validated runtime is needed before relying on
these constructs. No upstream issue/comment has been posted. Next compare an
alternative embedded compiler host using the same semantic checks, or evaluate
a verified upstream fix when available. Do not silently rewrite player programs
or skip semantic checks to hide this failure.
