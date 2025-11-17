# Test Availability API Script
$baseUrl = "http://localhost:5010/api/v1"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Testing Availability Controller API" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# First, let's get a list of providers
Write-Host "1. Fetching providers..." -ForegroundColor Yellow
try {
    $providersResponse = Invoke-RestMethod -Uri "$baseUrl/providers" -Method Get -ContentType "application/json"

    if ($providersResponse -and $providersResponse.Count -gt 0) {
        $firstProvider = $providersResponse[0]
        $providerId = $firstProvider.id
        $providerName = $firstProvider.name

        Write-Host "   Found Provider: $providerName" -ForegroundColor Green
        Write-Host "   Provider ID: $providerId" -ForegroundColor Green
        Write-Host ""

        # Get services for this provider
        Write-Host "2. Fetching services for provider..." -ForegroundColor Yellow
        $servicesResponse = Invoke-RestMethod -Uri "$baseUrl/providers/$providerId/services" -Method Get -ContentType "application/json"

        if ($servicesResponse -and $servicesResponse.Count -gt 0) {
            $firstService = $servicesResponse[0]
            $serviceId = $firstService.id
            $serviceName = $firstService.name

            Write-Host "   Found Service: $serviceName" -ForegroundColor Green
            Write-Host "   Service ID: $serviceId" -ForegroundColor Green
            Write-Host ""

            # Test 1: Get available slots for a specific date
            $testDate = (Get-Date).AddDays(7).ToString("yyyy-MM-dd")
            Write-Host "3. Testing GET /api/v1/availability/slots" -ForegroundColor Yellow
            Write-Host "   Parameters: ProviderId=$providerId, ServiceId=$serviceId, Date=$testDate" -ForegroundColor Gray

            $slotsUrl = "$baseUrl/availability/slots?ProviderId=$providerId&ServiceId=$serviceId&Date=$testDate"
            $slotsResponse = Invoke-RestMethod -Uri $slotsUrl -Method Get -ContentType "application/json"

            Write-Host "   Response: Found $($slotsResponse.Count) time slots" -ForegroundColor Green
            if ($slotsResponse.Count -gt 0) {
                Write-Host "   Sample slots:" -ForegroundColor Cyan
                $slotsResponse | Select-Object -First 5 | ForEach-Object {
                    Write-Host "     - $($_.startTime) to $($_.endTime) (Available: $($_.isAvailable))" -ForegroundColor White
                }
            }
            Write-Host ""

            # Test 2: Check specific slot availability
            if ($slotsResponse.Count -gt 0) {
                $testSlot = $slotsResponse[0]
                $testStartTime = $testSlot.startTime

                Write-Host "4. Testing GET /api/v1/availability/check" -ForegroundColor Yellow
                Write-Host "   Parameters: ProviderId=$providerId, ServiceId=$serviceId, StartTime=$testStartTime" -ForegroundColor Gray

                $checkUrl = "$baseUrl/availability/check?ProviderId=$providerId&ServiceId=$serviceId&StartTime=$testStartTime"
                $checkResponse = Invoke-RestMethod -Uri $checkUrl -Method Get -ContentType "application/json"

                Write-Host "   Response:" -ForegroundColor Green
                Write-Host "     - Is Available: $($checkResponse.isAvailable)" -ForegroundColor White
                Write-Host "     - Message: $($checkResponse.message)" -ForegroundColor White
                Write-Host ""
            }

            # Test 3: Get available dates in a range
            $fromDate = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
            $toDate = (Get-Date).AddDays(14).ToString("yyyy-MM-dd")

            Write-Host "5. Testing GET /api/v1/availability/dates" -ForegroundColor Yellow
            Write-Host "   Parameters: ProviderId=$providerId, ServiceId=$serviceId, FromDate=$fromDate, ToDate=$toDate" -ForegroundColor Gray

            $datesUrl = "$baseUrl/availability/dates?ProviderId=$providerId&ServiceId=$serviceId&FromDate=$fromDate&ToDate=$toDate"
            $datesResponse = Invoke-RestMethod -Uri $datesUrl -Method Get -ContentType "application/json"

            Write-Host "   Response: Found $($datesResponse.Count) dates with availability" -ForegroundColor Green
            if ($datesResponse.Count -gt 0) {
                Write-Host "   Sample dates:" -ForegroundColor Cyan
                $datesResponse | Select-Object -First 5 | ForEach-Object {
                    Write-Host "     - $($_.date): $($_.availableSlotsCount) slots available" -ForegroundColor White
                }
            }
            Write-Host ""

            Write-Host "=====================================" -ForegroundColor Cyan
            Write-Host "All tests completed successfully!" -ForegroundColor Green
            Write-Host "=====================================" -ForegroundColor Cyan

        } else {
            Write-Host "   No services found for provider" -ForegroundColor Red
        }
    } else {
        Write-Host "   No providers found" -ForegroundColor Red
    }
} catch {
    Write-Host "Error occurred: $_" -ForegroundColor Red
    Write-Host "Error Details: $($_.Exception.Message)" -ForegroundColor Red
}
