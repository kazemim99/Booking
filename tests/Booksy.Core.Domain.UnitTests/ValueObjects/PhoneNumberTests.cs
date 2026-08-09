using Booksy.Core.Domain.ValueObjects;
using FluentAssertions;

namespace Booksy.Core.Domain.UnitTests.ValueObjects;

/// <summary>
/// <see cref="PhoneNumber"/> is the canonical representation of a phone number
/// across the platform: it is persisted (users, phone verifications) and every
/// lookup compares against it. The same real-world number therefore MUST produce
/// one identical <see cref="PhoneNumber.Value"/> regardless of the input format
/// the caller happened to use (local <c>09…</c>, international <c>+98…</c>, the
/// <c>0098…</c> variant, or a country code concatenated onto a local number).
///
/// Regression cover for the auth defect where the Flutter apps (local format)
/// and the E2E scripts (E.164) produced two different user rows for one number,
/// colliding on the synthesized email with a 500 duplicate-key error.
/// </summary>
public sealed class PhoneNumberTests
{
    private const string ExpectedCanonical = "+989121234567";
    private const string ExpectedNational = "9121234567";

    public static TheoryData<string> EquivalentIranianFormats() => new()
    {
        "09121234567",       // local / trunk-zero form sent by the Flutter apps
        "+989121234567",     // E.164 form sent by the E2E scripts
        "989121234567",      // E.164 without the plus
        "00989121234567",    // international access-code variant
        "9121234567",        // bare national number
        "+98 912 123 4567",  // spaced
        "0912-123-4567",     // dashed
    };

    [Theory]
    [MemberData(nameof(EquivalentIranianFormats))]
    public void From_NormalizesEveryEquivalentFormat_ToTheSameCanonicalValue(string input)
    {
        var phone = PhoneNumber.From(input);

        phone.Value.Should().Be(ExpectedCanonical);
        phone.CountryCode.Should().Be("+98");
        phone.NationalNumber.Should().Be(ExpectedNational);
    }

    [Fact]
    public void From_TreatsLocalAndInternationalFormsAsEqual()
    {
        var local = PhoneNumber.From("09121234567");
        var international = PhoneNumber.From("+989121234567");

        // Value equality drives both persistence lookups and dedupe checks.
        local.Should().Be(international);
        local.Value.Should().Be(international.Value);
    }

    /// <summary>
    /// The original defect: callers concatenated a country code onto a local
    /// number (<c>"+98" + "09121234567"</c>), yielding <c>+9809121234567</c> —
    /// an invalid E.164 number that no lookup could match.
    /// </summary>
    [Fact]
    public void From_StripsTheTrunkZero_WhenCountryCodeWasConcatenatedOntoLocalNumber()
    {
        var phone = PhoneNumber.From("+98" + "09121234567");

        phone.Value.Should().Be(ExpectedCanonical);
        phone.NationalNumber.Should().Be(ExpectedNational);
        phone.NationalNumber.Should().NotStartWith("0");
    }

    [Fact]
    public void FromNational_AcceptsTrunkZeroAndBareNationalNumberAlike()
    {
        PhoneNumber.FromNational("09121234567").Value.Should().Be(ExpectedCanonical);
        PhoneNumber.FromNational("9121234567").Value.Should().Be(ExpectedCanonical);
    }

    [Fact]
    public void ToNational_ReturnsTheTrunkZeroFormForDisplay()
    {
        PhoneNumber.From("+989121234567").ToNational().Should().Be("09121234567");
    }

    [Fact]
    public void ToInternational_ReturnsTheCanonicalValue()
    {
        PhoneNumber.From("09121234567").ToInternational().Should().Be(ExpectedCanonical);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void From_RejectsEmptyInput(string? input)
    {
        var act = () => PhoneNumber.From(input!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("12345")]          // too short
    [InlineData("abcdefghij")]     // not digits
    [InlineData("+98812345678")]   // Iranian mobile must start with 9
    public void From_RejectsInvalidNumbers(string input)
    {
        var act = () => PhoneNumber.From(input);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EquivalentForms_CoversEveryFormatLegacyRowsMayAlreadyHold()
    {
        var forms = PhoneNumber.From("09121234567").EquivalentForms();

        forms.Should().Contain(new[]
        {
            "+989121234567", // canonical (written after the normalization fix)
            "989121234567",  // plus-less international
            "09121234567",   // legacy local, written by the mobile apps
            "9121234567",    // bare national
        });
    }

    [Fact]
    public void EquivalentForms_IsIdenticalRegardlessOfTheInputFormat()
    {
        var fromLocal = PhoneNumber.From("09121234567").EquivalentForms();
        var fromInternational = PhoneNumber.From("+989121234567").EquivalentForms();

        fromLocal.Should().BeEquivalentTo(fromInternational);
    }

    [Fact]
    public void From_KeepsNonIranianInternationalNumbersIntact()
    {
        // A UK mobile: the +98 trunk-zero rule must not corrupt other countries.
        var phone = PhoneNumber.From("+447911123456");

        phone.CountryCode.Should().Be("+44");
        phone.NationalNumber.Should().Be("7911123456");
        phone.Value.Should().Be("+447911123456");
    }
}
