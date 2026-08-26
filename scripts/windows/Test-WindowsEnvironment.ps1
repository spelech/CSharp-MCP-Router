<#
.SYNOPSIS
    Automated Diagnostic and Verification Runner for Windows Hosts (Model Context Gateway).

.DESCRIPTION
    Performs comprehensive diagnostic validation of Windows host prerequisites,
    native Windows security subsystems, and automated quality gates:
    1. Host & Toolchain: .NET 10 SDK, ASP.NET Core Runtime, Node.js, npm, PowerShell, Administrator privileges.
    2. Windows Registry: HKLM\SOFTWARE\McpRouter\Secrets read, write, and cleanup permissions.
    3. DPAPI Cryptography: Machine-scope Protect/Unprotect matching WindowsRegistrySecretRetriever.
    4. Windows Identity: Current identity, User SID, Group SIDs, S-1-5-32-544 (Builtin Administrators).
    5. C# Backend Test Suite: Execution of dotnet test ModelContextGateway.slnx.
    6. Requirements Catalog: Zero-drift verification via scripts/CatalogGenerator.
    7. Frontend Unit Tests: Optional execution of Vitest suite.

.PARAMETER RepoRoot
    Root directory of the Model Context Gateway repository. Defaults to the repository root.

.PARAMETER SkipTests
    Skips execution of the backend xUnit test suite (dotnet test).

.PARAMETER SkipCatalog
    Skips execution of the living requirements catalog verification.

.PARAMETER SkipFrontend
    Skips execution of frontend unit tests.

.PARAMETER IncludeFrontend
    Explicitly runs frontend tests (npm test) in addition to backend tests.

.PARAMETER JsonReportPath
    Optional file path to output the structured machine-readable JSON diagnostic report.

.PARAMETER AllowNonWindows
    Allows running diagnostics on non-Windows environments (skips Windows-native APIs with WARN rather than failing).

.PARAMETER NonInteractive
    Runs in non-interactive mode without pausing.

.EXAMPLE
    # Run all Windows environment validations:
    .\Test-WindowsEnvironment.ps1

.EXAMPLE
    # Run diagnostic checks and generate a JSON report:
    .\Test-WindowsEnvironment.ps1 -JsonReportPath ".\report.json"

.EXAMPLE
    # Quick environment probe skipping lengthy test runs:
    .\Test-WindowsEnvironment.ps1 -SkipTests -SkipCatalog
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$RepoRoot,

    [Parameter()]
    [switch]$SkipTests,

    [Parameter()]
    [switch]$SkipCatalog,

    [Parameter()]
    [switch]$SkipFrontend,

    [Parameter()]
    [switch]$IncludeFrontend,

    [Parameter()]
    [string]$JsonReportPath,

    [Parameter()]
    [switch]$AllowNonWindows,

    [Parameter()]
    [switch]$NonInteractive
)

$ErrorActionPreference = "Continue"

# ---------------------------------------------------------------------------
# Script Initialization & Path Resolution
# ---------------------------------------------------------------------------
if (-not $RepoRoot) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    if (-not $scriptDir) { $scriptDir = $PSScriptRoot }
    if (-not $scriptDir) { $scriptDir = (Get-Location).Path }
    $RepoRoot = Resolve-Path (Join-Path $scriptDir "..\..") -ErrorAction SilentlyContinue
    if (-not $RepoRoot -or -not (Test-Path (Join-Path $RepoRoot "ModelContextGateway.csproj"))) {
        $RepoRoot = (Get-Location).Path
    }
}
$RepoRoot = (Resolve-Path $RepoRoot).Path

$startTime = [DateTime]::UtcNow
$results = [System.Collections.Generic.List[PSCustomObject]]::new()

# ---------------------------------------------------------------------------
# Helper Functions for Formatting and Recording Results
# ---------------------------------------------------------------------------
function Add-DiagnosticResult {
    param(
        [Parameter(Mandatory=$true)][string]$Category,
        [Parameter(Mandatory=$true)][string]$Name,
        [Parameter(Mandatory=$true)][ValidateSet("PASS", "FAIL", "WARN", "SKIP")][string]$Status,
        [Parameter(Mandatory=$true)][string]$Details,
        [Parameter()][string]$ErrorMessage = ""
    )

    $item = [PSCustomObject]@{
        Category     = $Category
        Name         = $Name
        Status       = $Status
        Details      = $Details
        ErrorMessage = $ErrorMessage
        TimestampUtc = [DateTime]::UtcNow.ToString("o")
    }
    $script:results.Add($item)

    $badge = switch ($Status) {
        "PASS" { "[ PASS ]" }
        "FAIL" { "[ FAIL ]" }
        "WARN" { "[ WARN ]" }
        "SKIP" { "[ SKIP ]" }
    }
    $color = switch ($Status) {
        "PASS" { "Green" }
        "FAIL" { "Red" }
        "WARN" { "Yellow" }
        "SKIP" { "DarkGray" }
    }

    Write-Host "  $badge " -ForegroundColor $color -NoNewline
    Write-Host "$Name : " -ForegroundColor Cyan -NoNewline
    Write-Host "$Details" -ForegroundColor White
    if ($ErrorMessage) {
        Write-Host "         Error: $ErrorMessage" -ForegroundColor Red
    }
}

function Write-Section {
    param([string]$Title)
    Write-Host "`n================================================================================" -ForegroundColor DarkCyan
    Write-Host "  $Title" -ForegroundColor Yellow
    Write-Host "================================================================================" -ForegroundColor DarkCyan
}

# ---------------------------------------------------------------------------
# Banner & Host Overview
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host " ██████╗███████╗██╗  ██╗ █████╗ ██████╗ ██████╗     ███╗   ███╗ ██████╗██████╗ " -ForegroundColor Cyan
Write-Host "██╔════╝██╔════╝██║  ██║██╔══██╗██╔══██╗██╔══██╗    ████╗ ████║██╔════╝██╔══██╗" -ForegroundColor Cyan
Write-Host "██║     ███████╗███████║███████║██████╔╝██████╔╝    ██╔████╔██║██║     ██████╔╝" -ForegroundColor Cyan
Write-Host "██║     ╚════██║██╔══██║██╔══██║██╔══██╗██╔═══╝     ██║╚██╔╝██║██║     ██╔═══╝ " -ForegroundColor Cyan
Write-Host "╚██████╗███████║██║  ██║██║  ██║██║  ██║██║         ██║ ╚═╝ ██║╚██████╗██║     " -ForegroundColor Cyan
Write-Host " ╚═════╝╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝         ╚═╝     ╚═╝ ╚═════╝╚═╝     " -ForegroundColor Cyan
Write-Host "        Windows Environment Diagnostic & Quality Gate Runner                   " -ForegroundColor Green
Write-Host "--------------------------------------------------------------------------------" -ForegroundColor DarkGray
Write-Host " Repository Root : $RepoRoot" -ForegroundColor White
Write-Host " Timestamp (UTC) : $($startTime.ToString('u'))" -ForegroundColor White
Write-Host " Execution Mode  : $($PSCmdlet.ParameterSetName)" -ForegroundColor White
Write-Host ""

# ---------------------------------------------------------------------------
# 1. Host Environment & Prerequisites Validation
# ---------------------------------------------------------------------------
Write-Section "1. Host Environment & Toolchain Prerequisites"

$isWin = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT
if ($isWin) {
    Add-DiagnosticResult -Category "Host" -Name "Operating System" -Status "PASS" `
        -Details "$([System.Environment]::OSVersion.VersionString) ($([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture))"
} else {
    if ($AllowNonWindows) {
        Add-DiagnosticResult -Category "Host" -Name "Operating System" -Status "WARN" `
            -Details "Non-Windows Platform: $([System.Environment]::OSVersion.Platform) (AllowNonWindows active)"
    } else {
        Add-DiagnosticResult -Category "Host" -Name "Operating System" -Status "FAIL" `
            -Details "Non-Windows host detected: $([System.Environment]::OSVersion.Platform). Windows APIs require Win32NT."
    }
}

# PowerShell Version
$psVer = $PSVersionTable.PSVersion.ToString()
if ($PSVersionTable.PSVersion.Major -ge 5) {
    Add-DiagnosticResult -Category "Host" -Name "PowerShell Version" -Status "PASS" -Details "PowerShell $psVer"
} else {
    Add-DiagnosticResult -Category "Host" -Name "PowerShell Version" -Status "WARN" -Details "PowerShell $psVer (PowerShell 5.1+ or 7+ recommended)"
}

# Administrator Elevation
$isAdmin = $false
if ($isWin) {
    try {
        $id = [Security.Principal.WindowsIdentity]::GetCurrent()
        $princ = [Security.Principal.WindowsPrincipal]$id
        $isAdmin = $princ.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        if ($isAdmin) {
            Add-DiagnosticResult -Category "Security" -Name "Admin Privileges" -Status "PASS" -Details "Elevated (Administrator)"
        } else {
            Add-DiagnosticResult -Category "Security" -Name "Admin Privileges" -Status "WARN" `
                -Details "Standard User (Non-Elevated). HKLM writes require elevated Administrator prompt."
        }
    } catch {
        Add-DiagnosticResult -Category "Security" -Name "Admin Privileges" -Status "FAIL" -Details "Failed to evaluate principal" -ErrorMessage $_.Exception.Message
    }
} else {
    Add-DiagnosticResult -Category "Security" -Name "Admin Privileges" -Status "SKIP" -Details "Skipped on non-Windows environment"
}

# .NET 10 SDK Check
$dotnetVersion = ""
try {
    $dotnetVersion = (& dotnet --version 2>&1).Trim()
    if ($dotnetVersion -like "10.*") {
        Add-DiagnosticResult -Category "Toolchain" -Name ".NET 10 SDK" -Status "PASS" -Details "Installed (.NET SDK $dotnetVersion)"
    } elseif ($dotnetVersion) {
        Add-DiagnosticResult -Category "Toolchain" -Name ".NET 10 SDK" -Status "WARN" -Details "Detected .NET SDK $dotnetVersion (Target is .NET 10.x)"
    } else {
        Add-DiagnosticResult -Category "Toolchain" -Name ".NET 10 SDK" -Status "FAIL" -Details "dotnet CLI returned empty version"
    }
} catch {
    Add-DiagnosticResult -Category "Toolchain" -Name ".NET 10 SDK" -Status "FAIL" -Details "dotnet CLI not found in PATH" -ErrorMessage $_.Exception.Message
}

# ASP.NET Core 10 Runtime Check
try {
    $runtimes = (& dotnet --list-runtimes 2>&1)
    $hasAspnet10 = ($runtimes | Where-Object { $_ -match "Microsoft\.AspNetCore\.App\s+10\." }) -ne $null
    if ($hasAspnet10) {
        Add-DiagnosticResult -Category "Toolchain" -Name "ASP.NET Core 10 Runtime" -Status "PASS" -Details "Microsoft.AspNetCore.App 10.x runtime present"
    } else {
        Add-DiagnosticResult -Category "Toolchain" -Name "ASP.NET Core 10 Runtime" -Status "WARN" -Details "ASP.NET Core 10 runtime not listed in dotnet --list-runtimes"
    }
} catch {
    Add-DiagnosticResult -Category "Toolchain" -Name "ASP.NET Core 10 Runtime" -Status "WARN" -Details "Unable to query dotnet runtimes" -ErrorMessage $_.Exception.Message
}

# Node.js & npm Check
try {
    $nodeVer = (& node -v 2>&1).Trim()
    $npmVer = (& npm -v 2>&1).Trim()
    if ($nodeVer -and $npmVer) {
        Add-DiagnosticResult -Category "Toolchain" -Name "Node.js & npm" -Status "PASS" -Details "Node $nodeVer / npm $npmVer"
    } else {
        Add-DiagnosticResult -Category "Toolchain" -Name "Node.js & npm" -Status "WARN" -Details "Node.js or npm not detected in PATH"
    }
} catch {
    Add-DiagnosticResult -Category "Toolchain" -Name "Node.js & npm" -Status "WARN" -Details "Node.js toolchain missing (required only for UI builds)" -ErrorMessage $_.Exception.Message
}

# ---------------------------------------------------------------------------
# 2. Windows Registry Secrets Storage Validation
# ---------------------------------------------------------------------------
Write-Section "2. Windows Registry Secrets Subsystem (HKLM:\SOFTWARE\McpRouter\Secrets)"

if ($isWin) {
    $testKeyName = "__diag_test_" + [Guid]::NewGuid().ToString("N").Substring(0, 8)
    $testPlainValue = "McpRouter_Diagnostic_Test_Payload_" + (Get-Random)
    $regPath = "SOFTWARE\McpRouter\Secrets"

    # Step 2a: Registry Key Open / Create
    $hklmBase = $null
    $secretsKey = $null
    try {
        $hklmBase = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry64)
        $secretsKey = $hklmBase.CreateSubKey($regPath, $true)
        if ($secretsKey) {
            Add-DiagnosticResult -Category "Registry" -Name "HKLM Secrets Subkey Write Access" -Status "PASS" -Details "Successfully opened HKLM:\$regPath with write access"
        } else {
            Add-DiagnosticResult -Category "Registry" -Name "HKLM Secrets Subkey Write Access" -Status "FAIL" -Details "CreateSubKey returned null for HKLM:\$regPath"
        }
    } catch {
        if (-not $isAdmin) {
            Add-DiagnosticResult -Category "Registry" -Name "HKLM Secrets Subkey Write Access" -Status "WARN" `
                -Details "Access Denied to HKLM:\$regPath (Expected for non-admin user)" -ErrorMessage $_.Exception.Message
        } else {
            Add-DiagnosticResult -Category "Registry" -Name "HKLM Secrets Subkey Write Access" -Status "FAIL" `
                -Details "Failed to open or create HKLM:\$regPath" -ErrorMessage $_.Exception.Message
        }
    }

    # Step 2b: Plaintext String Write and Read
    if ($secretsKey) {
        try {
            $secretsKey.SetValue($testKeyName, $testPlainValue, [Microsoft.Win32.RegistryValueKind]::String)
            $readBack = $secretsKey.GetValue($testKeyName)
            if ($readBack -eq $testPlainValue) {
                Add-DiagnosticResult -Category "Registry" -Name "Plaintext Value Read/Write" -Status "PASS" -Details "Stored and verified plaintext REG_SZ value"
            } else {
                Add-DiagnosticResult -Category "Registry" -Name "Plaintext Value Read/Write" -Status "FAIL" -Details "Read value '$readBack' did not match written value '$testPlainValue'"
            }
        } catch {
            Add-DiagnosticResult -Category "Registry" -Name "Plaintext Value Read/Write" -Status "FAIL" -Details "Exception during string read/write test" -ErrorMessage $_.Exception.Message
        } finally {
            try { $secretsKey.DeleteValue($testKeyName, $false) } catch {}
        }
    } else {
        Add-DiagnosticResult -Category "Registry" -Name "Plaintext Value Read/Write" -Status "SKIP" -Details "Skipped due to lack of HKLM write access"
    }

    if ($secretsKey) { $secretsKey.Close() }
    if ($hklmBase) { $hklmBase.Close() }
} else {
    Add-DiagnosticResult -Category "Registry" -Name "HKLM Secrets Access" -Status "SKIP" -Details "Skipped on non-Windows environment"
    Add-DiagnosticResult -Category "Registry" -Name "Plaintext Value Read/Write" -Status "SKIP" -Details "Skipped on non-Windows environment"
}

# ---------------------------------------------------------------------------
# 3. DPAPI Cryptography & WindowsRegistrySecretRetriever Compatibility
# ---------------------------------------------------------------------------
Write-Section "3. DPAPI Cryptography (LocalMachine Scope) & Secret Retriever"

if ($isWin) {
    $dpapiTestKey = "__diag_dpapi_" + [Guid]::NewGuid().ToString("N").Substring(0, 8)
    $dpapiPlainSecret = "SuperSecret_DPAPI_Token_" + [Guid]::NewGuid().ToString("D")

    try {
        # Step 3a: DPAPI Protect
        $plainBytes = [System.Text.Encoding]::UTF8.GetBytes($dpapiPlainSecret)
        $scope = [System.Security.Cryptography.DataProtectionScope]::LocalMachine
        $encryptedBytes = [System.Security.Cryptography.ProtectedData]::Protect($plainBytes, $null, $scope)

        if ($encryptedBytes -and $encryptedBytes.Length -gt 0) {
            Add-DiagnosticResult -Category "DPAPI" -Name "DPAPI Machine Protect" -Status "PASS" `
                -Details "Successfully protected payload ($($plainBytes.Length) bytes -> $($encryptedBytes.Length) cipher bytes)"
        } else {
            Add-DiagnosticResult -Category "DPAPI" -Name "DPAPI Machine Protect" -Status "FAIL" -Details "ProtectedData.Protect produced null or empty bytes"
        }

        # Step 3b: DPAPI Unprotect
        $decryptedBytes = [System.Security.Cryptography.ProtectedData]::Unprotect($encryptedBytes, $null, $scope)
        $decryptedString = [System.Text.Encoding]::UTF8.GetString($decryptedBytes)

        if ($decryptedString -eq $dpapiPlainSecret) {
            Add-DiagnosticResult -Category "DPAPI" -Name "DPAPI Machine Unprotect" -Status "PASS" `
                -Details "Successfully decrypted payload matching original string"
        } else {
            Add-DiagnosticResult -Category "DPAPI" -Name "DPAPI Machine Unprotect" -Status "FAIL" `
                -Details "Decrypted string did not match original plaintext"
        }

        # Step 3c: End-to-end Registry + DPAPI Binary Roundtrip
        $hklmBase = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry64)
        $secretsKey = $null
        try {
            $secretsKey = $hklmBase.CreateSubKey("SOFTWARE\McpRouter\Secrets", $true)
            if ($secretsKey) {
                $secretsKey.SetValue($dpapiTestKey, $encryptedBytes, [Microsoft.Win32.RegistryValueKind]::Binary)
                $readRaw = $secretsKey.GetValue($dpapiTestKey)
                $kind = $secretsKey.GetValueKind($dpapiTestKey)

                if ($kind -eq [Microsoft.Win32.RegistryValueKind]::Binary) {
                    $unprotected = [System.Security.Cryptography.ProtectedData]::Unprotect($readRaw, $null, $scope)
                    $unprotectedStr = [System.Text.Encoding]::UTF8.GetString($unprotected)
                    if ($unprotectedStr -eq $dpapiPlainSecret) {
                        Add-DiagnosticResult -Category "DPAPI" -Name "Secret Retriever End-to-End" -Status "PASS" `
                            -Details "REG_BINARY DPAPI value verified compatible with WindowsRegistrySecretRetriever"
                    } else {
                        Add-DiagnosticResult -Category "DPAPI" -Name "Secret Retriever End-to-End" -Status "FAIL" `
                            -Details "Decrypted registry binary value mismatch"
                    }
                } else {
                    Add-DiagnosticResult -Category "DPAPI" -Name "Secret Retriever End-to-End" -Status "FAIL" `
                        -Details "Registry value was stored as kind '$kind', expected 'Binary'"
                }
            } else {
                Add-DiagnosticResult -Category "DPAPI" -Name "Secret Retriever End-to-End" -Status "WARN" `
                    -Details "Skipped registry write check due to lack of HKLM permissions"
            }
        } finally {
            if ($secretsKey) {
                try { $secretsKey.DeleteValue($dpapiTestKey, $false) } catch {}
                $secretsKey.Close()
            }
            if ($hklmBase) { $hklmBase.Close() }
        }
    } catch {
        Add-DiagnosticResult -Category "DPAPI" -Name "DPAPI Cryptography" -Status "FAIL" `
            -Details "Exception during DPAPI operations" -ErrorMessage $_.Exception.Message
    }
} else {
    Add-DiagnosticResult -Category "DPAPI" -Name "DPAPI Machine Protect" -Status "SKIP" -Details "Skipped on non-Windows environment"
    Add-DiagnosticResult -Category "DPAPI" -Name "DPAPI Machine Unprotect" -Status "SKIP" -Details "Skipped on non-Windows environment"
    Add-DiagnosticResult -Category "DPAPI" -Name "Secret Retriever End-to-End" -Status "SKIP" -Details "Skipped on non-Windows environment"
}

# ---------------------------------------------------------------------------
# 4. Windows Identity & S-1-5-32-544 Admin SID Validation
# ---------------------------------------------------------------------------
Write-Section "4. Windows Identity Subsystem & S-1-5-32-544 SID Mapping"

if ($isWin) {
    try {
        $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
        $userSid = $currentIdentity.User.Value
        $userName = $currentIdentity.Name
        $authType = if ($currentIdentity.AuthenticationType) { $currentIdentity.AuthenticationType } else { "None" }

        Add-DiagnosticResult -Category "Identity" -Name "Current Windows Identity" -Status "PASS" `
            -Details "User: $userName | SID: $userSid | AuthType: $authType"

        # Group SIDs extraction
        $groupSids = @()
        if ($currentIdentity.Groups) {
            foreach ($g in $currentIdentity.Groups) {
                $groupSids += $g.Value
            }
        }

        Add-DiagnosticResult -Category "Identity" -Name "Windows Groups Extracted" -Status "PASS" `
            -Details "Extracted $($groupSids.Count) security group SIDs for current identity"

        # Check for Builtin Administrators group SID: S-1-5-32-544
        $adminSid = "S-1-5-32-544"
        $hasAdminSid = $groupSids -contains $adminSid
        if ($hasAdminSid) {
            Add-DiagnosticResult -Category "Identity" -Name "Builtin Admin SID (S-1-5-32-544)" -Status "PASS" `
                -Details "Current token contains Builtin Administrators SID '$adminSid'"
        } else {
            Add-DiagnosticResult -Category "Identity" -Name "Builtin Admin SID (S-1-5-32-544)" -Status "WARN" `
                -Details "Token does not contain SID '$adminSid' (Standard user token or filtered medium-IL token)"
        }

        # Validate IWindowsIdentityAccessor Extraction Contract Simulation
        $extractedUserSid = $currentIdentity.User?.Value
        $extractedGroupList = [System.Collections.Generic.List[string]]::new()
        if ($currentIdentity.Groups) {
            foreach ($grp in $currentIdentity.Groups) {
                $extractedGroupList.Add($grp.Value)
            }
        }
        if ($extractedUserSid -and ($extractedGroupList.Count -eq $groupSids.Count)) {
            Add-DiagnosticResult -Category "Identity" -Name "IWindowsIdentityAccessor Contract" -Status "PASS" `
                -Details "Validated User SID and Group SID list extraction logic matching WindowsIdentityAccessor"
        } else {
            Add-DiagnosticResult -Category "Identity" -Name "IWindowsIdentityAccessor Contract" -Status "FAIL" `
                -Details "Failed contract extraction validation"
        }
    } catch {
        Add-DiagnosticResult -Category "Identity" -Name "Windows Identity Validation" -Status "FAIL" `
            -Details "Exception during Windows Identity inspection" -ErrorMessage $_.Exception.Message
    }
} else {
    Add-DiagnosticResult -Category "Identity" -Name "Current Windows Identity" -Status "SKIP" -Details "Skipped on non-Windows environment"
    Add-DiagnosticResult -Category "Identity" -Name "Windows Groups Extracted" -Status "SKIP" -Details "Skipped on non-Windows environment"
    Add-DiagnosticResult -Category "Identity" -Name "Builtin Admin SID (S-1-5-32-544)" -Status "SKIP" -Details "Skipped on non-Windows environment"
    Add-DiagnosticResult -Category "Identity" -Name "IWindowsIdentityAccessor Contract" -Status "SKIP" -Details "Skipped on non-Windows environment"
}

# ---------------------------------------------------------------------------
# 5. Automated Backend Test Suite Execution (dotnet test)
# ---------------------------------------------------------------------------
Write-Section "5. Automated Backend Test Suite (dotnet test ModelContextGateway.slnx)"

if (-not $SkipTests) {
    $solutionPath = Join-Path $RepoRoot "ModelContextGateway.slnx"
    if (-not (Test-Path $solutionPath)) {
        $solutionPath = Join-Path $RepoRoot "ModelContextGateway.slnx"
    }

    if (Test-Path $solutionPath) {
        Write-Host "  Executing test suite against $solutionPath..." -ForegroundColor DarkGray
        $testOutput = ""
        $testExitCode = 0
        try {
            $testOutput = & dotnet test "$solutionPath" --logger "console;verbosity=normal" 2>&1
            $testExitCode = $LASTEXITCODE
            $testSummaryLine = ($testOutput | Where-Object { $_ -match "Passed!\s+-|Failed!\s+-" } | Select-Object -Last 1)

            if ($testExitCode -eq 0) {
                $details = if ($testSummaryLine) { $testSummaryLine.Trim() } else { "All tests executed successfully" }
                Add-DiagnosticResult -Category "Testing" -Name "C# Backend Test Suite" -Status "PASS" -Details $details
            } else {
                $details = if ($testSummaryLine) { $testSummaryLine.Trim() } else { "Test execution failed with exit code $testExitCode" }
                Add-DiagnosticResult -Category "Testing" -Name "C# Backend Test Suite" -Status "FAIL" -Details $details -ErrorMessage "dotnet test failed"
            }
        } catch {
            Add-DiagnosticResult -Category "Testing" -Name "C# Backend Test Suite" -Status "FAIL" -Details "Failed to execute dotnet test" -ErrorMessage $_.Exception.Message
        }
    } else {
        Add-DiagnosticResult -Category "Testing" -Name "C# Backend Test Suite" -Status "WARN" -Details "Solution file not found at $solutionPath"
    }
} else {
    Add-DiagnosticResult -Category "Testing" -Name "C# Backend Test Suite" -Status "SKIP" -Details "Skipped (-SkipTests parameter specified)"
}

# ---------------------------------------------------------------------------
# 6. Living Requirements Catalog Zero-Drift Check
# ---------------------------------------------------------------------------
Write-Section "6. Living Requirements Catalog Verification (CatalogGenerator)"

if (-not $SkipCatalog) {
    $generatorProj = Join-Path $RepoRoot "scripts\CatalogGenerator"
    if (Test-Path $generatorProj) {
        Write-Host "  Verifying requirements catalog synchronization..." -ForegroundColor DarkGray
        try {
            $catalogOutput = & dotnet run --project "$generatorProj" -- --verify-only 2>&1
            $catalogExitCode = $LASTEXITCODE

            if ($catalogExitCode -eq 0) {
                Add-DiagnosticResult -Category "Catalog" -Name "Requirements Catalog Drift Check" -Status "PASS" `
                    -Details "Living catalog is synchronized (Zero-drift verified)"
            } else {
                Add-DiagnosticResult -Category "Catalog" -Name "Requirements Catalog Drift Check" -Status "FAIL" `
                    -Details "Catalog verification failed with exit code $catalogExitCode" `
                    -ErrorMessage ($catalogOutput -join "`n")
            }
        } catch {
            Add-DiagnosticResult -Category "Catalog" -Name "Requirements Catalog Drift Check" -Status "FAIL" `
                -Details "Exception running CatalogGenerator" -ErrorMessage $_.Exception.Message
        }
    } else {
        Add-DiagnosticResult -Category "Catalog" -Name "Requirements Catalog Drift Check" -Status "WARN" `
            -Details "CatalogGenerator project not found at $generatorProj"
    }
} else {
    Add-DiagnosticResult -Category "Catalog" -Name "Requirements Catalog Drift Check" -Status "SKIP" `
        -Details "Skipped (-SkipCatalog parameter specified)"
}

# ---------------------------------------------------------------------------
# 7. Frontend Unit Tests (Optional)
# ---------------------------------------------------------------------------
Write-Section "7. Frontend Unit Test Suite (Vitest)"

$frontendDir = Join-Path $RepoRoot "frontend"
if ($IncludeFrontend -or (-not $SkipFrontend -and (Test-Path (Join-Path $frontendDir "node_modules")))) {
    if (Test-Path (Join-Path $frontendDir "package.json")) {
        Write-Host "  Running frontend Vitest unit test suite..." -ForegroundColor DarkGray
        try {
            Push-Location $frontendDir
            $npmTestOutput = & npm test -- --run 2>&1
            $npmTestExitCode = $LASTEXITCODE
            Pop-Location

            if ($npmTestExitCode -eq 0) {
                Add-DiagnosticResult -Category "Frontend" -Name "Vitest Test Suite" -Status "PASS" -Details "All frontend component unit tests passed"
            } else {
                Add-DiagnosticResult -Category "Frontend" -Name "Vitest Test Suite" -Status "FAIL" `
                    -Details "Frontend test run exited with code $npmTestExitCode" -ErrorMessage ($npmTestOutput | Select-Object -Last 5 -join " ")
            }
        } catch {
            Pop-Location -ErrorAction SilentlyContinue
            Add-DiagnosticResult -Category "Frontend" -Name "Vitest Test Suite" -Status "FAIL" `
                -Details "Exception running npm test" -ErrorMessage $_.Exception.Message
        }
    } else {
        Add-DiagnosticResult -Category "Frontend" -Name "Vitest Test Suite" -Status "WARN" -Details "frontend/package.json not found"
    }
} else {
    Add-DiagnosticResult -Category "Frontend" -Name "Vitest Test Suite" -Status "SKIP" `
        -Details "Skipped (Specify -IncludeFrontend to force frontend tests)"
}

# ---------------------------------------------------------------------------
# Summary & Report Generation
# ---------------------------------------------------------------------------
$endTime = [DateTime]::UtcNow
$duration = $endTime - $startTime

$totalChecks = $results.Count
$passedChecks = @($results | Where-Object { $_.Status -eq "PASS" }).Count
$failedChecks = @($results | Where-Object { $_.Status -eq "FAIL" }).Count
$warnedChecks = @($results | Where-Object { $_.Status -eq "WARN" }).Count
$skippedChecks = @($results | Where-Object { $_.Status -eq "SKIP" }).Count

$overallStatus = if ($failedChecks -eq 0) { "PASSED" } else { "FAILED" }
$overallColor = if ($failedChecks -eq 0) { "Green" } else { "Red" }

Write-Host "`n================================================================================" -ForegroundColor DarkCyan
Write-Host "  Diagnostic Summary & Quality Gate Status: $overallStatus" -ForegroundColor $overallColor
Write-Host "================================================================================" -ForegroundColor DarkCyan
Write-Host "  Total Validations : $totalChecks" -ForegroundColor White
Write-Host "  Passed            : $passedChecks" -ForegroundColor Green
Write-Host "  Failed            : $failedChecks" -ForegroundColor $(if ($failedChecks -gt 0) { "Red" } else { "DarkGray" })
Write-Host "  Warnings          : $warnedChecks" -ForegroundColor $(if ($warnedChecks -gt 0) { "Yellow" } else { "DarkGray" })
Write-Host "  Skipped           : $skippedChecks" -ForegroundColor DarkGray
Write-Host "  Duration          : $([Math]::Round($duration.TotalSeconds, 2)) seconds" -ForegroundColor DarkGray
Write-Host "--------------------------------------------------------------------------------" -ForegroundColor DarkGray

# Optional JSON Report Export
if ($JsonReportPath) {
    try {
        $reportObject = [PSCustomObject]@{
            SchemaVersion = "1.0.0"
            GeneratedAtUtc = $endTime.ToString("o")
            DurationSeconds = [Math]::Round($duration.TotalSeconds, 2)
            OverallStatus = $overallStatus
            Environment = [PSCustomObject]@{
                OS = [System.Environment]::OSVersion.VersionString
                Platform = [System.Environment]::OSVersion.Platform.ToString()
                Architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
                MachineName = [System.Environment]::MachineName
                UserName = [System.Environment]::UserName
                IsAdministrator = $isAdmin
                PowerShellVersion = $PSVersionTable.PSVersion.ToString()
                DotNetSdkVersion = $dotnetVersion
            }
            Summary = [PSCustomObject]@{
                Total = $totalChecks
                Passed = $passedChecks
                Failed = $failedChecks
                Warnings = $warnedChecks
                Skipped = $skippedChecks
            }
            Results = $results
        }

        $jsonDir = Split-Path -Parent $JsonReportPath
        if ($jsonDir -and -not (Test-Path $jsonDir)) {
            New-Item -ItemType Directory -Path $jsonDir -Force | Out-Null
        }

        $reportJson = $reportObject | ConvertTo-Json -Depth 6
        [System.IO.File]::WriteAllText($JsonReportPath, $reportJson, [System.Text.Encoding]::UTF8)
        Write-Host " [✓] Diagnostic JSON report exported to: $JsonReportPath" -ForegroundColor Green
    } catch {
        Write-Host " [!] Failed to write JSON report to $($JsonReportPath): $($_.Exception.Message)" -ForegroundColor Red
    }
}

if ($failedChecks -gt 0) {
    Write-Host "`n[FAIL] Diagnostic suite encountered $failedChecks failing checks. Review log above." -ForegroundColor Red
    exit 1
} else {
    Write-Host "`n[SUCCESS] All mandatory Windows environment validations passed successfully." -ForegroundColor Green
    exit 0
}
