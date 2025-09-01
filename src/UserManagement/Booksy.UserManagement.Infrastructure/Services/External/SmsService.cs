// ========================================
// Security Services
// ========================================

// Booksy.UserManagement.Infrastructure/Services/Security/PasswordHasher.cs
using Booksy.Core.Domain.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Infrastructure.Services.External
{
    public class SmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromPhoneNumber;

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _accountSid = _configuration["Twilio:AccountSid"] ?? "";
            _authToken = _configuration["Twilio:AuthToken"] ?? "";
            _fromPhoneNumber = _configuration["Twilio:FromPhoneNumber"] ?? "";

            //if (!string.IsNullOrEmpty(_accountSid) && !string.IsNullOrEmpty(_authToken))
            //{
            //    TwilioClient.Init(_accountSid, _authToken);
            //}
        }

        public async Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
        {
            //if (string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken))
            //{
            //    _logger.LogWarning("SMS service not configured. Message not sent to {PhoneNumber}", phoneNumber);
            //    return;
            //}

            //try
            //{
            //    var messageResource = await MessageResource.CreateAsync(
            //        body: message,
            //        from: new PhoneNumber(_fromPhoneNumber),
            //        to: new PhoneNumber(phoneNumber)
            //    );

            //    _logger.LogInformation("SMS sent successfully to {PhoneNumber}. MessageSid: {MessageSid}",
            //        phoneNumber, messageResource.Sid);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            //    throw;
            //}
        }

        public async Task SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
        {
            var message = $"Your Booksy verification code is: {code}. This code will expire in 10 minutes.";
            await SendSmsAsync(phoneNumber, message, cancellationToken);
        }
    }
}

