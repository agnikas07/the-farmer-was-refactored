import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import { runInNewContext } from "node:vm";
import { compileForDevelopment as compile } from "../../src/Compiler/node-host.mjs";

const lesson = readFileSync(new URL("../../docs/in-game/first-program.md", import.meta.url), "utf8");
const examples = [...lesson.matchAll(/```\n([\s\S]*?)\n```/g)].map(match => match[1]);

for (const allWindows of [false, true]) {
  test(`lesson examples compile and follow their routes (all windows: ${allWindows})`, () => {
    assert.equal(examples.length, 6);
    const compiled = examples.map(example => {
      const result = compile(example.replaceAll("{{sourcePrefix}}", allWindows ? "" : "// @refactored\n"));
      assert.equal(result.ok, true, JSON.stringify(result.diagnostics));
      return result.javascript;
    });
    for (const ready of [false, true]) {
      const visit = ready ? ["query", "harvest"] : ["query"];
      for (const blocked of [null, "east", "west"]) {
        const expected = [
          ["flip"],
          ["harvest", "flip"],
          visit,
          Array(4).fill(visit).flat(),
          blocked === "east" ? ["east"] : ["east", "flip"],
          blocked === "east" ? [...visit, "east"] :
            blocked === "west" ? [...visit, "east", ...visit, "west"] :
              Array(2).fill([...visit, "east", ...visit, "west"]).flat(),
        ];
        compiled.forEach((javascript, index) => {
          const calls = [];
          const stopped = new Error("test observation complete");
          let queries = 0;
          try {
            runInNewContext(javascript, {
              doAFlip: () => calls.push("flip"),
              canHarvest: () => {
                if (queries++ === 4) throw stopped;
                calls.push("query");
                return ready;
              },
              harvest: () => { calls.push("harvest"); return true; },
              move: direction => { calls.push(direction); return direction !== blocked; },
            }, { timeout: 1000 });
            assert.ok(index !== 3 && !(index === 5 && blocked === null),
              "Continuous example unexpectedly terminated.");
          } catch (error) {
            assert.equal(error, stopped);
            assert.ok(index === 3 || (index === 5 && blocked === null));
          }
          assert.deepEqual(calls, expected[index]);
        });
      }
    }
  });
}
