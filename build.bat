@echo off
setlocal

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

set "DIST_DIR=%~dp0dist-win-unpacked"
if not exist "%DIST_DIR%" mkdir "%DIST_DIR%"

echo [1/2] Compiling OpenHostsFile.exe (Fast Taskbar Runner)...
"%CSC%" /nologo /target:winexe /optimize+ /out:"%DIST_DIR%\OpenHostsFile.exe" "%~dp0src\Program.cs"
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile OpenHostsFile.exe
    exit /b %ERRORLEVEL%
)

echo [2/2] Compiling HostsManager.exe (GUI, Shortcut Creator, Subscriptions, Scheduler)...
"%CSC%" /nologo /target:winexe /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /out:"%DIST_DIR%\HostsManager.exe" "%~dp0src\HostsManager.cs"
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile HostsManager.exe
    exit /b %ERRORLEVEL%
)

echo.
echo [OK] All components successfully built into:
echo      "%DIST_DIR%\OpenHostsFile.exe"
echo      "%DIST_DIR%\HostsManager.exe"

endlocal
