param(
    [string]$Target = "build",
    [string]$Configuration = "Release",
    [string]$GameType = "both"
)

$ErrorActionPreference = "Stop"

function Write-Banner {
    param([string]$Message)
    Write-Host "`n===============================================" -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "===============================================`n" -ForegroundColor Cyan
}

function Find-MSBuild {
    $msbuildPaths = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )
    
    foreach ($path in $msbuildPaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    
    throw "MSBuild not found. Please install Visual Studio."
}

function Get-GameInstallPath {
    param([string]$GameName)
    
    $registryPaths = @{
        "KK"  = "HKCU:\Software\Illusion\koikatu\koikatu"
        "KKS" = "HKCU:\Software\Illusion\KoikatsuSunshine\KoikatsuSunshine"
    }
    
    $regPath = $registryPaths[$GameName]
    if (-not $regPath) {
        throw "Unknown game: $GameName. Supported games: KK, KKS"
    }
    
    try {
        $installDir = Get-ItemProperty -Path $regPath -Name "INSTALLDIR" -ErrorAction Stop
        return $installDir.INSTALLDIR
    } catch {
        return $null
    }
}

function Invoke-Deploy {
    param([string]$GameType = "both")
    
    Write-Banner "Deploying DLLs to game directories"
    
    if (-not (Test-Path "bin")) {
        throw "No build outputs found. Please build the solution first."
    }
    
    $deployments = @()
    
    if ($GameType -eq "both" -or $GameType -eq "kk") {
        $kkPath = Get-GameInstallPath "KK"
        if ($kkPath -and (Test-Path $kkPath)) {
            $kkPluginPath = Join-Path $kkPath "BepInEx\plugins"
            if (Test-Path $kkPluginPath) {
                $deployments += @{
                    Game = "Koikatu (KK)"
                    SourceFile = "bin\KK_KKStudioSocket.dll"
                    TargetPath = $kkPluginPath
                }
            } else {
                Write-Host "Warning: BepInEx not found in KK installation ($kkPath)" -ForegroundColor Yellow
            }
        } else {
            Write-Host "Warning: Koikatu (KK) installation not found" -ForegroundColor Yellow
        }
    }
    
    if ($GameType -eq "both" -or $GameType -eq "kks") {
        $kksPath = Get-GameInstallPath "KKS"
        if ($kksPath -and (Test-Path $kksPath)) {
            $kksPluginPath = Join-Path $kksPath "BepInEx\plugins"
            if (Test-Path $kksPluginPath) {
                $deployments += @{
                    Game = "Koikatsu Sunshine (KKS)"
                    SourceFile = "bin\KKS_KKStudioSocket.dll"
                    TargetPath = $kksPluginPath
                }
            } else {
                Write-Host "Warning: BepInEx not found in KKS installation ($kksPath)" -ForegroundColor Yellow
            }
        } else {
            Write-Host "Warning: Koikatsu Sunshine (KKS) installation not found" -ForegroundColor Yellow
        }
    }
    
    if ($deployments.Count -eq 0) {
        Write-Host "No valid game installations found for deployment." -ForegroundColor Red
        return
    }
    
    foreach ($deployment in $deployments) {
        if (Test-Path $deployment.SourceFile) {
            $targetFile = Join-Path $deployment.TargetPath (Split-Path $deployment.SourceFile -Leaf)
            
            # Create backup if existing file exists
            if (Test-Path $targetFile) {
                $backupFile = "$targetFile.backup"
                Copy-Item $targetFile $backupFile -Force
                Write-Host "  Created backup: $backupFile" -ForegroundColor Gray
            }
            
            Copy-Item $deployment.SourceFile $targetFile -Force
            Write-Host "  Deployed to $($deployment.Game): $targetFile" -ForegroundColor Green
        } else {
            Write-Host "  Warning: Source file not found: $($deployment.SourceFile)" -ForegroundColor Yellow
        }
    }
    
    Write-Host "Deployment completed!" -ForegroundColor Green
}

function Invoke-Clean {
    Write-Banner "Cleaning build outputs"
    
    $pathsToClean = @(
        "bin",
        "src\KKStudioSocket.KK\obj",
        "src\KKStudioSocket.KKS\obj"
    )
    
    foreach ($path in $pathsToClean) {
        if (Test-Path $path) {
            Remove-Item -Recurse -Force $path
            Write-Host "Deleted: $path" -ForegroundColor Yellow
        }
    }
    
    New-Item -ItemType Directory -Force -Path "bin" | Out-Null
    Write-Host "Clean completed!" -ForegroundColor Green
}

function Invoke-Restore {
    Write-Banner "Restoring NuGet packages"
    
    $msbuild = Find-MSBuild
    & $msbuild "KKStudioSocket.sln" /t:Restore /nologo /verbosity:minimal
    
    if ($LASTEXITCODE -ne 0) {
        throw "Package restore failed!"
    }
    
    Write-Host "Package restore completed!" -ForegroundColor Green
}

function Invoke-Build {
    param([string]$Config)
    
    Write-Banner "Building $Config configuration"
    
    $msbuild = Find-MSBuild
    & $msbuild "KKStudioSocket.sln" /p:Configuration=$Config /nologo /verbosity:minimal
    
    if ($LASTEXITCODE -ne 0) {
        throw "$Config build failed!"
    }
    
    Write-Host "$Config build completed!" -ForegroundColor Green
}

function Show-Help {
    Write-Host "KKStudioSocket Build Script" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1 [Target] [Configuration] [GameType]" -ForegroundColor White
    Write-Host ""
    Write-Host "Targets:" -ForegroundColor Yellow
    Write-Host "  clean     - Clean build outputs" -ForegroundColor White
    Write-Host "  restore   - Restore NuGet packages" -ForegroundColor White
    Write-Host "  build     - Build the solution (default)" -ForegroundColor White
    Write-Host "  rebuild   - Clean + Restore + Build" -ForegroundColor White
    Write-Host "  all       - Build both Debug and Release" -ForegroundColor White
    Write-Host "  deploy    - Deploy DLLs to game directories" -ForegroundColor White
    Write-Host ""
    Write-Host "Configurations:" -ForegroundColor Yellow
    Write-Host "  Debug     - Debug build" -ForegroundColor White
    Write-Host "  Release   - Release build (default)" -ForegroundColor White
    Write-Host ""
    Write-Host "Deploy Options:" -ForegroundColor Yellow
    Write-Host "  both      - Deploy to both KK and KKS (default)" -ForegroundColor White
    Write-Host "  kk        - Deploy to Koikatu only" -ForegroundColor White
    Write-Host "  kks       - Deploy to Koikatsu Sunshine only" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1                    # Build Release" -ForegroundColor White
    Write-Host "  .\build.ps1 build Debug       # Build Debug" -ForegroundColor White
    Write-Host "  .\build.ps1 rebuild            # Clean + Restore + Build Release" -ForegroundColor White
    Write-Host "  .\build.ps1 all                # Build both Debug and Release" -ForegroundColor White
    Write-Host "  .\build.ps1 deploy Release both # Deploy to both games" -ForegroundColor White
    Write-Host "  .\build.ps1 deploy Release kk  # Deploy to KK only" -ForegroundColor White
    Write-Host "  .\build.ps1 clean              # Clean only" -ForegroundColor White
}

# Main execution logic
try {
    switch ($Target.ToLower()) {
        "help" {
            Show-Help
        }
        "clean" {
            Invoke-Clean
        }
        "restore" {
            Invoke-Restore
        }
        "build" {
            Invoke-Build $Configuration
            Write-Banner "Build Summary"
            if (Test-Path "bin") {
                $files = Get-ChildItem "bin" -Filter "*.dll"
                Write-Host "Output files:" -ForegroundColor Cyan
                foreach ($file in $files) {
                    Write-Host "  $($file.Name) ($([math]::Round($file.Length / 1KB, 1)) KB)" -ForegroundColor White
                }
            }
        }
        "rebuild" {
            Invoke-Clean
            Invoke-Restore
            Invoke-Build $Configuration
            Write-Banner "Rebuild completed successfully!"
        }
        "all" {
            Invoke-Clean
            Invoke-Restore
            Invoke-Build "Debug"
            Invoke-Build "Release"
            Write-Banner "All configurations built successfully!"
        }
        "deploy" {
            Invoke-Deploy $GameType
        }
        default {
            Write-Host "Unknown target: $Target" -ForegroundColor Red
            Write-Host "Use '.\build.ps1 help' to see available options." -ForegroundColor Yellow
            exit 1
        }
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}