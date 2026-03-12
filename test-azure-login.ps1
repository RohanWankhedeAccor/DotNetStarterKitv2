# PowerShell script to test Azure AD login endpoint
# Usage: .\test-azure-login.ps1

Write-Host "`n========== AZURE AD LOGIN ENDPOINT TEST ==========" -ForegroundColor Cyan
Write-Host "Testing endpoint: http://localhost:5031/api/v1/auth/azure-login" -ForegroundColor Gray
Write-Host "============================================`n" -ForegroundColor Cyan

$loginUrl = "http://localhost:5031/api/v1/auth/azure-login"

# Note: To test this endpoint, you need a valid Azure AD token
# Get token using: az account get-access-token --resource api://ae60f82b-2dc4-4212-884a-3a50d79bb768/access_as_user

Write-Host "To test Azure AD login, you need a valid Azure AD token." -ForegroundColor Yellow
Write-Host "`nStep 1: Get Azure AD token using Azure CLI:`n" -ForegroundColor Yellow
Write-Host "  az login" -ForegroundColor White
Write-Host "  az account get-access-token --resource api://ae60f82b-2dc4-4212-884a-3a50d79bb768/access_as_user" -ForegroundColor White
Write-Host "`nStep 2: Copy the accessToken value from the output`n" -ForegroundColor Yellow

$token = Read-Host "Paste your Azure AD access token here"

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host "Error: No token provided" -ForegroundColor Red
    exit 1
}

Write-Host "`nTesting Azure login..." -ForegroundColor Cyan

try {
    $response = Invoke-WebRequest -Uri $loginUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body @{ azureAdToken = $token } | ConvertTo-Json `
        -ErrorAction Stop

    Write-Host "✅ SUCCESS (HTTP 200)`n" -ForegroundColor Green

    $data = $response.Content | ConvertFrom-Json
    Write-Host "Response Details:" -ForegroundColor Cyan
    Write-Host "  ├─ Token: " -NoNewline -ForegroundColor Cyan
    Write-Host ($data.token.Substring(0, 50) + "...") -ForegroundColor White
    Write-Host "  ├─ User ID: " -NoNewline -ForegroundColor Cyan
    Write-Host $data.userId -ForegroundColor White
    Write-Host "  ├─ Email: " -NoNewline -ForegroundColor Cyan
    Write-Host $data.email -ForegroundColor White
    Write-Host "  ├─ Full Name: " -NoNewline -ForegroundColor Cyan
    Write-Host $data.fullName -ForegroundColor White
    Write-Host "  ├─ Roles: " -NoNewline -ForegroundColor Cyan
    Write-Host ($data.roles -join ", ") -ForegroundColor White
    Write-Host "  └─ Expires In: " -NoNewline -ForegroundColor Cyan
    Write-Host "$($data.expiresIn) seconds" -ForegroundColor White
}
catch {
    $errorResponse = $_.Exception.Response
    $statusCode = $errorResponse.StatusCode

    Write-Host "❌ FAILED (HTTP $statusCode)`n" -ForegroundColor Red
    Write-Host "Error Details:" -ForegroundColor Cyan
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Red

    try {
        $content = $_.Exception.Response.Content.ReadAsStreamAsync().Result
        $reader = New-Object System.IO.StreamReader($content)
        $errorBody = $reader.ReadToEnd() | ConvertFrom-Json
        Write-Host "  Detail: " -NoNewline -ForegroundColor Yellow
        Write-Host $errorBody.detail -ForegroundColor White
        Write-Host "  Trace ID: " -NoNewline -ForegroundColor Yellow
        Write-Host $errorBody.traceId -ForegroundColor White
    }
    catch {
        # Could not parse error response
    }
}

Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "API: http://localhost:5031" -ForegroundColor Cyan
Write-Host "Swagger UI: http://localhost:5031/swagger" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "For more info, see: TESTING_AZURE_AD.md`n" -ForegroundColor Gray
