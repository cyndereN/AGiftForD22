$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$blender=$env:BLENDER_BIN
if (-not $blender) {$blender='blender'}
& $blender --background (Join-Path $root 'blender/source/D22_Balanced_Lighting_v18.blend') --python (Join-Path $PSScriptRoot 'export_d22.py')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host 'Blender art export complete. Open Unity and run D22 > Publish > Update Art From Blender.'
