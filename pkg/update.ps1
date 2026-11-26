# Cap nhat DeskPanel: dung ban dang chay, chep ban moi vao thu muc da cai, chay lai.
# Giu nguyen config.json (cac nut da tao), log va thu muc platform-tools.
$ErrorActionPreference = 'Stop'
$src = (Resolve-Path (Split-Path -Parent $MyInvocation.MyCommand.Path)).Path
$task = Get-ScheduledTask -TaskName 'DeskPanel' -ErrorAction SilentlyContinue
if (-not $task) { Write-Host "Chua cai DeskPanel tren may nay. Hay chay install.cmd." -ForegroundColor Yellow; exit 1 }
$dst = (Resolve-Path (Split-Path -Parent $task.Actions[0].Execute)).Path

Write-Host "Dang dung DeskPanel..."
Stop-ScheduledTask -TaskName 'DeskPanel' -ErrorAction SilentlyContinue
Get-Process DeskPanel -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

if ($src -ne $dst) {
    Get-ChildItem -Path $src -Recurse | Unblock-File -ErrorAction SilentlyContinue
    Write-Host "Chep ban moi tu $src vao $dst ..."
    robocopy $src $dst /E /XF config.json config.bad.json deskpanel.log deskpanel.log.old /XD platform-tools /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Chep file that bai (robocopy ma loi $LASTEXITCODE)" }
}
Start-ScheduledTask -TaskName 'DeskPanel'
Start-Sleep -Seconds 3
Write-Host ""
Write-Host "Da cap nhat xong. Dien thoai se tu tai lai giao dien moi." -ForegroundColor Green
