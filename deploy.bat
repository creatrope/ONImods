@echo off
setlocal enabledelayedexpansion

:: === Use passed-in ProjectDir ===
set "PROJECT_DIR=%~1"
if "%PROJECT_DIR%"=="" (
    echo ERROR: You must pass the path to the project directory.
    exit /b 1
)
if "%PROJECT_DIR:~-1%"=="\" set "PROJECT_DIR=%PROJECT_DIR:~0,-1%"
for %%A in ("%PROJECT_DIR%") do set "MOD_NAME=%%~nxA"

:: Paths
set "BUILD_DIR=%PROJECT_DIR%\bin\Release"
set "DEST_DIR=%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Local\%MOD_NAME%"

echo [INFO] Mod Name: %MOD_NAME%
echo [INFO] Target Dir: %DEST_DIR%
echo.

if not exist "%DEST_DIR%" (
    mkdir "%DEST_DIR%"
    echo [INFO] Created destination directory
)

:: Track copied files
set "MOVED_FILES="

:: Static files to copy (file or folder)
set FILE_LIST=preview.png mod.yaml mod_info.yaml %MOD_NAME%.dll anim

for %%F in (%FILE_LIST%) do (
    set "SRC_FILE=%PROJECT_DIR%\%%F"
    set "SRC_BIN=%BUILD_DIR%\%%F"

    if exist "!SRC_FILE!" (
        if exist "!SRC_FILE!\*" (
            xcopy /Y /E /I "!SRC_FILE!" "%DEST_DIR%\%%F" >nul
            set "MOVED_FILES=!MOVED_FILES!%%F/, "
        ) else (
            copy /Y "!SRC_FILE!" "%DEST_DIR%\%%F" >nul
            set "MOVED_FILES=!MOVED_FILES!%%F, "
        )
    ) else if exist "!SRC_BIN!" (
        copy /Y "!SRC_BIN!" "%DEST_DIR%\%%F" >nul
        set "MOVED_FILES=!MOVED_FILES!%%F, "
    ) else (
        echo [WARN] %%F not found
    )
)

:: Summary
if not "%MOVED_FILES%"=="" (
    set "MOVED_FILES=%MOVED_FILES:~0,-2%"
    echo.
    echo [INFO] Files copied: !MOVED_FILES!
) else (
    echo.
    echo [INFO] No files copied.
)

echo.
pause
exit /b 0
