@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-and-test.ps1"
if errorlevel 1 (
  echo.
  echo Build or tests failed.
  pause
  exit /b 1
)
echo.
echo Build and tests passed.
pause
