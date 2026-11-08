#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
source scripts/dev-env.sh
[[ "$(uname -s)" == Linux && "$(uname -m)" == x86_64 ]] || {
    echo "This desktop probe currently targets Linux x64 only." >&2
    exit 1
}
mkdir -p work/compiler-host
probe_dir="$(mktemp -d "$PWD/work/compiler-host/v8.XXXXXX")"
trap 'rm -rf -- "$probe_dir"' EXIT
node tests/Compiler/prepare-host-probe.mjs "$probe_dir/bundle.json"
# Native Linux x64 desktop experiment, separate from the Unity plugin.
dotnet build tests/V8CompilerProbe/V8CompilerProbe.csproj --configuration Release -p:RestoreLockedMode=true
timeout 90s dotnet tests/V8CompilerProbe/bin/Release/net10.0/V8CompilerProbe.dll \
    "$probe_dir/bundle.json" "$PWD/tests/Compiler/loop-control-cases.json"
