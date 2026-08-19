// ========================================
// Booksy.ServiceCatalog.Application/Services/Notifications/NotificationDispatchOptions.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Switches for the notification delivery pipeline.
    /// </summary>
    public sealed class NotificationDispatchOptions
    {
        /// <summary>Configuration key backing <see cref="ReliableDispatch"/>.</summary>
        public const string ReliableDispatchKey = "Notifications:ReliableDispatch";

        /// <summary>
        /// When false, the preference gate and the de-duplication gate are bypassed and every named channel is
        /// simply attempted — the pre-change behaviour, kept as the rollback path for one release.
        /// </summary>
        /// <remarks>
        /// The per-channel fan-out is deliberately <i>not</i> behind this flag: sending only to the first channel
        /// of a combined value such as <c>Email | SMS | InApp</c> was a plain bug, not a behaviour worth keeping.
        /// </remarks>
        public bool ReliableDispatch { get; init; } = true;
    }
}
