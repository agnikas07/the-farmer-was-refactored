import assert from "node:assert/strict";
import ts from "typescript";
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { createRequire } from "node:module";
import { dirname, join } from "node:path";

// Development preparation only. The C# probe never launches Node.
const compilerPath = createRequire(import.meta.url).resolve("typescript");
const directory = dirname(compilerPath);
const adapter = readFileSync(new URL("../../src/Compiler/compile.mjs", import.meta.url), "utf8");
assert.ok(adapter.startsWith('import ts from "typescript";\n'));
assert.equal((adapter.match(/export function compile/g) ?? []).length, 1);
const bundle = {
  compiler: readFileSync(compilerPath, "utf8"),
  adapter: adapter.replace('import ts from "typescript";\n', "")
    .replace("export function compile", "function compile"),
  libraries: readdirSync(directory).filter(name => /^lib\..*\.d\.ts$/.test(name))
    .map(name => ["/" + name, readFileSync(join(directory, name), "utf8")]),
  api: readFileSync(new URL("../../src/Compiler/farm.d.ts", import.meta.url), "utf8"),
};
const library = bundle.libraries.find(([name]) => name === "/lib.es2020.d.ts")[1];
bundle.expectedReferences = ts.createSourceFile("/lib.es2020.d.ts", library,
  ts.ScriptTarget.ES2020, true).libReferenceDirectives.length;
assert.ok(bundle.expectedReferences > 0);
writeFileSync(process.argv[2], JSON.stringify(bundle));
