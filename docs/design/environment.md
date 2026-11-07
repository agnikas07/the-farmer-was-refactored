# Initial development environment

Inspected Steam build: `26462009`.

- Host: CachyOS, x86-64.
- Installed game: Windows x64 build through Proton.
- Unity: `6000.0.43f1`, Mono backend (managed assemblies and MonoBleedingEdge present).
- UnityEngine.CoreModule references .NET Standard 2.1; plugin target: `netstandard2.1`.
- Loader: official BepInEx Windows x64 `5.4.23.6`.
- Loader archive SHA-256: `6c57a714a892efb4e9e1fef63d608a3391f30ec984031c465404e4ee381a0755` (checked against GitHub release metadata).

Local game and loader files are copied into ignored `.tools/game`. Original saves are backed up under ignored `work/save-backups/initial`. Development execution must use an isolated Proton prefix under `.tools/compatdata`.

The first plugin only logs startup information. No language or gameplay hooks are installed yet. Windows and macOS validation remains pending.

## Verification

On 2026-11-07, the user launched the isolated copy with `bash scripts/launch-dev.sh`. The on-disk BepInEx log confirms plugin version `0.1.0`, the startup marker, and chainloader startup completion. This verifies loading only, not gameplay or TypeScript support.

## Sources

- https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/2_plugin_start.html
- https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6
- https://github.com/metruzanca/lucy-was-replaced/blob/main/docs/INSTALL.md
