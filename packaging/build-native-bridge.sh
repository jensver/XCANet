#!/usr/bin/env bash
set -euo pipefail

if [[ "${1:-}" == "" ]]; then
  echo "Usage: packaging/build-native-bridge.sh <rid> [output-root]" >&2
  echo "Example: packaging/build-native-bridge.sh osx-arm64 artifacts" >&2
  exit 1
fi

if ! command -v cc >/dev/null 2>&1; then
  echo "C compiler not found. Install clang/gcc before building the native bridge." >&2
  exit 1
fi

RID="$1"
OUTPUT_ROOT="${2:-artifacts}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd -L)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd -L)"
BRIDGE_SCRIPT="$REPO_ROOT/native/xcanet_ossl_bridge/build-bridge.sh"
OUTPUT_DIR="$REPO_ROOT/$OUTPUT_ROOT/native/$RID"

if [[ ! -x "$BRIDGE_SCRIPT" ]]; then
  echo "Native bridge build script not found or not executable: $BRIDGE_SCRIPT" >&2
  exit 1
fi

mkdir -p "$OUTPUT_DIR"
"$BRIDGE_SCRIPT" "$OUTPUT_DIR"
echo "Native bridge output: $OUTPUT_DIR"
