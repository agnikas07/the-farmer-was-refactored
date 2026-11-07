#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
source scripts/dev-env.sh
bash scripts/build.sh
timeout 15s dotnet run --project tests/RuntimeProbe/RuntimeProbe.csproj --configuration Release
