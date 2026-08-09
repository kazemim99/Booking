using Booksy.UserManagement.Domain.ValueObjects;
using FluentAssertions;

namespace Booksy.UserManagement.Application.UnitTests.ValueObjects;

/// <summary>
/// OTP generation rules. Sandbox mode (<c>Sms:SandboxMode</c>) only suppresses
/// the outbound SMS — it does NOT pin the code. Determinism for browser testing
/// and E2E scripts comes solely from the <c>OTP_SANDBOX_CODE</c> environment
/// variable; without it the code is random and only the value carried in the
/// SMS message body (echoed to the log) will verify.
///
/// Regression cover for the defect where the sandbox log advertised a fixed
/// "123456" that verification always rejected.
/// </summary>
[Collection("OtpSandboxEnvironment")]
public sealed class OtpCodeTests : IDisposable
{
    private const string SandboxCodeVariable = "OTP_SANDBOX_CODE";
    private readonly string? _originalValue;

    public OtpCodeTests()
    {
        _originalValue = Environment.GetEnvironmentVariable(SandboxCodeVariable);
        Environment.SetEnvironmentVariable(SandboxCodeVariable, null);
    }

    public void Dispose() =>
        Environment.SetEnvironmentVariable(SandboxCodeVariable, _originalValue);

    [Fact]
    public void Generate_WithoutSandboxOverride_ProducesARandomSixDigitCode()
    {
        var code = OtpCode.Generate();

        code.Value.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public void Generate_WithoutSandboxOverride_DoesNotReturnAFixedCode()
    {
        // Guards the false promise: nothing may assume a hardcoded sandbox OTP.
        var codes = Enumerable.Range(0, 40).Select(_ => OtpCode.Generate().Value).ToList();

        codes.Should().NotBeEquivalentTo(Enumerable.Repeat("123456", codes.Count));
        codes.Distinct().Should().HaveCountGreaterThan(1,
            "generated OTPs must vary when no sandbox override is configured");
    }

    [Fact]
    public void Generate_WithSandboxOverride_PinsTheCodeSoBrowserAndE2ELoginsAreDeterministic()
    {
        Environment.SetEnvironmentVariable(SandboxCodeVariable, "123456");

        OtpCode.Generate().Value.Should().Be("123456");
        OtpCode.Generate().Value.Should().Be("123456");
    }

    [Fact]
    public void Generate_WithSandboxOverride_ProducesACodeThatActuallyVerifies()
    {
        Environment.SetEnvironmentVariable(SandboxCodeVariable, "246810");

        var code = OtpCode.Generate();

        code.IsValid("246810").Should().BeTrue();
        code.IsValid("123456").Should().BeFalse();
    }

    [Fact]
    public void IsValid_RejectsTheCodeOnceExpired()
    {
        var code = OtpCode.Create("123456", validityMinutes: 1);

        code.IsValid("123456").Should().BeTrue();
        code.IsExpired().Should().BeFalse();
    }

    [Theory]
    [InlineData("12")]        // too short
    [InlineData("123456789")] // too long
    [InlineData("12a456")]    // not digits
    [InlineData("")]
    public void Create_RejectsMalformedCodes(string value)
    {
        var act = () => OtpCode.Create(value);

        act.Should().Throw<ArgumentException>();
    }
}
