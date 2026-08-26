<#
.SYNOPSIS
    Manages DPAPI-encrypted and plaintext secrets in the Windows Registry for Model Context Gateway.

.DESCRIPTION
    Writes, reads, decrypts, lists, and removes secrets in HKLM\SOFTWARE\McpRouter\Secrets.
    Encrypted values are protected with the Windows Data Protection API (DPAPI) under
    DataProtectionScope.LocalMachine, matching the retrieval logic of WindowsRegistrySecretRetriever.cs.

.PARAMETER SecretName
    The name of the secret value in the registry.

.PARAMETER SecretValue
    The plaintext secret value to write.

.PARAMETER Encrypt
    If specified, encrypts the SecretValue using DPAPI (DataProtectionScope.LocalMachine) and stores as REG_BINARY.
    If omitted, stores the SecretValue as a standard plaintext string (REG_SZ).

.PARAMETER SubKeyPath
    The registry subkey under HKLM. Default: "SOFTWARE\McpRouter\Secrets".

.PARAMETER Hive
    The registry hive to target: "LocalMachine" (default) or "CurrentUser".

.PARAMETER Scope
    DPAPI protection scope: "LocalMachine" (default) or "CurrentUser".

.PARAMETER Get
    Retrieves and displays the specified secret (auto-decrypting if DPAPI binary).

.PARAMETER List
    Lists all secrets present under the specified SubKeyPath.

.PARAMETER Delete
    Deletes the specified secret from the registry.

.EXAMPLE
    # Store a DPAPI machine-encrypted secret:
    .\Set-RegistrySecrets.ps1 -SecretName "DockerApiKey" -SecretValue "dckr_pat_12345abcdef" -Encrypt

.EXAMPLE
    # Store a plaintext secret:
    .\Set-RegistrySecrets.ps1 -SecretName "PlexToken" -SecretValue "plex_token_xyz"

.EXAMPLE
    # Read and decrypt a secret:
    .\Set-RegistrySecrets.ps1 -SecretName "DockerApiKey" -Get

.EXAMPLE
    # List all configured registry secrets:
    .\Set-RegistrySecrets.ps1 -List

.EXAMPLE
    # Delete a secret:
    .\Set-RegistrySecrets.ps1 -SecretName "DockerApiKey" -Delete
#>

[CmdletBinding(DefaultParameterSetName = "Set")]
param(
    [Parameter(ParameterSetName = "Set", Mandatory = $true, Position = 0)]
    [Parameter(ParameterSetName = "Get", Mandatory = $true, Position = 0)]
    [Parameter(ParameterSetName = "Delete", Mandatory = $true, Position = 0)]
    [string]$SecretName,

    [Parameter(ParameterSetName = "Set", Mandatory = $true, Position = 1, ValueFromPipeline = $true)]
    [string]$SecretValue,

    [Parameter(ParameterSetName = "Set")]
    [switch]$Encrypt,

    [Parameter()]
    [string]$SubKeyPath = "SOFTWARE\McpRouter\Secrets",

    [Parameter()]
    [ValidateSet("LocalMachine", "CurrentUser")]
    [string]$Hive = "LocalMachine",

    [Parameter()]
    [ValidateSet("LocalMachine", "CurrentUser")]
    [string]$Scope = "LocalMachine",

    [Parameter(ParameterSetName = "Get", Mandatory = $true)]
    [switch]$Get,

    [Parameter(ParameterSetName = "List", Mandatory = $true)]
    [switch]$List,

    [Parameter(ParameterSetName = "Delete", Mandatory = $true)]
    [switch]$Delete
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Dependency & OS Verification
# ---------------------------------------------------------------------------
if ([System.Environment]::OSVersion.Platform -ne [System.PlatformID]::Win32NT) {
    Write-Error "This script requires a Windows host environment with DPAPI and Windows Registry support."
    exit 1
}

# Ensure System.Security is loaded for DPAPI ProtectedData
try {
    Add-Type -AssemblyName System.Security -ErrorAction SilentlyContinue
} catch {
    # System.Security is built-in on .NET 5+ / .NET Core / Windows PowerShell
}

function Assert-Administrator {
    if ($Hive -eq "LocalMachine") {
        $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
        $principal = [Security.Principal.WindowsPrincipal]$currentIdentity
        if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
            Write-Error "Writing or modifying HKLM registry entries requires Administrator privileges. Please launch PowerShell as Administrator."
            exit 1
        }
    }
}

function Get-RegistryBaseKey {
    param([string]$TargetHive, [bool]$Writable = $false)
    $hiveEnum = if ($TargetHive -eq "CurrentUser") {
        [Microsoft.Win32.RegistryHive]::CurrentUser
    } else {
        [Microsoft.Win32.RegistryHive]::LocalMachine
    }
    return [Microsoft.Win32.RegistryKey]::OpenBaseKey($hiveEnum, [Microsoft.Win32.RegistryView]::Registry64)
}

# ---------------------------------------------------------------------------
# Action Execution
# ---------------------------------------------------------------------------
switch ($PSCmdlet.ParameterSetName) {
    "Set" {
        Assert-Administrator
        $baseKey = Get-RegistryBaseKey -TargetHive $Hive -Writable $true
        try {
            $subKey = $baseKey.CreateSubKey($SubKeyPath, $true)
            if (-not $subKey) {
                Write-Error "Failed to open or create registry subkey: HKLM:\$SubKeyPath"
                exit 1
            }

            try {
                if ($Encrypt) {
                    $scopeEnum = [System.Security.Cryptography.DataProtectionScope]::$Scope
                    $plainBytes = [System.Text.Encoding]::UTF8.GetBytes($SecretValue)
                    $encryptedBytes = [System.Security.Cryptography.ProtectedData]::Protect($plainBytes, $null, $scopeEnum)

                    $subKey.SetValue($SecretName, $encryptedBytes, [Microsoft.Win32.RegistryValueKind]::Binary)
                    Write-Host "[✓] Successfully saved DPAPI machine-encrypted secret '$SecretName' into HKLM:\$SubKeyPath" -ForegroundColor Green
                } else {
                    $subKey.SetValue($SecretName, $SecretValue, [Microsoft.Win32.RegistryValueKind]::String)
                    Write-Host "[✓] Successfully saved plaintext secret '$SecretName' into HKLM:\$SubKeyPath" -ForegroundColor Green
                }
            } finally {
                $subKey.Close()
            }
        } finally {
            $baseKey.Close()
        }
    }

    "Get" {
        $baseKey = Get-RegistryBaseKey -TargetHive $Hive -Writable $false
        try {
            $subKey = $baseKey.OpenSubKey($SubKeyPath, $false)
            if (-not $subKey) {
                Write-Error "Registry subkey not found: HKLM:\$SubKeyPath"
                exit 1
            }

            try {
                $rawVal = $subKey.GetValue($SecretName)
                if ($null -eq $rawVal) {
                    Write-Error "Secret value '$SecretName' not found under HKLM:\$SubKeyPath."
                    exit 1
                }

                $valKind = $subKey.GetValueKind($SecretName)
                if ($valKind -eq [Microsoft.Win32.RegistryValueKind]::Binary) {
                    $scopeEnum = [System.Security.Cryptography.DataProtectionScope]::$Scope
                    $decryptedBytes = [System.Security.Cryptography.ProtectedData]::Unprotect($rawVal, $null, $scopeEnum)
                    $decryptedStr = [System.Text.Encoding]::UTF8.GetString($decryptedBytes)

                    Write-Host "Secret Name : $SecretName" -ForegroundColor Cyan
                    Write-Host "Value Type  : Binary (DPAPI Encrypted)" -ForegroundColor DarkGray
                    Write-Host "Plaintext   : $decryptedStr" -ForegroundColor Green
                } else {
                    Write-Host "Secret Name : $SecretName" -ForegroundColor Cyan
                    Write-Host "Value Type  : String (Plaintext)" -ForegroundColor DarkGray
                    Write-Host "Plaintext   : $rawVal" -ForegroundColor Green
                }
            } finally {
                $subKey.Close()
            }
        } finally {
            $baseKey.Close()
        }
    }

    "List" {
        $baseKey = Get-RegistryBaseKey -TargetHive $Hive -Writable $false
        try {
            $subKey = $baseKey.OpenSubKey($SubKeyPath, $false)
            if (-not $subKey) {
                Write-Host "[!] Registry subkey does not exist yet: HKLM:\$SubKeyPath" -ForegroundColor Yellow
                return
            }

            try {
                $valueNames = $subKey.GetValueNames()
                Write-Host "`nSecrets configured in HKLM:\$SubKeyPath ($($valueNames.Length) items):" -ForegroundColor Cyan
                Write-Host ("-" * 60) -ForegroundColor DarkGray

                $results = @()
                foreach ($name in $valueNames) {
                    $kind = $subKey.GetValueKind($name)
                    $typeStr = switch ($kind) {
                        ([Microsoft.Win32.RegistryValueKind]::Binary) { "Binary (DPAPI Encrypted)" }
                        ([Microsoft.Win32.RegistryValueKind]::String) { "String (Plaintext)" }
                        Default { $kind.ToString() }
                    }
                    $results += [PSCustomObject]@{
                        SecretName = $name
                        Type       = $typeStr
                    }
                }

                $results | Format-Table -AutoSize
            } finally {
                $subKey.Close()
            }
        } finally {
            $baseKey.Close()
        }
    }

    "Delete" {
        Assert-Administrator
        $baseKey = Get-RegistryBaseKey -TargetHive $Hive -Writable $true
        try {
            $subKey = $baseKey.OpenSubKey($SubKeyPath, $true)
            if (-not $subKey) {
                Write-Error "Registry subkey not found: HKLM:\$SubKeyPath"
                exit 1
            }

            try {
                $rawVal = $subKey.GetValue($SecretName)
                if ($null -eq $rawVal) {
                    Write-Warn "Secret '$SecretName' does not exist in HKLM:\$SubKeyPath."
                } else {
                    $subKey.DeleteValue($SecretName)
                    Write-Host "[✓] Successfully deleted secret '$SecretName' from HKLM:\$SubKeyPath" -ForegroundColor Green
                }
            } finally {
                $subKey.Close()
            }
        } finally {
            $baseKey.Close()
        }
    }
}
