@echo off
setlocal

echo [0/3] Running Automated Test Suite...
call "%~dp0test.bat"
if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Build aborted because tests failed!
    exit /b %ERRORLEVEL%
)
echo.

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
if not exist "%CSC%" (
    for /f "delims=" %%i in ('where csc 2^>nul') do set "CSC=%%i"
)

if "%CSC%"=="" (
    echo [ERROR] csc.exe not found. Please install .NET Framework 4.x.
    exit /b 1
)

set "DIST_UNPACKED=%~dp0dist-win-unpacked"
set "DIST_ZIP_DIR=%~dp0dist"
if not exist "%DIST_UNPACKED%" mkdir "%DIST_UNPACKED%"
if not exist "%DIST_ZIP_DIR%" mkdir "%DIST_ZIP_DIR%"

echo [1/3] Compiling OpenHostsFile.exe (Fast Taskbar Runner)...
"%CSC%" /nologo /target:winexe /optimize+ /win32icon:"%~dp0resources\app.ico" /out:"%DIST_UNPACKED%\OpenHostsFile.exe" "%~dp0src\Program.cs"
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile OpenHostsFile.exe
    exit /b %ERRORLEVEL%
)

echo [2/3] Compiling HostsManager.exe (Modular GUI, Subscriptions, Scheduler, i18n)...
"%CSC%" /nologo /target:winexe /optimize+ /win32icon:"%~dp0resources\app.ico" /resource:"%~dp0resources\app.ico",app.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /out:"%DIST_UNPACKED%\HostsManager.exe" "%~dp0src\Models\*.cs" "%~dp0src\Localization\*.cs" "%~dp0src\Services\*.cs" "%~dp0src\UI\*.cs" "%~dp0src\HostsManagerProgram.cs"
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile HostsManager.exe
    exit /b %ERRORLEVEL%
)

echo [3/3] Packaging portable release ZIP into dist/...
set "APP_VERSION="
for /f "usebackq tokens=*" %%v in (`powershell -NoProfile -Command "(Get-Content '%~dp0version.json' | ConvertFrom-Json).version"`) do set "APP_VERSION=%%v"
if "%APP_VERSION%"=="" set "APP_VERSION=1.2.0"

powershell -NoProfile -Command "Compress-Archive -Path '%DIST_UNPACKED%\*', '%~dp0README.md', '%~dp0LICENSE' -DestinationPath '%DIST_ZIP_DIR%\HostsLauncher-v%APP_VERSION%-portable.zip' -Force"
if %ERRORLEVEL% neq 0 (
    echo [WARNING] Could not create zip archive.
) else (
    echo [OK] Package created: dist\HostsLauncher-v%APP_VERSION%-portable.zip
)

echo.
echo ========================================================
echo [SUCCESS] Build and Test completed successfully!
echo - Unpacked binaries: %DIST_UNPACKED%
echo - Release archive:   %DIST_ZIP_DIR%\HostsLauncher-v%APP_VERSION%-portable.zip
echo ========================================================

endlocal
