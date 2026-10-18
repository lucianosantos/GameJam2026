$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Add-Type -Path @(
    (Join-Path $projectRoot 'Assets/Scripts/Wires/WiresPuzzle.cs'),
    (Join-Path $projectRoot 'Tests/WiresPuzzleChecks.cs')
)
[WiresPuzzleChecks]::Run()
