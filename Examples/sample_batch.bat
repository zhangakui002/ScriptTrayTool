@echo off
echo ========================================
echo 示例批处理脚本
echo ========================================
echo 当前时间: %date% %time%
echo 当前用户: %username%
echo 当前目录: %cd%
echo.
echo 系统信息:
systeminfo | findstr /C:"OS Name" /C:"OS Version" /C:"System Type"
echo.
echo 网络配置:
ipconfig | findstr /C:"IPv4"
echo.
echo 脚本执行完成！
pause
