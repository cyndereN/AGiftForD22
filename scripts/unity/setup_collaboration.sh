#!/usr/bin/env bash
set -euo pipefail
D22_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
D22_VERSION="$(awk '/m_EditorVersion:/{print $2}' "$D22_ROOT/unity/D22Game/ProjectSettings/ProjectVersion.txt")"
D22_MERGE="${UNITY_YAML_MERGE:-/Applications/Unity/Hub/Editor/$D22_VERSION/Unity.app/Contents/Helpers/UnityYAMLMerge}"
git -C "$D22_ROOT" lfs install --local
if [[ -x "$D22_MERGE" ]]; then
  git -C "$D22_ROOT" config --local merge.unityyamlmerge.name 'Unity SmartMerge'
  git -C "$D22_ROOT" config --local merge.unityyamlmerge.driver "\"$D22_MERGE\" merge -p \"%O\" \"%B\" \"%A\" \"%A\""
  git -C "$D22_ROOT" config --local merge.unityyamlmerge.recursive binary
else
  echo "SmartMerge not found; install Unity $D22_VERSION and rerun this script."
fi
git -C "$D22_ROOT" lfs pull
