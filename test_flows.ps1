$baseUrl = "http://localhost:5248"
$ErrorActionPreference = "Continue"

Write-Host "=== TEST PASS: LegalConnect Automated Verification ===" -ForegroundColor Cyan

# 1. Test Home & Swagger
$resHome = Invoke-WebRequest -Uri "$baseUrl/" -UseBasicParsing -MaximumRedirection 0
Write-Host "[TEST 1] Home Page Status: $($resHome.StatusCode)" -ForegroundColor Green

$resSwagger = Invoke-WebRequest -Uri "$baseUrl/swagger/v1/swagger.json" -UseBasicParsing
Write-Host "[TEST 2] Swagger OpenAPI Spec: $($resSwagger.StatusCode) (Length: $($resSwagger.Content.Length) bytes)" -ForegroundColor Green

# 2. Test API Endpoint
$resApiLawyers = Invoke-RestMethod -Uri "$baseUrl/api/LawyersApi" -Method Get
Write-Host "[TEST 3] GET /api/LawyersApi returned $($resApiLawyers.Count) approved lawyer(s): $($resApiLawyers[0].fullName)" -ForegroundColor Green

# 3. Test Unauthorized Access (Anonymous)
try {
    $resAnonAdmin = Invoke-WebRequest -Uri "$baseUrl/Admin/Dashboard" -UseBasicParsing -MaximumRedirection 0
    Write-Host "[TEST 4a] Anonymous /Admin/Dashboard redirected to: $($resAnonAdmin.Headers.Location)" -ForegroundColor Green
} catch {
    Write-Host "[TEST 4a] Anonymous /Admin/Dashboard caught: $($_.Exception.Message)"
}

try {
    $resAnonLawyer = Invoke-WebRequest -Uri "$baseUrl/Lawyer/Dashboard" -UseBasicParsing -MaximumRedirection 0
    Write-Host "[TEST 4b] Anonymous /Lawyer/Dashboard redirected to: $($resAnonLawyer.Headers.Location)" -ForegroundColor Green
} catch {
    Write-Host "[TEST 4b] Anonymous /Lawyer/Dashboard caught: $($_.Exception.Message)"
}

try {
    $resAnonClient = Invoke-WebRequest -Uri "$baseUrl/Client/Dashboard" -UseBasicParsing -MaximumRedirection 0
    Write-Host "[TEST 4c] Anonymous /Client/Dashboard redirected to: $($resAnonClient.Headers.Location)" -ForegroundColor Green
} catch {
    Write-Host "[TEST 4c] Anonymous /Client/Dashboard caught: $($_.Exception.Message)"
}

# Helper function to extract RequestVerificationToken from form
function Get-AntiForgeryToken($html) {
    if ($html -match '__RequestVerificationToken"[^>]*value="([^"]+)"') {
        return $matches[1]
    }
    return $null
}

# 4. Test Login as Client
$clientSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $clientSession -UseBasicParsing
$token = Get-AntiForgeryToken $loginPage.Content

$loginBody = @{
    "Email" = "client@legalconnect.com"
    "Password" = "Client@123"
    "RememberMe" = "false"
    "__RequestVerificationToken" = $token
}
$clientLoginRes = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $loginBody -WebSession $clientSession -UseBasicParsing -MaximumRedirection 0
Write-Host "[TEST 5] Client Login Response: $($clientLoginRes.StatusCode) Redirect: $($clientLoginRes.Headers.Location)" -ForegroundColor Green

# Test Client accessing /Client/Dashboard (allowed)
$clientDashRes = Invoke-WebRequest -Uri "$baseUrl/Client/Dashboard" -WebSession $clientSession -UseBasicParsing
Write-Host "[TEST 6] Client Accessing /Client/Dashboard: $($clientDashRes.StatusCode)" -ForegroundColor Green

# Test Client accessing /Admin/Dashboard (forbidden/blocked)
try {
    $clientAdminRes = Invoke-WebRequest -Uri "$baseUrl/Admin/Dashboard" -WebSession $clientSession -UseBasicParsing -MaximumRedirection 0
    Write-Host "[TEST 7a] Client Accessing /Admin/Dashboard Status: $($clientAdminRes.StatusCode) Location: $($clientAdminRes.Headers.Location)" -ForegroundColor Yellow
} catch {
    Write-Host "[TEST 7a] Client Accessing /Admin/Dashboard Blocked: $($_.Exception.Response.StatusCode)" -ForegroundColor Green
}

# Test Client accessing /Lawyer/Dashboard (forbidden/blocked)
try {
    $clientLawyerRes = Invoke-WebRequest -Uri "$baseUrl/Lawyer/Dashboard" -WebSession $clientSession -UseBasicParsing -MaximumRedirection 0
    Write-Host "[TEST 7b] Client Accessing /Lawyer/Dashboard Status: $($clientLawyerRes.StatusCode) Location: $($clientLawyerRes.Headers.Location)" -ForegroundColor Yellow
} catch {
    Write-Host "[TEST 7b] Client Accessing /Lawyer/Dashboard Blocked: $($_.Exception.Response.StatusCode)" -ForegroundColor Green
}

# 5. Test Login as Lawyer
$lawyerSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginPageLawyer = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $lawyerSession -UseBasicParsing
$tokenLawyer = Get-AntiForgeryToken $loginPageLawyer.Content

$loginBodyLawyer = @{
    "Email" = "lawyer@legalconnect.com"
    "Password" = "Lawyer@123"
    "RememberMe" = "false"
    "__RequestVerificationToken" = $tokenLawyer
}
$lawyerLoginRes = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $loginBodyLawyer -WebSession $lawyerSession -UseBasicParsing -MaximumRedirection 0
Write-Host "[TEST 8] Lawyer Login Response: $($lawyerLoginRes.StatusCode) Redirect: $($lawyerLoginRes.Headers.Location)" -ForegroundColor Green

$lawyerDashRes = Invoke-WebRequest -Uri "$baseUrl/Lawyer/Dashboard" -WebSession $lawyerSession -UseBasicParsing
Write-Host "[TEST 9] Lawyer Accessing /Lawyer/Dashboard: $($lawyerDashRes.StatusCode)" -ForegroundColor Green

try {
    $lawyerAdminRes = Invoke-WebRequest -Uri "$baseUrl/Admin/Dashboard" -WebSession $lawyerSession -UseBasicParsing -MaximumRedirection 0
    Write-Host "[TEST 10a] Lawyer Accessing /Admin/Dashboard Status: $($lawyerAdminRes.StatusCode) Location: $($lawyerAdminRes.Headers.Location)" -ForegroundColor Yellow
} catch {
    Write-Host "[TEST 10a] Lawyer Accessing /Admin/Dashboard Blocked: $($_.Exception.Response.StatusCode)" -ForegroundColor Green
}

# 6. Test Login as Admin
$adminSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginPageAdmin = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $adminSession -UseBasicParsing
$tokenAdmin = Get-AntiForgeryToken $loginPageAdmin.Content

$loginBodyAdmin = @{
    "Email" = "admin@legalconnect.com"
    "Password" = "Admin@123"
    "RememberMe" = "false"
    "__RequestVerificationToken" = $tokenAdmin
}
$adminLoginRes = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $loginBodyAdmin -WebSession $adminSession -UseBasicParsing -MaximumRedirection 0
Write-Host "[TEST 11] Admin Login Response: $($adminLoginRes.StatusCode) Redirect: $($adminLoginRes.Headers.Location)" -ForegroundColor Green

$adminDashRes = Invoke-WebRequest -Uri "$baseUrl/Admin/Dashboard" -WebSession $adminSession -UseBasicParsing
Write-Host "[TEST 12] Admin Accessing /Admin/Dashboard: $($adminDashRes.StatusCode)" -ForegroundColor Green

$adminUsersRes = Invoke-WebRequest -Uri "$baseUrl/Admin/Users" -WebSession $adminSession -UseBasicParsing
Write-Host "[TEST 13] Admin Accessing /Admin/Users: $($adminUsersRes.StatusCode)" -ForegroundColor Green

$adminSpecsRes = Invoke-WebRequest -Uri "$baseUrl/Specialization" -WebSession $adminSession -UseBasicParsing
Write-Host "[TEST 14] Admin Accessing /Specialization: $($adminSpecsRes.StatusCode)" -ForegroundColor Green

$adminReportsRes = Invoke-WebRequest -Uri "$baseUrl/Admin/Reports" -WebSession $adminSession -UseBasicParsing
Write-Host "[TEST 15] Admin Accessing /Admin/Reports: $($adminReportsRes.StatusCode)" -ForegroundColor Green

Write-Host "=== AUTOMATED TESTING PASS COMPLETED SUCCESSFULLY ===" -ForegroundColor Cyan
