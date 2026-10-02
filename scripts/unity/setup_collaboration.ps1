$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $Root
git lfs install --local
if ($LASTEXITCODE -ne 0) { throw 'Install Git for Windows with Git LFS first.' }
$merge = Join-Path $env:ProgramFiles 'Unity\Hub\Editor\6000.6.4f1\Editor\Data\Tools\UnityYAMLMerge.exe'
if (Test-Path $merge) {
  git config --local merge.unityyamlmerge.name 'Unity SmartMerge'
  git config --local merge.unityyamlmerge.driver ('"{0}" merge -p "%O" "%B" "%A" "%A"' -f $merge)
  git config --local merge.unityyamlmerge.recursive binary
  Write-Host "Configured Unity SmartMerge: $merge"
} else { Write-Warning 'UnityYAMLMerge not found; install Unity 6000.6.4f1 and rerun.' }
git lfs pull
if ($LASTEXITCODE -ne 0) { throw 'Git LFS pull failed; check remote access.' }
Write-Host 'Ready. Open unity/D22Game in Unity Hub.'
