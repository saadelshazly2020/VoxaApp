@echo off
echo ============================================
echo   Video Chat Application - Quick Start
echo ============================================
echo.

REM Check if PowerShell scripts exist
if not exist "start-dev.ps1" (
    echo Error: start-dev.ps1 not found!
    pause
    exit /b 1
)

echo Starting development environment...
echo.
echo This will open 2 PowerShell windows:
echo   1. .NET Backend Server (port 5000)
echo   2. Vue Frontend Dev Server (port 3000)
echo.
echo Press any key to continue...
pause > nul

REM Execute PowerShell script
powershell -ExecutionPolicy Bypass -File "start-dev.ps1"

pause
