# Phone Verification Quick Start Guide

## 🚀 Getting Started

### Prerequisites
1. **Database Setup:**
   ```bash
   # Run the migration script
   sqlcmd -S localhost -d booksy_user_management -i migrations/001_add_phone_verification.sql
   ```

2. **Configuration Check:**
   - File: `Booksy.UserManagement.API/appsettings.Development.json`
   - Verify OTP and SMS settings are configured (already done ✅)

3. **Start the API:**
   ```bash
   cd src/UserManagement/Booksy.UserManagement.API
   dotnet run
   ```

---

## 🧪 Testing with Postman/cURL

### Test 1: Send Verification Code (Test Number)

**Request:**
```bash
curl -X POST https://localhost:7001/api/v1/auth/send-verification-code \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "2222",
    "countryCode": "DE"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Verification code sent successfully",
  "maskedPhoneNumber": "2222",
  "expiresIn": 300
}
```

**What Happens:**
- ✅ Test number detected
- ✅ OTP not sent via SMS (sandbox mode)
- ✅ Code logged: Check console for `[SANDBOX] SMS to 2222: Your Booksy verification code is: 123456...`

---

### Test 2: Verify Code (Test Number)

**Request:**
```bash
curl -X POST https://localhost:7001/api/v1/auth/verify-code \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "2222",
    "code": "123456",
    "userType": "Provider"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": null,
  "expiresIn": 3600,
  "user": {
    "id": "00000000-0000-0000-0000-000000000000",
    "phoneNumber": "2222",
    "phoneVerified": true,
    "userType": "Provider",
    "roles": ["Provider"]
  },
  "errorMessage": null,
  "remainingAttempts": null
}
```

**What Happens:**
- ✅ Test number accepts ANY 6-digit code in sandbox mode
- ✅ User created or retrieved
- ✅ JWT token generated
- ✅ User status set to Active

---

### Test 3: Send Verification Code (Real Number - Sandbox)

**Request:**
```bash
curl -X POST https://localhost:7001/api/v1/auth/send-verification-code \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "+4917012345678",
    "countryCode": "DE"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Verification code sent successfully",
  "maskedPhoneNumber": "+49 170 ••• ••78",
  "expiresIn": 300
}
```

**What Happens:**
- ✅ OTP generated using OtpSharpService
- ✅ SMS NOT sent (sandbox mode)
- ✅ Check console logs for the code: `[SANDBOX] SMS to +49 170 ••• ••78: Your Booksy verification code is: XXXXXX`

---

### Test 4: Verify Code (Real Number - Sandbox)

**Find the code from console logs, then:**

**Request:**
```bash
curl -X POST https://localhost:7001/api/v1/auth/verify-code \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "+4917012345678",
    "code": "XXXXXX",
    "userType": "Provider"
  }'
```
*(Replace XXXXXX with actual code from logs or use sandbox code "123456")*

**Expected Response:**
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": null,
  "expiresIn": 3600,
  "user": {
    "id": "newly-generated-guid",
    "phoneNumber": "+4917012345678",
    "phoneVerified": true,
    "userType": "Provider",
    "roles": ["Provider"]
  }
}
```

---

### Test 5: Invalid Code (Attempt Limiting)

**Request:**
```bash
curl -X POST https://localhost:7001/api/v1/auth/verify-code \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "+4917012345678",
    "code": "000000",
    "userType": "Provider"
  }'
```

**Expected Response (1st attempt):**
```json
{
  "success": false,
  "message": "Invalid code. 2 attempts remaining.",
  "remainingAttempts": 2
}
```

**After 3 failed attempts:**
```json
{
  "success": false,
  "message": "Maximum attempts reached. Please request a new code.",
  "remainingAttempts": 0
}
```

---

### Test 6: Expired Code

**Wait 5+ minutes after sending code, then verify:**

**Expected Response:**
```json
{
  "success": false,
  "message": "Verification code has expired. Please request a new code.",
  "remainingAttempts": 0
}
```

---

### Test 7: Rate Limiting

**Send 4 verification requests to the same number within 15 minutes:**

**4th Request Response:**
```
HTTP 429 Too Many Requests
{
  "error": "Rate limit exceeded. Try again in 15 minutes."
}
```

---

## 🔍 Verify in Database

**Check PhoneVerifications Table:**
```sql
SELECT TOP 10
    PhoneNumber,
    IsVerified,
    AttemptCount,
    ExpiresAt,
    CreatedAt
FROM user_management.PhoneVerifications
ORDER BY CreatedAt DESC;
```

**Check Users Table:**
```sql
SELECT
    Id,
    Email,
    PhoneNumber,
    PhoneNumberVerified,
    PhoneVerifiedAt,
    Status,
    Type
FROM user_management.Users
WHERE PhoneNumber IS NOT NULL;
```

---

## 🎯 JWT Token Usage

**After receiving access token, use it for authenticated requests:**

```bash
curl -X GET https://localhost:7001/api/v1/providers/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Decode JWT Token:**
- Visit: https://jwt.io
- Paste token
- Verify claims:
  - `nameid`: User ID
  - `email`: User email
  - `role`: Provider
  - `user_type`: Provider
  - `exp`: Expiration timestamp

---

## 📊 Console Logs to Watch

**Successful Flow:**
```
[Information] Sending verification code to phone: +49 170 ••• ••78, Country: DE
[Information] Generated OTP code for phone: +49 170 ••• ••78
[Information] [SANDBOX] SMS to +49 170 ••• ••78: Your Booksy verification code is: 123456. Valid for 5 minutes...
[Information] Verification code sent to: +49 170 ••• ••78

[Information] Verifying code for phone: +49 170 ••• ••78
[Information] Phone successfully verified: +49 170 ••• ••78
[Information] Created new user from phone verification. User ID: {guid}
[Information] Phone verified successfully for user: {guid}
```

**Failed Verification:**
```
[Warning] Invalid code for phone: +49 170 ••• ••78. Attempts: 1/3
[Warning] Code verification failed for phone ending in: ••••5678. Remaining attempts: 2
```

---

## 🚨 Troubleshooting

### Issue: "Phone verification service not found"
**Solution:**
```csharp
// In Program.cs or DependencyInjection.cs, add:
services.AddScoped<IPhoneVerificationService, PhoneVerificationService>();
services.AddScoped<ISmsService, SmsService>();
services.AddScoped<IPhoneVerificationRepository, PhoneVerificationRepository>();
```

### Issue: "OtpService not registered"
**Solution:**
```csharp
// In Program.cs, add:
var otpSettings = builder.Configuration.GetSection("Otp").Get<OtpSettings>();
services.AddSingleton(otpSettings);
services.AddSingleton<IOtpService, OtpSharpService>();
```

### Issue: "PhoneVerifications table not found"
**Solution:**
```bash
# Run migration
sqlcmd -S localhost -d booksy_user_management -i migrations/001_add_phone_verification.sql
```

### Issue: Test number rejected in production
**Solution:**
- This is expected! Test numbers are blocked in production for security
- Use real phone numbers in production environment

---

## 🔄 Next Steps

1. **Run Database Migration:**
   ```bash
   cd migrations
   sqlcmd -S localhost -d booksy_user_management -i 001_add_phone_verification.sql
   ```

2. **Test Complete Flow:**
   - Send code to test number (2222)
   - Verify with any code (123456)
   - Receive JWT token
   - Use token for authenticated requests

3. **Frontend Integration:**
   - Build phone input component
   - Build OTP input component
   - Integrate with API endpoints

4. **Production Preparation:**
   - Add Twilio NuGet package
   - Configure real SMS credentials
   - Disable sandbox mode
   - Test with real phone numbers

---

## 📝 Sample Postman Collection

**Import this JSON into Postman:**

```json
{
  "info": {
    "name": "Booksy Phone Verification",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Send Verification Code (Test Number)",
      "request": {
        "method": "POST",
        "header": [{"key": "Content-Type", "value": "application/json"}],
        "url": "{{baseUrl}}/api/v1/auth/send-verification-code",
        "body": {
          "mode": "raw",
          "raw": "{\n  \"phoneNumber\": \"2222\",\n  \"countryCode\": \"DE\"\n}"
        }
      }
    },
    {
      "name": "Verify Code",
      "request": {
        "method": "POST",
        "header": [{"key": "Content-Type", "value": "application/json"}],
        "url": "{{baseUrl}}/api/v1/auth/verify-code",
        "body": {
          "mode": "raw",
          "raw": "{\n  \"phoneNumber\": \"2222\",\n  \"code\": \"123456\",\n  \"userType\": \"Provider\"\n}"
        }
      }
    }
  ],
  "variable": [
    {"key": "baseUrl", "value": "https://localhost:7001"}
  ]
}
```

---

**Happy Testing! 🎉**
