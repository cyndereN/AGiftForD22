#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
BLENDER_BIN="${BLENDER_BIN:-/Applications/Blender.app/Contents/MacOS/Blender}"
SOURCE="$ROOT/blender/source/D22_Balanced_Lighting_v18.blend"

if [[ ! -x "$BLENDER_BIN" ]]; then
  echo "Blender executable not found: $BLENDER_BIN" >&2
  exit 1
fi
if [[ ! -f "$SOURCE" ]]; then
  echo "Blender source not found: $SOURCE" >&2
  exit 1
fi

exec "$BLENDER_BIN" -b "$SOURCE" -P "$ROOT/scripts/unity/export_d22.py"
