# V8 in-game loader probe

This opt-in, standalone BepInEx plugin checks ClearScript/V8 7.5.1.1 in the
Windows x64 Unity/Mono process (including Proton). The Linux desktop compiler
comparison passed. On 2026-11-07, the user screenshot and fresh BepInEx log
confirmed all loader checks under Unity/Mono on CachyOS/Proton: worker thread
10 versus Unity thread 1, native evaluation/loop control PASS, disposal/fresh
engine PASS, and ALL PASS. The local diagnostic was disabled afterward.
This verifies the loader only; in-game TypeScript compilation is the next test.

The worker creates V8, evaluates arithmetic and the labeled-loop reproduction,
disposes the engine, then creates and disposes a fresh engine. Unity Update
only drains log messages. The loader-only mode has no farm actions or TypeScript compilation.
The separately enabled [compiler probe](v8-game-compiler-probe.md) adds compilation checks.
Fixed scripts terminate themselves; this is not a cancellation/lifecycle design.

## Run locally

Close the development game before deploying:

```bash
bash scripts/deploy-v8-probe.sh
```

The plugin defaults to disabled. Launch once to create its config, then close
and set `[Diagnostics] Enabled = true` in
`.tools/game/BepInEx/config/io.github.agnikas07.thefarmerwasrefactored.v8probe.cfg`.
Launch again, wait several seconds at the title screen, then close:

```bash
bash scripts/launch-dev.sh
rg 'V8 loader probe' .tools/game/BepInEx/LogOutput.log
```

Require `native evaluation and loop control PASS` with different worker/Unity
thread IDs, `disposal and fresh engine PASS`, and `ALL PASS`. A starting line
alone is not success. Failures include exception details; if no lines appear,
inspect the full BepInEx log for assembly/plugin loading errors.

The deployment script stages Windows native V8, managed dependencies, and
package notices in a separate plugin directory, omitting framework DLLs already
supplied by Unity. Assembly resolution is process-wide, so this is not dependency
isolation. The deployment script also updates the main plugin, now required by the optional
[compiled-action mode](typescript-native-actions.md). No binaries enter Git.
Set Enabled back to false after testing; remove the diagnostic directory when
its dependencies are no longer needed. Other operating systems, compiler
hosting, player scripts, and shutdown during native loading remain unverified.
