#!/bin/bash
set -euo pipefail
D22_ROOT="$(cd "$(dirname "$0")" && pwd)"
D22_VERSION="$(awk '/m_EditorVersion:/{print $2}' "$D22_ROOT/unity/D22Game/ProjectSettings/ProjectVersion.txt")"
D22_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/$D22_VERSION/Unity.app/Contents/MacOS/Unity}"
if [ ! -x "$D22_EDITOR" ]; then
  echo "Install Unity $D22_VERSION in Unity Hub, or set UNITY_EDITOR to its executable."
  exit 1
fi
exec "$D22_EDITOR" -projectPath "$D22_ROOT/unity/D22Game"
