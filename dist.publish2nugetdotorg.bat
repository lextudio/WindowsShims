@echo off
setlocal

if "%~1"=="" (
  set "PACKAGE_DIR=dist"
) else (
  set "PACKAGE_DIR=%~1"
)

if "%NUGET_API_KEY%"=="" (
  echo ERROR: NUGET_API_KEY environment variable is not set.
  exit /b 1
)

dotnet nuget push "%PACKAGE_DIR%\*.nupkg" -s https://api.nuget.org/v3/index.json -k "%NUGET_API_KEY%" --skip-duplicate
if errorlevel 1 exit /b 1
endlocal
