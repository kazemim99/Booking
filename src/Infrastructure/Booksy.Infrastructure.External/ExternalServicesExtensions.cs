using Booksy.Infrastructure.External.Analytics;
using Booksy.Infrastructure.External.Marketing;
using Booksy.Infrastructure.External.Notifications;
using Booksy.Infrastructure.External.OTP;
using Booksy.Infrastructure.External.OTP.DTO;
using Booksy.Infrastructure.External.sms;
using Booksy.Infrastructure.External.sms.Rahyab;
using Booksy.Infrastructure.External.Storage;
using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;


/// <summary>
/// Extension methods for external services configuration
/// </summary>
public static class ExternalServicesExtensions
{
    /// <summary>
    /// Adds external services
    /// </summary>
    public static IServiceCollection AddExternalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OtpSettings>(configuration.GetSection("Otp"));

        var otpSettings = new OtpSettings();
        configuration.GetSection("Otp").Bind(otpSettings);
        services.AddSingleton(otpSettings);

        services.AddScoped<IOtpService, OtpSharpService>();
        services.AddScoped<IRahyabSmsSender, SmsService>();

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();


        // Analytics
        var analyticsProvider = configuration["Analytics:Provider"];
        switch (analyticsProvider?.ToLower())
        {
            case "google":
                services.Configure<GoogleAnalyticsSettings>(configuration.GetSection("Analytics:GoogleAnalytics"));
                services.AddHttpClient<IAnalyticsService, GoogleAnalyticsService>();
                break;
            case "mixpanel":
                services.Configure<MixpanelSettings>(configuration.GetSection("Analytics:Mixpanel"));
                services.AddHttpClient<IAnalyticsService, MixpanelService>();
                break;
            default:
                services.AddSingleton<IAnalyticsService, NullAnalyticsService>();
                break;
        }

        // Marketing
        var marketingProvider = configuration["Marketing:Provider"];
        switch (marketingProvider?.ToLower())
        {
            case "mailchimp":
                services.Configure<MailchimpSettings>(configuration.GetSection("Marketing:Mailchimp"));
                services.AddHttpClient<IMarketingService, MailchimpService>();
                break;
            default:
                services.AddSingleton<IMarketingService, NullMarketingService>();
                break;
        }

        // Payment
        var paymentProvider = configuration["Payment:Provider"];
        switch (paymentProvider?.ToLower())
        {
            case "stripe":
                services.Configure<StripeSettings>(configuration.GetSection("Payment:Stripe"));
                services.AddScoped<IPaymentGateway, StripePaymentGateway>();
                break;
            default:
                // Add null implementation if needed
                break;
        }

        // Storage
        var storageProvider = configuration["Storage:Provider"];
        switch (storageProvider?.ToLower())
        {
            case "azure":
                services.Configure<AzureStorageSettings>(configuration.GetSection("Storage:Azure"));
                services.AddSingleton<IBlobStorage, AzureBlobStorage>();
                break;
            default:
                // Add local file storage if needed
                break;
        }

        return services;
    }
}
