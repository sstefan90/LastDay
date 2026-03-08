#!/bin/bash
# ─────────────────────────────────────────────────────────────────────────────
#  Last Day — macOS Permission Fix
#  Double-click this file (or run from Terminal) from the same folder as
#  "Last Day.app" to fix Gatekeeper quarantine and code-signing.
#
#  Required because distributing a Unity app outside the Mac App Store means
#  macOS quarantines it and strips execute bits. The LLMUnity native library
#  also needs the disable-library-validation entitlement so macOS loads it.
# ─────────────────────────────────────────────────────────────────────────────

# Make this script executable in case it lost its permissions during transfer.
chmod +x "${BASH_SOURCE[0]}" 2>/dev/null || true

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP="$SCRIPT_DIR/Last Day.app"

echo ""
echo "========================================="
echo "  Last Day — macOS Permission Fix"
echo "========================================="
echo ""

if [ ! -d "$APP" ]; then
    echo "ERROR: Could not find 'Last Day.app' in the same folder as this script."
    echo "Please make sure both files are in the same folder and try again."
    echo ""
    read -p "Press Enter to close..."
    exit 1
fi

echo "Found: $APP"
echo ""

# ── Step 1: Remove quarantine ──────────────────────────────────────────────
echo "Step 1/4: Removing quarantine flag..."
xattr -rd com.apple.quarantine "$APP" 2>/dev/null || true
echo "  Done."

# ── Step 2: Restore execute permissions ────────────────────────────────────
echo "Step 2/4: Restoring execute permissions..."
chmod +x "$APP/Contents/MacOS/Last Day" 2>/dev/null || true
find "$APP/Contents/Frameworks" -name "*.dylib" -exec chmod +x {} \; 2>/dev/null || true
find "$APP/Contents/PlugIns"    -exec chmod +x {} \; 2>/dev/null || true
echo "  Done."

# ── Step 3: Build entitlements plist ──────────────────────────────────────
# LLMUnity's llamacpp native library needs:
#   • allow-unsigned-executable-memory  — to mmap model weights as executable
#   • disable-library-validation        — to load its own unsigned dylibs
echo "Step 3/4: Preparing entitlements..."
ENTITLEMENTS=$(mktemp /tmp/lastday.XXXXXX.plist)
cat > "$ENTITLEMENTS" << 'PLIST_EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
  "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.cs.allow-jit</key>
    <true/>
    <key>com.apple.security.cs.allow-unsigned-executable-memory</key>
    <true/>
    <key>com.apple.security.cs.disable-library-validation</key>
    <true/>
</dict>
</plist>
PLIST_EOF
echo "  Done."

# ── Step 4: Re-sign ────────────────────────────────────────────────────────
# Sign inside-out: individual dylibs first, then bundles, then the app.
# LLMUnity puts its native libraries in StreamingAssets (not Frameworks),
# so we must recurse the entire bundle — not just standard subdirectories.
echo "Step 4/4: Re-signing the app (this may take a moment)..."

# All dylibs anywhere in the bundle (covers Frameworks AND StreamingAssets/LlamaLib)
find "$APP" -name "*.dylib" | while read -r lib; do
    codesign --force --sign - --entitlements "$ENTITLEMENTS" "$lib" 2>/dev/null || true
done

# .bundle plugins
find "$APP" -name "*.bundle" | while read -r b; do
    codesign --force --sign - --entitlements "$ENTITLEMENTS" "$b" 2>/dev/null || true
done

# .framework bundles
find "$APP" -name "*.framework" | while read -r fw; do
    codesign --force --sign - --entitlements "$ENTITLEMENTS" "$fw" 2>/dev/null || true
done

# Finally the app bundle itself
codesign --force --sign - --entitlements "$ENTITLEMENTS" "$APP"

rm -f "$ENTITLEMENTS"
echo "  Done."

echo ""
echo "========================================="
echo "  All done! You can now open Last Day.app"
echo "  (right-click → Open on first launch)."
echo "========================================="
echo ""
read -p "Press Enter to close this window..."
