$baseUrl = "http://localhost:5248"
$ErrorActionPreference = "Continue"

Write-Host "=== TEST PASS 3: Lawyer Registration & Admin Verification Flow ===" -ForegroundColor Cyan

function Get-AntiForgeryToken($html) {
    if ($html -match '__RequestVerificationToken"[^>]*value="([^"]+)"') {
        return $matches[1]
    }
    return $null
}

# 1. Register a new Lawyer
$newLawyerEmail = "newlawyer_$(Get-Random)@legalconnect.com"
$newLawyerSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$regPage = Invoke-WebRequest -Uri "$baseUrl/Account/Register?role=Lawyer" -WebSession $newLawyerSession -UseBasicParsing
$token = Get-AntiForgeryToken $regPage.Content

$regBody = @{
    "Role" = "Lawyer"
    "FullName" = "Adv. Vikrant Deshmukh"
    "Email" = $newLawyerEmail
    "Phone" = "9820998877"
    "Password" = "Lawyer@123"
    "ConfirmPassword" = "Lawyer@123"
    "__RequestVerificationToken" = $token
}

$regRes = Invoke-WebRequest -Uri "$baseUrl/Account/Register" -Method Post -Body $regBody -WebSession $newLawyerSession -UseBasicParsing
Write-Host "[VERIFY TEST 1] New Lawyer Registered: $newLawyerEmail" -ForegroundColor Green

# 2. Check if new lawyer appears in public search
$searchBefore = Invoke-RestMethod -Uri "$baseUrl/api/LawyersApi?search=Vikrant"
if ($searchBefore.Count -eq 0) {
    Write-Host "[VERIFY TEST 2] SUCCESS: Unapproved lawyer does NOT appear in public lawyer search!" -ForegroundColor Green
} else {
    Write-Host "[VERIFY TEST 2] FAILED: Unapproved lawyer appeared in search!" -ForegroundColor Red
}

# 3. Admin logs in and checks VerifyLawyers queue
$adminSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginAdmin = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $adminSession -UseBasicParsing
$adminToken = Get-AntiForgeryToken $loginAdmin.Content
$adminBody = @{
    "Email" = "admin@legalconnect.com"
    "Password" = "Admin@123"
    "RememberMe" = "false"
    "__RequestVerificationToken" = $adminToken
}
$null = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $adminBody -WebSession $adminSession -UseBasicParsing

$verifyQueue = Invoke-WebRequest -Uri "$baseUrl/Admin/VerifyLawyers" -WebSession $adminSession -UseBasicParsing
if ($verifyQueue.Content -match 'Adv\. Vikrant Deshmukh') {
    Write-Host "[VERIFY TEST 3] SUCCESS: Adv. Vikrant Deshmukh is listed in Admin Verification Queue!" -ForegroundColor Green
}

# Extract lawyerId for approve
if ($verifyQueue.Content -match 'name="lawyerId"\s+value="(\d+)"') {
    $lawyerIdToApprove = $matches[1]
    $verifyToken = Get-AntiForgeryToken $verifyQueue.Content
    $approveRes = Invoke-WebRequest -Uri "$baseUrl/Admin/ApproveLawyer" -Method Post -Body @{ "lawyerId" = $lawyerIdToApprove; "__RequestVerificationToken" = $verifyToken } -WebSession $adminSession -UseBasicParsing
    Write-Host "[VERIFY TEST 4] Admin Approved Lawyer #$lawyerIdToApprove" -ForegroundColor Green
}

# 4. Check if lawyer now appears in public search
$searchAfter = Invoke-RestMethod -Uri "$baseUrl/api/LawyersApi?search=Vikrant"
if ($searchAfter.Count -gt 0) {
    Write-Host "[VERIFY TEST 5] SUCCESS: After Admin approval, Adv. Vikrant Deshmukh now appears in public lawyer search!" -ForegroundColor Green
} else {
    Write-Host "[VERIFY TEST 5] FAILED: Lawyer still not appearing after approval!" -ForegroundColor Red
}

Write-Host "=== TEST PASS 3 COMPLETED SUCCESSFULLY ===" -ForegroundColor Cyan
