# Two-tile traversal lesson

Extends the existing First Program replacement; no additional native docs paths
are intercepted. Examples introduce continuous harvesting, direction strings,
conditional movement, and an east/west patrol that exits on a failed move.
The lesson requires movement unlocked and at least two columns for the patrol.
It does not assume a fixed farm size or teach full-field coverage.

Compiler/example checks run in both marker modes with ready and unready crops,
successful movement, blocked east, and blocked west. Tests interrupt continuous
examples through a stub query after four visits; they do not verify native Stop
or game movement costs. Existing runtime behavior is unchanged.

Manual validation: the user confirms new-section rendering and the east/west
patrol with harvesting and clean Stop under Unity/Proton on CachyOS. New examples
are compiler-tested in both marker modes; marker-mode rendering has not been
rechecked in-game for this extension. Reproduction/checklist:
1. Restart and open First Program. Check the new sections render correctly.
2. Run the patrol on the development grass farm: expect alternating east/west,
   ripe-only harvesting, and Stop without an error.
3. Check the marker-based lesson still includes the marker on each new example.

Other pages, syntax highlighting, progression gating of TypeScript language
constructs, and full-field route teaching remain future work.
