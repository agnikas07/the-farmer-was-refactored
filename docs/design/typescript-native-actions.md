# Compiled TypeScript to native actions

This opt-in diagnostic joins the previously verified pieces: V8 compiles a fixed
TypeScript program; Jint executes the emitted JavaScript; the action mailbox
routes doAFlip calls through the native simulation scheduler and operation costs.
It is not yet an editor replacement or arbitrary player-source support.

The fixed source is:

```typescript
const count: number = 2;
for (let i = 0; i < count; i++) doAFlip();
```

Compilation and a bounded counting-stub preflight run on the V8 diagnostic worker.
Only successful emitted code crosses the queue to Unity Update, which arms the
main plugin's existing native-action hook. V8 is disposed before arming. Failed
compilation/preflight never arms the hook. The action worker receives the emitted
string unchanged. The native scheduler, command completion, 30-second watchdog,
and Stop behavior are inherited from the [JavaScript probe](javascript-action-probe.md).

The V8 diagnostic now declares a dependency on the main plugin. Its deployment
script updates both plugins. Main-plugin code has no ClearScript dependency;
the compiler diagnostic calls its development-only arming method on Unity's thread.
Both plugin destruction paths dispose the same idempotent action probe.

## Manual validation

Close the game, then run `bash scripts/deploy-v8-probe.sh`. In the separate V8
config (`io.github.agnikas07.thefarmerwasrefactored.v8probe.cfg`), set Enabled and
RunCompiledActionProbe true; leave RunCompilerProbe and RunJintPipelineProbe false.
Keep the main plugin's RunNativeActionProbe and all other diagnostics false.
Action mode takes precedence over the older compiler modes and defaults off.

Launch with `bash scripts/launch-dev.sh`, wait about 15 seconds for compilation,
and enter a normal farm with one drone and the flip function unlocked. Paste this
native-parser-compatible trigger into a code window and press ordinary Run:

```python
# refactored-typescript-probe
doAFlip()
```

The trigger is not the TypeScript source; it selects the already compiled program.
Expect two flips and normal completion. Close the game, then inspect:

```bash
rg 'TypeScript action probe' .tools/game/BepInEx/LogOutput.log
```

Require compiler/preflight PASS and native probe ready, custom-node selection,
two doAFlip completions (400 operations each at default speed),
`two doAFlip calls completed ALL PASS`, and `worker released`.
Only one marked run is accepted per launch. For Stop validation, restart, run
again, and press Stop during the first flip. Require worker released without a
second completed action or ALL PASS; an already applied animation may finish.
Disable Enabled and RunCompiledActionProbe after testing.

## Evidence and remaining work

Locked builds and desktop runtime/mailbox/compiler suites pass, including the
exact fixed-source compiler helper and its Jint preflight. Unity/Mono normal and Stop runs passed on CachyOS/Proton on 2026-11-07.
The normal screenshot confirms two completions at 400 operations each, ALL PASS,
and worker released. The Stop screenshot and fresh BepInEx log confirm worker
released without completed-action or ALL PASS markers. The Stop run overwrote
the normal log, so normal-run evidence is screenshot-only. All local diagnostic
settings are now disabled. This verifies these two manual scenarios, not all
cancellation races. The diagnostic still suspends world timers while awaiting
worker requests. Source locations refer to the trigger, not the compiled source.
Editor integration, production scheduling, source-map error display, repeated runs,
world changes, additional APIs, and other platforms remain unverified.
