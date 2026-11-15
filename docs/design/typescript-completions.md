# Initial TypeScript completion vocabulary

The source-mode checksum gate installs a prefix on CodeCompleter.Open. It
replaces native candidates only when the owning window is selected as TypeScript.
Native positioning, ranking, arrow-key selection and Tab insertion remain.
Python-mode windows retain their native candidates and tooltips.

Candidates are the four currently bound farm functions, Direction, Error, and a
small TypeScript keyword/type vocabulary. Lists are copied before native sorting.
No Python built-ins, snake_case APIs, or unsupported module imports are seeded.
Member completion after a dot is suppressed rather than offering Python members.
Native completion tooltips are suppressed in TypeScript windows because even
shared names such as harvest resolve to Python teaching material.

This is not semantic completion: it does not infer local variables, member types,
scope, comments/strings, or progression unlocks. The game still handles identifier
boundaries and highlighting. Static keyword suggestions are not a promise that
every language construct has been validated in the game.

## Validation

Both locked plugin builds and runtime regression suites pass. Vocabulary checks
cover supported names, exclusion of native/unsupported names, duplicate removal,
fresh-list isolation and dot-context suppression.

Manual checks passed by user confirmation under Unity/Proton on CachyOS:
doAFlip/canHarvest suggestions and Tab insertion, dot suppression, native
suggestions in unmarked marker mode, and TypeScript suggestions after adding
the marker. Tooltip behavior was not separately inspected. Reproduction steps:
1. Restart, type do in a TypeScript window: doAFlip should appear; do_a_flip,
   __name__, and Python and should not. Select doAFlip with arrows and Tab.
2. Check canHarvest, harvest, move and const; dot completion should not expose
   Python member names.
3. In marker mode, compare marked TypeScript and unmarked native windows.

The deployed branch starts from main and excludes draft lifecycle PR #37.
