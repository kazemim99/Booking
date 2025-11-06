using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.FinancialValueObjects;

public class CommissionRateTests
{
    [Fact]
    public void CreatePercentage_Should_Create_Percentage_Based_Commission()
    {
        // Arrange & Act
        var commission = CommissionRate.CreatePercentage(15m);

        // Assert
        Assert.Equal(CommissionType.Percentage, commission.Type);
        Assert.Equal(15m, commission.Percentage);
        Assert.Equal(Money.Zero("USD"), commission.FixedAmount);
    }

    [Fact]
    public void CreateFixed_Should_Create_Fixed_Amount_Commission()
    {
        // Arrange
        var fixedAmount = Money.Create(25, "USD");

        // Act
        var commission = CommissionRate.CreateFixed(fixedAmount);

        // Assert
        Assert.Equal(CommissionType.Fixed, commission.Type);
        Assert.Equal(0m, commission.Percentage);
        Assert.Equal(fixedAmount, commission.FixedAmount);
    }

    [Fact]
    public void CreateMixed_Should_Create_Combined_Commission()
    {
        // Arrange
        var fixedAmount = Money.Create(10, "USD");

        // Act
        var commission = CommissionRate.CreateMixed(5m, fixedAmount);

        // Assert
        Assert.Equal(CommissionType.Mixed, commission.Type);
        Assert.Equal(5m, commission.Percentage);
        Assert.Equal(fixedAmount, commission.FixedAmount);
    }

    [Fact]
    public void Default_Should_Return_15_Percent_Commission()
    {
        // Arrange & Act
        var commission = CommissionRate.Default;

        // Assert
        Assert.Equal(CommissionType.Percentage, commission.Type);
        Assert.Equal(15m, commission.Percentage);
    }

    [Fact]
    public void CalculateCommission_Percentage_Should_Calculate_Correctly()
    {
        // Arrange
        var commission = CommissionRate.CreatePercentage(15m);
        var grossAmount = Money.Create(1000, "USD");

        // Act
        var result = commission.CalculateCommission(grossAmount);

        // Assert
        Assert.Equal(150m, result.Amount); // 15% of 1000
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void CalculateCommission_Fixed_Should_Return_Fixed_Amount()
    {
        // Arrange
        var fixedAmount = Money.Create(50, "USD");
        var commission = CommissionRate.CreateFixed(fixedAmount);
        var grossAmount = Money.Create(1000, "USD");

        // Act
        var result = commission.CalculateCommission(grossAmount);

        // Assert
        Assert.Equal(50m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void CalculateCommission_Mixed_Should_Combine_Percentage_And_Fixed()
    {
        // Arrange
        var fixedAmount = Money.Create(25, "USD");
        var commission = CommissionRate.CreateMixed(10m, fixedAmount);
        var grossAmount = Money.Create(1000, "USD");

        // Act
        var result = commission.CalculateCommission(grossAmount);

        // Assert
        Assert.Equal(125m, result.Amount); // (10% of 1000) + 25 = 100 + 25
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void CalculateNetAmount_Should_Subtract_Commission_From_Gross()
    {
        // Arrange
        var commission = CommissionRate.CreatePercentage(15m);
        var grossAmount = Money.Create(1000, "USD");

        // Act
        var netAmount = commission.CalculateNetAmount(grossAmount);

        // Assert
        Assert.Equal(850m, netAmount.Amount); // 1000 - 150
        Assert.Equal("USD", netAmount.Currency);
    }

    [Fact]
    public void CreatePercentage_Should_Throw_When_Percentage_Is_Negative()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => CommissionRate.CreatePercentage(-5m));
        Assert.Contains("percentage", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreatePercentage_Should_Throw_When_Percentage_Exceeds_100()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => CommissionRate.CreatePercentage(105m));
        Assert.Contains("percentage", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateFixed_Should_Throw_When_Amount_Is_Negative()
    {
        // Arrange
        var negativeAmount = Money.Create(-10, "USD");

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => CommissionRate.CreateFixed(negativeAmount));
        Assert.Contains("fixed amount", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0, 1000, 0)]
    [InlineData(10, 1000, 100)]
    [InlineData(20, 500, 100)]
    [InlineData(25, 400, 100)]
    [InlineData(50, 200, 100)]
    public void CalculateCommission_Should_Calculate_Various_Percentages_Correctly(
        decimal percentage, decimal amount, decimal expectedCommission)
    {
        // Arrange
        var commission = CommissionRate.CreatePercentage(percentage);
        var grossAmount = Money.Create(amount, "USD");

        // Act
        var result = commission.CalculateCommission(grossAmount);

        // Assert
        Assert.Equal(expectedCommission, result.Amount);
    }
}
