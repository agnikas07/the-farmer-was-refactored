#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
source scripts/dev-env.sh
assembly="${TFWR_GAME_DIR:-$PWD/.tools/game}/TheFarmerWasReplaced_Data/Managed/Core.dll"
tool="$PWD/.tools/ilspy/ilspycmd"
[[ -f "$assembly" ]] || { echo "Core.dll missing; set TFWR_GAME_DIR to the local game copy." >&2; exit 1; }
[[ -x "$tool" ]] || {
    echo "Install the inspection tool: dotnet tool install ilspycmd --version 11.2.0.10050 --tool-path .tools/ilspy" >&2
    exit 1
}
# Proprietary source stays in ignored scratch storage, never in distributable artifacts.
mkdir -p work/inspection
sha256sum "$assembly" > work/inspection/Core.sha256
"$tool" --version > work/inspection/tool-version.txt
for type in MainSim Simulation Execution ProgramState BuiltinFunctions CallNode PyFunction Drone Farm CodeWindow ISimRunner; do
    "$tool" -t "$type" "$assembly" > "work/inspection/$type.cs"
done
echo "Local inspection output: work/inspection (ignored; do not commit or distribute)."
