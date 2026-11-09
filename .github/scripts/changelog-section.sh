#!/usr/bin/env bash
# Prints the CHANGELOG.md section of the given version (without its heading), for use as release notes.
# Exits with 1 when the version has no section, or when the section is empty.
#
# Usage: .github/scripts/changelog-section.sh 0.2.0 [path/to/CHANGELOG.md]
set -euo pipefail

version="${1:?usage: changelog-section.sh <version> [changelog]}"
changelog="${2:-CHANGELOG.md}"

notes="$(awk -v heading="## [${version}]" '
  /^## \[/ {
    if (found) exit
    if (index($0, heading) == 1) { found = 1; next }
  }
  found { print }
' "$changelog")"

# Trim blank lines around the section.
notes="$(printf '%s\n' "$notes" | sed -e '/./,$!d' | sed -e ':a' -e '/^\n*$/{$d;N;ba' -e '}')"

if [ -z "$notes" ]; then
  echo "CHANGELOG.md has no entry for version ${version}: add a '## [${version}] - YYYY-MM-DD' section first." >&2
  exit 1
fi

printf '%s\n' "$notes"
