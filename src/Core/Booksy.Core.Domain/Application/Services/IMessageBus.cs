namespace Booksy.Core.Domain.Application.Services
{

    /// <summary>
    /// Message bus for cross-context communication
    /// </summary>
    public interface IMessageBus
    {
        Task SendAsync<T>(T command, string targetContext, CancellationToken cancellationToken = default) where T : class;
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
    }
}