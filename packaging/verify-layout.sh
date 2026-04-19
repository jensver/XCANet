#!/usr/bin/env bash
set -euo pipefail

if [[ "${1:-}" == "" || "${2:-}" == "" ]]; then
  echo "Usage: packaging/verify-layout.sh <rid> <configuration> [output-root]" >&2
  exit 1
fi

RID="$1"
CONFIGURATION="$2"
OUTPUT_ROOT="${3:-artifacts}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd -L)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd -L)"
PUBLISH_DIR="$REPO_ROOT/$OUTPUT_ROOT/publish/$RID/$CONFIGURATION/app"
PACKAGE_DIR="$REPO_ROOT/$OUTPUT_ROOT/packages/$RID/$CONFIGURATION"
MANIFEST_PATH="$PACKAGE_DIR/manifest.txt"

if [[ ! -d "$PUBLISH_DIR" ]]; then
  echo "Publish directory not found: $PUBLISH_DIR" >&2
  exit 1
fi

if [[ ! -f "$PUBLISH_DIR/XcaNet.App.Desktop.dll" ]]; then
  echo "Missing desktop application assembly in $PUBLISH_DIR" >&2
  exit 1
fi

if [[ ! -f "$PUBLISH_DIR/appsettings.json" ]]; then
  echo "Missing appsettings.json in $PUBLISH_DIR" >&2
  exit 1
fi

if [[ ! -f "$PUBLISH_DIR/XcaNet.App.Desktop.runtimeconfig.json" ]]; then
  echo "Missing runtimeconfig in $PUBLISH_DIR" >&2
  exit 1
fi

if [[ ! -f "$MANIFEST_PATH" ]]; then
  echo "Missing package manifest: $MANIFEST_PATH" >&2
  exit 1
fi

echo "Verified publish layout: $PUBLISH_DIR"
