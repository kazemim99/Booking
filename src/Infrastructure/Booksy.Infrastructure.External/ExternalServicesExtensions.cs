using Booksy.Infrastructure.External.Analytics;
using Booksy.Infrastructure.External.Hubs;
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

        // Notification Services (Multi-Channel)
        services.AddScoped<Booksy.ServiceCatalog.Application.Services.Notifications.IEmailNotificationService,
            Booksy.Infrastructure.External.Notifications.Email.SendGridEmailNotificationService>();
        services.AddScoped<Booksy.ServiceCatalog.Application.Services.Notifications.ISmsNotificationService,
            Booksy.Infrastructure.External.Notifications.Sms.RahyabSmsNotificationService>();
        services.AddScoped<Booksy.ServiceCatalog.Application.Services.Notifications.IPushNotificationService,
            Booksy.Infrastructure.External.Notifications.Push.FirebasePushNotificationService>();
        services.AddScoped<Booksy.ServiceCatalog.Application.Services.Notifications.IInAppNotificationService,
            Booksy.Infrastructure.External.Notifications.InAppNotificationService>();

        // Template Engine & Services
        services.AddSingleton<Booksy.ServiceCatalog.Application.Services.Notifications.ITemplateEngine,
            Booksy.Infrastructure.External.Notifications.TemplateEngine>();
        services.AddScoped<Booksy.ServiceCatalog.Application.Services.Notifications.INotificationTemplateService,
            Booksy.Infrastructure.External.Notifications.NotificationTemplateService>();

        // SendGrid Client
        services.AddSingleton<SendGrid.ISendGridClient>(sp =>
        {
            var apiKey = configuration["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid API key not configured");
            return new SendGrid.SendGridClient(apiKey);
        });

        // HTTP Clients for notification services
        services.AddHttpClient<Booksy.Infrastructure.External.Notifications.Email.SendGridEmailNotificationService>();
        services.AddHttpClient<Booksy.Infrastructure.External.Notifications.Sms.RahyabSmsNotificationService>();


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

    /// <summary>
    /// Adds SignalR for real-time notifications
    /// </summary>
    public static IServiceCollection AddSignalRNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add SignalR with configuration
        services.AddSignalR(options =>
        {
            // Configure timeouts
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
            options.HandshakeTimeout = TimeSpan.FromSeconds(30);
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);

            // Enable detailed errors in development
            var environment = configuration["ASPNETCORE_ENVIRONMENT"];
            options.EnableDetailedErrors = environment == "Development";

            // Max message size (1MB)
            options.MaximumReceiveMessageSize = 1024 * 1024;
        });

        // Add CORS for SignalR (if needed)
        var allowedOrigins = configuration.GetSection("SignalR:AllowedOrigins").Get<string[]>();
        if (allowedOrigins?.Length > 0)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("SignalRPolicy", builder =>
                {
                    builder.WithOrigins(allowedOrigins)
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });
            });
        }

        return services;
    }
}
