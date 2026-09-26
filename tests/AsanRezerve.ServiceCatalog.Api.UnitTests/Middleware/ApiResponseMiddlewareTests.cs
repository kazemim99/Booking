using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AsanRezerve.Core.Domain.Infrastructure.Configuration;
using AsanRezerve.Core.Domain.Infrastructure.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AsanRezerve.ServiceCatalog.Api.UnitTests.Middleware;

/// <summary>
/// The success envelope every <c>/api</c> response is wrapped in. It used to parse the controller's JSON into an
/// object and serialise it again — twice the serialisation work on every response, escaping every Persian
/// character as <c>\uXXXX</c> on the way. It now embeds the controller's bytes as they are. The envelope's shape
/// is a contract for four client apps and must not change.
/// </summary>
public class ApiResponseMiddlewareTests
{
    private static async Task<(HttpContext Context, string Body)> Run(
        Func<HttpContext, Task> endpoint, string path = "/api/v1/providers/1")
    {
        var middleware = new ApiResponseMiddleware(
            ctx => endpoint(ctx),
            NullLogger<ApiResponseMiddleware>.Instance,
            Options.Create(new ApiResponseOptions()));

        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "GET";
        context.TraceIdentifier = "req-1";
        var body = new MemoryStream();
        context.Response.Body = body;

        await middleware.InvokeAsync(context);
        return (context, Encoding.UTF8.GetString(body.ToArray()));
    }

    private static Func<HttpContext, Task> Writes(string content, int status = 200, string contentType = "application/json; charset=utf-8") =>
        async ctx =>
        {
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = contentType;
            await ctx.Response.WriteAsync(content);
        };

    [Fact]
    public async Task The_envelope_keeps_its_shape()
    {
        var (_, body) = await Run(Writes("""{"id":7,"name":"x","tags":["a"],"nested":{"ok":true},"missing":null}"""));

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        root.EnumerateObject().Select(p => p.Name).Should().Equal("success", "statusCode", "message", "data", "metadata");
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("statusCode").GetInt32().Should().Be(200);
        root.GetProperty("message").GetString().Should().Be("Request completed successfully");
        root.GetProperty("data").GetRawText().Should().Be("""{"id":7,"name":"x","tags":["a"],"nested":{"ok":true},"missing":null}""");
        var metadata = root.GetProperty("metadata");
        metadata.EnumerateObject().Select(p => p.Name).Should()
            .Equal("requestId", "traceId", "timestamp", "duration", "path", "method", "version");
        metadata.GetProperty("requestId").GetString().Should().Be("req-1");
        metadata.GetProperty("path").GetString().Should().Be("/api/v1/providers/1");
    }

    [Fact]
    public async Task Persian_text_is_written_as_utf8_not_escaped()
    {
        var (_, body) = await Run(Writes("""{"businessName":"سالن نهال"}"""));

        body.Should().Contain("سالن نهال").And.NotContain("\\u0633");
    }

    [Fact]
    public async Task A_201_says_created()
    {
        var (_, body) = await Run(Writes("""{"id":1}""", status: 201));

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("statusCode").GetInt32().Should().Be(201);
        json.RootElement.GetProperty("message").GetString().Should().Be("Resource created successfully");
    }

    [Fact]
    public async Task A_body_that_is_not_json_becomes_a_string()
    {
        var (_, body) = await Run(Writes("plain text", contentType: "text/plain"));

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("data").GetString().Should().Be("plain text");
    }

    [Fact]
    public async Task An_empty_body_has_no_data_property()
    {
        var (_, body) = await Run(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        using var json = JsonDocument.Parse(body);
        json.RootElement.TryGetProperty("data", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(204, "")]
    [InlineData(404, """{"message":"nope"}""")]
    [InlineData(400, """{"error":"bad"}""")]
    public async Task Non_2xx_and_204_responses_pass_through_untouched(int status, string content)
    {
        var (context, body) = await Run(Writes(content, status));

        body.Should().Be(content);
        context.Response.StatusCode.Should().Be(status);
    }

    [Fact]
    public async Task Paths_outside_the_api_pass_through()
    {
        var (_, body) = await Run(Writes("""{"a":1}"""), path: "/health");

        body.Should().Be("""{"a":1}""");
    }

    [Fact]
    public async Task The_log_export_streams_unwrapped()
    {
        var (_, body) = await Run(Writes("{\"a\":1}\n{\"a\":2}\n", contentType: "application/x-ndjson"),
            path: "/api/v1/admin/observability/logs/export");

        body.Should().Be("{\"a\":1}\n{\"a\":2}\n");
    }

    [Fact]
    public async Task The_envelope_carries_the_trace_id()
    {
        using var activity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();

        var (_, body) = await Run(Writes("""{"id":1}"""));

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("metadata").GetProperty("traceId").GetString().Should().Be(activity.TraceId.ToHexString());
    }

    [Fact]
    public async Task The_content_length_matches_the_wrapped_body()
    {
        var (context, body) = await Run(async ctx =>
        {
            ctx.Response.ContentLength = 8;
            await ctx.Response.WriteAsync("""{"id":1}""");
        });

        context.Response.ContentLength.Should().Be(Encoding.UTF8.GetByteCount(body));
    }
}
