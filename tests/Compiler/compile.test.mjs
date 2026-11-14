import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import { runInNewContext } from "node:vm";
import { compileForDevelopment as compile } from "../../src/Compiler/node-host.mjs";

test("typed camelCase program emits executable JS and original source map", () => {
  const source = readFileSync(new URL("../../examples/two-flips.ts", import.meta.url), "utf8");
  const result = compile(source);
  assert.equal(result.ok, true, JSON.stringify(result.diagnostics));
  let flips = 0;
  runInNewContext(result.javascript, { doAFlip: () => flips++ }, { timeout: 1000 });
  assert.equal(flips, 2);
  const map = JSON.parse(result.sourceMap);
  assert.deepEqual(map.sources, ["main.ts"]);
  assert.deepEqual(map.sourcesContent, [source]);
  assert.ok(map.mappings.length > 0);
});

test("semantic errors carry original one-based positions and block all output", () => {
  const result = compile('doAFlip();\nconst count: number = "two";');
  assert.equal(result.ok, false);
  assert.equal(result.javascript, undefined);
  assert.equal(result.sourceMap, undefined);
  const error = result.diagnostics.find(item => item.code === 2322);
  assert.equal(error.file, "/main.ts");
  assert.equal(error.line, 2);
  assert.equal(error.column, 7);
  assert.equal(error.start, 17);
  assert.equal(error.length, 5);
});

for (const source of [
  "do_a_flip();",
  "doAFlip(1);",
  "function flip(value) { return value; }",
  "const broken: = 1;",
  "document.title;",
  "process.exit();",
  'fetch("https://example.com");',
  'import "./missing";',
  "export const value = 1;",
  'import("./missing");',
]) {
  test("rejects unsupported or invalid source: " + source, () => {
    const result = compile(source);
    assert.equal(result.ok, false);
    assert.ok(result.diagnostics.some(item => item.category === "error"));
    assert.equal(result.javascript, undefined);
  });
}

test("compilations do not leak globals or stale successful output", () => {
  assert.equal(compile("const oldValue = 1;").ok, true);
  assert.equal(compile("oldValue;").ok, false);
  assert.equal(compile("doAFlip();").ok, true);
});
