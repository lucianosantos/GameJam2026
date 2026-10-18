$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$modelSource = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'Assets/Scripts/Hydraulics/HydraulicsPuzzle.cs')
$checksSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'HydraulicsPuzzleChecks.cs')
Add-Type -TypeDefinition ($modelSource + [Environment]::NewLine + $checksSource)
[GameJam2026.Hydraulics.HydraulicsPuzzleChecks]::Run()
