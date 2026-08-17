<#
.SYNOPSIS
    Windows Service Management Script for C# MCP Router Gateway.

.DESCRIPTION
    Automates publishing the C# MCP Router binary, registering it as a native Windows Service
    with automatic recovery triggers, and managing its lifecycle (Install, Uninstall, Start, Stop, Restart, Status).

.PARAMETER Action
    Service action to perform: "Install", "Uninstall", "Start", "Stop", "Restart", "Status".

.PARAMETER ServiceName
    Name of the Windows Service in SCM. Default: "McpRouter".

.PARAMETER DisplayName
    Display name of the Windows Service. Default: "MCP Router Gateway Service".

.PARAMETER Description
    Description text for the Windows Service.

.PARAMETER InstallDir
    Target directory for publishing service binaries. Default: "C:\Program Files\McpRouter".

.PARAMETER Port
    Listening port for the service. Default: 8080.

.PARAMETER Urls
    Binding URLs for ASP.NET Core Kestrel. Default: "http://0.0.0.0:8080".

.PARAMETER Configuration
    Build configuration: "Release" or "Debug". Default: "Release".

.PARAMETER RepoRoot
    Root directory of the CSharp-MCP-Router repository.

.PARAMETER SkipFrontend
    If specified, skips building the frontend UI.

.PARAMETER SkipBuild
    If specified, skips both frontend and backend builds during installation.

.PARAMETER SelfContained
    If specified, publishes a self-contained single-folder bundle including the .NET runtime.

.PARAMETER RuntimeIdentifier
    Target runtime identifier for publish. Default: "win-x64".

.PARAMETER ServiceAccount
    Account under which the service runs. Default: "NT AUTHORITY\LocalSystem" (or "NT AUTHORITY\NetworkService").

.EXAMPLE
    .\Setup-WindowsService.ps1 -Action Install -Port 8080

.EXAMPLE
    .\Setup-WindowsService.ps1 -Action Status

.EXAMPLE
    .\Setup-WindowsService.ps1 -Action Restart
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("Install", "Uninstall", "Start", "Stop", "Restart", "Status")]
    [string]$Action,

    [Parameter()]
    [string]$ServiceName = "McpRouter",

    [Parameter()]
    [string]$DisplayName = "MCP Router Gateway Service",

    [Parameter()]
    [string]$Description = "High-performance C# ASP.NET Core gateway router for the Model Context Protocol (MCP)",

    [Parameter()]
    [string]$InstallDir = "C:\Program Files\McpRouter",

    [Parameter()]
    [int]$Port = 8080,

    [Parameter()]
    [string]$Urls,

    [Parameter()]
    [string]$Configuration = "Release",

    [Parameter()]
    [string]$RepoRoot,

    [Parameter()]
    [switch]$SkipFrontend,

    [Parameter()]
    [switch]$SkipBuild,

    [Parameter()]
    [switch]$SelfContained,

    [Parameter()]
    [string]$RuntimeIdentifier = "win-x64",

    [Parameter()]
    [string]$ServiceAccount = "NT AUTHORITY\LocalSystem"
)

$ErrorActionPreference = "Stop"

if (-not $Urls) {
    $Urls = "http://0.0.0.0:$Port"
}

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

function Assert-Administrator {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]$currentIdentity
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Error "Managing Windows Services requires Administrator privileges. Please launch PowerShell as Administrator."
        exit 1
    }
}

function Probe-Health {
    param([int]$CheckPort)
    $healthUrl = "http://localhost:$CheckPort/health"
    $maxRetries = 10
    $retryCount = 0
    $healthy = $false

    Write-Host "Probing health endpoint at $healthUrl..." -ForegroundColor DarkGray
    while ($retryCount -lt $maxRetries -and -not $healthy) {
        $retryCount++
        try {
            $response = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec 4 -ErrorAction Stop
            if ($response.status -eq "healthy" -or $response.service -eq "McpRouter") {
                $healthy = $true
                Write-Success "Health check passed! Response: $($response | ConvertTo-Json -Compress)"
            }
        } catch {
            Start-Sleep -Seconds 2
        }
    }

    if (-not $healthy) {
        Write-Warn "Health check probe could not reach '$healthUrl'. Check Windows Event Viewer (Application log) or service logs."
    }
}

# ---------------------------------------------------------------------------
# Path Resolution
# ---------------------------------------------------------------------------
if (-not $RepoRoot) {
    $RepoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
}

switch ($Action) {
    "Install" {
        Assert-Administrator
        Write-Step "Installing Windows Service '$ServiceName'..."

        # 1. Build frontend if needed
        if (-not $SkipBuild -and -not $SkipFrontend) {
            $frontendPath = Join-Path $RepoRoot "frontend"
            if (Test-Path $frontendPath) {
                Write-Step "Building Frontend Assets..."
                Push-Location $frontendPath
                try {
                    $npmCmd = Get-Command npm -ErrorAction SilentlyContinue
                    if ($npmCmd) {
                        Write-Host "Running npm run build..." -ForegroundColor DarkGray
                        & npm run build
                        if ($LASTEXITCODE -ne 0) {
                            Write-Error "Frontend build failed with exit code $LASTEXITCODE."
                            exit $LASTEXITCODE
                        }
                        Write-Success "Frontend assets built into wwwroot."
                    } else {
                        Write-Warn "npm not found. Skipping frontend build."
                    }
                } finally {
                    Pop-Location
                }
            }
        }

        # 2. Publish backend
        if (-not $SkipBuild) {
            Write-Step "Publishing Backend to '$InstallDir'..."
            $csprojPath = Join-Path $RepoRoot "mcp-router.csproj"
            if (-not (Test-Path $csprojPath)) {
                Write-Error "Project file not found at $csprojPath."
                exit 1
            }

            if (-not (Test-Path $InstallDir)) {
                New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
            }

            $publishArgs = @(
                "publish",
                $csprojPath,
                "-c", $Configuration,
                "-r", $RuntimeIdentifier,
                "-o", $InstallDir
            )

            if ($SelfContained) {
                $publishArgs += @("--self-contained", "true")
            } else {
                $publishArgs += @("--self-contained", "false")
            }

            Write-Host "Executing: dotnet $($publishArgs -join ' ')" -ForegroundColor DarkGray
            & dotnet @publishArgs
            if ($LASTEXITCODE -ne 0) {
                Write-Error "dotnet publish failed with exit code $LASTEXITCODE."
                exit $LASTEXITCODE
            }
            Write-Success "Backend published successfully."
        }

        # 3. Create log directory
        $logsDir = Join-Path $InstallDir "logs"
        if (-not (Test-Path $logsDir)) {
            New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
        }

        # 4. Check if service already exists
        $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($existingService) {
            Write-Warn "Service '$ServiceName' already exists. Stopping and removing previous registration..."
            if ($existingService.Status -eq "Running") {
                Stop-Service -Name $ServiceName -Force
                Start-Sleep -Seconds 2
            }
            & sc.exe delete $ServiceName | Out-Null
            Start-Sleep -Seconds 1
        }

        # 5. Register Windows Service
        Write-Step "Registering Windows Service with Service Control Manager (SCM)..."
        $exePath = Join-Path $InstallDir "mcp-router.exe"
        if (-not (Test-Path $exePath)) {
            # Fallback for dotnet dll execution if exe is not present
            $dllPath = Join-Path $InstallDir "mcp-router.dll"
            if (Test-Path $dllPath) {
                $dotnetPath = (Get-Command dotnet).Source
                $binPath = "`"$dotnetPath`" `"$dllPath`" --urls `"$Urls`""
            } else {
                Write-Error "Could not find mcp-router.exe or mcp-router.dll in '$InstallDir'."
                exit 1
            }
        } else {
            $binPath = "`"$exePath`" --urls `"$Urls`""
        }

        # Create service via sc.exe for maximum compatibility with recovery flags
        $scCreateArgs = @(
            "create", $ServiceName,
            "binPath=", $binPath,
            "DisplayName=", $DisplayName,
            "start=", "auto"
        )
        if ($ServiceAccount -and $ServiceAccount -ne "NT AUTHORITY\LocalSystem") {
            $scCreateArgs += @("obj=", $ServiceAccount)
        }

        & sc.exe @scCreateArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Error "sc.exe create failed with exit code $LASTEXITCODE."
            exit $LASTEXITCODE
        }

        # Configure service description
        & sc.exe description $ServiceName $Description | Out-Null

        # Configure automatic restart on failure: restart after 1m on 1st, 2nd, and subsequent crashes
        Write-Step "Configuring service recovery actions (auto-restart on crash)..."
        & sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
        & sc.exe failureflag $ServiceName 1 | Out-Null

        Write-Success "Service '$ServiceName' created and recovery actions configured."

        # 6. Start Service
        Write-Step "Starting service '$ServiceName'..."
        Start-Service -Name $ServiceName
        Start-Sleep -Seconds 3

        $svc = Get-Service -Name $ServiceName
        if ($svc.Status -eq "Running") {
            Write-Success "Service '$ServiceName' is running."
            Probe-Health -CheckPort $Port
        } else {
            Write-Warn "Service status is: $($svc.Status). Check logs in '$logsDir' or Event Viewer."
        }
    }

    "Uninstall" {
        Assert-Administrator
        Write-Step "Uninstalling Windows Service '$ServiceName'..."
        $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if (-not $svc) {
            Write-Warn "Service '$ServiceName' is not installed."
            return
        }

        if ($svc.Status -eq "Running") {
            Write-Host "Stopping service '$ServiceName'..." -ForegroundColor DarkGray
            Stop-Service -Name $ServiceName -Force
            Start-Sleep -Seconds 2
        }

        & sc.exe delete $ServiceName
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Service '$ServiceName' successfully removed."
        } else {
            Write-Error "Failed to delete service '$ServiceName'."
        }
    }

    "Start" {
        Assert-Administrator
        Write-Step "Starting service '$ServiceName'..."
        Start-Service -Name $ServiceName
        Start-Sleep -Seconds 2
        $svc = Get-Service -Name $ServiceName
        Write-Success "Service '$ServiceName' status: $($svc.Status)."
        if ($svc.Status -eq "Running") {
            Probe-Health -CheckPort $Port
        }
    }

    "Stop" {
        Assert-Administrator
        Write-Step "Stopping service '$ServiceName'..."
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
        $svc = Get-Service -Name $ServiceName
        Write-Success "Service '$ServiceName' status: $($svc.Status)."
    }

    "Restart" {
        Assert-Administrator
        Write-Step "Restarting service '$ServiceName'..."
        Restart-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 3
        $svc = Get-Service -Name $ServiceName
        Write-Success "Service '$ServiceName' status: $($svc.Status)."
        if ($svc.Status -eq "Running") {
            Probe-Health -CheckPort $Port
        }
    }

    "Status" {
        Write-Step "Querying service status for '$ServiceName'..."
        $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if (-not $svc) {
            Write-Warn "Service '$ServiceName' is not installed."
        } else {
            [PSCustomObject]@{
                ServiceName = $svc.Name
                DisplayName = $svc.DisplayName
                Status      = $svc.Status
                StartType   = $svc.StartType
            } | Format-List
            
            if ($svc.Status -eq "Running") {
                Probe-Health -CheckPort $Port
            }
        }
    }
}
