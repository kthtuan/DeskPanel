# Cai DeskPanel: tu chay khi dang nhap Windows, voi quyen admin (de doc nhiet do va SMART).
$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe = Join-Path $dir 'DeskPanel.exe'
if (-not (Test-Path $exe)) { throw "Khong thay DeskPanel.exe trong $dir" }

# Neu dang chay ban cu thi dung lai truoc
Stop-ScheduledTask -TaskName 'DeskPanel' -ErrorAction SilentlyContinue
Get-Process DeskPanel -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# Bo dau "tai tu Internet" de Windows khong chan
Get-ChildItem -Path $dir -Recurse | Unblock-File -ErrorAction SilentlyContinue

$user = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$action    = New-ScheduledTaskAction -Execute $exe -WorkingDirectory $dir
$trigger   = New-ScheduledTaskTrigger -AtLogOn -User $user
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Highest
$settings  = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -AllowStartIfOnBatteries `
             -DontStopIfGoingOnBatteries -MultipleInstances IgnoreNew -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)
Register-ScheduledTask -TaskName 'DeskPanel' -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null
Start-ScheduledTask -TaskName 'DeskPanel'
Start-Sleep -Seconds 3
Write-Host ""
Write-Host "Da cai xong. DeskPanel se tu chay moi khi ban dang nhap Windows." -ForegroundColor Green
Write-Host "Trang cai dat nut: http://localhost:8765/settings.html"
Start-Process "http://localhost:8765/settings.html"
