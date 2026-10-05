@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-portable.ps1"
if errorlevel 1 (
  echo.
  echo Portable build failed.
  pause
  exit /b 1
)
echo.
echo Portable build completed.
pause
