$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../Assets/Scripts/CelestialLock/CelestialLockPuzzle.cs')
function Check($value, $message) { if (-not $value) { throw $message } }
$accepted = 0
foreach ($n in 0..255) {
    $start = [int[]]@(($n % 4), ([math]::Floor($n / 4) % 4), ([math]::Floor($n / 16) % 4), ([math]::Floor($n / 64) % 4))
    $p = [GameJam2026.CelestialLock.CelestialLockPuzzle]::new([int[]]@(0,1,2,3), $start)
    Check (-not $p.IsCompleted) 'Never completes before explicit submit'
    if ($p.Submit()) {
        $accepted++
        Check ($p.IsCompleted -and -not $p.Submit() -and -not $p.Advance(0)) 'Success is emitted once and locks changes'
    } else {
        foreach ($i in 0..3) { Check ($p.SymbolAt($i) -eq $start[$i]) 'Wrong submit preserves every symbol' }
        foreach ($i in 0..3) { while ($p.SymbolAt($i) -ne $i) { Check ($p.Advance($i)) 'Correcting a wrong attempt stays possible' } }
        Check (-not $p.IsCompleted -and $p.Submit()) 'A corrected sequence requires confirmation'
    }
    $p.Restart()
    Check (-not $p.IsCompleted) 'Restart clears completion'
    foreach ($i in 0..3) { Check ($p.SymbolAt($i) -eq $start[$i]) 'Restart restores authored starting order' }
}
Check ($accepted -eq 1) 'Exactly one of 256 combinations is correct'
$solution = [int[]]@(3,3,3,3); $initial = [int[]]@(3,3,3,3)
$p = [GameJam2026.CelestialLock.CelestialLockPuzzle]::new($solution,$initial)
$solution[0]=0; $initial[0]=0
Check (-not $p.Advance(-1) -and -not $p.Advance(4)) 'Invalid roller indices are ignored'
Check ($p.Advance(0) -and $p.SymbolAt(0) -eq 0) 'Saturn wraps to Moon'
foreach ($unused in 1..3) { $null=$p.Advance(0) }
Check ($p.Submit()) 'Repeated symbols and copied settings work'
'PASS: all 256 combinations, confirmation, correction, wraparound, repeated symbols, immutable settings, completion lock and restart.'
