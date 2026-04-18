#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd -L)"
OUTPUT_DIR="${1:-$SCRIPT_DIR/build}"
SOURCE_FILE="$SCRIPT_DIR/xcanet_ossl_bridge.c"
HEADER_DIR="$SCRIPT_DIR"

mkdir -p "$OUTPUT_DIR"

OPENSSL_PREFIX="${OPENSSL_PREFIX:-}"
if [[ -z "$OPENSSL_PREFIX" ]]; then
  if command -v brew >/dev/null 2>&1; then
    OPENSSL_PREFIX="$(brew --prefix openssl@3 2>/dev/null || true)"
  fi
fi

if [[ -z "$OPENSSL_PREFIX" ]]; then
  OPENSSL_PREFIX="/opt/homebrew/opt/openssl@3"
fi

INCLUDE_FLAGS=("-I$HEADER_DIR")
LIBRARY_FLAGS=()

if [[ -d "$OPENSSL_PREFIX/include" && -d "$OPENSSL_PREFIX/lib" ]]; then
  INCLUDE_FLAGS+=("-I$OPENSSL_PREFIX/include")
  LIBRARY_FLAGS+=("-L$OPENSSL_PREFIX/lib")
fi

case "$(uname -s)" in
  Darwin)
    OUTPUT_FILE="$OUTPUT_DIR/libxcanet_ossl_bridge.dylib"
    cc -dynamiclib -O2 -fPIC "${INCLUDE_FLAGS[@]}" "$SOURCE_FILE" "${LIBRARY_FLAGS[@]}" -lssl -lcrypto -o "$OUTPUT_FILE"
    ;;
  Linux)
    OUTPUT_FILE="$OUTPUT_DIR/libxcanet_ossl_bridge.so"
    cc -shared -O2 -fPIC "${INCLUDE_FLAGS[@]}" "$SOURCE_FILE" "${LIBRARY_FLAGS[@]}" -lssl -lcrypto -o "$OUTPUT_FILE"
    ;;
  MINGW*|MSYS*|CYGWIN*)
    OUTPUT_FILE="$OUTPUT_DIR/xcanet_ossl_bridge.dll"
    cc -shared -O2 "${INCLUDE_FLAGS[@]}" "$SOURCE_FILE" "${LIBRARY_FLAGS[@]}" -lssl -lcrypto -o "$OUTPUT_FILE"
    ;;
  *)
    echo "Unsupported platform: $(uname -s)" >&2
    exit 1
    ;;
esac

echo "$OUTPUT_FILE"
