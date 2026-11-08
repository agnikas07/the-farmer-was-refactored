#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
source scripts/dev-env.sh
# A fresh directory prevents a failed compile from reusing earlier successful JS.
mkdir -p work/compiler-jint
fixture_dir="$(mktemp -d "$PWD/work/compiler-jint/run.XXXXXX")"
trap 'rm -rf -- "$fixture_dir"' EXIT
npm run test:compiler
node tests/Compiler/emit-jint-cases.mjs "$fixture_dir/cases.json"
bash scripts/build.sh
timeout 20s dotnet run --project tests/RuntimeProbe/RuntimeProbe.csproj \
    --configuration Release -- --compiled-typescript "$fixture_dir/cases.json"
