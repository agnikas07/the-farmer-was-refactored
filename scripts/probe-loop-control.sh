#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
source scripts/dev-env.sh
cases="$PWD/tests/Compiler/loop-control-cases.json"
node tests/Compiler/check-loop-control.mjs "$cases"
bash scripts/build.sh
# Separate exploratory probe: currently fails three cases under pinned Jint.
timeout 20s dotnet run --project tests/RuntimeProbe/RuntimeProbe.csproj \
    --configuration Release -- --loop-control "$cases"
