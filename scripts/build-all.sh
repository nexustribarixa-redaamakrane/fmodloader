#!/usr/bin/env bash
# Build everything for the current host OS.
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"

case "$(uname -s)" in
    Linux)  exec bash "$HERE/build-linux.sh" ;;
    Darwin) exec bash "$HERE/build-macos.sh" ;;
    FreeBSD|NetBSD|OpenBSD|DragonFly) exec bash "$HERE/build-bsd.sh" ;;
    *) echo "Unsupported host: $(uname -s). Use scripts/build-linux.sh / -macos.sh / -bsd.sh directly." >&2; exit 1 ;;
esac
