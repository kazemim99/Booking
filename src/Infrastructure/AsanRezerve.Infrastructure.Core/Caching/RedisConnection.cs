using StackExchange.Redis;

namespace AsanRezerve.Infrastructure.Core.Caching;

/// <summary>
/// The application's one Redis connection, opened on first use rather than while the container is being built.
/// <para>Connecting inside service registration (as the retired cache did) blocked host startup for the connect
/// timeout whenever Redis was slow, and opened a second connection next to the distributed cache's own. Redis
/// multiplexers are designed to be shared; this one serves the distributed cache (and so rate limiting, OTP state
/// and HybridCache's second level).</para>
/// </summary>
public sealed class RedisConnection : IAsyncDisposable
{
    private readonly ConfigurationOptions _options;
    private readonly Lazy<Task<IConnectionMultiplexer>> _connection;

    public RedisConnection(string connectionString)
    {
        _options = ConfigurationOptions.Parse(connectionString);

        // Never fail the connect: a Redis that is down at startup must not take the API down with it. The
        // multiplexer keeps reconnecting in the background and the cache's circuit breaker covers the gap.
        _options.AbortOnConnectFail = false;

        _connection = new Lazy<Task<IConnectionMultiplexer>>(
            async () => await ConnectionMultiplexer.ConnectAsync(_options).ConfigureAwait(false),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>The shared multiplexer, connecting on the first call.</summary>
    public Task<IConnectionMultiplexer> GetAsync() => _connection.Value;

    /// <summary>Whether a connection has been opened and is currently up.</summary>
    public bool IsConnected =>
        _connection.IsValueCreated
        && _connection.Value.IsCompletedSuccessfully
        && _connection.Value.Result.IsConnected;

    /// <summary>The configured endpoints, without credentials — safe to show on the admin cache page.</summary>
    public string Endpoints => string.Join(", ", _options.EndPoints.Select(e => e.ToString()));

    public async ValueTask DisposeAsync()
    {
        if (_connection.IsValueCreated && _connection.Value.IsCompletedSuccessfully)
        {
            await _connection.Value.Result.DisposeAsync().ConfigureAwait(false);
        }
    }
}
