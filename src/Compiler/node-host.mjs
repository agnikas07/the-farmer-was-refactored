// Development host only; this does not choose the game's compiler hosting model.
import { readFileSync, readdirSync } from "node:fs";
import { createRequire } from "node:module";
import { dirname, join } from "node:path";
import { compile } from "./compile.mjs";

const libDirectory = dirname(createRequire(import.meta.url).resolve("typescript"));
const libraries = new Map(readdirSync(libDirectory)
  .filter(name => /^lib\..*\.d\.ts$/.test(name))
  .map(name => ["/" + name, readFileSync(join(libDirectory, name), "utf8")]));
const api = readFileSync(new URL("./farm.d.ts", import.meta.url), "utf8");

export function compileForDevelopment(source) {
  return compile(source, libraries, api);
}
