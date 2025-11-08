# ZarinPal Sandbox Testing Guide

This guide provides comprehensive instructions for testing the ZarinPal payment integration in sandbox mode.

## Table of Contents
- [Setup](#setup)
- [Configuration](#configuration)
- [Testing Workflows](#testing-workflows)
- [API Endpoint Testing](#api-endpoint-testing)
- [Error Scenarios](#error-scenarios)
- [Test Cards](#test-cards)
- [Common Issues](#common-issues)

## Setup

### 1. Get ZarinPal Sandbox Credentials

1. Visit [ZarinPal Sandbox](https://sandbox.zarinpal.com/)
2. Register for a sandbox merchant account
3. Obtain your sandbox Merchant ID

### 2. Configure Application

Update `appsettings.Development.json`:

```json
{
  "Payment": {
    "Provider": "ZarinPal",
    "ZarinPal": {
      "MerchantId": "your-sandbox-merchant-id",
      "IsSandbox": true,
      "CallbackUrl": "https://localhost:7002/api/v1/payments/callback"
    }
  }
}
```

**Important**: Ensure `IsSandbox` is set to `true` for testing.

### 3. Frontend Configuration

Your frontend needs to handle the callback redirect. Example success/failure URLs:
- Success: `https://your-frontend.com/payment/success?paymentId={paymentId}&refNumber={refNumber}`
- Failure: `https://your-frontend.com/payment/failure?paymentId={paymentId}&reason={reason}`

## Configuration

### Application Settings

```json
{
  "Payment": {
    "Provider": "ZarinPal",
    "ZarinPal": {
      "MerchantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
      "IsSandbox": true,
      "CallbackUrl": "https://localhost:7002/api/v1/payments/callback"
    }
  },
  "FrontendSettings": {
    "PaymentSuccessUrl": "https://localhost:3000/payment/success",
    "PaymentFailureUrl": "https://localhost:3000/payment/failure"
  }
}
```

### Environment Variables (Alternative)

```bash
export Payment__Provider="ZarinPal"
export Payment__ZarinPal__MerchantId="your-sandbox-merchant-id"
export Payment__ZarinPal__IsSandbox="true"
export Payment__ZarinPal__CallbackUrl="https://localhost:7002/api/v1/payments/callback"
```

## Testing Workflows

### Complete Payment Flow Test

#### 1. Create Payment Request

**Endpoint**: `POST /api/v1/payments/zarinpal/create`

**Request**:
```json
{
  "bookingId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "providerId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "amount": 50000,
  "description": "Test booking payment",
  "mobile": "09123456789",
  "email": "customer@example.com",
  "metadata": {
    "ip_address": "192.168.1.1",
    "user_agent": "Mozilla/5.0"
  }
}
```

**Response**:
```json
{
  "isSuccessful": true,
  "paymentId": "7fa85f64-5717-4562-b3fc-2c963f66afa8",
  "authority": "A00000000000000000000000000000123456",
  "paymentUrl": "https://sandbox.zarinpal.com/pg/StartPay/A00000000000000000000000000000123456",
  "amount": 50000,
  "currency": "IRR"
}
```

#### 2. Redirect User to Payment Gateway

Redirect the user to the `paymentUrl` from the response. In sandbox mode, you'll see a test payment page.

#### 3. Complete Payment on ZarinPal

On the ZarinPal sandbox page:
- Enter any 16-digit card number (e.g., `6274129005473742`)
- Enter any CVV2 and expiry date
- Click "Pay"

#### 4. Handle Callback

ZarinPal redirects to: `https://localhost:7002/api/v1/payments/callback?Authority=A00000000000000000000000000000123456&Status=OK`

The API automatically:
- Retrieves the payment by Authority
- Calls ZarinPal verify API
- Updates payment status
- Redirects to frontend success/failure page

#### 5. Verify Payment Details

**Endpoint**: `GET /api/v1/payments/{paymentId}`

**Response**:
```json
{
  "paymentId": "7fa85f64-5717-4562-b3fc-2c963f66afa8",
  "bookingId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerId": "...",
  "providerId": "...",
  "amount": 50000,
  "currency": "IRR",
  "paidAmount": 50000,
  "refundedAmount": 0,
  "status": "Paid",
  "method": "ZarinPal",
  "authority": "A00000000000000000000000000000123456",
  "refNumber": "123456789",
  "cardPan": "6274****3742",
  "fee": 500,
  "capturedAt": "2024-01-15T10:30:00Z"
}
```

## API Endpoint Testing

### 1. Create ZarinPal Payment

```bash
curl -X POST https://localhost:7002/api/v1/payments/zarinpal/create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "providerId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "amount": 100000,
    "description": "Service payment",
    "mobile": "09121234567",
    "email": "test@example.com"
  }'
```

### 2. Get Payment by Authority

```bash
curl -X GET "https://localhost:7002/api/v1/payments/authority/A00000000000000000000000000000123456" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 3. Get Customer Payment History

```bash
curl -X GET "https://localhost:7002/api/v1/payments/customer/history?startDate=2024-01-01&endDate=2024-12-31&page=1&pageSize=20" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 4. Get Provider Revenue

```bash
curl -X GET "https://localhost:7002/api/v1/payments/provider/3fa85f64-5717-4562-b3fc-2c963f66afa7/revenue?startDate=2024-01-01&endDate=2024-12-31" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 5. Refund Payment

```bash
curl -X POST https://localhost:7002/api/v1/payments/refund \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "paymentId": "7fa85f64-5717-4562-b3fc-2c963f66afa8",
    "amount": 25000,
    "reason": "Customer requested refund"
  }'
```

### 6. Get Payment Reconciliation Report

```bash
curl -X GET "https://localhost:7002/api/v1/payments/reconciliation?startDate=2024-01-01&endDate=2024-01-31" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Error Scenarios

### Testing Common Error Codes

#### 1. Invalid Merchant ID (Error -11)

Update configuration with invalid merchant ID:
```json
{
  "MerchantId": "invalid-merchant-id"
}
```

**Expected Response**:
```json
{
  "isSuccessful": false,
  "errorCode": -11,
  "errorMessage": "Invalid merchant credentials",
  "paymentId": "00000000-0000-0000-0000-000000000000"
}
```

#### 2. Amount Too Low (Error -1)

**Request**:
```json
{
  "amount": 500,
  "description": "Test"
}
```

**Expected**: Validation error - minimum 1000 Rials

#### 3. User Cancellation

User clicks "Cancel" on ZarinPal page.

**Callback**: `?Authority=A123&Status=NOK`

**Expected Behavior**:
- Payment status: `Failed`
- Failure reason: `User cancelled the payment`

#### 4. Invalid Authority on Verification (Error -54)

**Request**: Verify with non-existent authority
```json
{
  "authority": "INVALID_AUTHORITY",
  "status": "OK"
}
```

**Expected Response**:
```json
{
  "isSuccessful": false,
  "errorCode": -54,
  "errorMessage": "Authority not found"
}
```

#### 5. Verification Failed (Error -53)

Simulate by verifying with wrong amount.

**Expected Response**:
```json
{
  "isSuccessful": false,
  "errorCode": -53,
  "errorMessage": "Transaction verification unsuccessful"
}
```

### ZarinPal Error Code Reference

| Code | Description |
|------|-------------|
| 100  | Success |
| 101  | Transaction already verified |
| -1   | Invalid information submitted |
| -2   | IP or merchant code is incorrect |
| -3   | Amount must be greater than 100 Rials |
| -4   | Acceptable level of the merchant |
| -11  | Request not found |
| -12  | Unable to edit the request |
| -21  | Financial operation not found |
| -22  | Transaction unsuccessful |
| -33  | Transaction amount does not match |
| -34  | Division of the transaction failed |
| -40  | Important parameters are missing |
| -41  | Currency format is incorrect |
| -42  | Currency is not valid |
| -50  | Amount cannot exceed 500,000,000 Rials |
| -51  | Amount cannot be less than 100 Rials |
| -52  | Transaction not found for refund |
| -53  | Transaction verification unsuccessful |
| -54  | Authority not found |

## Test Cards

In **sandbox mode**, you can use any card number for testing. Common test cards:

| Bank | Card Number | CVV2 | Expiry |
|------|-------------|------|--------|
| Any Bank | 6274129005473742 | 123 | 12/30 |
| Any Bank | 6037997000000001 | 456 | 01/29 |
| Any Bank | 5022291234567890 | 789 | 06/28 |

**Note**: In sandbox, all card numbers work. The system will always succeed if you complete the payment.

## Testing Scenarios

### Scenario 1: Successful Booking Payment

**Steps**:
1. Create payment for a booking
2. Redirect to ZarinPal
3. Complete payment
4. Verify callback updates payment
5. Check booking status is updated
6. Verify payment history shows the transaction

### Scenario 2: Direct Payment (No Booking)

**Steps**:
1. Create payment without `bookingId`
2. Complete payment flow
3. Verify payment is recorded correctly

### Scenario 3: Payment Cancellation

**Steps**:
1. Create payment
2. Redirect to ZarinPal
3. Click "Cancel" button
4. Verify payment status is `Failed`
5. Verify failure reason is recorded

### Scenario 4: Partial Refund

**Steps**:
1. Create and complete payment for 100,000 Rials
2. Refund 40,000 Rials
3. Verify payment status is `PartiallyRefunded`
4. Verify `refundedAmount` is 40,000
5. Verify `GetNetAmount()` returns 60,000

### Scenario 5: Full Refund

**Steps**:
1. Create and complete payment for 100,000 Rials
2. Refund full 100,000 Rials
3. Verify payment status is `Refunded`
4. Verify net amount is 0

### Scenario 6: Multiple Payments for Customer

**Steps**:
1. Create 5 payments for the same customer
2. Complete 3, cancel 1, leave 1 pending
3. Request customer payment history
4. Verify pagination works
5. Verify status filtering

### Scenario 7: Revenue Calculation

**Steps**:
1. Create multiple payments for a provider
2. Complete some payments
3. Refund portions of some payments
4. Request provider revenue report
5. Verify calculations:
   - Total revenue
   - Total refunds
   - Net revenue
   - Success rate

### Scenario 8: Daily Reconciliation

**Steps**:
1. Create payments throughout a day
2. Complete various transactions
3. Request reconciliation report for that day
4. Verify all transactions are included
5. Verify summary calculations are correct

## Postman Collection Example

```json
{
  "info": {
    "name": "ZarinPal Payment Tests",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Create Payment",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "url": "{{baseUrl}}/api/v1/payments/zarinpal/create",
        "body": {
          "mode": "raw",
          "raw": "{\n  \"providerId\": \"{{providerId}}\",\n  \"amount\": 50000,\n  \"description\": \"Test payment\",\n  \"mobile\": \"09123456789\"\n}"
        }
      }
    },
    {
      "name": "Get Payment Details",
      "request": {
        "method": "GET",
        "url": "{{baseUrl}}/api/v1/payments/{{paymentId}}"
      }
    }
  ]
}
```

## Common Issues

### Issue 1: Callback URL Not Accessible

**Problem**: ZarinPal can't reach your localhost callback URL.

**Solution**:
- Use a tunneling service like ngrok: `ngrok http 7002`
- Update `CallbackUrl` to the ngrok URL
- Restart the application

### Issue 2: CORS Errors

**Problem**: Frontend can't call API endpoints.

**Solution**: Ensure CORS is configured in `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

### Issue 3: Payment Status Not Updating

**Problem**: Callback is called but payment remains Pending.

**Troubleshooting**:
1. Check logs for exceptions in `VerifyZarinPalPaymentCommandHandler`
2. Verify authority matches exactly
3. Check database for transaction records
4. Ensure UnitOfWork is committing changes

### Issue 4: Verification Failing

**Problem**: Verification returns error -53.

**Possible Causes**:
- Amount mismatch between request and verify
- Authority is invalid or expired
- Payment was already verified (returns 101, which is success)

### Issue 5: Missing Domain Events

**Problem**: SMS notifications not sent.

**Solution**:
- Verify domain event handlers are registered
- Check MediatR is configured correctly
- Ensure events are published before committing

## Automated Testing

### Unit Test Example

```csharp
[Fact]
public async Task Complete_Payment_Flow_Should_Work()
{
    // Create payment
    var createCommand = new CreateZarinPalPaymentCommand(
        BookingId: Guid.NewGuid(),
        CustomerId: TestData.CustomerId,
        ProviderId: TestData.ProviderId,
        Amount: 50000m,
        Currency: "IRR",
        Description: "Test payment",
        Mobile: "09123456789");

    var createResult = await _mediator.Send(createCommand);
    createResult.IsSuccessful.Should().BeTrue();

    // Simulate user completing payment on ZarinPal
    // In real tests, this would be manual or use Selenium

    // Verify payment
    var verifyCommand = new VerifyZarinPalPaymentCommand(
        createResult.Authority,
        "OK");

    var verifyResult = await _mediator.Send(verifyCommand);
    verifyResult.IsSuccessful.Should().BeTrue();
    verifyResult.PaymentStatus.Should().Be("Paid");
}
```

## Production Readiness Checklist

Before deploying to production:

- [ ] Update `IsSandbox` to `false`
- [ ] Use production Merchant ID
- [ ] Update `CallbackUrl` to production URL
- [ ] Configure SSL/TLS certificates
- [ ] Set up monitoring and logging
- [ ] Test with real ZarinPal account
- [ ] Configure frontend redirect URLs
- [ ] Set up webhook handlers (if applicable)
- [ ] Test all error scenarios
- [ ] Verify refund flow works
- [ ] Test reconciliation reports
- [ ] Set up automated daily reconciliation
- [ ] Configure payment retry logic
- [ ] Set up SMS notifications
- [ ] Review security settings
- [ ] Test with actual Iranian card numbers
- [ ] Verify amount calculations (Rial handling)

## Additional Resources

- [ZarinPal API Documentation](https://docs.zarinpal.com/)
- [ZarinPal Sandbox Portal](https://sandbox.zarinpal.com/)
- [ZarinPal Error Codes](https://docs.zarinpal.com/paymentGateway/error.html)
- [ZarinPal Support](https://www.zarinpal.com/support)

## Support

For issues or questions:
1. Check application logs in `logs/` directory
2. Review this guide
3. Check ZarinPal documentation
4. Contact development team
