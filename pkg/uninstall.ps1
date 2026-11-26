$ErrorActionPreference = 'SilentlyContinue'
Stop-ScheduledTask -TaskName 'DeskPanel'
Unregister-ScheduledTask -TaskName 'DeskPanel' -Confirm:$false
netsh advfirewall firewall delete rule name="DeskPanel LAN" | Out-Null
Get-Process DeskPanel | Stop-Process -Force
Write-Host "Da go DeskPanel khoi phan tu khoi dong. Ban co the xoa thu muc nay." -ForegroundColor Green
