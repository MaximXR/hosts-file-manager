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

set "TEST_OUT=%~dp0tests\TestSuite.exe"

echo [TEST] Compiling Automated Test Suite...
"%CSC%" /nologo /target:exe /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /out:"%TEST_OUT%" "%~dp0src\Models\*.cs" "%~dp0src\Localization\*.cs" "%~dp0src\Services\*.cs" "%~dp0src\UI\*.cs" "%~dp0tests\TestSuite.cs"

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Test suite compilation failed!
    exit /b %ERRORLEVEL%
)

echo [TEST] Executing Automated Tests...
echo.
"%TEST_OUT%"
set TEST_STATUS=%ERRORLEVEL%

if exist "%TEST_OUT%" del "%TEST_OUT%"

if %TEST_STATUS% neq 0 (
    echo.
    echo [FATAL] Automated tests failed! Build/release aborted.
    exit /b %TEST_STATUS%
)

endlocal
exit /b 0
