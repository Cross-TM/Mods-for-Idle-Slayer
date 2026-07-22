$game = Get-Process -Name 'Idle Slayer' -ErrorAction SilentlyContinue
$logPath = Join-Path $env:LOCALAPPDATA 'IdleSlayerModManager\ModLoader\MelonLoader\Latest.log'

if (-not $game) {
    Write-Output 'Idle Slayer is not running.'
    exit 1
}

$age = (Get-Date) - $game.StartTime
$exceptionCount = 0
if (Test-Path -LiteralPath $logPath) {
    $exceptionCount = @(Select-String -LiteralPath $logPath -Pattern 'AutoJumpMod\.AutoJump\.LateUpdate').Count
}

Write-Output ('ProcessId={0} Responding={1} AgeMinutes={2:N1} WorkingSetMB={3:N1} AutoJumpLateUpdateExceptions={4}' -f `
    $game.Id,
    $game.Responding,
    $age.TotalMinutes,
    ($game.WorkingSet64 / 1MB),
    $exceptionCount)

if (-not $game.Responding -or $age.TotalMinutes -lt 10 -or $exceptionCount -ne 0) {
    exit 1
}

exit 0
