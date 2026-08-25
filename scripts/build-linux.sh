#!/usr/bin/env bash
# Build all Linux packages: tar.xz (x64+arm64), .deb, .rpm, .AppImage.
# Run on a Linux host (or any host with the required tools).
set -euo pipefail
# shellcheck source=common.sh
source "$(dirname "$0")/common.sh"

have dotnet || die "dotnet SDK not found"
mkdir -p "$DIST"

DEB_ARCH() { case "$1" in linux-x64) echo amd64;; linux-arm64) echo arm64;; *) echo "$1";; esac; }
RPM_ARCH() { case "$1" in linux-x64) echo x86_64;; linux-arm64) echo aarch64;; *) echo "$1";; esac; }

# Stage a FHS-style tree: usr/bin/{fModLoader,fModLoader_CLI} + desktop + icon + metainfo
stage_tree() {
    local rid="$1" dest="$2"
    rm -rf "$dest"
    mkdir -p "$dest/usr/bin" \
             "$dest/usr/share/applications" \
             "$dest/usr/share/metainfo" \
             "$dest/usr/share/icons/hicolor/scalable/apps"

    install -m755 "$DIST/$rid/fModLoader"     "$dest/usr/bin/fModLoader"
    install -m755 "$DIST/$rid/fModLoader_CLI" "$dest/usr/bin/fModLoader_CLI"
    install -m644 "$FML_ROOT/packaging/linux/fmodloader.desktop"       "$dest/usr/share/applications/fmodloader.desktop"
    install -m644 "$FML_ROOT/assets/fmodloader.svg"                    "$dest/usr/share/icons/hicolor/scalable/apps/fmodloader.svg"
    install -m644 "$FML_ROOT/packaging/linux/fmodloader.metainfo.xml"  "$dest/usr/share/metainfo/com.nexustribarixa.fModLoader.metainfo.xml"
}

for RID in linux-x64 linux-arm64; do
    log "=== $RID ==="
    publish_pair "$RID"

    # ── tar.xz ──────────────────────────────────────────────────────────
    TAROUT="$DIST/fModLoader-$VERSION-$RID.tar.xz"
    stage_tree "$RID" "$DIST/stage-tar"
    tar -cJf "$TAROUT" -C "$DIST/stage-tar" usr
    ok "tar.xz -> $TAROUT"

    # ── .deb ────────────────────────────────────────────────────────────
    if have dpkg-deb; then
        DEBROOT="$DIST/deb-root"
        stage_tree "$RID" "$DEBROOT"
        mkdir -p "$DEBROOT/DEBIAN"
        cat > "$DEBROOT/DEBIAN/control" <<EOF
Package: fmodloader
Version: $VERSION
Section: graphics
Priority: optional
Architecture: $(DEB_ARCH "$RID")
Maintainer: Nexus Tribarixa <nexustribarixa@users.noreply.github.com>
Installed-Size: $(du -sk "$DEBROOT/usr" | cut -f1)
Depends: libfontconfig1 | fonts-liberation
Description: Dynamic font glyph modification utility
 fModLoader (FML) applies .ttfm/.otfm glyph mods onto compatible
 (.modcompat) TrueType/OpenType fonts. Includes CLI helper.
Homepage: https://github.com/nexustribarixa-redaamakrane/fmodloader
EOF
        dpkg-deb --build --root-owner-group "$DEBROOT" \
            "$DIST/fmodloader_${VERSION}_$(DEB_ARCH "$RID").deb" >/dev/null
        ok "deb -> fmodloader_${VERSION}_$(DEB_ARCH "$RID").deb"
    else
        warn "dpkg-deb not found — skipping .deb"
    fi

    # ── .rpm ────────────────────────────────────────────────────────────
    HOST_ARCH=$(uname -m)
    RPM_TARGET="$(RPM_ARCH "$RID")"
    if have rpmbuild && [ "$RPM_TARGET" = "$HOST_ARCH" ]; then
        RPROOT="$HOME/rpmbuild"
        mkdir -p "$RPROOT"/{BUILD,SOURCES,SPECS,RPMS,SRPMS}
        stage_tree "$RID" "$DIST/stage-rpm"
        GZ="$RPROOT/SOURCES/fmodloader-$VERSION.tar.gz"
        tar -czf "$GZ" -C "$DIST/stage-rpm" usr
        cat > "$RPROOT/SPECS/fmodloader.spec" <<EOF
Name:           fmodloader
Version:        $VERSION
Release:        1%{?dist}
Summary:        Dynamic font glyph modification utility
License:        GPL-3.0-or-later
URL:            https://github.com/nexustribarixa-redaamakrane/fmodloader
Source0:        fmodloader-%{version}.tar.gz
BuildArch:      $(RPM_ARCH "$RID")
AutoReqProv:    no

%description
fModLoader (FML) applies .ttfm/.otfm glyph mods onto compatible
(.modcompat) TrueType/OpenType fonts. Includes CLI helper.

%prep
%setup -c -q

%install
mkdir -p %{buildroot}
cp -r usr %{buildroot}/

%files
%{_bindir}/fModLoader
%{_bindir}/fModLoader_CLI
/usr/share/applications/fmodloader.desktop
/usr/share/icons/hicolor/scalable/apps/fmodloader.svg
/usr/share/metainfo/com.nexustribarixa.fModLoader.metainfo.xml
EOF
        rpmbuild -bb --quiet --target "$(RPM_ARCH "$RID")" "$RPROOT/SPECS/fmodloader.spec" >/dev/null 2>&1 || \
            rpmbuild -bb --target "$(RPM_ARCH "$RID")" "$RPROOT/SPECS/fmodloader.spec"
        find "$RPROOT/RPMS" -name 'fmodloader-*.rpm' -exec cp {} "$DIST/" \;
        ok "rpm -> $DIST"
    else
        if ! have rpmbuild; then
            warn "rpmbuild not found — skipping .rpm"
        else
            warn "Skipping .rpm for non-native arch ($RID on $HOST_ARCH host)"
        fi
    fi

    # ── AppImage (x64 only) ─────────────────────────────────────────────
    if [ "$RID" = "linux-x64" ]; then
        APPDIR="$DIST/AppDir"
        stage_tree "$RID" "$APPDIR"
        LDEPLOY="$DIST/linuxdeploy-x86_64.AppImage"
        if [ ! -f "$LDEPLOY" ]; then
            log "Fetching linuxdeploy"
            curl -fsSL -o "$LDEPLOY" \
                "https://github.com/linuxdeploy/linuxdeploy/releases/download/continuous/linuxdeploy-x86_64.AppImage" || true
        fi
        if [ -f "$LDEPLOY" ]; then
            chmod +x "$LDEPLOY"
            export APPIMAGE_EXTRACT_AND_RUN=1
            "$LDEPLOY" --appdir "$APPDIR" \
                -e "$APPDIR/usr/bin/fModLoader" \
                -d "$APPDIR/usr/share/applications/fmodloader.desktop" \
                -i "$APPDIR/usr/share/icons/hicolor/scalable/apps/fmodloader.svg" \
                --output appimage --verbosity=1 || warn "linuxdeploy failed"
            mv "$FML_ROOT"/fModLoader-*.AppImage "$DIST/" 2>/dev/null || true
            ls "$DIST"/*.AppImage >/dev/null 2>&1 && ok "AppImage -> $DIST"
        else
            warn "linuxdeploy unavailable — skipping AppImage"
        fi
    fi
done

log "Linux packaging complete. Artifacts:"
ls -lh "$DIST" 2>/dev/null | grep -E '\.(tar\.xz|deb|rpm|AppImage)$' || warn "no artifacts found"
