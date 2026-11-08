# V8 compiler to Jint player probe

PR #19 verified compilation in V8 under Unity/Mono, with emitted code executed
in another V8 engine. This follow-up checks the intended compiler/player boundary:
V8 compiles TypeScript, then only the emitted JavaScript string reaches Jint 4.17.0.
The player runtime remains a candidate; COMP-001's labeled-loop bug is still open.

The existing semantic-error, source-map, snake_case/module rejection, and recovery
checks are reused. Valid fixtures include a typed loop, two direct calls, and
an enum/generic/optional-access example. Each runs in a fresh bounded Jint engine
with only a counting doAFlip callback. It must call twice on the worker thread.
A second engine with a one-statement budget must stop the same emitted code
before both calls complete. That budget is only a test, not gameplay accounting.

The compiler never calls farm APIs. This is not native scheduler integration;
there are no drone actions, editor changes, or saved player scripts.

## Manual check

The probe is disabled by default. Close the game before deploying:

```bash
bash scripts/deploy-v8-probe.sh
```

In `.tools/game/BepInEx/config/io.github.agnikas07.thefarmerwasrefactored.v8probe.cfg`:

```ini
[Diagnostics]
Enabled = true
RunCompilerProbe = false
RunJintPipelineProbe = true
```

The pipeline setting implies compilation and takes precedence over the older
compiler-only mode. Launch, wait at the title screen about 15 seconds, close:

```bash
bash scripts/launch-dev.sh
rg 'V8 to Jint game probe' .tools/game/BepInEx/LogOutput.log
```

Require seven checks (load plus six cases) and `V8 to Jint game probe: ALL PASS`.
Valid-case PASS includes both successful execution and the budget check. An older
loader/compiler ALL PASS marker is insufficient. Disable all three settings afterward.

## Evidence and limits

Locked Unity netstandard2.1 and Linux desktop builds pass with zero warnings/errors.
`scripts/probe-v8-compiler-host.sh` now runs the same checks with both V8 and Jint
output execution; all pass on desktop. That runner uses NuGet's desktop Jint assets;
the Unity build stages the netstandard assets. Manual Unity/Proton testing is pending.

All input is trusted diagnostic text. Jint has a two-second execution timeout,
1,000-statement normal budget, 8 MB allocation limit, and recursion limit 64.
The V8 compiler still only checks shutdown between cases, not during compilation.
The main plugin's native-action probe is unchanged and disabled. Once this passes,
the next integration is compilation followed by native action-mailbox delivery.
