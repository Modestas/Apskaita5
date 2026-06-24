@echo off
setlocal EnableDelayedExpansion

set "INSTALL_DIR=%ProgramFiles(x86)%\Apskaita5"
set "SCRIPT_DIR=%~dp0"

echo Checking install location: !INSTALL_DIR!

if not exist "!INSTALL_DIR!\" (
    echo ERROR: Directory not found: !INSTALL_DIR!
    echo Apskaita5 does not appear to be installed.
    pause
    exit /b 1
)

set "MISSING="
if not exist "!INSTALL_DIR!\AccDataBindingsWinForms.dll" set "MISSING=1"
if not exist "!INSTALL_DIR!\ApskaitaObjects.dll" set "MISSING=1"

if defined MISSING (
    echo ERROR: One or more patch files not found in install directory.
    echo Expected files:
    echo   !INSTALL_DIR!\AccDataBindingsWinForms.dll
    echo   !INSTALL_DIR!\ApskaitaObjects.dll
    echo This patch may not be compatible with the installed version.
    pause
    exit /b 1
)

echo Found installation. Backing up existing files...

copy /y "!INSTALL_DIR!\AccDataBindingsWinForms.dll" "!INSTALL_DIR!\AccDataBindingsWinForms.dll.bak"
if errorlevel 1 (
    echo ERROR: Failed to back up AccDataBindingsWinForms.dll. Try running as Administrator.
    pause
    exit /b 1
)

copy /y "!INSTALL_DIR!\ApskaitaObjects.dll" "!INSTALL_DIR!\ApskaitaObjects.dll.bak"
if errorlevel 1 (
    echo ERROR: Failed to back up ApskaitaObjects.dll. Try running as Administrator.
    pause
    exit /b 1
)

echo Applying patch...

copy /y "!SCRIPT_DIR!AccDataBindingsWinForms.dll" "!INSTALL_DIR!\AccDataBindingsWinForms.dll"
if errorlevel 1 (
    echo ERROR: Failed to copy AccDataBindingsWinForms.dll. Try running as Administrator.
    pause
    exit /b 1
)

copy /y "!SCRIPT_DIR!ApskaitaObjects.dll" "!INSTALL_DIR!\ApskaitaObjects.dll"
if errorlevel 1 (
    echo ERROR: Failed to copy ApskaitaObjects.dll. Try running as Administrator.
    pause
    exit /b 1
)

echo.
echo Patch applied successfully.
pause
endlocal
