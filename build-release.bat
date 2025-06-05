@echo off
echo Building ScriptTrayTool Release...
echo.

REM Clean previous publish files
if exist "publish" rmdir /s /q "publish"
mkdir "publish"

echo 1. Building Self-Contained Version...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true -o "./publish/ScriptTrayTool-Standalone"

echo.
echo 2. Building Framework-Dependent Version...
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "./publish/ScriptTrayTool-FrameworkDependent"

echo.
echo 3. Building Portable Version...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o "./publish/ScriptTrayTool-Portable"

echo.
echo Build Complete! Files are in the publish folder:
echo - ScriptTrayTool-Standalone: Self-contained single file (Recommended)
echo - ScriptTrayTool-FrameworkDependent: Requires .NET 8.0 Runtime
echo - ScriptTrayTool-Portable: Portable version with all files
echo.
pause
