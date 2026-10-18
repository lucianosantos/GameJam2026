$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $projectRoot 'Assets/Scripts/LightSequence/LightSequencePuzzle.cs')
function Assert-Sequence($condition, $message) { if (-not $condition) { throw $message } }
foreach ($errorAt in 0..7) {
    $puzzle = [GameJam2026.LightSequence.LightSequencePuzzle]::new([int[]](1..8))
    for ($i = 1; $i -le $errorAt; $i++) { Assert-Sequence ($puzzle.Press($i) -eq 'Correct') 'Prefix must be correct' }
    Assert-Sequence ($puzzle.Press(99) -eq 'Incorrect') 'Wrong ID must fail'
    Assert-Sequence ($puzzle.Progress -eq 0 -and -not $puzzle.IsCompleted) 'Error must reset progress'
    foreach ($id in 1..8) { $result = $puzzle.Press($id) }
    Assert-Sequence ($result -eq 'Completed' -and $puzzle.IsCompleted) 'Retry must complete'
    Assert-Sequence ($puzzle.Press(1) -eq 'Ignored') 'Completed puzzle must ignore input'
    $puzzle.Restart()
    Assert-Sequence ($puzzle.Progress -eq 0 -and -not $puzzle.IsCompleted) 'Restart must reset'
}
$order = [int[]]@(8, 3, 1, 7, 2, 5, 4, 6)
$puzzle = [GameJam2026.LightSequence.LightSequencePuzzle]::new($order)
$order[0] = 99
foreach ($id in @(8, 3, 1, 7, 2, 5, 4, 6)) { $result = $puzzle.Press($id) }
Assert-Sequence ($result -eq 'Completed') 'Custom order must be supported and copied'
$puzzle = [GameJam2026.LightSequence.LightSequencePuzzle]::new([int[]]@(1, 1, 2))
Assert-Sequence ($puzzle.Press(1) -eq 'Correct') 'First repeated ID'
Assert-Sequence ($puzzle.Press(1) -eq 'Correct') 'Second repeated ID'
Assert-Sequence ($puzzle.Press(2) -eq 'Completed') 'Repeated sequence completion'
$puzzle = [GameJam2026.LightSequence.LightSequencePuzzle]::new([int[]]@(1, 2))
$null = $puzzle.Press(1)
Assert-Sequence ($puzzle.Press(1) -eq 'Incorrect') 'Repressing an earlier correct button must fail when out of order'
foreach ($invalid in @(@{value=$null}, @{value=[int[]]@()})) {
    $rejected = $false
    try { $null = [GameJam2026.LightSequence.LightSequencePuzzle]::new($invalid.value) } catch { $rejected = $true }
    Assert-Sequence $rejected 'Empty/null order must be rejected'
}
'PASS: eight error positions, resets, retry, completion lock, restart, configurable order, copied configuration, repeated IDs, wrong repeated press and invalid orders.'
