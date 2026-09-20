using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications
{
    /// <summary>The push channel's address book.</summary>
    public interface IDeviceTokenRegistry
    {
        /// <summary>
        /// Registers a device for a person, or refreshes it if it is already known. A token currently held by
        /// somebody else moves to this person.
        /// </summary>
        Task RegisterAsync(
            Guid userId,
            string token,
            DevicePlatform platform,
            CancellationToken cancellationToken = default);

        /// <summary>Stops pushing to one device. Idempotent.</summary>
        Task<bool> RevokeAsync(Guid userId, string token, CancellationToken cancellationToken = default);

        /// <summary>Every live token for a person, in registration order.</summary>
        Task<IReadOnlyList<DeviceToken>> GetActiveForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retires a token the gateway has rejected as permanently invalid — an uninstalled app, a token
        /// rotated by the OS. Retiring on the gateway's word is the only way this table stays honest; nothing
        /// else tells us a device is gone.
        /// </summary>
        Task RetireAsync(string token, string reason, CancellationToken cancellationToken = default);
    }

    /// <inheritdoc />
    public sealed class DeviceTokenRegistry : IDeviceTokenRegistry
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<DeviceTokenRegistry> _logger;

        public DeviceTokenRegistry(ServiceCatalogDbContext context, ILogger<DeviceTokenRegistry> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task RegisterAsync(
            Guid userId,
            string token,
            DevicePlatform platform,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("A device token cannot be blank.", nameof(token));

            var now = DateTime.UtcNow;
            var existing = await _context.Set<DeviceToken>()
                .FirstOrDefaultAsync(d => d.Token == token, cancellationToken);

            if (existing is null)
            {
                _context.Set<DeviceToken>().Add(new DeviceToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Token = token.Trim(),
                    Platform = platform,
                    RegisteredAt = now,
                    LastSeenAt = now,
                });

                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            if (existing.UserId != userId)
            {
                // The handset changed hands. Continuing to treat it as the old owner's device would deliver
                // their notifications to somebody else.
                _logger.LogInformation(
                    "Device token reassigned from {PreviousUserId} to {UserId}", existing.UserId, userId);

                existing.UserId = userId;
                existing.RegisteredAt = now;
            }

            existing.Platform = platform;
            existing.LastSeenAt = now;

            // A returning device: the person signed out and back in, or the gateway rejected the token and it
            // has since become valid again.
            existing.RevokedAt = null;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> RevokeAsync(
            Guid userId,
            string token,
            CancellationToken cancellationToken = default)
        {
            // Scoped to the caller: revoking by token alone would let anyone silence anyone else's device.
            var affected = await _context.Set<DeviceToken>()
                .Where(d => d.Token == token && d.UserId == userId && d.RevokedAt == null)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(d => d.RevokedAt, DateTime.UtcNow),
                    cancellationToken);

            return affected > 0;
        }

        public async Task<IReadOnlyList<DeviceToken>> GetActiveForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            await _context.Set<DeviceToken>()
                .AsNoTracking()
                .Where(d => d.UserId == userId && d.RevokedAt == null)
                .OrderBy(d => d.RegisteredAt)
                .ToListAsync(cancellationToken);

        public async Task RetireAsync(
            string token,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var affected = await _context.Set<DeviceToken>()
                .Where(d => d.Token == token && d.RevokedAt == null)
                .ExecuteUpdateAsync(
                    set => set.SetProperty(d => d.RevokedAt, DateTime.UtcNow),
                    cancellationToken);

            if (affected > 0)
                _logger.LogInformation("Device token retired by the gateway: {Reason}", reason);
        }
    }
}
