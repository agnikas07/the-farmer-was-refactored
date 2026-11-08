import ts from "typescript";

// Virtual files only. The caller supplies trusted compiler-library declarations.
export function compile(source, libraries, api) {
  const files = new Map(libraries);
  files.set("/main.ts", source);
  files.set("/farm.d.ts", api);
  const output = new Map();
  const options = {
    target: ts.ScriptTarget.ES2020,
    module: ts.ModuleKind.ES2020,
    moduleResolution: ts.ModuleResolutionKind.Bundler,
    moduleDetection: ts.ModuleDetectionKind.Legacy,
    lib: ["lib.es2020.d.ts"],
    types: [],
    strict: true,
    noEmitOnError: true,
    sourceMap: true,
    inlineSources: true,
    newLine: ts.NewLineKind.LineFeed,
  };
  const host = {
    getSourceFile: (name, version) => files.has(name)
      ? ts.createSourceFile(name, files.get(name), version, true) : undefined,
    getDefaultLibFileName: () => "/lib.es2020.d.ts",
    writeFile: (name, text) => output.set(name, text),
    getCurrentDirectory: () => "/",
    getDirectories: () => [],
    resolveModuleNames: names => names.map(() => undefined),
    fileExists: name => files.has(name),
    readFile: name => files.get(name),
    getCanonicalFileName: name => name,
    useCaseSensitiveFileNames: () => true,
    getNewLine: () => "\n",
  };
  const main = ts.createSourceFile("/main.ts", source, options.target, true);
  const diagnostics = [];
  const reject = (node, message) => diagnostics.push({
    category: ts.DiagnosticCategory.Error, code: 90001,
    file: main, start: node.getStart(main), length: node.getWidth(main), messageText: message,
  });
  if (ts.isExternalModule(main)) reject(main, "Modules are not supported by this prototype.");
  function visit(node) {
    if (ts.isCallExpression(node) && node.expression.kind === ts.SyntaxKind.ImportKeyword)
      reject(node, "Dynamic imports are not supported by this prototype.");
    ts.forEachChild(node, visit);
  }
  visit(main);
  if (diagnostics.length === 0) {
    const program = ts.createProgram(["/main.ts", "/farm.d.ts"], options, host);
    diagnostics.push(...ts.getPreEmitDiagnostics(program));
    if (!diagnostics.some(item => item.category === ts.DiagnosticCategory.Error))
      diagnostics.push(...program.emit().diagnostics);
  }
  const errors = diagnostics.map(item => {
    const position = item.file && item.start !== undefined
      ? item.file.getLineAndCharacterOfPosition(item.start) : undefined;
    return {
      code: item.code,
      category: ts.DiagnosticCategory[item.category].toLowerCase(),
      message: ts.flattenDiagnosticMessageText(item.messageText, "\n"),
      file: item.file?.fileName,
      line: position ? position.line + 1 : undefined,
      column: position ? position.character + 1 : undefined,
    };
  });
  const ok = !errors.some(item => item.category === "error");
  return {
    ok, diagnostics: errors,
    javascript: ok ? output.get("/main.js") : undefined,
    sourceMap: ok ? output.get("/main.js.map") : undefined,
  };
}
