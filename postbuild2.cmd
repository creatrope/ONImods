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
set "ILMERGE=C:\Users\sendh\Documents\GitHub\Sendhb-ONI\packages\ILMerge.3.0.29\tools\net452\ILMerge.exe
set "MERGED_DLL=%BUILD_DIR%\%MOD_NAME%.Merged.dll"

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

:: === Merge PLib.dll with mod if present ===
if exist "%BUILD_DIR%\Plib.dll" (
    echo INFO: Plib.dll found, merging with %MOD_NAME%.dll using ILMerge

    "%ILMERGE%" /out:"%MERGED_DLL%" /target:library /internalize /ndebug ^
        "%BUILD_DIR%\Plib.dll" ^
        "%BUILD_DIR%\%MOD_NAME%.dll"

    if exist "%MERGED_DLL%" (
        echo OK: Merge complete, copying merged DLL to %DEST_DIR%
        copy /Y "%MERGED_DLL%" "%DEST_DIR%\%MOD_NAME%.dll"
        echo INFO: Skipping Plib.dll and original mod DLL
    ) else (
        echo ERROR: Merge failed, merged DLL not created
        echo ERROR: Aborting DLL copy
    )
) else (
    echo INFO: Plib.dll not found, skipping merge
    if exist "%BUILD_DIR%\%MOD_NAME%.dll" (
        echo OK: Copying unmerged DLL
        copy /Y "%BUILD_DIR%\%MOD_NAME%.dll" "%DEST_DIR%\%MOD_NAME%.dll"
    ) else (
        echo ERROR: %MOD_NAME%.dll missing from %BUILD_DIR%
    )
)

:: Copy anim folder if it exists
if exist "%PROJECT_DIR%\anim" (
    echo OK: anim folder found
    xcopy /Y /E /I "%PROJECT_DIR%\anim" "%DEST_DIR%\anim"
) else (
    echo INFO: No anim folder found to copy
)

:: Optional: Clean up merged DLL (uncomment to delete after copy)
:: if exist "%MERGED_DLL%" del "%MERGED_DLL%"

pause
exit /b 0
