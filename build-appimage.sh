#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOTNET_BIN="${DOTNET_BIN:-$(command -v dotnet || echo "$HOME/.dotnet/dotnet")}"
APPIMAGETOOL_BIN="${APPIMAGETOOL_BIN:-$(command -v appimagetool || echo "$HOME/.local/bin/appimagetool")}"
APPDIR="$ROOT_DIR/AppDir"
OUT_APPIMAGE="$ROOT_DIR/PS4PKGTool-x86_64.AppImage"

echo "==> Publishing PS4PKGTool (linux-x64 self-contained)..."
rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin"

"$DOTNET_BIN" publish "$ROOT_DIR/PS4PKGTool/PS4PKGTool.csproj" \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:ErrorOnDuplicatePublishOutputFiles=false \
  -o "$APPDIR/usr/bin"

cat > "$APPDIR/AppRun" << 'EOF'
#!/usr/bin/env bash
set -e
HERE="$(dirname "$(readlink -f "${0}")")"
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0
export WEBKIT_DISABLE_COMPOSITING_MODE=1
exec "${HERE}/usr/bin/PS4PKGTool" "$@"
EOF

cat > "$APPDIR/ps4pkgtool.desktop" << 'EOF'
[Desktop Entry]
Type=Application
Name=PS4PKGTool
GenericName=PS4 Package Manager & Inspector
Comment=Native Linux PS4 PKG Library Manager, Asset Viewer, Merger, FFPFSC Converter & shadPS4 Manager
Exec=PS4PKGTool %f
Icon=ps4pkgtool
Terminal=false
Categories=Utility;Game;Emulator;
MimeType=application/x-ps4-pkg;
StartupWMClass=PS4PKGTool
EOF

chmod +x "$APPDIR/AppRun" "$APPDIR/usr/bin/PS4PKGTool"
cp "$ROOT_DIR/PS4PKGTool/Resources/PackageIcon.png" "$APPDIR/ps4pkgtool.png"
cp "$ROOT_DIR/PS4PKGTool/Resources/PackageIcon.png" "$APPDIR/.DirIcon"
mkdir -p "$APPDIR/usr/bin/Resources"
cp "$ROOT_DIR/PS4PKGTool/Resources/PackageIcon.png" "$APPDIR/usr/bin/Resources/PackageIcon.png"

echo "==> Packaging $OUT_APPIMAGE..."
ARCH=x86_64 "$APPIMAGETOOL_BIN" --no-appstream "$APPDIR" "$OUT_APPIMAGE"
chmod +x "$OUT_APPIMAGE"
echo "==> Done: $OUT_APPIMAGE"
