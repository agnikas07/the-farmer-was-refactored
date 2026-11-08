import assert from "node:assert/strict";
import { readFileSync, writeFileSync } from "node:fs";
import { compileForDevelopment } from "../../src/Compiler/node-host.mjs";

// Trusted test inputs, compiled fresh each run; never hand-maintained emitted JS.
const cases = [
  { name: "typed loop", outcome: "success", flips: 2,
    source: readFileSync(new URL("../../examples/two-flips.ts", import.meta.url), "utf8") },
  { name: "enum, generics and optional access", outcome: "success", flips: 3,
    source: `
enum Count { Three = 3 }
function identity<T>(value: T): T { return value; }
const settings: { count?: number } = { count: identity(Count.Three) };
const count = identity<{ count?: number } | undefined>(settings)?.count;
[1, 2, 3].slice(0, count ?? 0).forEach(() => doAFlip());
` },
  { name: "runtime failure stops later actions", outcome: "throw", flips: 1,
    source: 'doAFlip(); throw new Error("expected fixture failure"); doAFlip();' },
  { name: "statement limit", outcome: "budget", flips: 0,
    source: "while (true) {}" },
  { name: "semantic failure blocks earlier valid action", outcome: "rejected", flips: 0,
    source: 'doAFlip(); const count: number = "two";' },
  { name: "snake_case rejected", outcome: "rejected", flips: 0,
    source: "do_a_flip();" },
  { name: "fresh engine after failures", outcome: "success", flips: 1,
    source: "const count: number = 1; for (let i = 0; i < count; i++) doAFlip();" },
];
const results = cases.map(({ source, ...expectation }) => {
  const result = compileForDevelopment(source);
  assert.equal(result.ok, expectation.outcome !== "rejected",
    expectation.name + ": " + JSON.stringify(result.diagnostics));
  if (result.ok) {
    assert.equal(typeof result.javascript, "string");
    assert.deepEqual(JSON.parse(result.sourceMap).sourcesContent, [source]);
  } else {
    assert.equal(result.javascript, undefined);
    assert.equal(result.sourceMap, undefined);
  }
  return { ...expectation, ...result };
});
assert.equal(process.argv.length, 3, "Supply a destination JSON path.");
writeFileSync(process.argv[2], JSON.stringify(results));
console.log("Compiled seven fresh TypeScript-to-Jint fixtures.");
