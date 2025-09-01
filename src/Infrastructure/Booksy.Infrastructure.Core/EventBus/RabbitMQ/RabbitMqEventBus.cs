// ========================================
// EventBus/RabbitMQ/RabbitMqEventBus.cs
// ========================================
using System.Text;
using System.Text.Json;
using Booksy.Core.Domain.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Booksy.Infrastructure.Core.EventBus.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of event bus for production use
/// </summary>
public sealed class RabbitMqEventBus : IEventBus, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly Dictionary<string, List<Type>> _handlers = new();

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IDomainEvent
        where THandler : IEventHandler<TEvent>
    {
        throw new NotImplementedException();
    }

    public void Unsubscribe<TEvent, THandler>()
        where TEvent : IDomainEvent
        where THandler : IEventHandler<TEvent>
    {
        throw new NotImplementedException();
    }

    //public RabbitMqEventBus(
    //    IOptions<RabbitMqSettings> settings,
    //    ILogger<RabbitMqEventBus> logger,
    //    IServiceProvider serviceProvider)
    //{
    //    _settings = settings.Value;
    //    _logger = logger;
    //    _serviceProvider = serviceProvider;

    //    var factory = new ConnectionFactory
    //    {
    //        HostName = _settings.HostName,
    //        UserName = _settings.UserName,
    //        Password = _settings.Password,
    //        VirtualHost = _settings.VirtualHost,
    //        Port = _settings.Port,
    //        DispatchConsumersAsync = true
    //    };

    //    _connection = factory.CreateConnection();
    //    _channel = _connection.CreateModel();

    //    DeclareExchange();
    //}

    //public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    //{
    //    var eventName = domainEvent.GetType().Name;
    //    var message = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
    //    var body = Encoding.UTF8.GetBytes(message);

    //    var properties = _channel.CreateBasicProperties();
    //    properties.DeliveryMode = 2; // Persistent
    //    properties.MessageId = domainEvent.EventId.ToString();
    //    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    //    _logger.LogDebug("Publishing event {EventName} to RabbitMQ", eventName);

    //    _channel.BasicPublish(
    //        exchange: _settings.ExchangeName,
    //        routingKey: eventName,
    //        mandatory: true,
    //        basicProperties: properties,
    //        body: body);

    //    await Task.CompletedTask;
    //}

    //public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    //{
    //    var batch = _channel.CreateBasicPublishBatch();

    //    foreach (var domainEvent in domainEvents)
    //    {
    //        var eventName = domainEvent.GetType().Name;
    //        var message = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
    //        var body = Encoding.UTF8.GetBytes(message);

    //        var properties = _channel.CreateBasicProperties();
    //        properties.DeliveryMode = 2;
    //        properties.MessageId = domainEvent.EventId.ToString();

    //        batch.Add(
    //            exchange: _settings.ExchangeName,
    //            routingKey: eventName,
    //            mandatory: true,
    //            properties: properties,
    //            body: new ReadOnlyMemory<byte>(body));
    //    }

    //    batch.Publish();
    //    await Task.CompletedTask;
    //}

    //public void Subscribe<TEvent, THandler>()
    //    where TEvent : IDomainEvent
    //    where THandler : IEventHandler<TEvent>
    //{
    //    var eventName = typeof(TEvent).Name;
    //    var handlerType = typeof(THandler);

    //    BindQueue(eventName);

    //    if (!_handlers.ContainsKey(eventName))
    //    {
    //        _handlers[eventName] = new List<Type>();
    //    }

    //    if (!_handlers[eventName].Contains(handlerType))
    //    {
    //        _handlers[eventName].Add(handlerType);
    //        StartBasicConsume(eventName);
    //        _logger.LogInformation("Subscribed {Handler} to {Event}", handlerType.Name, eventName);
    //    }
    //}

    //public void Unsubscribe<TEvent, THandler>()
    //    where TEvent : IDomainEvent
    //    where THandler : IEventHandler<TEvent>
    //{
    //    var eventName = typeof(TEvent).Name;
    //    var handlerType = typeof(THandler);

    //    if (_handlers.ContainsKey(eventName))
    //    {
    //        _handlers[eventName].Remove(handlerType);
    //        _logger.LogInformation("Unsubscribed {Handler} from {Event}", handlerType.Name, eventName);
    //    }
    //}

    //private void DeclareExchange()
    //{
    //    _channel.ExchangeDeclare(
    //        exchange: _settings.ExchangeName,
    //        type: ExchangeType.Topic,
    //        durable: true,
    //        autoDelete: false);
    //}

    //private void BindQueue(string eventName)
    //{
    //    var queueName = $"{_settings.QueuePrefix}.{eventName}";

    //    _channel.QueueDeclare(
    //        queue: queueName,
    //        durable: true,
    //        exclusive: false,
    //        autoDelete: false);

    //    _channel.QueueBind(
    //        queue: queueName,
    //        exchange: _settings.ExchangeName,
    //        routingKey: eventName);
    //}

    //private void StartBasicConsume(string eventName)
    //{
    //    var queueName = $"{_settings.QueuePrefix}.{eventName}";
    //    var consumer = new AsyncEventingBasicConsumer(_channel);

    //    consumer.Received += async (model, ea) =>
    //    {
    //        var body = ea.Body.ToArray();
    //        var message = Encoding.UTF8.GetString(body);

    //        try
    //        {
    //            await ProcessEventAsync(eventName, message);
    //            _channel.BasicAck(ea.DeliveryTag, false);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error processing message {EventName}", eventName);
    //            _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue
    //        }
    //    };

    //    _channel.BasicConsume(
    //        queue: queueName,
    //        autoAck: false,
    //        consumer: consumer);
    //}

    //private async Task ProcessEventAsync(string eventName, string message)
    //{
    //    if (_handlers.TryGetValue(eventName, out var handlerTypes))
    //    {
    //        using var scope = _serviceProvider.CreateScope();

    //        foreach (var handlerType in handlerTypes)
    //        {
    //            var handler = scope.ServiceProvider.GetService(handlerType);
    //            if (handler != null)
    //            {
    //                var eventType = handlerType.GetInterfaces()
    //                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
    //                    .GetGenericArguments()[0];

    //                var domainEvent = JsonSerializer.Deserialize(message, eventType);

    //                if (domainEvent != null)
    //                {
    //                    var handleMethod = handlerType.GetMethod("HandleAsync");
    //                    if (handleMethod != null)
    //                    {
    //                        var task = (Task?)handleMethod.Invoke(handler, new[] { domainEvent, CancellationToken.None });
    //                        if (task != null)
    //                        {
    //                            await task;
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}

    //public void Dispose()
    //{
    //    _channel?.Dispose();
    //    _connection?.Dispose();
    //}
}



public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public int Port { get; set; } = 5672;
    public string ExchangeName { get; set; } = "booksy_events";
    public string QueuePrefix { get; set; } = "booksy";
    public int RetryCount { get; set; } = 5;
    public int RetryDelay { get; set; } = 1000;
}