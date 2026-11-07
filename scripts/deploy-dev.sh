#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
bash scripts/build.sh
[[ -f .tools/game/BepInEx/core/BepInEx.dll ]] || { echo "Development loader missing" >&2; exit 1; }
mkdir -p .tools/game/BepInEx/plugins/TheFarmerWasRefactored
cp artifacts/plugin/*.dll .tools/game/BepInEx/plugins/TheFarmerWasRefactored/
echo "Deployed to the isolated .tools/game copy."
