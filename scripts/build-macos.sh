#!/usr/bin/env bash
# Build macOS .app bundles and .dmg images (osx-x64 + osx-arm64).
# Run on a macOS host.
set -euo pipefail
# shellcheck source=common.sh
source "$(dirname "$0")/common.sh"

have dotnet || die "dotnet SDK not found"
mkdir -p "$DIST"

make_app() {
    local rid="$1" appdir="$2"
    rm -rf "$appdir"
    mkdir -p "$appdir/Contents/MacOS" "$appdir/Contents/Resources"

    install -m755 "$DIST/$rid/fModLoader"     "$appdir/Contents/MacOS/fModLoader"
    install -m755 "$DIST/$rid/fModLoader_CLI" "$appdir/Contents/MacOS/fModLoader_CLI"
    install -m644 "$FML_ROOT/packaging/mac/Info.plist" "$appdir/Contents/Info.plist"

    # Icon: rasterize SVG then build .icns when iconutil is available.
    if have iconutil && [ -f "$FML_ROOT/assets/fmodloader.svg" ]; then
        local iconset="$DIST/fmodloader.iconset"
        rm -rf "$iconset"; mkdir -p "$iconset"
        if have rsvg-convert; then
            for s in 16 32 128 256 512; do
                rsvg-convert -w "$s" -h "$s" "$FML_ROOT/assets/fmodloader.svg" -o "$iconset/icon_${s}x$s.png"
            done
        elif have magick; then
            for s in 16 32 128 256 512; do
                magick -background none "$FML_ROOT/assets/fmodloader.svg" -resize "${s}x${s}" "$iconset/icon_${s}x$s.png"
            done
        fi
        if compgen -G "$iconset/*.png" >/dev/null; then
            iconutil -c icns "$iconset" -o "$appdir/Contents/Resources/fmodloader.icns" || true
        fi
    fi

    # Ad-hoc codesign so the bundle launches locally without warnings.
    if have codesign; then
        codesign --force --deep --sign - "$appdir" || warn "codesign failed (continuing)"
    fi
}

for RID in osx-x64 osx-arm64; do
    log "=== $RID ==="
    publish_pair "$RID"

    APP="fModLoader.app"
    make_app "$RID" "$DIST/$APP"

    DMG="$DIST/fModLoader-$VERSION-$RID.dmg"
    rm -f "$DMG"
    if have hdiutil; then
        hdiutil create -volname "fModLoader $VERSION" \
            -srcfolder "$DIST/$APP" -ov -format UDZO "$DMG"
        ok "dmg -> $DMG"
    else
        warn "hdiutil not found — skipping dmg"
    fi

    TARBALL="$DIST/fModLoader-$VERSION-$RID.tar.gz"
    tar -czf "$TARBALL" -C "$DIST" "$APP"
    ok "tar.gz -> $TARBALL"
done

log "macOS packaging complete. Artifacts:"
ls -lh "$DIST" | grep -E '\.(dmg|tar\.gz)$' || warn "no artifacts found"
