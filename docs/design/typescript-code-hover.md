# TypeScript code hover

Code-window hover uses the same five help entries as completion: doAFlip,
canHarvest, harvest, move and Direction. Unknown names return no tooltip rather
than Python help. Python runtime variable inspection is skipped for TypeScript
windows, so it cannot overwrite the help with unrelated native values.

A checksum-gated transpiler changes only the two lookups inside
CodeWindow.GetTooltipInfo, preserving the native hover callback, delay, anchor
and positioning. Installation requires exactly one word-help and one name-value
lookup. Native-mode windows retain both original lookups.

This is static API help, not TypeScript symbol resolution or variable inspection.
Shadowed API names, comments/strings and member scopes are not distinguished by
the native word selector. Syntax coloring is unchanged.

Both locked plugin builds and runtime regressions pass. The user confirms the
move code-hover signature/help after restart under Unity/Proton on CachyOS.
Unknown/Python names and native-mode code-hover isolation were not separately
rechecked. To repeat the check: After restarting, type
move("east"); and hover move in the code itself with the suggestions closed.
Check the TypeScript signature/example, then unknown/Python names and native-mode
isolation. Completion help should remain unchanged.
