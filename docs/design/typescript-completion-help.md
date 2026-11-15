# TypeScript completion help

Hovering a farm API suggestion now provides its TypeScript signature, a short
explanation, and an example. Coverage: doAFlip, canHarvest, harvest, move and
Direction. The harvest description distinguishes successful harvesting from crop
readiness. Direction explains quoted lowercase strings and that the type is not
a runtime object.

The tooltip lookup uses the current sorted candidate list and the hovered index,
so native ranking cannot associate the wrong help with a suggestion. Each lookup
returns a fresh TooltipInfo because the native option assigns hover positioning.
Uncovered keywords return no help instead of falling back to Python material.
Native Python windows retain their normal help.

This does not change code-text hover help, signatures while typing arguments,
completion insertion, unlock filtering or semantic analysis. Content is English
and manually maintained alongside farm.d.ts.

## Validation

Desktop checks cover reordered candidates, unmapped/native names and invalid
indices. Both locked plugin builds and runtime regressions pass.
Native tooltip layout, hover selection and mode isolation remain pending.

Manual check: restart, type move, then hover the move entry in the suggestions.
Expect move(direction: Direction): boolean, lowercase direction strings, and
move("east");. Next check harvest's readiness warning and Tab insertion.
