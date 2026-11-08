#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
source scripts/dev-env.sh
mkdir -p .tools/runtime
chmod 700 .tools/runtime
# The diagnostic now calls the main plugin after successful compilation.
XDG_RUNTIME_DIR="$PWD/.tools/runtime" bash scripts/deploy-dev.sh
XDG_RUNTIME_DIR="$PWD/.tools/runtime" dotnet build \
  src/V8Probe/TheFarmerWasRefactored.V8Probe.csproj -c Release -p:RestoreLockedMode=true
mkdir -p work/compiler-host
node tests/Compiler/prepare-host-probe.mjs work/compiler-host/game-bundle.json
# This diagnostic has its own directory and is not included by deploy-dev.sh.
python3 - <<'PY'
import json
import os
from pathlib import Path
import shutil

output = Path('src/V8Probe/bin/Release/netstandard2.1')
stage = Path('artifacts/v8-probe')
destination = Path('.tools/game/BepInEx/plugins/TheFarmerWasRefactored.V8Probe')
if stage.exists():
    shutil.rmtree(stage)
stage.mkdir(parents=True)
for dll in output.glob('*.dll'):
    # Unity supplies framework assemblies; avoid shadowing those with NuGet copies.
    if (Path('.tools/game/TheFarmerWasReplaced_Data/Managed') / dll.name).exists():
        continue
    shutil.copy2(dll, stage)
shutil.copy2('work/compiler-host/game-bundle.json', stage / 'compiler-bundle.json')
ts_notices = stage / 'licenses' / 'TypeScript'
ts_notices.mkdir(parents=True)
for name in ('LICENSE.txt', 'ThirdPartyNoticeText.txt'):
    shutil.copy2(Path('node_modules/typescript') / name, ts_notices)
packages = json.loads(Path('src/V8Probe/packages.lock.json').read_text())
for name, package in packages['dependencies']['.NETStandard,Version=v2.1'].items():
    if package['type'] == 'Project':
        continue
    root = Path(os.environ['NUGET_PACKAGES']) / name.lower() / package['resolved']
    notices = stage / 'licenses' / name
    for source in root.iterdir():
        if source.name.lower().startswith(('license', 'notice')):
            notices.mkdir(parents=True, exist_ok=True)
            target = notices / source.name
            if source.is_dir():
                shutil.copytree(source, target)
            else:
                shutil.copy2(source, target)
shutil.copytree(stage, destination, dirs_exist_ok=True)
print(f'Deployed V8 diagnostic to {destination}; enable its separate config to run.')
PY
