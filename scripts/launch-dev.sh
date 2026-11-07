#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.."
repo_dir="$PWD"
steam_dir="${TFWR_STEAM_DIR:-$HOME/.local/share/Steam}"
proton_dir="${TFWR_PROTON_DIR:-$steam_dir/steamapps/common/Proton - Experimental}"
[[ -f "$proton_dir/proton" ]] || { echo "Set TFWR_PROTON_DIR to an installed Proton directory." >&2; exit 1; }
[[ -f .tools/game/BepInEx/plugins/TheFarmerWasRefactored/TheFarmerWasRefactored.dll ]] || { echo "Run bash scripts/deploy-dev.sh first." >&2; exit 1; }
mkdir -p .tools/compatdata
export STEAM_COMPAT_CLIENT_INSTALL_PATH="$steam_dir"
export STEAM_COMPAT_DATA_PATH="$repo_dir/.tools/compatdata"
export WINEDLLOVERRIDES="winhttp=n,b${WINEDLLOVERRIDES:+;$WINEDLLOVERRIDES}"
export SteamAppId=2060160
export SteamGameId=2060160
export PYTHONDONTWRITEBYTECODE=1
cd .tools/game
exec python3 "$proton_dir/proton" run "$PWD/TheFarmerWasReplaced.exe" "$@"
