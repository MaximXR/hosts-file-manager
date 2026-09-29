@echo off
setlocal

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
if not exist "%CSC%" (
    for /f "delims=" %%i in ('where csc 2^>nul') do set "CSC=%%i"
)

if not exist "%CSC%" (
    echo [ERROR] csc.exe not found. Please install .NET Framework 4.x.
    exit /b 1
)

set "DIST_DIR=%~dp0dist-win-unpacked"
if not exist "%DIST_DIR%" mkdir "%DIST_DIR%"

echo Compiling HostsLauncher...
"%CSC%" /nologo /target:winexe /optimize+ /out:"%DIST_DIR%\HostsLauncher.exe" "%~dp0src\Program.cs"

if %ERRORLEVEL% equ 0 (
    echo [OK] Build successful: "%DIST_DIR%\HostsLauncher.exe"
) else (
    echo [ERROR] Compilation failed with code %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

endlocal
