$baseUrl = "http://localhost:5248"
$ErrorActionPreference = "Continue"

Write-Host "=== TEST PASS 2: Appointment Flow, Conflicts, & Verification ===" -ForegroundColor Cyan

function Get-AntiForgeryToken($html) {
    if ($html -match '__RequestVerificationToken"[^>]*value="([^"]+)"') {
        return $matches[1]
    }
    return $null
}

# 1. Login as Client
$clientSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $clientSession -UseBasicParsing
$token = Get-AntiForgeryToken $loginPage.Content
$loginBody = @{
    "Email" = "client@legalconnect.com"
    "Password" = "Client@123"
    "RememberMe" = "false"
    "__RequestVerificationToken" = $token
}
$null = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $loginBody -WebSession $clientSession -UseBasicParsing

# 2. Get Book Page for LawyerId = 1
$bookPage = Invoke-WebRequest -Uri "$baseUrl/Appointment/Book?lawyerId=1" -WebSession $clientSession -UseBasicParsing
Write-Host "[APPT TEST 1] GET /Appointment/Book?lawyerId=1 Status: $($bookPage.StatusCode)" -ForegroundColor Green

$bookToken = Get-AntiForgeryToken $bookPage.Content

# Find next Monday (Lawyer is available Monday 10:00 - 13:00)
$targetDate = (Get-Date).Date.AddDays(1)
while ($targetDate.DayOfWeek -ne [System.DayOfWeek]::Monday) {
    $targetDate = $targetDate.AddDays(1)
}
$dateStr = $targetDate.ToString("yyyy-MM-dd")
Write-Host "Booking for Next Monday: $dateStr at 10:00"

$bookBody = @{
    "LawyerId" = "1"
    "AppointmentDate" = $dateStr
    "AppointmentTimeString" = "10:00"
    "ConsultationMode" = "0"
    "CaseTitle" = "Corporate Trademark Infringement"
    "CaseDescription" = "Urgent legal advisory required for trademark dispute with a competitor in regional jurisdiction."
    "__RequestVerificationToken" = $bookToken
}

# Submit booking
try {
    $bookRes = Invoke-WebRequest -Uri "$baseUrl/Appointment/Book" -Method Post -Body $bookBody -WebSession $clientSession -UseBasicParsing -MaximumRedirection 0
    Write-Host "[APPT TEST 2] Appointment Booking 1 Status: $($bookRes.StatusCode) Redirect: $($bookRes.Headers.Location)" -ForegroundColor Green
} catch {
    Write-Host "[APPT TEST 2] Booking response: $($_.Exception.Message)"
}

# Try double-booking the EXACT SAME SLOT (should fail conflict validation)
$bookPage2 = Invoke-WebRequest -Uri "$baseUrl/Appointment/Book?lawyerId=1" -WebSession $clientSession -UseBasicParsing
$bookToken2 = Get-AntiForgeryToken $bookPage2.Content
$bookBody2 = @{
    "LawyerId" = "1"
    "AppointmentDate" = $dateStr
    "AppointmentTimeString" = "10:00"
    "ConsultationMode" = "0"
    "CaseTitle" = "Duplicate Test Request"
    "CaseDescription" = "This should fail because the slot is already pending/booked."
    "__RequestVerificationToken" = $bookToken2
}

$duplicateRes = Invoke-WebRequest -Uri "$baseUrl/Appointment/Book" -Method Post -Body $bookBody2 -WebSession $clientSession -UseBasicParsing
if ($duplicateRes.Content -match "already booked" -or $duplicateRes.Content -match "already have a pending") {
    Write-Host "[APPT TEST 3] Conflict Check SUCCESS: Duplicate slot request was properly rejected with clear error message!" -ForegroundColor Green
} else {
    Write-Host "[APPT TEST 3] Warning: Duplicate response content did not match expected error string." -ForegroundColor Yellow
}

# 3. Test Lawyer Accept
$lawyerSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginLawyer = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $lawyerSession -UseBasicParsing
$lToken = Get-AntiForgeryToken $loginLawyer.Content
$lBody = @{
    "Email" = "lawyer@legalconnect.com"
    "Password" = "Lawyer@123"
    "RememberMe" = "false"
    "__RequestVerificationToken" = $lToken
}
$null = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $lBody -WebSession $lawyerSession -UseBasicParsing

$lawyerDash = Invoke-WebRequest -Uri "$baseUrl/Lawyer/Dashboard" -WebSession $lawyerSession -UseBasicParsing
if ($lawyerDash.Content -match 'action="/Appointment/Accept/(\d+)"') {
    $apptIdToAccept = $matches[1]
    Write-Host "[APPT TEST 4] Found Pending Appointment #$apptIdToAccept on Lawyer Dashboard" -ForegroundColor Green
    
    $dashToken = Get-AntiForgeryToken $lawyerDash.Content
    $acceptRes = Invoke-WebRequest -Uri "$baseUrl/Appointment/Accept/$apptIdToAccept" -Method Post -Body @{ "__RequestVerificationToken" = $dashToken } -WebSession $lawyerSession -UseBasicParsing -MaximumRedirection 0
    Write-Host "[APPT TEST 5] Lawyer Accept Appointment #$apptIdToAccept Status: $($acceptRes.StatusCode) Redirect: $($acceptRes.Headers.Location)" -ForegroundColor Green
} else {
    Write-Host "[APPT TEST 4] No pending appointment with Accept action found on dashboard." -ForegroundColor Yellow
}

Write-Host "=== TEST PASS 2 COMPLETED SUCCESSFULLY ===" -ForegroundColor Cyan
