# TypeScript editor coloring and indentation

TypeScript code windows now use TypeScript syntax coloring, `{}`/`()`/`[]` and
quote (`"`, `'`, `` ` ``) auto-closing, and `{`/`(`/`[` newline auto-indentation
instead of Python rules. Native Python windows retain native highlighting (`#`
comments, Python keywords/built-ins) and `:` indentation.

Checksum-gated Harmony patches in
[TypeScriptEditorStylePatch.cs](../../src/Plugin/TypeScriptEditorStylePatch.cs)
hook three `CodeWindow` methods:
- `UpdateSearch` and `OnThemeChanged`: transpilers replace `CodeUtilities.SyntaxColor2`
  with window-aware dispatch to `TypeScriptEditorStyle.ColorCode`, preserving
  TextMeshPro `<noparse>` alignment, search `<mark>` highlights, and IME `<u>` spans.
- `InsertNewLineIndentation`: a prefix handles TypeScript auto-closing (`{}`, `()`,
  `[]`, `"`, `'`, `` ` `` via `TypeScriptEditorStyle.TryAutoCloseBrace`, inserting the
  closing character with caret between and stepping over an excess/matching closing
  character) and newline indentation (`TypeScriptEditorStyle.TryIndentNewLine`),
  preserving current indentation, adding one level after `{`, `(`, or `[`, and
  expanding `{|}`, `(|)`, or `[|]` across an indented middle line with the closing
  delimiter aligned below.

Limitations: Coloring uses a regex tokenizer rather than TypeScript AST or semantic
classification. Auto-closing and indentation do not ignore delimiters inside
comments/strings, auto-format existing code, or auto-dedent `}`/`)`/`]` on standalone
type. In-game markdown docs (`CodeUtilities.ApplyCodeTags`) remain unchanged.

## Validation

Desktop runtime tests (`EditorStyleChecks`) verify `//` and `/* ... */` comments,
exclusion of `#` and Python keywords/literals, TypeScript keywords/types/functions,
strings/numbers, search and IME tags, `{}`/`()`/`[]` and `"`, `'`, `` ` ``
auto-closing/overtyping, and `{|}`/`(|)`/`[|]`/`:` newline indentation cases.
Locked plugin builds and the full desktop runtime/V8/compiler suites pass. Under
Unity/Proton on CachyOS, the user confirms TypeScript syntax coloring,
`{}`/`()`/`[]` and `"`, `'`, `` ` `` auto-closing/overtyping, and `{|}`/`(|)`/`[|]`
Enter expansion in-game. Search highlights, IME underlines, and Python-mode
isolation are covered by desktop checks (`EditorStyleChecks`) and were not
separately rechecked in-game.
