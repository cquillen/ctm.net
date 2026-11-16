#!/usr/bin/env sh
# The gate for this repository: run it before pushing. CI runs exactly this script.
set -eu

cd "$(dirname "$0")/.."

sh eng/check-no-spec.sh
dotnet restore
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release
dotnet format --verify-no-changes --no-restore
