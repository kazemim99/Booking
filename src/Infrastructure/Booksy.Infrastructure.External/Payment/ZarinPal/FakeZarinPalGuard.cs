using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.Infrastructure.External.Payment.ZarinPal
{
    /// <summary>
    /// Decides — safely — whether the deterministic <see cref="FakeZarinPalService"/> may replace the real gateway.
    ///
    /// <para>The fake exists solely so automated end-to-end tests can drive the real create → callback → verify
    /// contract without a live bank. Because a payment gateway is the last place a silent misconfiguration should be
    /// possible, this guard is deliberately paranoid:</para>
    /// <list type="number">
    ///   <item><description><b>Off by default.</b> Absent configuration means the real gateway.</description></item>
    ///   <item><description><b>Hard fail-closed in Production.</b> Requesting the fake in Production does not fall
    ///   back quietly — it throws at startup, so the application refuses to boot rather than accept money through a
    ///   fake gateway.</description></item>
    ///   <item><description><b>Unknown environment counts as Production.</b> If the environment cannot be determined
    ///   we assume the most dangerous case.</description></item>
    /// </list>
    /// </summary>
    public static class FakeZarinPalGuard
    {
        /// <summary>Configuration key that requests the fake gateway.</summary>
        public const string ConfigKey = "Payments:UseFakeZarinPal";

        private const string ProductionEnvironmentName = "Production";

        /// <summary>
        /// True when the fake gateway is both requested and permitted. Throws <see cref="InvalidOperationException"/>
        /// when it is requested in Production (or in an environment that cannot be identified).
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="environmentName">
        /// The host environment name. Pass the host's real environment; when null the guard falls back to the
        /// <c>ASPNETCORE_ENVIRONMENT</c>/<c>DOTNET_ENVIRONMENT</c> configuration values, and treats an
        /// undeterminable environment as Production.
        /// </param>
        public static bool ShouldUseFake(IConfiguration configuration, string? environmentName)
        {
            var requested = configuration.GetValue(ConfigKey, false);
            if (!requested)
                return false;

            var environment = ResolveEnvironment(configuration, environmentName);
            var isProduction = string.IsNullOrWhiteSpace(environment) ||
                               environment.Equals(ProductionEnvironmentName, StringComparison.OrdinalIgnoreCase);

            if (isProduction)
            {
                throw new InvalidOperationException(
                    $"FATAL: '{ConfigKey}' is enabled but the host environment is " +
                    $"'{environment ?? "(undetermined)"}'. The fake ZarinPal gateway is a test-only seam and must " +
                    "never be active in Production — it does not move real money. Remove the setting (or set it to " +
                    "false) before deploying. Startup is aborted deliberately rather than serving payments through a fake gateway.");
            }

            return true;
        }

        private static string? ResolveEnvironment(IConfiguration configuration, string? environmentName)
        {
            if (!string.IsNullOrWhiteSpace(environmentName))
                return environmentName;

            return configuration["ASPNETCORE_ENVIRONMENT"]
                   ?? configuration["DOTNET_ENVIRONMENT"];
        }

        /// <summary>
        /// Registers the fake gateway in place of the real one and writes an unmistakable startup banner, so an
        /// operator reading logs can never mistake a faked payment path for a real one.
        /// </summary>
        public static void RegisterFake(IServiceCollection services, string? environmentName)
        {
            // Last registration wins for the default resolution, so this replaces the real IZarinPalService.
            services.AddScoped<IZarinPalService, FakeZarinPalService>();

            var banner = new string('!', 100);
            Console.WriteLine(banner);
            Console.WriteLine("!!  FAKE ZARINPAL GATEWAY ACTIVE — NO REAL PAYMENTS WILL BE PROCESSED");
            Console.WriteLine($"!!  Enabled by '{ConfigKey}=true'. Environment: {environmentName ?? "(from configuration)"}");
            Console.WriteLine("!!  Deterministic authorities/RefIds are issued for automated end-to-end testing only.");
            Console.WriteLine("!!  This configuration is rejected outright in Production.");
            Console.WriteLine(banner);
        }
    }
}
