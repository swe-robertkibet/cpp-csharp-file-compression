# PowerShell build script for Windows deployment of Multi-Algorithm Compression Tool
# This script builds both the C++ CLI application and C# GUI application

Write-Host "==================================================" -ForegroundColor Green
Write-Host "Building Multi-Algorithm Compression Tool for Windows" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green

# Check if we're running from the project root
if (-not (Test-Path "CMakeLists.txt")) {
    Write-Error "Please run this script from the project root directory"
    Read-Host "Press Enter to exit"
    exit 1
}

try {
    # Build C++ Console Application
    Write-Host "`n[1/4] Building C++ Console Application..." -ForegroundColor Yellow

    if (-not (Test-Path "build")) {
        New-Item -ItemType Directory -Path "build" | Out-Null
    }

    Set-Location "build"

    # Configure with CMake
    Write-Host "Configuring with CMake..." -ForegroundColor Cyan
    & cmake .. -G "Visual Studio 17 2022" -A x64
    if ($LASTEXITCODE -ne 0) {
        throw "CMake configuration failed"
    }

    # Build the project
    Write-Host "Building C++ project..." -ForegroundColor Cyan
    & cmake --build . --config Release
    if ($LASTEXITCODE -ne 0) {
        throw "C++ build failed"
    }

    Set-Location ".."

    # Build C# GUI Application
    Write-Host "`n[2/4] Building C# GUI Application..." -ForegroundColor Yellow
    Set-Location "CSharpUI\CompressionTool"

    # Clean previous builds
    Write-Host "Cleaning previous builds..." -ForegroundColor Cyan
    & dotnet clean -c Release

    # Build and publish for Windows
    Write-Host "Publishing C# application..." -ForegroundColor Cyan
    & dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false -p:DebugType=none
    if ($LASTEXITCODE -ne 0) {
        throw "C# build failed"
    }

    Set-Location "..\..\"

    # Create installer
    Write-Host "`n[3/4] Creating Windows Installer..." -ForegroundColor Yellow

    $nsisPath = Get-Command "makensis" -ErrorAction SilentlyContinue
    if ($nsisPath) {
        Write-Host "Creating NSIS installer..." -ForegroundColor Cyan
        & makensis installer.nsi
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Installer creation failed"
        } else {
            Write-Host "Installer created successfully!" -ForegroundColor Green
        }
    } else {
        Write-Warning "NSIS not found in PATH. Please install NSIS to create installer."
        Write-Host "You can still run the applications manually:" -ForegroundColor Yellow
        Write-Host "  - Console: build\Release\compress.exe" -ForegroundColor White
        Write-Host "  - GUI: CSharpUI\CompressionTool\bin\Release\net8.0\win-x64\publish\CompressionToolGUI.exe" -ForegroundColor White
    }

    # Build Summary
    Write-Host "`n[4/4] Build Summary" -ForegroundColor Yellow
    Write-Host "==================================================" -ForegroundColor Green

    $cppExe = "build\Release\compress.exe"
    $cppLib = "build\Release\compression_lib.dll"
    $csharpExe = "CSharpUI\CompressionTool\bin\Release\net8.0\win-x64\publish\CompressionToolGUI.exe"
    $installer = "MultiAlgorithmCompressionTool-1.0.0-Setup.exe"

    if (Test-Path $cppExe) {
        $size = [Math]::Round((Get-Item $cppExe).Length / 1KB, 2)
        Write-Host "✓ C++ Console App: $cppExe ($size KB)" -ForegroundColor Green
    } else {
        Write-Host "✗ C++ Console App: Not found" -ForegroundColor Red
    }

    if (Test-Path $cppLib) {
        $size = [Math]::Round((Get-Item $cppLib).Length / 1KB, 2)
        Write-Host "✓ C++ Library:     $cppLib ($size KB)" -ForegroundColor Green
    } else {
        Write-Host "✗ C++ Library: Not found" -ForegroundColor Red
    }

    if (Test-Path $csharpExe) {
        $size = [Math]::Round((Get-Item $csharpExe).Length / 1MB, 2)
        Write-Host "✓ C# GUI App:      $csharpExe ($size MB)" -ForegroundColor Green
    } else {
        Write-Host "✗ C# GUI App: Not found" -ForegroundColor Red
    }

    if (Test-Path $installer) {
        $size = [Math]::Round((Get-Item $installer).Length / 1MB, 2)
        Write-Host "✓ Installer:       $installer ($size MB)" -ForegroundColor Green
    }

    Write-Host "`nBuild completed successfully!" -ForegroundColor Green

} catch {
    Write-Error "Build failed: $($_.Exception.Message)"
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "`nPress Enter to exit..." -ForegroundColor Cyan
Read-Host