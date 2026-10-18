$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../Assets/Scripts/Levers/LeversPuzzle.cs')
function Check($condition, $message) { if (-not $condition) { throw $message } }
foreach ($a in 1..4) { foreach ($b in 1..4) { foreach ($c in 1..4) {
    $solution = [int[]]@($a,$b,$c)
    $p = [GameJam2026.Levers.LeversPuzzle]::new($solution)
    Check (-not $p.IsCompleted) 'Starts unsolved for every solution, including 1/1/1'
    $solution[0] = 99
    for ($i=0; $i -lt 3; $i++) { $null = $p.SetPosition($i, @($a,$b,$c)[$i]) }
    Check ($p.IsCompleted) 'All 64 solutions must be reachable; solution must be copied'
    Check (-not $p.SetPosition(0, ($a % 4 + 1))) 'Success locks input'
    $p.Restart()
    Check (-not $p.IsCompleted) 'Restart unlocks'
    Check (-not $p.SetPosition(-1,2)) 'Invalid index'
    Check (-not $p.SetPosition(3,2)) 'Index beyond third lever'
    Check (-not $p.SetPosition(0,0)) 'Position below range'
    Check (-not $p.SetPosition(0,5)) 'Position above range'
} } }
foreach ($a in 1..4) { foreach ($b in 1..4) { foreach ($c in 1..4) {
    $p = [GameJam2026.Levers.LeversPuzzle]::new([int[]]@(1,2,3))
    # Set the lever that differs from the solution first, avoiding an earlier valid combination.
    $values = @($a,$b,$c)
    foreach ($i in 0..2 | Sort-Object { if ($values[$_] -ne @(1,2,3)[$_]) { 0 } else { 1 } }) {
        $null = $p.SetPosition($i,$values[$i])
    }
    Check ($p.IsCompleted -eq ($a -eq 1 -and $b -eq 2 -and $c -eq 3)) 'Only the complete exact combination succeeds'
    for ($i=0; $i -lt 3; $i++) { Check ($p.GetPosition($i) -eq $values[$i]) 'Incorrect positions persist' }
} } }
foreach ($invalid in @(@{v=$null},@{v=[int[]]@()},@{v=[int[]]@(1,2)},@{v=[int[]]@(1,2,3,4)},@{v=[int[]]@(0,2,3)},@{v=[int[]]@(1,2,5)})) {
    $rejected=$false
    try { $null=[GameJam2026.Levers.LeversPuzzle]::new($invalid.v) } catch { $rejected=$true }
    Check $rejected 'Invalid configuration must fail'
}
'PASS: all 64 solutions, all 64 combinations, no partial success, wrong positions preserved, completion lock, restart, defensive copy and invalid inputs/configurations.'
