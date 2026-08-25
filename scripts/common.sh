#!/usr/bin/env bash
# Shared helpers for fModLoader packaging scripts.

FML_ROOT="$(cd "$(dirname "${BASH_SOURCE[1]}")/.." && pwd)"
DIST="$FML_ROOT/dist"
VERSION="${VERSION:-$(sed -n 's/.*AppVersion { get; } = "\([^"]*\)".*/\1/p' "$FML_ROOT/installer/Models/InstallerConfig.cs" | head -n1)}"
VERSION="${VERSION:-1.0.65}"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

log()  { printf '\033[1;36m==> %s\033[0m\n' "$*"; }
ok()   { printf '\033[1;32m ✔ %s\033[0m\n' "$*"; }
warn() { printf '\033[1;33m ! %s\033[0m\n' "$*"; }
die()  { printf '\033[1;31m ✘ %s\033[0m\n' "$*" >&2; exit 1; }

# dotnet publish <project> <rid> [extra args...] -> output dir
fml_publish() {
    local proj="$1" rid="$2"; shift 2
    local out="$$"
    local dest="$DIST/$rid"
    log "Publishing $(basename "$proj") ($rid)"
    dotnet publish "$FML_ROOT/$proj" -c Release -f net8.0 -r "$rid" \
        --self-contained true -p:PublishSingleFile=true \
        -o "$dest" "$@" || return 1
    ok "$(basename "$proj") -> $dest"
}

# Publish both app and cli for a RID. Returns 1 if the app failed.
publish_pair() {
    local rid="$1"; shift
    fml_publish "cli/fModLoader_CLI.csproj"   "$rid" "$@" || return 1
    fml_publish "app/fModLoader.csproj"       "$rid" "$@" || return 1
}

have() { command -v "$1" >/dev/null 2>&1; }
