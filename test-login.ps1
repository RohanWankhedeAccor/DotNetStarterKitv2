# Kill all dotnet processes using > 50MB
Write-Host "Killing existing dotnet processes..." -ForegroundColor Cyan
Get-Process dotnet -ErrorAction SilentlyContinue | Where-Object {$_.WorkingSet64 -gt 50000000} | ForEach-Object {
    Write-Host "  - PID $($_.Id) ($([math]::Round($_.WorkingSet64/1MB)) MB)"
    $_.Kill()
}

Write-Host "Waiting 4 seconds for port 5031 to be released..." -ForegroundColor Cyan
Start-Sleep -Seconds 4

# Start the API
Write-Host "`nStarting API server..." -ForegroundColor Cyan
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Api --no-build" -WorkingDirectory "D:\RWANKHEDE\Claude\DotNetStarterKitv2" -NoNewWindow -PassThru

Write-Host "API process started (PID: $($apiProcess.Id))" -ForegroundColor Green
Write-Host "Waiting 6 seconds for API to fully initialize..." -ForegroundColor Cyan
Start-Sleep -Seconds 6

# Test the login endpoint
Write-Host "`n=== Testing Login Endpoint with Admin Account ===" -ForegroundColor Green

$loginUrl = "http://localhost:5031/api/v1/auth/login"
$credentials = @{
    email = "admin@example.com"
    password = "password123"
} | ConvertTo-Json

Write-Host "POST $loginUrl" -ForegroundColor Yellow
Write-Host "Response:" -ForegroundColor Yellow

try {
    # Skip certificate validation for localhost
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

    $response = Invoke-WebRequest -Uri $loginUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body $credentials `
        -ErrorAction Stop

    $jsonResponse = $response.Content | ConvertFrom-Json

    Write-Host "[OK] Login successful!" -ForegroundColor Green
    Write-Host "  Token: $($jsonResponse.token.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host "  User ID: $($jsonResponse.userId)" -ForegroundColor Green
    Write-Host "  Email: $($jsonResponse.email)" -ForegroundColor Green
    Write-Host "  Full Name: $($jsonResponse.fullName)" -ForegroundColor Green
    Write-Host "  Roles: $($jsonResponse.roles -join ', ')" -ForegroundColor Green
    Write-Host "  Expires In: $($jsonResponse.expiresIn) seconds" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] Login failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Testing Login Endpoint with Regular User ===" -ForegroundColor Green

$credentials2 = @{
    email = "user@example.com"
    password = "password123"
} | ConvertTo-Json

try {
    $response2 = Invoke-WebRequest -Uri $loginUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body $credentials2 `
        -ErrorAction Stop

    $jsonResponse2 = $response2.Content | ConvertFrom-Json

    Write-Host "[OK] Login successful!" -ForegroundColor Green
    Write-Host "  Token: $($jsonResponse2.token.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host "  User ID: $($jsonResponse2.userId)" -ForegroundColor Green
    Write-Host "  Email: $($jsonResponse2.email)" -ForegroundColor Green
    Write-Host "  Full Name: $($jsonResponse2.fullName)" -ForegroundColor Green
    Write-Host "  Roles: $($jsonResponse2.roles -join ', ')" -ForegroundColor Green
}
catch {
    Write-Host "[ERROR] Login failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Testing Invalid Credentials ===" -ForegroundColor Green

$invalidCreds = @{
    email = "admin@example.com"
    password = "wrongpassword"
} | ConvertTo-Json

Write-Host "Testing with wrong password..." -ForegroundColor Yellow

try {
    $response3 = Invoke-WebRequest -Uri $loginUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body $invalidCreds `
        -ErrorAction Stop

    Write-Host "[ERROR] Should have failed!" -ForegroundColor Red
}
catch {
    Write-Host "[OK] Correctly rejected invalid password" -ForegroundColor Green
    Write-Host "  Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Yellow
}

Write-Host "`nTest complete!" -ForegroundColor Green
Write-Host "API is running on: http://localhost:5031" -ForegroundColor Cyan
Write-Host "Swagger UI: http://localhost:5031/swagger" -ForegroundColor Cyan

Write-Host "`nPress Enter to stop the API server..."
Read-Host

Write-Host "`nStopping API server..." -ForegroundColor Yellow
$apiProcess | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "Done!" -ForegroundColor Green
