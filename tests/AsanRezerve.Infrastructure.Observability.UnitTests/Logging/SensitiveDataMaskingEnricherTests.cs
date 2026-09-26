using AsanRezerve.Infrastructure.Observability.Logging.Masking;
using FluentAssertions;
using Serilog;
using Serilog.Events;

namespace AsanRezerve.Infrastructure.Observability.UnitTests.Logging;

/// <summary>
/// Every log event passes one masking step before any sink sees it (system-logging: "Secrets and personal data
/// are masked before any sink").
/// <para>The review that opened add-observability-and-caching found passwords, OTP codes and refresh tokens in the
/// logs: <c>LoggingBehavior</c> wrote every command in full, at Information. Masking at the call sites alone
/// cannot hold — the next command with a <c>Password</c> property would leak again — so it happens once, here,
/// by property name at any depth of a destructured object, and by value for phone numbers and e-mails.</para>
/// </summary>
public sealed class SensitiveDataMaskingEnricherTests
{
    private readonly CollectingSink _sink = new();

    private ILogger Logger() =>
        new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.With(new SensitiveDataMaskingEnricher())
            .WriteTo.Sink(_sink)
            .CreateLogger();

    private static object? Scalar(LogEventPropertyValue value) => ((ScalarValue)value).Value;

    private static IReadOnlyDictionary<string, LogEventPropertyValue> Fields(LogEventPropertyValue value) =>
        ((StructureValue)value).Properties.ToDictionary(p => p.Name, p => p.Value);

    public sealed record AuthenticateUserCommand(string Email, string Password, string? TwoFactorCode);

    public sealed record Nested(string Name, AuthenticateUserCommand Inner, string[] Tokens);

    [Theory]
    [InlineData("Password")]
    [InlineData("NewPassword")]
    [InlineData("password_hash")]
    [InlineData("Code")]
    [InlineData("OtpCode")]
    [InlineData("VerificationCode")]
    [InlineData("TwoFactorCode")]
    [InlineData("Token")]
    [InlineData("RefreshToken")]
    [InlineData("AccessToken")]
    [InlineData("ClientSecret")]
    [InlineData("ApiKey")]
    [InlineData("Authorization")]
    [InlineData("CardNumber")]
    [InlineData("Cvv2")]
    [InlineData("Pin")]
    public void A_secret_is_redacted_whatever_its_value(string property)
    {
        Logger().Information("Value {" + property + "}", "123456");

        Scalar(_sink.Single().Properties[property]).Should().Be("***");
    }

    [Fact]
    public void A_numeric_secret_is_redacted_too()
    {
        Logger().Information("Verifying {Code}", 123456);

        Scalar(_sink.Single().Properties["Code"]).Should().Be("***");
    }

    [Fact]
    public void The_rendered_message_carries_the_masked_value()
    {
        Logger().Information("Login for {Email} with {Password}", "ali@example.com", "Secret!123");

        _sink.Single().RenderMessage().Should().Be("Login for \"a***@example.com\" with \"***\"");
    }

    [Fact]
    public void A_destructured_command_is_masked_inside()
    {
        Logger().Information("Handling {@Request}", new AuthenticateUserCommand("ali@example.com", "Secret!123", "654321"));

        var fields = Fields(_sink.Single().Properties["Request"]);
        Scalar(fields["Password"]).Should().Be("***");
        Scalar(fields["TwoFactorCode"]).Should().Be("***");
        Scalar(fields["Email"]).Should().Be("a***@example.com");
    }

    [Fact]
    public void Masking_reaches_nested_objects_and_sequences()
    {
        Logger().Information("Handling {@Request}",
            new Nested("x", new AuthenticateUserCommand("b@c.ir", "p", null), ["t1", "t2"]));

        var fields = Fields(_sink.Single().Properties["Request"]);
        Scalar(Fields(fields["Inner"])["Password"]).Should().Be("***");
        ((SequenceValue)fields["Tokens"]).Elements.Select(Scalar).Should().Equal("***", "***");
        Scalar(fields["Name"]).Should().Be("x");
    }

    [Fact]
    public void Dictionary_entries_are_masked_by_key()
    {
        Logger().Information("Headers {@Headers}", new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer abc.def",
            ["Accept"] = "application/json",
        });

        var elements = ((DictionaryValue)_sink.Single().Properties["Headers"]).Elements
            .ToDictionary(kv => (string)kv.Key.Value!, kv => Scalar(kv.Value));
        elements["Authorization"].Should().Be("***");
        elements["Accept"].Should().Be("application/json");
    }

    [Theory]
    [InlineData("PhoneNumber", "09121234567", "0912*****67")]
    [InlineData("Phone", "+989121234567", "+989*******67")]
    [InlineData("MobileNumber", "09351112233", "0935*****33")]
    public void A_phone_number_keeps_only_its_prefix_and_last_two_digits(string property, string value, string masked)
    {
        Logger().Information("Sending to {" + property + "}", value);

        Scalar(_sink.Single().Properties[property]).Should().Be(masked);
    }

    [Theory]
    [InlineData("To")]
    [InlineData("Recipient")]
    [InlineData("Destination")]
    [InlineData("UserName")]
    public void A_mobile_number_is_masked_whatever_the_property_is_called(string property)
    {
        Logger().Information("Sending to {" + property + "}", "09121234567");

        Scalar(_sink.Single().Properties[property]).Should().Be("0912*****67");
    }

    [Fact]
    public void An_email_is_masked_whatever_the_property_is_called()
    {
        Logger().Information("Mail to {Recipient}", "sara.m@example.co");

        Scalar(_sink.Single().Properties["Recipient"]).Should().Be("s***@example.co");
    }

    [Fact]
    public void An_already_masked_value_is_left_alone()
    {
        Logger().Information("OTP sent to {MaskedPhone}", "0912***4567");

        Scalar(_sink.Single().Properties["MaskedPhone"]).Should().Be("0912***4567");
    }

    [Theory]
    [InlineData("StatusCode", 404)]
    [InlineData("PostalCode", "1234567890")]
    [InlineData("PromotionCode", "NOWRUZ1405")]
    [InlineData("ErrorCode", "PROVIDER_NOT_FOUND")]
    [InlineData("ProviderId", "0b8f3c3e-6f5a-4f7e-9b0e-3a2d1c4b5a69")]
    [InlineData("ElapsedMs", 12.5)]
    public void What_is_not_a_secret_stays_readable(string property, object value)
    {
        Logger().Information("Value {" + property + "}", value);

        Scalar(_sink.Single().Properties[property]).Should().Be(value);
    }

    [Fact]
    public void A_short_number_is_not_mistaken_for_a_phone()
    {
        Logger().Information("Found {Count} rows in {Table}", 9121234, "Bookings");

        Scalar(_sink.Single().Properties["Count"]).Should().Be(9121234);
    }
}
