@echo off
setlocal

if "%~1"=="" (
  set "DISTDIR=dist"
) else (
  set "DISTDIR=%~1"
)

if "%~2"=="" (
  set "CONFIG=Release"
) else (
  set "CONFIG=%~2"
)

if not exist "%DISTDIR%" mkdir "%DISTDIR%"

echo Packing LeXtudio.Windows into "%DISTDIR%" ...
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0pack.ps1" -OutDir "%DISTDIR%" -Configuration "%CONFIG%"
if errorlevel 1 exit /b 1

echo Signing packages in "%DISTDIR%" ...
powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0sign.ps1" -PackageDir "%DISTDIR%"
if errorlevel 1 exit /b 1

echo Done. Packages available in "%DISTDIR%"
endlocal
