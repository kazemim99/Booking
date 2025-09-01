namespace Booksy.Core.Domain.Application.Services
{
    public interface IAnalyticsService
    {
        Task TrackEventAsync(
            string eventName,
            string userId,
            Dictionary<string, object>? properties = null,
            CancellationToken cancellationToken = default);
    }

}