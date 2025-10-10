//using Booksy.Infrastructure.External.sms;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using System.Text.RegularExpressions;

//namespace Booksy.Infrastructure.External.Notifications;

///// <summary>
///// SMS Service for sending verification codes and notifications
///// Supports Twilio and sandbox mode for development
///// </summary>
//public class SmsService : IRahyabSmsSender
//{
//    private readonly IConfiguration _configuration;
//    private readonly ILogger<SmsService> _logger;
//    private readonly string _accountSid;
//    private readonly string _authToken;
//    private readonly string _fromPhoneNumber;
//    private readonly bool _sandboxMode;

//    public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
//    {
//        _configuration = configuration;
//        _logger = logger;

//        _accountSid = _configuration["Sms:AccountSid"] ?? _configuration["Twilio:AccountSid"] ?? "";
//        _authToken = _configuration["Sms:AuthToken"] ?? _configuration["Twilio:AuthToken"] ?? "";
//        _fromPhoneNumber = _configuration["Sms:FromPhoneNumber"] ?? _configuration["Twilio:FromPhoneNumber"] ?? "+4930123456789";
//        _sandboxMode = bool.Parse(_configuration["Sms:SandboxMode"] ?? "false");

 
//    public async Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
//    {
//        // Sandbox mode: log instead of sending
//        if (_sandboxMode)
//        {
//            _logger.LogInformation("[SANDBOX] SMS to {PhoneNumber}: {Message}", MaskPhoneNumber(phoneNumber), message);
//            await Task.Delay(100, cancellationToken); // Simulate delay
//            return;
//        }

//        // Check if SMS service is configured
//        if (string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken))
//        {
//            _logger.LogWarning("SMS service not configured. Message not sent to {PhoneNumber}", MaskPhoneNumber(phoneNumber));

//            // In development, log the message for testing
//            if (_configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
//            {
//                _logger.LogInformation("[DEV] SMS to {PhoneNumber}: {Message}", MaskPhoneNumber(phoneNumber), message);
//            }
//            return;
//        }


//        await Task.CompletedTask;
//    }

//    public async Task SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
//    {
//        var message = $"Your Booksy verification code is: {code}. Valid for 5 minutes. Do not share this code.";
//        await SendSmsAsync(phoneNumber, message, cancellationToken);
//    }

//    private string MaskPhoneNumber(string phoneNumber)
//    {
//        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
//            return phoneNumber;

//        // Example: +4917012345678 → +49 170 ••• ••78
//        phoneNumber = Regex.Replace(phoneNumber, @"[\s\-\(\)]", "");

//        if (phoneNumber.StartsWith("+"))
//        {
//            int length = phoneNumber.Length;
//            string last2 = phoneNumber.Substring(length - 2);
//            string first = phoneNumber.Substring(0, Math.Min(7, length - 2));
//            return $"{first} ••• ••{last2}";
//        }

//        return phoneNumber.Substring(0, 2) + "•••" + phoneNumber.Substring(phoneNumber.Length - 2);
//    }
//}

