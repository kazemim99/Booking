using Booksy.Infrastructure.External.Analytics;
using Booksy.Infrastructure.External.Marketing;
using Booksy.Infrastructure.External.Notifications;
using Booksy.Infrastructure.External.OTP;
using Booksy.Infrastructure.External.OTP.DTO;
using Booksy.Infrastructure.External.sms;
using Booksy.Infrastructure.External.sms.Rahyab;
using Booksy.Infrastructure.External.Storage;
using Booksy.Infrastructure.External.Payment;
using Booksy.Infrastructure.External.Payment.ZarinPal;
using Booksy.Infrastructure.External.Payment.IDPay;
using Booksy.Infrastructure.External.Payment.Behpardakht;
using Booksy.Infrastructure.External.Payment.Parsian;
using Booksy.Infrastructure.External.Payment.Saman;
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

        // NOTE: Notification Services (Multi-Channel) registration has been moved to
        // ServiceCatalog.Infrastructure to respect bounded context architecture.
        // See ServiceCatalogInfrastructureExtensions.AddNotificationServices()

        // SendGrid Client (shared infrastructure)
        services.AddSingleton<SendGrid.ISendGridClient>(sp =>
        {
            var apiKey = configuration["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid API key not configured");
            return new SendGrid.SendGridClient(apiKey);
        });


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

        // Payment Gateway Factory and Services
        // Register all payment gateway implementations

        // Stripe (keeping existing implementation)
        services.Configure<StripeSettings>(configuration.GetSection("Payment:Stripe"));
        services.AddScoped<StripePaymentGateway>();

        // ZarinPal
        services.Configure<ZarinPalSettings>(configuration.GetSection("Payment:ZarinPal"));
        services.AddHttpClient<IZarinPalService, ZarinPalService>();
        services.AddScoped<IZarinPalService, ZarinPalService>();
        services.AddScoped<ZarinPalPaymentGateway>();

        // IDPay
        services.Configure<IDPaySettings>(configuration.GetSection("Payment:IDPay"));
        services.AddHttpClient<IIDPayService, IDPayService>();
        services.AddScoped<IIDPayService, IDPayService>();
        services.AddScoped<IDPayPaymentGateway>();

        // Behpardakht
        services.Configure<BehpardakhtSettings>(configuration.GetSection("Payment:Behpardakht"));
        services.AddHttpClient<IBehpardakhtService, BehpardakhtService>();
        services.AddScoped<IBehpardakhtService, BehpardakhtService>();
        services.AddScoped<BehpardakhtPaymentGateway>();

        // Parsian (placeholder)
        services.AddScoped<ParsianPaymentGateway>();

        // Saman (placeholder)
        services.AddScoped<SamanPaymentGateway>();

        // Register the Payment Gateway Factory
        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

        // Register the default payment gateway for backward compatibility
        services.AddScoped<IPaymentGateway>(sp =>
        {
            var factory = sp.GetRequiredService<IPaymentGatewayFactory>();
            return factory.GetDefaultGateway();
        });

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
            options.HandshakeTimeout = TimeSpan.FromSeconds(60);
            options.HandshakeTimeout = TimeSpan.FromSeconds(30);
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);

            // Enable detailed errors in development
            var environment = configuration["ASPNETCORE_ENVIRONMENT"];
            options.EnableDetailedErrors = environment == "Development";

            // Max message size (1MB)
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
