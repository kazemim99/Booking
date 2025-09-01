namespace Booksy.Core.Domain.Application.Services
{

    /// <summary>
    /// Message queue for retry operations
    /// </summary>
    public interface IMessageQueue
    {
        Task EnqueueAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
        Task<T?> DequeueAsync<T>(CancellationToken cancellationToken = default) where T : class;
    }
}