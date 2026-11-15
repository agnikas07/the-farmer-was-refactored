#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
source scripts/dev-env.sh
bash scripts/build.sh
timeout 15s dotnet run --project tests/RuntimeProbe/RuntimeProbe.csproj --configuration Release
# Keep the labeled-continue regression in the normal runtime gate.
node tests/Compiler/check-loop-control.mjs tests/Compiler/loop-control-cases.json
timeout 20s dotnet run --no-build --project tests/RuntimeProbe/RuntimeProbe.csproj \
    --configuration Release -- --loop-control tests/Compiler/loop-control-cases.json
