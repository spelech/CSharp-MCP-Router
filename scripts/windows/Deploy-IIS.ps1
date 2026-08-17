<#
.SYNOPSIS
    Automated IIS Deployment and Configuration Script for C# MCP Router Gateway.

.DESCRIPTION
    Builds the frontend and backend, configures the IIS Application Pool with 'No Managed Code',
    provisions the IIS Website, configures folder security permissions for the AppPool identity,
    deploys web.config with unbuffered SSE streaming settings, and verifies deployment health.

.PARAMETER SiteName
    Name of the IIS Website. Default: "McpRouter".

.PARAMETER AppPoolName
    Name of the IIS Application Pool. Default: "McpRouterAppPool".

.PARAMETER Port
    HTTP port for the IIS Website binding. Default: 8080.

.PARAMETER HostName
    Optional host header binding (e.g., "mcp.corp.local"). Default: "" (all host headers).

.PARAMETER PhysicalPath
    Target physical deployment directory. Default: "C:\inetpub\mcp-router".

.PARAMETER Configuration
    Build configuration: "Release" or "Debug". Default: "Release".

.PARAMETER RepoRoot
    Root directory of the CSharp-MCP-Router repository. Defaults to the repository containing this script.

.PARAMETER SkipFrontend
    If specified, skips running the npm frontend build.

.PARAMETER SkipBuild
    If specified, skips both frontend and backend builds (assumes binaries already compiled).

.PARAMETER SelfContained
    If specified, publishes a self-contained .NET binary including the runtime.

.PARAMETER RuntimeIdentifier
    Target runtime identifier for publish. Default: "win-x64".

.PARAMETER EnableWindowsAuth
    If specified, explicitly enables Windows Authentication on the IIS site.

.EXAMPLE
    .\Deploy-IIS.ps1 -Port 8080 -PhysicalPath "C:\inetpub\mcp-router"

.EXAMPLE
    .\Deploy-IIS.ps1 -SiteName "McpRouterProd" -Port 443 -HostName "mcp.corp.local" -EnableWindowsAuth
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$SiteName = "McpRouter",

    [Parameter()]
    [string]$AppPoolName = "McpRouterAppPool",

    [Parameter()]
    [int]$Port = 8080,

    [Parameter()]
    [string]$HostName = "",

    [Parameter()]
    [string]$PhysicalPath = "C:\inetpub\mcp-router",

    [Parameter()]
    [string]$Configuration = "Release",

    [Parameter()]
    [string]$RepoRoot,

    [Parameter()]
    [string]$FrontendPath,

    [Parameter()]
    [switch]$SkipFrontend,

    [Parameter()]
    [switch]$SkipBuild,

    [Parameter()]
    [switch]$SelfContained,

    [Parameter()]
    [string]$RuntimeIdentifier = "win-x64",

    [Parameter()]
    [switch]$EnableWindowsAuth
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "`n[+] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[✓] $Message" -ForegroundColor Green
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[!] $Message" -ForegroundColor Yellow
}

# ---------------------------------------------------------------------------
# 1. Administrator Elevation Check
# ---------------------------------------------------------------------------
$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]$currentIdentity
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script requires Administrator privileges to configure IIS. Please run PowerShell as Administrator."
    exit 1
}

# ---------------------------------------------------------------------------
# 2. IIS Module Verification
# ---------------------------------------------------------------------------
Write-Step "Verifying IIS Administration Modules..."
if (-not (Get-Module -ListAvailable -Name WebAdministration)) {
    Write-Error "The 'WebAdministration' PowerShell module was not found. Please ensure IIS Management Scripts and Tools are installed."
    exit 1
}
Import-Module WebAdministration -ErrorAction Stop

# ---------------------------------------------------------------------------
# 3. Path Resolution
# ---------------------------------------------------------------------------
if (-not $RepoRoot) {
    $RepoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
}
if (-not $FrontendPath) {
    $FrontendPath = Join-Path $RepoRoot "frontend"
}

Write-Host "Repository Root: $RepoRoot" -ForegroundColor DarkGray
Write-Host "Target Deploy Path: $PhysicalPath" -ForegroundColor DarkGray

# ---------------------------------------------------------------------------
# 4. Frontend Build
# ---------------------------------------------------------------------------
if (-not $SkipBuild -and -not $SkipFrontend) {
    Write-Step "Building Frontend UI Assets..."
    if (Test-Path $FrontendPath) {
        Push-Location $FrontendPath
        try {
            $npmCmd = Get-Command npm -ErrorAction SilentlyContinue
            if (-not $npmCmd) {
                Write-Warn "npm was not found in PATH. Skipping frontend build. Ensure wwwroot is pre-compiled."
            } else {
                Write-Host "Running npm run build in $FrontendPath..." -ForegroundColor DarkGray
                & npm run build
                if ($LASTEXITCODE -ne 0) {
                    Write-Error "Frontend build failed with exit code $LASTEXITCODE."
                    exit $LASTEXITCODE
                }
                Write-Success "Frontend build completed successfully."
            }
        } finally {
            Pop-Location
        }
    } else {
        Write-Warn "Frontend directory not found at $FrontendPath. Proceeding with existing static assets."
    }
}

# ---------------------------------------------------------------------------
# 5. Backend Build & Publish
# ---------------------------------------------------------------------------
if (-not $SkipBuild) {
    Write-Step "Publishing Backend (.NET 10 Web App)..."
    $csprojPath = Join-Path $RepoRoot "mcp-router.csproj"
    if (-not (Test-Path $csprojPath)) {
        Write-Error "Project file not found at $csprojPath."
        exit 1
    }

    $publishArgs = @(
        "publish",
        $csprojPath,
        "-c", $Configuration,
        "-o", $PhysicalPath
    )

    if ($SelfContained) {
        $publishArgs += @("-r", $RuntimeIdentifier, "--self-contained", "true")
    } else {
        $publishArgs += @("--self-contained", "false")
    }

    Write-Host "Executing: dotnet $($publishArgs -join ' ')" -ForegroundColor DarkGray
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed with exit code $LASTEXITCODE."
        exit $LASTEXITCODE
    }
    Write-Success "Backend published to $PhysicalPath."
} else {
    Write-Step "Skipping build as requested (-SkipBuild)."
    if (-not (Test-Path $PhysicalPath)) {
        New-Item -ItemType Directory -Path $PhysicalPath -Force | Out-Null
    }
}

# ---------------------------------------------------------------------------
# 6. Ensure Required Directories & Permissions
# ---------------------------------------------------------------------------
Write-Step "Configuring Directory Permissions & Log Folders..."
$logsDir = Join-Path $PhysicalPath "logs"
if (-not (Test-Path $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
}

$appPoolIdentity = "IIS AppPool\$AppPoolName"
Write-Host "Granting permissions to '$appPoolIdentity' on '$PhysicalPath'..." -ForegroundColor DarkGray

# Use icacls for reliable inheritance configuration on Windows
& icacls "$PhysicalPath" /grant "${appPoolIdentity}:(OI)(CI)M" /T /Q
if ($LASTEXITCODE -ne 0) {
    Write-Warn "icacls exited with code $LASTEXITCODE when setting ACL for $appPoolIdentity. Ensure AppPool exists."
} else {
    Write-Success "Directory permissions configured for $appPoolIdentity."
}

# ---------------------------------------------------------------------------
# 7. Configure web.config
# ---------------------------------------------------------------------------
$destWebConfig = Join-Path $PhysicalPath "web.config"
$exampleWebConfig = Join-Path $PSScriptRoot "web.config.example"

if (-not (Test-Path $destWebConfig)) {
    if (Test-Path $exampleWebConfig) {
        Write-Step "Copying web.config.example to destination web.config..."
        Copy-Item -Path $exampleWebConfig -Destination $destWebConfig -Force
        Write-Success "Created $destWebConfig from example."
    } else {
        Write-Warn "web.config.example not found at $exampleWebConfig. Please ensure web.config is present in $PhysicalPath."
    }
} else {
    Write-Host "Existing web.config found in $PhysicalPath. Preserving file." -ForegroundColor DarkGray
}

# ---------------------------------------------------------------------------
# 8. Configure IIS Application Pool
# ---------------------------------------------------------------------------
Write-Step "Configuring IIS Application Pool: $AppPoolName..."
if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    Write-Host "Creating new AppPool: $AppPoolName..." -ForegroundColor DarkGray
    $appPool = New-WebAppPool -Name $AppPoolName
} else {
    Write-Host "AppPool $AppPoolName already exists. Updating settings..." -ForegroundColor DarkGray
}

# In-Process hosting requires .NET CLR Version: No Managed Code ("")
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""

# Auto-start / AlwaysRunning to ensure persistent upstream connections
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "startMode" -Value "AlwaysRunning"

# Disable idle timeout (0) so the gateway is not stopped during quiet periods
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "processModel.idleTimeout" -Value ([TimeSpan]::Zero)

Write-Success "AppPool '$AppPoolName' configured (No Managed Code, AlwaysRunning, IdleTimeout: 0)."

# ---------------------------------------------------------------------------
# 9. Configure IIS Website
# ---------------------------------------------------------------------------
Write-Step "Configuring IIS Website: $SiteName..."
$existingSite = Get-Website -Name $SiteName -ErrorAction SilentlyContinue

if (-not $existingSite) {
    Write-Host "Creating new IIS Website: $SiteName on port $Port..." -ForegroundColor DarkGray
    $newSiteParams = @{
        Name = $SiteName
        Port = $Port
        PhysicalPath = $PhysicalPath
        ApplicationPool = $AppPoolName
    }
    if ($HostName) {
        $newSiteParams["HostHeader"] = $HostName
    }
    New-Website @newSiteParams | Out-Null
    Write-Success "IIS Website '$SiteName' created on port $Port."
} else {
    Write-Host "Website '$SiteName' exists. Updating physical path and AppPool..." -ForegroundColor DarkGray
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name "physicalPath" -Value $PhysicalPath
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name "applicationPool" -Value $AppPoolName
    Write-Success "IIS Website '$SiteName' updated."
}

# Enable Windows Authentication if requested
if ($EnableWindowsAuth) {
    Write-Step "Enabling Windows Authentication for '$SiteName'..."
    try {
        Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" `
            -Name "enabled" -Value $true -PSPath "IIS:\" -Location $SiteName
        Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" `
            -Name "enabled" -Value $true -PSPath "IIS:\" -Location $SiteName
        Write-Success "Windows Authentication enabled on '$SiteName'."
    } catch {
        Write-Warn "Could not enable Windows Authentication via WebConfiguration: $_. Ensure the Windows Authentication IIS feature is installed."
    }
}

# ---------------------------------------------------------------------------
# 10. Restart & Health Probe Verification
# ---------------------------------------------------------------------------
Write-Step "Restarting Application Pool and Website..."
Restart-WebAppPool -Name $AppPoolName
Start-Sleep -Seconds 2

$siteState = (Get-Website -Name $SiteName).State
if ($siteState -ne "Started") {
    Start-Website -Name $SiteName
}
Write-Success "IIS AppPool '$AppPoolName' and Site '$SiteName' restarted."

Write-Step "Performing Health Check Probe..."
$healthUrl = "http://localhost:$Port/health"
$maxRetries = 10
$retryCount = 0
$healthy = $false

while ($retryCount -lt $maxRetries -and -not $healthy) {
    $retryCount++
    try {
        Write-Host "Probing $healthUrl (Attempt $retryCount of $maxRetries)..." -ForegroundColor DarkGray
        $response = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec 5 -ErrorAction Stop
        if ($response.status -eq "healthy" -or $response.service -eq "McpRouter") {
            $healthy = $true
            Write-Success "Health check passed! Response: $($response | ConvertTo-Json -Compress)"
        }
    } catch {
        Start-Sleep -Seconds 2
    }
}

if (-not $healthy) {
    Write-Warn "Health check probe could not reach '$healthUrl'. Please check IIS logs in '$logsDir' or Event Viewer (Application log)."
} else {
    Write-Host "`n========================================================" -ForegroundColor Green
    Write-Host "  MCP Router Successfully Deployed to IIS!" -ForegroundColor Green
    Write-Host "  URL: http://localhost:$Port" -ForegroundColor Green
    Write-Host "  Health Endpoint: http://localhost:$Port/health" -ForegroundColor Green
    Write-Host "========================================================`n" -ForegroundColor Green
}
