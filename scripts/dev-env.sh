# Source this file from bash: source scripts/dev-env.sh
_tfwr_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
export DOTNET_ROOT="$_tfwr_root/.tools/dotnet"
export DOTNET_CLI_HOME="$_tfwr_root/.tools/dotnet-home"
export NUGET_PACKAGES="$_tfwr_root/.tools/nuget/packages"
export PATH="$DOTNET_ROOT:$PATH"
unset _tfwr_root
