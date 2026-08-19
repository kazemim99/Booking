// ========================================
// Booksy.Infrastructure.External/Notifications/Sms/SmsNotificationServiceExtensions.cs
// ========================================
using Booksy.Core.Application.Services.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.Infrastructure.External.Notifications.Sms
{
    /// <summary>
    /// The one and only registration point for <see cref="ISmsNotificationService"/>.
    /// </summary>
    public static class SmsNotificationServiceExtensions
    {
        /// <summary>
        /// Registers exactly one SMS gateway for the whole process, chosen by
        /// <c>Notifications:SMS:Provider</c> (<c>Rahyab</c> — the default — or <c>Kavenegar</c>).
        /// </summary>
        /// <remarks>
        /// Every bounded context's <c>AddXInfrastructure()</c> calls this, and the modular-monolith host composes
        /// several of them, so the method is idempotent: the first caller wires the gateway and later callers are
        /// no-ops. Without that guard a second registration would silently win at resolution time, which is exactly
        /// how the codebase ended up running two different SMS gateways at once (Rahyab for notifications, Kavenegar
        /// for staff invitations) depending on which duplicate interface a caller imported.
        /// </remarks>
        public static IServiceCollection AddSmsNotificationService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            if (services.Any(d => d.ServiceType == typeof(ISmsNotificationService)))
                return services;

            var provider = configuration["Notifications:SMS:Provider"];

            if (string.Equals(provider, "Kavenegar", StringComparison.OrdinalIgnoreCase))
            {
                services.AddHttpClient<ISmsNotificationService, KavenegarSmsNotificationService>();
            }
            else
            {
                services.AddHttpClient<ISmsNotificationService, RahyabSmsNotificationService>();
            }

            return services;
        }
    }
}
