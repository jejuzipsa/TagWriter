@echo off
setlocal
cd /d "%~dp0"

echo ============================================================
echo  TagWriter - Local Build
echo ============================================================
echo.

dotnet --list-sdks > "%TEMP%\\tagwriter_sdks.txt" 2>nul
for %%A in ("%TEMP%\\tagwriter_sdks.txt") do if %%~zA==0 (
    del "%TEMP%\\tagwriter_sdks.txt" >nul 2>nul
    echo [ERROR] .NET 8 SDK is required.
    echo Install the .NET 8 SDK and run BUILD.bat again.
    pause
    exit /b 1
)
del "%TEMP%\\tagwriter_sdks.txt" >nul 2>nul

echo [1/2] Building latest local source...
if exist "publish" rmdir /s /q "publish"

dotnet publish "TagWriter.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o "publish"
if errorlevel 1 (
    echo.
    echo [ERROR] Build failed.
    pause
    exit /b 1
)

echo.
echo [2/2] Build complete.
echo Output: %CD%\publish\TagWriter.exe
echo Starting TagWriter...
start "" "%CD%\publish\TagWriter.exe"
exit /b 0
