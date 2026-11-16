#!/usr/bin/env sh
# Fails if an OpenAPI or Swagger document is present anywhere in the working tree.
#
# BMC's API spec file is confidential and must never enter this repository. The only
# OpenAPI documents allowed are the small ones written for this project's own
# spec-check tests, which live under the folder named in ALLOWED.
set -eu

ALLOWED='tests/ControlM.SpecCheck.Tests/TestData/'
MARKER='^[[:space:]]*["'\'']?(swagger|openapi)["'\'']?[[:space:]]*:[[:space:]]*["'\'']?[0-9]'

cd "$(dirname "$0")/.."

found=$(git ls-files --cached --others --exclude-standard -- '*.json' '*.yaml' '*.yml' \
  | grep -v "^$ALLOWED" \
  | while IFS= read -r file; do
      if [ -f "$file" ] && grep -Eqi "$MARKER" "$file"; then
        printf '%s\n' "$file"
      fi
    done)

if [ -n "$found" ]; then
  echo "error: OpenAPI or Swagger documents found outside $ALLOWED" >&2
  printf '%s\n' "$found" | sed 's/^/  /' >&2
  echo "BMC's spec file must never be committed. Remove these files." >&2
  exit 1
fi

echo "No OpenAPI or Swagger documents outside $ALLOWED"
