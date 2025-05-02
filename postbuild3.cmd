@echo off
setlocal enabledelayedexpansion

:: === Use passed-in ProjectDir ===
set "PROJECT_DIR=%~1"
echo [%PROJECT_DIR%]

:: Clean trailing backslash if present
if "%PROJECT_DIR:~-1%"=="\" set "PROJECT_DIR=%PROJECT_DIR:~0,-1%"
for %%A in ("%PROJECT_DIR%") do set "MOD_NAME=%%~nxA"

:: Define paths
set "BUILD_DIR=%PROJECT_DIR%\bin\Release"
set "DEST_DIR=%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Local\%MOD_NAME%"

echo INFO: Mod Name: %MOD_NAME%
echo INFO: Source Directory: %PROJECT_DIR%
echo INFO: Build Directory:  %BUILD_DIR%
echo INFO: Target Directory: %DEST_DIR%

:: Ensure destination folder exists
if not exist "%DEST_DIR%" (
    echo INFO: Creating destination mod folder: %DEST_DIR%
    mkdir "%DEST_DIR%"
)

:: Copy mod.yaml
if exist "%PROJECT_DIR%\mod.yaml" (
    echo OK: mod.yaml found
    copy /Y "%PROJECT_DIR%\mod.yaml" "%DEST_DIR%\mod.yaml"
) else (
    echo ERROR: mod.yaml is missing
)

:: Copy mod_info.yaml
if exist "%PROJECT_DIR%\mod_info.yaml" (
    echo OK: mod_info.yaml found
    copy /Y "%PROJECT_DIR%\mod_info.yaml" "%DEST_DIR%\mod_info.yaml"
) else (
    echo ERROR: mod_info.yaml is missing
)

:: === Copy merged or regular mod DLL ===
if exist "%BUILD_DIR%\%MOD_NAME%.Merged.dll" (
    echo INFO: Merged DLL found, copying as %MOD_NAME%.dll
    copy /Y "%BUILD_DIR%\%MOD_NAME%.Merged.dll" "%DEST_DIR%\%MOD_NAME%.dll"
) else if exist "%BUILD_DIR%\%MOD_NAME%.dll" (
    echo INFO: No merged DLL found, copying original
    copy /Y "%BUILD_DIR%\%MOD_NAME%.dll" "%DEST_DIR%\%MOD_NAME%.dll"
) else (
    echo ERROR: No DLL found to copy for %MOD_NAME%
)

:: === DO NOT copy PLib.dll — assumed to be merged ===

:: Copy anim folder if it exists
if exist "%PROJECT_DIR%\anim" (
    echo OK: anim folder found
    xcopy /Y /E /I "%PROJECT_DIR%\anim" "%DEST_DIR%\anim"
) else (
    echo INFO: No anim folder found to copy
)

pause
exit /b 0
