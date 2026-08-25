#!/usr/bin/env bash
# Build BSD / generic Unix artifacts.
#  - freebsd-x64: self-contained tar.xz when the runtime pack is available,
#    otherwise falls back to a framework-dependent bundle (requires the
#    'dotnet' runtime on the target system).
#  - unix-generic: framework-dependent build runnable on any Unix with .NET.
set -euo pipefail
# shellcheck source=common.sh
source "$(dirname "$0")/common.sh"

have dotnet || die "dotnet SDK not found"
mkdir -p "$DIST"

pack_tar() {
    local name="$1" srcdir="$2"
    local out="$DIST/fModLoader-$VERSION-$name.tar.xz"
    rm -f "$out"
    tar -cJf "$out" -C "$srcdir" .
    ok "tar.xz -> $out"
}

# ── Generic Unix (framework-dependent, any Unix with .NET installed) ────
GEN="$DIST/unix-generic"
rm -rf "$GEN"
log "=== unix-generic (framework-dependent) ==="
dotnet publish "$FML_ROOT/cli/fModLoader_CLI.csproj" -c Release -f net8.0 -p:UseAppHost=false -o "$GEN"
dotnet publish "$FML_ROOT/app/fModLoader.csproj" -c Release -f net8.0 -p:UseAppHost=false -o "$GEN"
cat > "$GEN/README.txt" <<EOF
fModLoader $VERSION — generic Unix build

Requires the .NET 8 runtime:  https://dotnet.microsoft.com/download

Run:
  dotnet fModLoader.dll       # GUI (needs X11/Wayland)
  dotnet fModLoader_CLI.dll   # CLI

On FreeBSD install .NET via:  pkg install dotnet
EOF
pack_tar "unix-generic" "$GEN"

# ── FreeBSD ─────────────────────────────────────────────────────────────
FB="$DIST/stage-freebsd"
rm -rf "$FB"

log "=== freebsd-x64 (self-contained attempt) ==="
if publish_pair freebsd-x64 && [ -f "$DIST/freebsd-x64/fModLoader" ]; then
    mkdir -p "$FB"
    install -m755 "$DIST/freebsd-x64/fModLoader"     "$FB/fModLoader"
    install -m755 "$DIST/freebsd-x64/fModLoader_CLI" "$FB/fModLoader_CLI"
    cat > "$FB/README.txt" <<EOF
fModLoader $VERSION — FreeBSD x86-64 (self-contained)

Run:
  ./fModLoader        # GUI (needs X11/Wayland)
  ./fModLoader_CLI    # CLI
EOF
    pack_tar "freebsd-x64" "$FB"
else
    warn "Self-contained freebsd-x64 unavailable (no official runtime pack)."
    warn "Repacking the framework-dependent build for FreeBSD instead."
    mkdir -p "$FB"
    cp -r "$GEN/." "$FB/"
    pack_tar "freebsd-unix-generic" "$FB"
fi

log "BSD/Unix packaging complete."
