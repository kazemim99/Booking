using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.FinancialValueObjects;

public class TaxRateTests
{
    [Fact]
    public void Create_Should_Create_Tax_Rate_With_Exclusive_Mode()
    {
        // Arrange & Act
        var taxRate = TaxRate.Create(10m, "Sales Tax", "ST", isInclusive: false);

        // Assert
        Assert.Equal(10m, taxRate.Percentage);
        Assert.Equal("Sales Tax", taxRate.Name);
        Assert.Equal("ST", taxRate.Code);
        Assert.False(taxRate.IsInclusive);
    }

    [Fact]
    public void Create_Should_Create_Tax_Rate_With_Inclusive_Mode()
    {
        // Arrange & Act
        var taxRate = TaxRate.Create(20m, "VAT", "VAT", isInclusive: true);

        // Assert
        Assert.Equal(20m, taxRate.Percentage);
        Assert.Equal("VAT", taxRate.Name);
        Assert.Equal("VAT", taxRate.Code);
        Assert.True(taxRate.IsInclusive);
    }

    [Fact]
    public void CalculateTaxAmount_Exclusive_Should_Calculate_Tax_On_Top_Of_Base()
    {
        // Arrange
        var taxRate = TaxRate.Create(10m, "Sales Tax", "ST", isInclusive: false);
        var baseAmount = Money.Create(100, "USD");

        // Act
        var taxAmount = taxRate.CalculateTaxAmount(baseAmount);

        // Assert
        Assert.Equal(10m, taxAmount.Amount); // 10% of 100
        Assert.Equal("USD", taxAmount.Currency);
    }

    [Fact]
    public void CalculateTaxAmount_Inclusive_Should_Extract_Tax_From_Total()
    {
        // Arrange
        var taxRate = TaxRate.Create(20m, "VAT", "VAT", isInclusive: true);
        var totalAmount = Money.Create(120, "USD");

        // Act
        var taxAmount = taxRate.CalculateTaxAmount(totalAmount);

        // Assert
        Assert.Equal(20m, taxAmount.Amount); // Tax portion of 120 with 20% VAT = 20
        Assert.Equal("USD", taxAmount.Currency);
    }

    [Fact]
    public void CalculateTotalWithTax_Exclusive_Should_Add_Tax_To_Base()
    {
        // Arrange
        var taxRate = TaxRate.Create(10m, "Sales Tax", "ST", isInclusive: false);
        var baseAmount = Money.Create(100, "USD");

        // Act
        var totalAmount = taxRate.CalculateTotalWithTax(baseAmount);

        // Assert
        Assert.Equal(110m, totalAmount.Amount); // 100 + 10
        Assert.Equal("USD", totalAmount.Currency);
    }

    [Fact]
    public void CalculateTotalWithTax_Inclusive_Should_Return_Same_Amount()
    {
        // Arrange
        var taxRate = TaxRate.Create(20m, "VAT", "VAT", isInclusive: true);
        var totalAmount = Money.Create(120, "USD");

        // Act
        var result = taxRate.CalculateTotalWithTax(totalAmount);

        // Assert
        Assert.Equal(120m, result.Amount); // Same amount for inclusive tax
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void CalculateBaseAmount_Exclusive_Should_Return_Same_Amount()
    {
        // Arrange
        var taxRate = TaxRate.Create(10m, "Sales Tax", "ST", isInclusive: false);
        var baseAmount = Money.Create(100, "USD");

        // Act
        var result = taxRate.CalculateBaseAmount(baseAmount);

        // Assert
        Assert.Equal(100m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void CalculateBaseAmount_Inclusive_Should_Extract_Base_From_Total()
    {
        // Arrange
        var taxRate = TaxRate.Create(20m, "VAT", "VAT", isInclusive: true);
        var totalAmount = Money.Create(120, "USD");

        // Act
        var baseAmount = taxRate.CalculateBaseAmount(totalAmount);

        // Assert
        Assert.Equal(100m, baseAmount.Amount); // 120 / 1.20 = 100
        Assert.Equal("USD", baseAmount.Currency);
    }

    [Fact]
    public void Create_Should_Throw_When_Percentage_Is_Negative()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            TaxRate.Create(-5m, "Invalid Tax", "IT", false));
        Assert.Contains("percentage", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_Should_Throw_When_Percentage_Exceeds_100()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            TaxRate.Create(105m, "Invalid Tax", "IT", false));
        Assert.Contains("percentage", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_Should_Throw_When_Name_Is_Empty()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            TaxRate.Create(10m, "", "ST", false));
        Assert.Contains("name", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_Should_Throw_When_Code_Is_Empty()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            TaxRate.Create(10m, "Sales Tax", "", false));
        Assert.Contains("code", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(5, 100, false, 5)]      // 5% exclusive on 100 = 5
    [InlineData(10, 100, false, 10)]    // 10% exclusive on 100 = 10
    [InlineData(20, 100, false, 20)]    // 20% exclusive on 100 = 20
    [InlineData(20, 120, true, 20)]     // 20% inclusive of 120 = 20
    [InlineData(10, 110, true, 10)]     // 10% inclusive of 110 = 10
    public void CalculateTaxAmount_Should_Calculate_Various_Scenarios_Correctly(
        decimal taxPercentage, decimal amount, bool isInclusive, decimal expectedTax)
    {
        // Arrange
        var taxRate = TaxRate.Create(taxPercentage, "Tax", "TX", isInclusive);
        var money = Money.Create(amount, "USD");

        // Act
        var taxAmount = taxRate.CalculateTaxAmount(money);

        // Assert
        Assert.Equal(expectedTax, taxAmount.Amount);
    }

    [Theory]
    [InlineData(10, 100, false, 110)]   // 10% exclusive: 100 + 10 = 110
    [InlineData(20, 100, false, 120)]   // 20% exclusive: 100 + 20 = 120
    [InlineData(20, 120, true, 120)]    // 20% inclusive: stays 120
    [InlineData(10, 110, true, 110)]    // 10% inclusive: stays 110
    public void CalculateTotalWithTax_Should_Calculate_Various_Scenarios_Correctly(
        decimal taxPercentage, decimal amount, bool isInclusive, decimal expectedTotal)
    {
        // Arrange
        var taxRate = TaxRate.Create(taxPercentage, "Tax", "TX", isInclusive);
        var money = Money.Create(amount, "USD");

        // Act
        var totalAmount = taxRate.CalculateTotalWithTax(money);

        // Assert
        Assert.Equal(expectedTotal, totalAmount.Amount);
    }
}
