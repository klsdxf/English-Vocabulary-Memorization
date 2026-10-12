@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-to-github.ps1"
if errorlevel 1 (
    echo Push did not finish. Check the error above.
) else (
    echo Push completed.
)
pause
