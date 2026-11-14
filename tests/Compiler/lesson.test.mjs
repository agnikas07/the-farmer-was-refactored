import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import { runInNewContext } from "node:vm";
import { compileForDevelopment as compile } from "../../src/Compiler/node-host.mjs";

const lesson = readFileSync(new URL("../../docs/in-game/first-program.md", import.meta.url), "utf8");
const examples = [...lesson.matchAll(/```\n([\s\S]*?)\n```/g)].map(match => match[1]);

for (const allWindows of [false, true]) {
  test(`first lesson examples compile and behave correctly (all windows: ${allWindows})`, () => {
    assert.equal(examples.length, 3);
    for (const ready of [false, true]) {
      const expected = [["flip"], ["harvest", "flip"], ready ? ["query", "harvest"] : ["query"]];
      examples.forEach((example, index) => {
        const source = example.replaceAll("{{sourcePrefix}}", allWindows ? "" : "// @refactored\n");
        const result = compile(source);
        assert.equal(result.ok, true, JSON.stringify(result.diagnostics));
        const calls = [];
        runInNewContext(result.javascript, {
          doAFlip: () => calls.push("flip"),
          canHarvest: () => { calls.push("query"); return ready; },
          harvest: () => { calls.push("harvest"); return true; },
        }, { timeout: 1000 });
        assert.deepEqual(calls, expected[index]);
      });
    }
  });
}
