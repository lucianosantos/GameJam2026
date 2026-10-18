$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../Assets/Scripts/SymbolPassword/SymbolPasswordPuzzle.cs')
function Check($condition, $message) { if (-not $condition) { throw $message } }
foreach ($count in 0..4) {
 $p = [GameJam2026.SymbolPassword.SymbolPasswordPuzzle]::new([int[]]@(4,1,0,3,6))
 foreach ($digit in (@(4,1,0,3,6) | Select-Object -First $count)) { $null=$p.Enter($digit) }
 Check (-not $p.Submit()) 'Incomplete input must fail'
 Check ($p.Entered.Count -eq $count) 'Incomplete submit must preserve input'
}
foreach ($wrongAt in 0..4) {
 $p = [GameJam2026.SymbolPassword.SymbolPasswordPuzzle]::new([int[]]@(4,1,0,3,6))
 $digits=@(4,1,0,3,6); $digits[$wrongAt]=9
 foreach ($digit in $digits) { Check ($p.Enter($digit)) 'Must accept input' }
 Check (-not $p.IsCompleted) 'No automatic validation'
 Check (-not $p.Enter(2)) 'Limit input to five symbols'
 Check (-not $p.Submit()) 'Wrong input must fail'
 Check (($p.Entered -join ',') -eq ($digits -join ',')) 'Error preserves exact input'
 $p.Clear(); Check ($p.Entered.Count -eq 0) 'Clear empties attempt'
 foreach ($digit in @(4,1,0,3,6)) { $null=$p.Enter($digit) }
 Check ($p.Submit() -and $p.IsCompleted) 'Retry must complete'
 Check (-not $p.Submit()) 'Only one successful submit'
 Check (-not $p.Enter(1)) 'Success blocks input'
 $p.Clear(); Check ($p.Entered.Count -eq 5) 'Success locks clear'
 $p.Restart(); Check (-not $p.IsCompleted -and $p.Entered.Count -eq 0) 'Restart resets completion'
 Check (-not $p.Enter(-1) -and -not $p.Enter(10)) 'Reject invalid digits'
}
$solution=[int[]]@(0,0,1); $p=[GameJam2026.SymbolPassword.SymbolPasswordPuzzle]::new($solution); $solution[0]=9
foreach ($digit in @(0,0,1)) { $null=$p.Enter($digit) }; Check ($p.Submit()) 'Solution copy and repeated zeroes'
'PASS: incomplete/wrong submissions preserve input, five-symbol capacity, explicit submit, clear, retry, single completion, completion lock, restart, invalid digits, copied solution and repeated zeroes.'
