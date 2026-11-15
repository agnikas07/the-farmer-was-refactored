#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
source scripts/dev-env.sh
cases="$PWD/tests/Compiler/loop-control-cases.json"
node tests/Compiler/check-loop-control.mjs "$cases"
bash scripts/build.sh
# Standalone loop-control regression; also included in test-runtime.sh.
timeout 20s dotnet run --project tests/RuntimeProbe/RuntimeProbe.csproj \
    --configuration Release -- --loop-control "$cases"
