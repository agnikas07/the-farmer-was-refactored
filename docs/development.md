# Development on Linux / Proton

## Prerequisites

- A locally installed, owned Windows copy of The Farmer Was Replaced.
- Steam and Proton Experimental installed; keep Steam running when launching.
- Bash, Python 3 (used by Proton), and the .NET SDK pinned in `global.json`.
- Official BepInEx Windows x64 5.4.23.6 from its [release page](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6).

The current machine is already prepared. For a fresh checkout:

1. Install the pinned .NET SDK into `.tools/dotnet`, or make that path a symlink to a compatible SDK installation.
2. Copy the game installation into `.tools/game`; do not symlink the game directory or use hardlinks, since loader patching must stay isolated.
3. Extract the BepInEx archive into `.tools/game`, so `winhttp.dll` sits next to `TheFarmerWasReplaced.exe` and `BepInEx/core/BepInEx.dll` exists.
4. Back up normal saves before testing. The launcher creates a separate prefix at `.tools/compatdata`; it does not import normal saves.

Game binaries, the loader, the SDK, and saves are local prerequisites, not distributed in this repository.

## Build and deploy

Run from the repository root with the development game closed:

```bash
bash scripts/deploy-dev.sh
```

This builds the Release DLL, stages it in `artifacts/plugin`, and copies only our plugin into the isolated game's plugin directory. `bash scripts/build.sh` builds and stages without deploying.

The project accepts MSBuild `GameDirectory` and `BepInExDirectory` overrides for build references. Deployment and launch scripts always use `.tools/game`.

## Run and verify

```bash
bash scripts/launch-dev.sh
```

Wait for the main menu, close the game, then run in the same terminal:

```bash
rg 'The Farmer was Refactored' .tools/game/BepInEx/LogOutput.log
```

Expected marker: `The Farmer was Refactored loaded.` Also check the full log for errors and `Chainloader startup complete`.

The log must come from the latest launch; an old success does not verify a new build. There are no visible TypeScript features yet.

The launcher sets the `winhttp` override needed by BepInEx and uses a separate Proton prefix. A normal Steam launch uses the original game installation instead.

For non-default installations, set `TFWR_STEAM_DIR` to the Steam directory and/or `TFWR_PROTON_DIR` to the Proton directory containing the `proton` script.

## Troubleshooting

- Missing loader or Unity reference: check the development-copy layout above.
- Missing Proton: install it through Steam or set `TFWR_PROTON_DIR`.
- Build succeeds but no startup marker: inspect the complete BepInEx log and terminal output; do not assume that compilation proves loading.
- In a restricted shell, .NET may fail to create its runtime mutex under `/run/user`. For the build command only, use a writable private runtime directory:

```bash
mkdir -p .tools/runtime
chmod 700 .tools/runtime
XDG_RUNTIME_DIR="$PWD/.tools/runtime" bash scripts/build.sh
```

Do not apply that runtime-directory override to the graphical game launch.
