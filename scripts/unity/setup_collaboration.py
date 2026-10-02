"""Configure local Git LFS and Unity SmartMerge without changing user globals.

Run after cloning: python scripts/unity/setup_collaboration.py [UnityYAMLMerge-path]
"""
import os
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
VERSION = '6000.6.4f1'

def run(*args):
    return subprocess.run(args, cwd=ROOT, check=True, text=True)

run('git', 'lfs', 'install', '--local')
if len(sys.argv) > 1:
    candidates = [Path(sys.argv[1])]
else:
    candidates = [
        Path('/Applications/Unity/Hub/Editor') / VERSION / 'Unity.app/Contents/Helpers/UnityYAMLMerge',
        Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'Unity/Hub/Editor' / VERSION / 'Editor/Data/Tools/UnityYAMLMerge.exe',
        Path.home() / 'Unity/Hub/Editor' / VERSION / 'Editor/Data/Tools/UnityYAMLMerge',
    ]
merge = next((p for p in candidates if p.is_file()), None)
if merge:
    run('git', 'config', '--local', 'merge.unityyamlmerge.name', 'Unity SmartMerge')
    run('git', 'config', '--local', 'merge.unityyamlmerge.driver', f'"{merge.as_posix()}" merge -p "%O" "%B" "%A" "%A"')
    run('git', 'config', '--local', 'merge.unityyamlmerge.recursive', 'binary')
    print('Configured Unity SmartMerge:', merge)
else:
    print('Git LFS is ready. UnityYAMLMerge was not found; rerun with its full path after installing Unity', VERSION)
print('Open in Unity Hub:', ROOT / 'unity/D22Game')
print('Machine-specific credentials and MCP configurations are not copied into this repository.')
