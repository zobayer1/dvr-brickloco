<#
.SYNOPSIS
    Builds BrickLoco and installs it into Derail Valley's Mods folder (Unity Mod Manager).

.EXAMPLE
    .\scripts\deploy.ps1
    .\scripts\deploy.ps1 -Configuration Release
    .\scripts\deploy.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Derail Valley"
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    # Overrides DerailValleyDir in BrickLoco.csproj / $env:DERAIL_VALLEY_DIR
    [string]$GameDir,

    # Deploy even if the game appears to be running (the copy will likely fail)
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

# Target the project explicitly: the solution also contains the test project, which has no Deploy target.
$project = Join-Path $projectRoot 'src\BrickLoco\BrickLoco.csproj'

$msbuildArgs = @('build', $project, '-t:Deploy', "-c:$Configuration", '-v:minimal', '-nologo')
if ($GameDir) { $msbuildArgs += "-p:DerailValleyDir=$GameDir" }

& dotnet @msbuildArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "Deploy failed (exit $LASTEXITCODE)." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Deploy complete. Enable BrickLoco in the UMM window (Ctrl+F10 in-game);" -ForegroundColor Green
Write-Host "logs: DerailValley_Data\Managed\UnityModManager\Log.txt" -ForegroundColor Green
