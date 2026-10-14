@echo off
chcp 65001 >nul
echo 请先关闭六级词汇。此操作会恢复升级前档案，并保留升级后的安全副本。
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Restore-V1Backup.ps1" -Latest
if errorlevel 1 (
 echo 回退未完成，请查看上面的原因。
 pause
 exit /b 1
)
start "" "%~dp0rollback\V1.7\六级词汇.exe"
