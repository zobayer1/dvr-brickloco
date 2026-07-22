<#
.SYNOPSIS
    Removes BrickLoco from Derail Valley's Mods folder so the mod stops loading.
    Also sweeps up any BepInEx-era install under BepInEx/plugins.

.EXAMPLE
    .\scripts\undeploy.ps1
    .\scripts\undeploy.ps1 -PurgeConfig
#>
[CmdletBinding()]
param(
    # Overrides DerailValleyDir in BrickLoco.csproj / $env:DERAIL_VALLEY_DIR
    [string]$GameDir,

    # Also delete the legacy BepInEx config (com.zobayer.brickloco.cfg). The UMM Settings.xml
    # lives inside Mods\BrickLoco and is always removed with the folder.
    [switch]$PurgeConfig,

    # Undeploy even if the game appears to be running (the delete will likely fail)
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# Run against the project root, whatever directory the script was invoked from.
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location -Path $projectRoot

$running = Get-Process -Name 'DerailValley*' -ErrorAction SilentlyContinue
if ($running -and -not $Force) {
    Write-Host "Derail Valley is running (PID $($running.Id -join ', ')). It holds a lock on the deployed DLL." -ForegroundColor Yellow
    Write-Host "Quit the game and re-run, or pass -Force to try anyway." -ForegroundColor Yellow
    exit 1
}

# Target the project explicitly: the solution also contains the test project, which has no Undeploy target.
$project = Join-Path $projectRoot 'src\BrickLoco\BrickLoco.csproj'

$msbuildArgs = @('build', $project, '-t:Undeploy', '-v:minimal', '-nologo')
if ($GameDir) { $msbuildArgs += "-p:DerailValleyDir=$GameDir" }
if ($PurgeConfig) { $msbuildArgs += '-p:PurgeConfig=true' }

& dotnet @msbuildArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "Undeploy failed (exit $LASTEXITCODE)." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Undeploy complete." -ForegroundColor Green
