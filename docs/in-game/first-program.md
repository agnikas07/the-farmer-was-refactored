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

## Keep the harvest going

Once the query is unlocked, a loop can wait for each new crop:

```
{{sourcePrefix}}while (true) {
  if (canHarvest()) {
    harvest();
  }
}
```

`while` repeats the block as long as its condition is true. Here that condition
never changes: the drone keeps checking and collecting until you press Stop.
A false query leaves the growing plant alone and the next iteration checks again.
Stop the program before replacing its code.

## Take a step

After expanding your farm and unlocking movement, try:

```
{{sourcePrefix}}if (move("east")) {
  doAFlip();
}
```

`move()` takes one direction string: `"north"`, `"east"`, `"south"`, or
`"west"`. Keep the quotes and use lowercase. It returns a boolean; this example
flips only after a successful move. A type error for `"up"` means you should
choose one of those four directions.

## Patrol two neighboring tiles

Use a farm with at least two columns, with grass on the current tile and its
eastern neighbor. This program checks one tile, steps east, checks the next tile,
and steps west. It repeats that short route until you press Stop.

```
{{sourcePrefix}}while (true) {
  if (canHarvest()) {
    harvest();
  }
  if (!move("east")) {
    break;
  }
  if (canHarvest()) {
    harvest();
  }
  if (!move("west")) {
    break;
  }
}
```

`!` means “not”: `!move("east")` is true when the move fails. `break` leaves
the loop, so a failed move ends the patrol instead of continuing from an
unexpected position. On an ordinary grass farm, the drone should keep moving
between the two tiles. It collects only crops that are ready when it visits.
This route covers two tiles, not the whole farm.

## Read and fix an error

If Run shows a TypeScript error, read the message and correct the highlighted
code. Type errors prevent the whole program from starting; correcting the code
and pressing Run tries again.

A runtime error means the program started and then stopped. Actions already
performed are not undone. For example, a loop with no farm calls can reach the
computation limit. Give a farming loop useful work such as checking, harvesting,
or moving. Runtime errors currently mark the start of the program; the popup
explains that its exact source location is not yet available. Editing clears the
old message. Press Run to try the corrected program.

## Where we are growing next

This lesson is written for The Farmer was Refactored. You can now collect grass
and patrol a small route. More farming commands and lessons are coming later.
Other documentation pages still teach the original language, and editor
colors and suggestions are not yet fully adapted to TypeScript.

[Back to documentation home](docs/home.md)
