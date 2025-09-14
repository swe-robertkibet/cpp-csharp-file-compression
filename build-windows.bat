@echo off
REM Build script for Windows deployment of Multi-Algorithm Compression Tool
REM This script builds both the C++ CLI application and C# GUI application

echo ==================================================
echo Building Multi-Algorithm Compression Tool for Windows
echo ==================================================

REM Check if we're running from the project root
if not exist CMakeLists.txt (
    echo Error: Please run this script from the project root directory
    pause
    exit /b 1
)

REM Create build directory for C++ application
echo.
echo [1/4] Building C++ Console Application...
if not exist build mkdir build
cd build

REM Configure and build C++ project with Visual Studio
cmake .. -G "Visual Studio 17 2022" -A x64
if errorlevel 1 (
    echo Error: CMake configuration failed
    cd ..
    pause
    exit /b 1
)

cmake --build . --config Release
if errorlevel 1 (
    echo Error: C++ build failed
    cd ..
    pause
    exit /b 1
)

cd ..

REM Build C# GUI Application
echo.
echo [2/4] Building C# GUI Application...
cd CSharpUI\CompressionTool

REM Clean previous builds
dotnet clean -c Release

REM Build and publish for Windows
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 (
    echo Error: C# build failed
    cd ..\..
    pause
    exit /b 1
)

cd ..\..

REM Create installer (requires NSIS to be installed)
echo.
echo [3/4] Creating Windows Installer...
where makensis >nul 2>nul
if errorlevel 1 (
    echo Warning: NSIS not found in PATH. Please install NSIS to create installer.
    echo You can still run the applications manually:
    echo   - Console: build\Release\compress.exe
    echo   - GUI: CSharpUI\CompressionTool\bin\Release\net8.0\win-x64\publish\CompressionToolGUI.exe
) else (
    makensis installer.nsi
    if errorlevel 1 (
        echo Warning: Installer creation failed
    ) else (
        echo Installer created successfully: MultiAlgorithmCompressionTool-1.0.0-Setup.exe
    )
)

echo.
echo [4/4] Build Summary
echo ==================================================
echo C++ Console App: build\Release\compress.exe
echo C++ Library:     build\Release\compression_lib.dll
echo C# GUI App:      CSharpUI\CompressionTool\bin\Release\net8.0\win-x64\publish\CompressionToolGUI.exe

if exist MultiAlgorithmCompressionTool-1.0.0-Setup.exe (
    echo Installer:       MultiAlgorithmCompressionTool-1.0.0-Setup.exe
)

echo.
echo Build completed successfully!
echo.
pause