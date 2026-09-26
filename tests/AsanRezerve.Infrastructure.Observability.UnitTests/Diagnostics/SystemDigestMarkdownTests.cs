using AsanRezerve.Infrastructure.Core.Caching;
using AsanRezerve.Infrastructure.Observability.Diagnostics;
using AsanRezerve.Infrastructure.Observability.LogStore;
using FluentAssertions;

namespace AsanRezerve.Infrastructure.Observability.UnitTests.Diagnostics;

/// <summary>
/// The AI digest: a window of system behaviour as compact Markdown an assistant can read as-is (log-explorer:
/// "System overview and AI digest"; decision 2026-09-25 — the MCP server and admins fetch it; the server sends it
/// nowhere).
/// </summary>
public sealed class SystemDigestMarkdownTests
{
    private static readonly DateTimeOffset From = new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);

    private static SystemDigest Digest(
        IReadOnlyList<ErrorGroup>? groups = null,
        IReadOnlyList<RouteLatency>? routes = null,
        IReadOnlyList<LevelCount>? levels = null) =>
        new(From, From.AddHours(1), From.AddHours(1),
            levels ?? [],
            groups ?? [],
            routes ?? [],
            [new CacheRegionStats("GetProviderByIdQuery", 100, 90, 10)],
            new LogStoreStats(true, 500, 490, 10, 0, 0, null, From));

    [Fact]
    public void Repeated_errors_appear_once_with_their_count_and_sample_traces()
    {
        var markdown = SystemDigestMarkdown.Render(Digest(groups:
        [
            new ErrorGroup("Error", "AsanRezerve.API.Middleware.ExceptionHandlingMiddleware",
                "Request {RequestMethod} {RequestPath} failed with {StatusCode}", "System.InvalidOperationException: db down",
                20, From.AddMinutes(1), From.AddMinutes(55), ["4bf92f3577b34da6a3ce929d0e0e4736", "00f067aa0ba902b7aaaaaaaaaaaaaaaa"]),
        ]));

        markdown.Should().Contain("| Error | 20 |")
            .And.Contain("2026-09-25 10:01:00Z")
            .And.Contain("4bf92f3577b34da6a3ce929d0e0e4736, 00f067aa0ba902b7aaaaaaaaaaaaaaaa")
            .And.Contain("System.InvalidOperationException: db down");
    }

    [Fact]
    public void Slow_routes_show_percentiles()
    {
        var markdown = SystemDigestMarkdown.Render(Digest(routes:
        [
            new RouteLatency("api/v{version:apiVersion}/Providers/search", 1200, 3, 45.25, 480, 2100),
        ]));

        markdown.Should().Contain("| api/v{version:apiVersion}/Providers/search | 1200 | 3 | 45.3 | 480 | 2100 |");
    }

    [Fact]
    public void An_empty_window_says_so_instead_of_showing_empty_tables()
    {
        var markdown = SystemDigestMarkdown.Render(Digest());

        markdown.Should().Contain("No stored events in this window.")
            .And.Contain("No warnings or errors in this window.")
            .And.Contain("No request events in this window.");
    }

    [Fact]
    public void Table_cells_cannot_break_the_table()
    {
        var markdown = SystemDigestMarkdown.Render(Digest(groups:
        [
            new ErrorGroup("Warning", "A|B", "line one\nline two", null, 1, From, From, []),
        ]));

        markdown.Should().Contain("A\\|B").And.Contain("line one line two");
    }

    [Fact]
    public void Cache_hit_ratios_and_log_store_health_are_included()
    {
        var markdown = SystemDigestMarkdown.Render(Digest(levels: [new LevelCount("Information", 400)]));

        markdown.Should().Contain("| GetProviderByIdQuery | 100 | 90% |")
            .And.Contain("Written 490, dropped 10")
            .And.Contain("| Information | 400 |");
    }
}
