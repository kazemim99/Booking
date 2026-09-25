using System.Diagnostics;

namespace AsanRezerve.Core.Domain.Application.Services
{
    /// <summary>
    /// Telemetry service for distributed tracing
    /// </summary>
    public static class Telemetry
    {
        private static readonly ActivitySource ActivitySource = new("AsanRezerve.UserManagement");

        /// <summary>
        /// Starts a new activity for tracing
        /// </summary>
        public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        {
            return ActivitySource.StartActivity(name, kind);
        }
    }
}