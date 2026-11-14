[Documentation home](docs/home.md)
---
# Your First TypeScript Harvest

One tile is enough to start. You will tell the drone to flip, collect grass, and
check whether a crop is ready.

## Give the drone a command

Open a code window and replace its contents with this example. Press the filled
green Run button. The drone should flip once.

```
{{sourcePrefix}}doAFlip();
```

`doAFlip` is a function name. The parentheses call it, and the semicolon ends the
statement. Capital letters matter: use `doAFlip`, exactly as shown.

{{modeNote}}

## Collect your first grass

Wait until the grass is ready, then replace the code with:

```
{{sourcePrefix}}harvest();
doAFlip();
```

The commands run in order: harvest first, then celebrate with a flip. Harvesting
mature grass collects hay. You can spend hay in the [tech tree](tech_tree).

`harvest()` returns `true` when it harvests an entity and `false` when there is
nothing to harvest. It can also harvest an unready plant, so a successful result
alone does not promise a ripe crop or a particular yield.

## Check before collecting

When `canHarvest()` is unlocked in your save, try this on mature grass:

```
{{sourcePrefix}}if (canHarvest()) {
  harvest();
}
```

`canHarvest()` checks the entity under the drone without collecting it. Its result
is a boolean: either `true` or `false`. The `if` statement runs the code between
`{` and `}` only when that result is true. An unready or empty tile is left alone.

Each press of Run checks once. If nothing happens, wait for the grass to grow and
run the program again. You do not need more land to try this lesson.

## Read and fix an error

If Run shows a TypeScript error, read the message and correct the highlighted
code. Type errors prevent the whole program from starting; correcting the code
and pressing Run tries again. Use the normal Stop control to end a running program.

## Where we are growing next

This first lesson is written for The Farmer was Refactored. The three commands
above are the current farm API. Movement and more farming commands are coming
later. Other documentation pages still teach the original language, and editor
colors and suggestions are not yet fully adapted to TypeScript.

[Back to documentation home](docs/home.md)
