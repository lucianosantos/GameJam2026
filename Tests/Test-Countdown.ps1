$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../Assets/Scripts/CountdownClock.cs')
function Check($condition, $message) { if (-not $condition) { throw $message } }
$clock = [GameJam2026.CountdownClock]::new(600)
Check ($clock.DisplaySeconds -eq 600 -and -not $clock.IsExpired) 'Starts at 10:00'
$clock.Advance(.1)
Check ($clock.DisplaySeconds -eq 600) 'Initial second is not skipped'
$clock.Advance(.9)
Check ($clock.DisplaySeconds -eq 599) 'After one second shows 09:59'
$clock.Advance(539)
Check ($clock.DisplaySeconds -eq 60) 'Minute boundary shows 01:00'
$clock.Advance(.01)
Check ($clock.DisplaySeconds -eq 60) 'Fractions round up until the next elapsed second'
$clock.Advance(59)
Check ($clock.DisplaySeconds -eq 1 -and -not $clock.IsExpired) 'Last fraction still shows 00:01'
$clock.Advance(1)
Check ($clock.DisplaySeconds -eq 0 -and $clock.IsExpired) 'Crossing deadline expires at 00:00'
$clock.Advance(1000)
Check ($clock.RemainingSeconds -eq 0) 'Expired clock never becomes negative'
$clock = [GameJam2026.CountdownClock]::new(600)
$clock.Advance(720)
Check ($clock.IsExpired -and $clock.DisplaySeconds -eq 0) 'A long frame cannot skip expiration'
foreach ($invalid in @(0, -1, [double]::NaN, [double]::PositiveInfinity)) {
    $rejected = $false
    try { $null = [GameJam2026.CountdownClock]::new($invalid) } catch { $rejected = $true }
    Check $rejected 'Invalid duration rejected'
}
foreach ($invalid in @(-1, [double]::NaN, [double]::PositiveInfinity)) {
    $rejected = $false
    try { $clock.Advance($invalid) } catch { $rejected = $true }
    Check $rejected 'Invalid elapsed time rejected'
}
'PASS: 10:00, second/minute boundaries, rounding, expiration, long frames, nonnegative time and invalid inputs.'
