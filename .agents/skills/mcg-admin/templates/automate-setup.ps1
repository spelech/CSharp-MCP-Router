<#
.SYNOPSIS
    Model Context Gateway (MCG) Automated Provisioning Script for Windows & IIS Environments.
.EXAMPLE
    .\automate-setup.ps1 -RouterUrl "http://localhost:8080" -AdminKey "mcp-adm-Xk9L2mPq-7vN3wZ8aB1cE4fG9"
#>
param(
    [string]$RouterUrl = "http://localhost:8080",
    [string]$AdminKey = "mcp-adm-Xk9L2mPq-7vN3wZ8aB1cE4fG9"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host " Starting Model Context Gateway Windows / IIS Automated Provisioning" -ForegroundColor Cyan
Write-Host " Target: $RouterUrl" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

function Invoke-AdminTool {
    param(
        [string]$ToolName,
        [hashtable]$Arguments
    )

    $payload = @{
        jsonrpc = "2.0"
        id = [Guid]::NewGuid().ToString()
        method = "tools/call"
        params = @{
            name = $ToolName
            arguments = $Arguments
        }
    } | ConvertTo-Json -Depth 10

    $headers = @{
        "Authorization" = "Bearer $AdminKey"
        "Content-Type"  = "application/json"
    }

    $response = Invoke-RestMethod -Uri "$RouterUrl/admin" -Method Post -Headers $headers -Body $payload
    return $response
}

# 1. Probe Diagnostics
Write-Host "[1/4] Probing gateway health & diagnostics..." -ForegroundColor Yellow
$diag = Invoke-AdminTool -ToolName "manage_system" -Arguments @{ action = "diagnostics" }
Write-Host "Diagnostics OK: $($diag | ConvertTo-Json -Compress)" -ForegroundColor Green

# 2. Configure Active Directory LDAPS Provider
Write-Host "[2/4] Configuring Active Directory LDAPS provider..." -ForegroundColor Yellow
$adConfig = @{
    server = "dc01.internal.corp"
    port = 636
    useSsl = $true
    domain = "INTERNAL"
    baseDn = "DC=internal,DC=corp"
    bindDn = "CN=svc-mcg,OU=ServiceAccounts,DC=internal,DC=corp"
    bindPassword = "StrongPassword123!"
} | ConvertTo-Json -Compress

$adParams = @{
    action = "save_auth"
    providerName = "ActiveDirectory"
    displayName = "Corporate Active Directory"
    isEnabled = $true
    configJson = $adConfig
}
$adResp = Invoke-AdminTool -ToolName "manage_providers" -Arguments $adParams
Write-Host "Active Directory Configured: $($adResp | ConvertTo-Json -Compress)" -ForegroundColor Green

# 3. Create Group Mapping for Domain Admins (S-1-5-32-544)
Write-Host "[3/4] Creating Domain Admin SID mapping..." -ForegroundColor Yellow
$mappingParams = @{
    action = "save"
    externalId = "S-1-5-32-544"
    internalGroup = "full_admin"
}
$mapResp = Invoke-AdminTool -ToolName "manage_group_mappings" -Arguments $mappingParams
Write-Host "Group Mapping Created: $($mapResp | ConvertTo-Json -Compress)" -ForegroundColor Green

# 4. Create IIS Developer AppKey
Write-Host "[4/4] Issuing Developer AppKey..." -ForegroundColor Yellow
$keyParams = @{
    action = "create"
    name = "Windows IIS Developer Key"
    username = "win-dev"
    scopes = @("all")
    expiresInDays = 90
}
$keyResp = Invoke-AdminTool -ToolName "manage_appkeys" -Arguments $keyParams
Write-Host "Developer AppKey Issued: $($keyResp | ConvertTo-Json -Compress)" -ForegroundColor Green

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host " Windows / IIS Automated Provisioning Completed!" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
