# ValheimTwitch packaging script.
#
# Builds the mod and produces a Thunderstore/r2modman-compatible zip in dist/.
#
# Usage:
#   ./package.ps1                              # build + package
#   ./package.ps1 -GameDir "D:\Steam\...\Valheim"
#   ./package.ps1 -Profile "Server"            # also deploy straight into a Mod Manager profile
#   ./package.ps1 -ProfileDir "C:\...\profiles\Server"
#
# Import the resulting zip into r2modman/Thunderstore Mod Manager via:
#   Profile -> Select profile -> "Import local mod" (or drag-and-drop on the mod list)
# then launch the game with "Start modded".

param(
    # Game install to compile against. Empty = use the csproj default, which points
    # at the live Steam install (see ValheimTwitch.csproj). IMPORTANT: this must be
    # the install that actually runs - compiling against a stale copy of Valheim
    # causes "Method not found" errors in-game when game signatures have changed.
    [string]$GameDir = "",
    # Thunderstore Mod Manager profile name (quick dev loop: DLLs are copied
    # straight into the profile after building).
    [string]$Profile = "",
    # Full path to a profile folder (alternative to -Profile).
    [string]$ProfileDir = ""
)

$ErrorActionPreference = "Stop"

# Keep these in sync with Plugin.cs (PluginVersion) and manifest.json (version_number).
$Author = "CodeWithTD"
$Version = "0.8.0"

$Root = $PSScriptRoot
$BuildDir = Join-Path $Root "bin\Release"
$DistDir = Join-Path $Root "dist"
$StagingDir = Join-Path $DistDir "staging"
$PluginStageDir = Join-Path $StagingDir "BepInEx\plugins\ValheimTwitch"
$ZipPath = Join-Path $DistDir "$Author-ValheimTwitch-$Version.zip"

Write-Host "==> Building$(if ($GameDir) { " (GameDir: $GameDir)" })" -ForegroundColor Cyan
if ($GameDir) {
    dotnet build (Join-Path $Root "ValheimTwitch.csproj") -c Release -p:GameDir=$GameDir
} else {
    dotnet build (Join-Path $Root "ValheimTwitch.csproj") -c Release
}
if ($LASTEXITCODE -ne 0) {
    throw "Build failed; packaging aborted."
}

Write-Host "==> Staging package" -ForegroundColor Cyan
if (Test-Path $StagingDir) {
    Remove-Item -Recurse -Force $StagingDir
}
New-Item -ItemType Directory -Force -Path $PluginStageDir | Out-Null

Copy-Item (Join-Path $Root "manifest.json") $StagingDir
Copy-Item (Join-Path $Root "icon.png") $StagingDir
Copy-Item (Join-Path $Root "README.md") $StagingDir
Copy-Item (Join-Path $Root "CHANGELOG.md") $StagingDir
Copy-Item (Join-Path $Root "SPAWNS.md") $StagingDir

Copy-Item (Join-Path $BuildDir "ValheimTwitch.dll") $PluginStageDir
Copy-Item (Join-Path $BuildDir "websocket-sharp.dll") $PluginStageDir
Copy-Item (Join-Path $BuildDir "Newtonsoft.Json.dll") $PluginStageDir

Write-Host "==> Creating $ZipPath" -ForegroundColor Cyan
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (Test-Path $ZipPath) {
    Remove-Item -Force $ZipPath
}

# Zip entries are written with forward slashes so the package extracts
# correctly on every platform/tool (Compress-Archive would use '\').
$zip = [System.IO.Compression.ZipFile]::Open($ZipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem -Recurse -File $StagingDir | ForEach-Object {
        $relative = $_.FullName.Substring($StagingDir.Length + 1).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip, $_.FullName, $relative, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        Write-Host "    + $relative" -ForegroundColor DarkGray
    }
}
finally {
    $zip.Dispose()
}

# Manifest sanity checks before we call it a package.
$manifest = Get-Content (Join-Path $StagingDir "manifest.json") -Raw | ConvertFrom-Json
if ($manifest.version_number -ne $Version) {
    throw "manifest.json version_number ($($manifest.version_number)) does not match script version ($Version)."
}

Write-Host ""
Write-Host "Package created: $ZipPath" -ForegroundColor Green

# Optional quick-deploy into a Thunderstore Mod Manager / r2modman profile.
if (-not $ProfileDir -and $Profile) {
    $ProfileDir = Join-Path $env:APPDATA "Thunderstore Mod Manager\DataFolder\Valheim\profiles\$Profile"
    if (-not (Test-Path $ProfileDir)) {
        $ProfileDir = Join-Path $env:APPDATA "r2modmanPlus-local\Valheim\profiles\$Profile"
    }
}
if ($ProfileDir) {
    if (-not (Test-Path (Join-Path $ProfileDir "BepInEx\core\BepInEx.dll"))) {
        throw "'$ProfileDir' does not look like a modded profile (BepInEx\core\BepInEx.dll not found)."
    }
    $pluginTarget = Join-Path $ProfileDir "BepInEx\plugins\ValheimTwitch"
    New-Item -ItemType Directory -Force -Path $pluginTarget | Out-Null
    Copy-Item (Join-Path $BuildDir "ValheimTwitch.dll") $pluginTarget -Force
    Copy-Item (Join-Path $BuildDir "websocket-sharp.dll") $pluginTarget -Force
    Copy-Item (Join-Path $BuildDir "Newtonsoft.Json.dll") $pluginTarget -Force
    Write-Host "Deployed DLLs to: $pluginTarget" -ForegroundColor Green
    Write-Host "Note: direct deploys load fine but do not appear in the Mod Manager's mod list;"
    Write-Host "      import the zip with 'Import local mod' for a managed/visible install."
} else {
    Write-Host "Import it with 'Import local mod', enable it, then 'Start modded'."
}
