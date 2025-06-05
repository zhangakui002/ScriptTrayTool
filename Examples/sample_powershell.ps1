# 示例 PowerShell 脚本
Write-Host "========================================" -ForegroundColor Green
Write-Host "示例 PowerShell 脚本" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# 显示系统信息
Write-Host "`n系统信息:" -ForegroundColor Yellow
Write-Host "计算机名: $env:COMPUTERNAME"
Write-Host "用户名: $env:USERNAME"
Write-Host "操作系统: $((Get-WmiObject Win32_OperatingSystem).Caption)"
Write-Host "PowerShell 版本: $($PSVersionTable.PSVersion)"

# 显示当前时间
Write-Host "`n当前时间:" -ForegroundColor Yellow
Get-Date -Format "yyyy-MM-dd HH:mm:ss"

# 显示磁盘空间
Write-Host "`n磁盘空间:" -ForegroundColor Yellow
Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3} | ForEach-Object {
    $freeSpace = [math]::Round($_.FreeSpace / 1GB, 2)
    $totalSpace = [math]::Round($_.Size / 1GB, 2)
    Write-Host "$($_.DeviceID) 可用: ${freeSpace}GB / 总计: ${totalSpace}GB"
}

# 显示正在运行的进程数量
Write-Host "`n进程信息:" -ForegroundColor Yellow
$processCount = (Get-Process).Count
Write-Host "正在运行的进程数量: $processCount"

# 显示网络适配器
Write-Host "`n网络适配器:" -ForegroundColor Yellow
Get-NetAdapter | Where-Object {$_.Status -eq "Up"} | Select-Object Name, InterfaceDescription | Format-Table -AutoSize

Write-Host "`n脚本执行完成！" -ForegroundColor Green
