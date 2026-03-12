# Simple login endpoint test
Write-Host "`n========== LOGIN ENDPOINT TEST ==========" -ForegroundColor Cyan
Write-Host "Testing endpoint: http://localhost:5031/api/v1/auth/login" -ForegroundColor Gray
Write-Host "==========================================`n" -ForegroundColor Cyan

$loginUrl = "http://localhost:5031/api/v1/auth/login"

# Test 1: Admin login
Write-Host "Test 1: Admin Login" -ForegroundColor Green
Write-Host "  Email: admin@example.com" -ForegroundColor Gray
Write-Host "  Password: password123" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri $loginUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body '{"email":"admin@example.com","password":"password123"}' `
        -ErrorAction Stop

    Write-Host "  Result: " -NoNewline -ForegroundColor Yellow
    Write-Host "SUCCESS (HTTP 200)" -ForegroundColor Green

    $data = $response.Content | ConvertFrom-Json
    Write-Host "  ├─ Token: " -NoNewline -ForegroundColor Cyan
    Write-Host $data.token.Substring(0, 40) + "..." -ForegroundColor White
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
    Write-Host "  Result: " -NoNewline -ForegroundColor Yellow
    Write-Host "FAILED" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Regular user login
Write-Host "`nTest 2: Regular User Login" -ForegroundColor Green
Write-Host "  Email: user@example.com" -ForegroundColor Gray
Write-Host "  Password: password123" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri $loginUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body '{"email":"user@example.com","password":"password123"}' `
        -ErrorAction Stop

    Write-Host "  Result: " -NoNewline -ForegroundColor Yellow
    Write-Host "SUCCESS (HTTP 200)" -ForegroundColor Green

    $data = $response.Content | ConvertFrom-Json
    Write-Host "  ├─ Token: " -NoNewline -ForegroundColor Cyan
    Write-Host $data.token.Substring(0, 40) + "..." -ForegroundColor White
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
    Write-Host "  Result: " -NoNewline -ForegroundColor Yellow
    Write-Host "FAILED" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Wrong password
Write-Host "`nTest 3: Wrong Password (Expected to Fail)" -ForegroundColor Yellow
Write-Host "  Email: admin@example.com" -ForegroundColor Gray
Write-Host "  Password: wrongpassword" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri $loginUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body '{"email":"admin@example.com","password":"wrongpassword"}' `
        -ErrorAction Stop

    Write-Host "  Result: " -NoNewline -ForegroundColor Yellow
    Write-Host "UNEXPECTED SUCCESS" -ForegroundColor Red
}
catch {
    Write-Host "  Result: " -NoNewline -ForegroundColor Yellow
    Write-Host "CORRECTLY REJECTED (HTTP 401)" -ForegroundColor Green
    Write-Host "  Status Code: " -NoNewline -ForegroundColor Gray
    Write-Host $_.Exception.Response.StatusCode -ForegroundColor White
}

# Test 4: Non-existent user
Write-Host "`nTest 4: Non-existent User (Expected to Fail)" -ForegroundColor Yellow
Write-Host "  Email: nonexistent@example.com" -ForegroundColor Gray
Write-Host "  Password: password123" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri $loginUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body '{"email":"nonexistent@example.com","password":"password123"}' `
        -ErrorAction Stop

    Write-Host "  Result: " -NoNewline -ForegroundColor Yellow
    Write-Host "UNEXPECTED SUCCESS" -ForegroundColor Red
}
catch {
    Write-Host "  Result: " -NoNewline -ForegroundColor Yellow
    Write-Host "CORRECTLY REJECTED (HTTP 404)" -ForegroundColor Green
    Write-Host "  Status Code: " -NoNewline -ForegroundColor Gray
    Write-Host $_.Exception.Response.StatusCode -ForegroundColor White
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "API: http://localhost:5031" -ForegroundColor Cyan
Write-Host "Swagger UI: http://localhost:5031/swagger" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
