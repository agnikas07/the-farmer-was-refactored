# First TypeScript lesson

Source mode replaces `docs/first_program.md` with the original lesson authored in
`docs/in-game/first-program.md`, embedded in the main plugin. The existing
Documentation home → First Program link reaches it. Navigation, scrolling, theme
rendering, and history remain owned by the game's DocsWindow.

A postfix on Localizer.LoadDoc replaces only that exact page, preserving native
language initialization. Installation is
part of the existing checksum-gated source mode; disabling source mode and
restarting restores the native page. No game documentation files or saves are
rewritten. Other documentation and function tooltips are unchanged. This first
lesson is English even when the game uses another language; localization is
future work, not an established capability.

The resource uses the game's supported plain code fences (no language tag) and
links. Examples are static text for the player to enter in a normal code window,
not native embedded simulations, which cannot yet execute this TypeScript path.
Marker-based mode inserts the required first line into every complete example;
all-window mode omits it. Runtime mode switches require restart as before.

Teaching starts with one flip and then harvesting plus a flip. The conditional
example explicitly requires canHarvest to be unlocked. It explains true/false,
parentheses, semicolons, case-sensitive names, braces, and compile-error recovery.
It does not promise that a successful harvest was ripe. Scope limitations and
untranslated pages are stated without sending players into Python examples.

## Validation

Automated checks compile all three exact lesson snippets in both source modes
and verify action order and ready/unready branching with stub APIs. Build embeds
the lesson; native rendering and navigation still need manual validation.

Manual checks: open Documentation home → First Program and verify the title
"Your First TypeScript Harvest", readable layout, code blocks, home navigation,
and absence of the native runnable Python example. Try the first code example.
Then check marker-based mode after restart and confirm its samples include the
marker. Disabling source mode and restarting should restore the original page.
Localization and other pages remain outside this PR.
