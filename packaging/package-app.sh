#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat >&2 <<'EOF'
Usage: packaging/package-app.sh <rid> [configuration] [output-root] [bridge-path]

Examples:
  packaging/package-app.sh osx-arm64 Release artifacts
  packaging/package-app.sh linux-x64 Release artifacts artifacts/native/linux-x64/libxcanet_ossl_bridge.so
EOF
}

if [[ "${1:-}" == "" ]]; then
  usage
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet SDK not found. Install .NET 10 before packaging the application." >&2
  exit 1
fi

RID="$1"
CONFIGURATION="${2:-Release}"
OUTPUT_ROOT="${3:-artifacts}"
BRIDGE_PATH="${4:-}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd -L)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd -L)"
PUBLISH_DIR="$REPO_ROOT/$OUTPUT_ROOT/publish/$RID/$CONFIGURATION/app"
PACKAGE_DIR="$REPO_ROOT/$OUTPUT_ROOT/packages/$RID/$CONFIGURATION"
PROJECT_RELATIVE_PATH="src/XcaNet.App.Desktop/XcaNet.App.Desktop.csproj"
VERSION="$(sed -n 's:.*<VersionPrefix>\(.*\)</VersionPrefix>.*:\1:p' "$REPO_ROOT/Directory.Build.props" | head -n 1)"

case "$RID" in
  win-x64|linux-x64|osx-x64|osx-arm64) ;;
  *)
    echo "Unsupported RID '$RID'. Supported RIDs: win-x64, linux-x64, osx-x64, osx-arm64." >&2
    exit 1
    ;;
esac

mkdir -p "$PUBLISH_DIR" "$PACKAGE_DIR"

pushd "$REPO_ROOT" >/dev/null
DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/dotnet-home}" \
DOTNET_SKIP_FIRST_TIME_EXPERIENCE="${DOTNET_SKIP_FIRST_TIME_EXPERIENCE:-1}" \
dotnet restore "$PROJECT_RELATIVE_PATH" \
  -r "$RID"

DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/dotnet-home}" \
DOTNET_SKIP_FIRST_TIME_EXPERIENCE="${DOTNET_SKIP_FIRST_TIME_EXPERIENCE:-1}" \
dotnet publish "$PROJECT_RELATIVE_PATH" \
  -c "$CONFIGURATION" \
  -r "$RID" \
  --self-contained false \
  --no-restore \
  -m:1 \
  -o "$PUBLISH_DIR"
popd >/dev/null

if [[ -z "$BRIDGE_PATH" ]]; then
  case "$RID" in
    win-*) BRIDGE_PATH="$REPO_ROOT/$OUTPUT_ROOT/native/$RID/xcanet_ossl_bridge.dll" ;;
    osx-*) BRIDGE_PATH="$REPO_ROOT/$OUTPUT_ROOT/native/$RID/libxcanet_ossl_bridge.dylib" ;;
    linux-*) BRIDGE_PATH="$REPO_ROOT/$OUTPUT_ROOT/native/$RID/libxcanet_ossl_bridge.so" ;;
  esac
fi

if [[ -n "$BRIDGE_PATH" && -f "$BRIDGE_PATH" ]]; then
  BRIDGE_PATH="$(cd "$(dirname "$BRIDGE_PATH")" && pwd -L)/$(basename "$BRIDGE_PATH")"
fi

mkdir -p "$PUBLISH_DIR/native"

if [[ -n "$BRIDGE_PATH" ]]; then
  if [[ -f "$BRIDGE_PATH" ]]; then
    cp "$BRIDGE_PATH" "$PUBLISH_DIR/native/"
    echo "Copied OpenSSL bridge: $BRIDGE_PATH"
  else
    echo "OpenSSL bridge not found at $BRIDGE_PATH. Packaging will continue in managed-only mode." >&2
  fi
fi

MANIFEST_PATH="$PACKAGE_DIR/manifest.txt"
cat > "$MANIFEST_PATH" <<EOF
RID=$RID
Configuration=$CONFIGURATION
Version=${VERSION:-unknown}
PublishDir=$PUBLISH_DIR
BridgePath=${BRIDGE_PATH:-"(none)"}
BridgeMode=$([[ -n "${BRIDGE_PATH:-}" && -f "${BRIDGE_PATH:-}" ]] && echo "optional-openssl-present" || echo "managed-only")
Timestamp=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
EOF

echo "Application publish directory: $PUBLISH_DIR"
echo "Package manifest: $MANIFEST_PATH"
