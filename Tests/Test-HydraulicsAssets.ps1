$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path $PSScriptRoot -Parent
$assetRoot = Join-Path $projectRoot 'Assets/Art/Minigames/Hidraulics'

function Read-PipeMetrics($path) {
    $bitmap = [System.Drawing.Bitmap]::FromFile($path)
    try {
        $bottom = @(); $right = @()
        for ($x = 0; $x -lt $bitmap.Width; $x++) {
            if ($bitmap.GetPixel($x, $bitmap.Height - 1).A -gt 127) { $bottom += $x }
        }
        for ($y = 0; $y -lt $bitmap.Height; $y++) {
            if ($bitmap.GetPixel($bitmap.Width - 1, $y).A -gt 127) { $right += $y }
        }
        return @{
            Width = $bitmap.Width; Height = $bitmap.Height
            BottomCenter = ($bottom[0] + $bottom[-1] + 1) / 2.0
            BottomWidth = $bottom.Count
            RightCenter = ($right[0] + $right[-1] + 1) / 2.0
            RightWidth = $right.Count
        }
    } finally { $bitmap.Dispose() }
}

$straight = Read-PipeMetrics (Join-Path $assetRoot 'pipeGreen-flatEmpty.png')
$cellPixels = $straight.Height
$half = $cellPixels / 2.0
foreach ($state in @('Empty', 'Full')) {
    $elbow = Read-PipeMetrics (Join-Path $assetRoot "pipeGreen-curve$state.png")
    $line = Read-PipeMetrics (Join-Path $assetRoot "pipeGreen-flat$state.png")
    # The elbow occupies the bottom-right at the same native pixel scale as the straight.
    $bottomX = $cellPixels - $elbow.Width + $elbow.BottomCenter
    $rightY = $elbow.Height - $elbow.RightCenter
    $straightX = ($cellPixels - $line.Width) / 2.0 + $line.BottomCenter
    if ([Math]::Abs($bottomX - $half) -gt 0.5 -or
        [Math]::Abs($rightY - $half) -gt 0.5 -or
        [Math]::Abs($straightX - $half) -gt 0.5) {
        throw "Off-center ports: $state"
    }
    if ($elbow.BottomWidth -ne $line.BottomWidth -or $elbow.RightWidth -ne $line.BottomWidth) {
        throw "Mismatched port thickness: $state"
    }
    # Rotate both ports around the CELL center; they must remain at edge midpoints.
    foreach ($port in @(@($bottomX, 0), @($cellPixels, $rightY))) {
        $px = [double]$port[0]; $py = [double]$port[1]
        for ($turn = 0; $turn -lt 4; $turn++) {
            $atHorizontalEdge = [Math]::Abs($px - $half) -le 0.5 -and
                ([Math]::Abs($py) -le 0.5 -or [Math]::Abs($py - $cellPixels) -le 0.5)
            $atVerticalEdge = [Math]::Abs($py - $half) -le 0.5 -and
                ([Math]::Abs($px) -le 0.5 -or [Math]::Abs($px - $cellPixels) -le 0.5)
            if (-not ($atHorizontalEdge -or $atVerticalEdge)) { throw "Rotation misaligns port: $state/$turn" }
            $nextX = $py; $py = $cellPixels - $px; $px = $nextX
        }
    }
}

$scene = Get-Content -Raw (Join-Path $projectRoot 'Assets/Scenes/ControllerScene.unity')
$board = [regex]::Match($scene, '(?s)m_EditorClassIdentifier: [^\r\n]*HydraulicsBoard\r?\n.*?(?=--- !u!)').Value
$target = [regex]::Match($board, 'cameraTarget: \{fileID: (\d+)\}').Groups[1].Value
$controller = [regex]::Match($board, 'cameraController: \{fileID: (\d+)\}').Groups[1].Value
$areas = [regex]::Matches($scene, '(?s)m_EditorClassIdentifier: [^\r\n]*::CameraClickArea\r?\n.*?(?=--- !u!)')
$matchingArea = @($areas | Where-Object {
    $_.Value.Contains("targetPosition: {fileID: $target}") -and
    $_.Value.Contains("cameraController: {fileID: $controller}")
})
if (-not $target -or -not $controller -or $matchingArea.Count -eq 0) {
    throw 'Board camera target/controller do not match an approach area in the saved scene.'
}
'PASS: dry/wet port thickness and centering, four curve rotations, saved scene camera target.'
