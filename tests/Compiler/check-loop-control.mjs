import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { runInNewContext } from "node:vm";

const cases = JSON.parse(readFileSync(process.argv[2], "utf8"));
assert.equal(cases.length, 6);
for (const { name, body } of cases) {
  const value = runInNewContext("(function(){" + body + "})()", {}, { timeout: 1000 });
  assert.equal(value, 2, name);
  console.log("Node loop control: " + name + " PASS");
}
