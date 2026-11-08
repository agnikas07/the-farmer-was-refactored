#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
source scripts/dev-env.sh
mkdir -p work/compiler-host
probe_dir="$(mktemp -d "$PWD/work/compiler-host/run.XXXXXX")"
trap 'rm -rf -- "$probe_dir"' EXIT
node tests/Compiler/prepare-host-probe.mjs "$probe_dir/bundle.json"
bash scripts/build.sh
# This exploratory check currently exits nonzero; see the compiler-host findings.
timeout 90s dotnet run --project tests/RuntimeProbe/RuntimeProbe.csproj \
    --configuration Release -- --compiler-host "$probe_dir/bundle.json"
