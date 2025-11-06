# Notification System Setup Instructions

## ⚠️ IMPORTANT: SendGrid API Key Required

The notification system requires a SendGrid API key to send email notifications.

### How to Get Your SendGrid API Key:

1. **Sign up for SendGrid**:
   - Go to https://signup.sendgrid.com/
   - Create a free account (100 emails/day free tier)

2. **Create API Key**:
   - Log in to SendGrid dashboard
   - Go to: Settings > API Keys
   - Click "Create API Key"
   - Name it: "Booksy Development"
   - Select "Full Access" or "Mail Send" permission
   - Click "Create & View"
   - **COPY THE KEY** (you won't see it again!)

3. **Add to Configuration**:

   **Option A: appsettings.Development.json** (for local testing):
   ```json
   {
     "SendGrid": {
       "ApiKey": "SG.paste-your-actual-key-here",
       "FromEmail": "noreply@booksy.local",
       "FromName": "Booksy Development"
     }
   }
   ```

   **Option B: Environment Variable** (more secure):
   ```bash
   export SendGrid__ApiKey="SG.your-actual-sendgrid-api-key-here"
   ```

   **Option C: User Secrets** (recommended for development):
   ```bash
   dotnet user-secrets init --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
   dotnet user-secrets set "SendGrid:ApiKey" "SG.your-actual-key-here" --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
   ```

4. **Verify Configuration**:
   - The API key should start with "SG."
   - Never commit real API keys to git
   - Use different keys for development and production

### Test Email Sending:

Once configured, you can test by sending a booking confirmation or using the test endpoint.

### SMS Configuration (Rahyab):

SMS credentials are already configured in `appsettings.json`:
```json
{
  "Rahyab": {
    "ApiUrl": "https://api.rahyab.ir/sms/send",
    "UserName": "web_negahno",
    "Password": "B3q71jaY96",
    "Number": "1000110110001",
    "Company": "NEGAHNO"
  }
}
```

**⚠️ If these credentials are incorrect, update them in your configuration.**

### Next Steps:

After adding the SendGrid API key:
1. Build the solution: `dotnet build`
2. Run migrations: `dotnet ef database update`
3. Start the API
4. Test notifications

### Troubleshooting:

**Error: "SendGrid API key not configured"**
- Solution: Add API key using one of the methods above

**Error: "401 Unauthorized" from SendGrid**
- Solution: Your API key is invalid or expired. Generate a new one

**Emails not arriving:**
- Check SendGrid dashboard for delivery status
- Check spam folder
- Verify "From" email is verified in SendGrid (for free tier)

### Security Notes:

- **Never** commit API keys to source control
- Use different API keys for each environment
- Rotate keys regularly
- Use User Secrets or Azure Key Vault for production
- Monitor SendGrid dashboard for unusual activity

