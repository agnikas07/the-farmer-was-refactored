#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
source scripts/dev-env.sh
dotnet build src/Plugin/TheFarmerWasRefactored.Plugin.csproj --configuration Release "$@"
mkdir -p artifacts/plugin
cp src/Plugin/bin/Release/netstandard2.1/*.dll artifacts/plugin/
