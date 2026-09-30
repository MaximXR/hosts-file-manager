@echo off
setlocal

set "DIST_DIR=%~dp0dist-win-unpacked"
set "EXE_PATH=%DIST_DIR%\OpenHostsFile.exe"

if not exist "%EXE_PATH%" (
    echo [INFO] OpenHostsFile.exe not found. Building first...
    call "%~dp0build.bat"
)

if not exist "%EXE_PATH%" (
    echo [ERROR] Build failed or binary not found.
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$desktop = [System.Environment]::GetFolderPath('Desktop');" ^
  "$wsh = New-Object -ComObject WScript.Shell;" ^
  "$s = $wsh.CreateShortcut((Join-Path $desktop 'Hosts.lnk'));" ^
  "$s.TargetPath = '%EXE_PATH%';" ^
  "$s.IconLocation = '%SystemRoot%\System32\shell32.dll,0';" ^
  "$s.Description = 'Hosts file launcher';" ^
  "$s.Save();" ^
  "Write-Host '[OK] Shortcut created on Desktop with system icon shell32.dll,0';"

endlocal
