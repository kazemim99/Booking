using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ValueObjects;

public class BookingIdTests
{
    [Fact]
    public void New_Should_Create_BookingId_With_Valid_Guid()
    {
        // Act
        var bookingId = BookingId.New();

        // Assert
        Assert.NotNull(bookingId);
        Assert.NotEqual(Guid.Empty, bookingId.Value);
    }

    [Fact]
    public void From_Should_Create_BookingId_From_Guid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var bookingId = BookingId.From(guid);

        // Assert
        Assert.Equal(guid, bookingId.Value);
    }

    [Fact]
    public void From_Should_Create_BookingId_From_String()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString();

        // Act
        var bookingId = BookingId.From(guidString);

        // Assert
        Assert.Equal(guid, bookingId.Value);
    }

    [Fact]
    public void From_Should_Throw_When_Guid_Is_Empty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => BookingId.From(Guid.Empty));
    }

    [Fact]
    public void ToString_Should_Return_Guid_String()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var bookingId = BookingId.From(guid);

        // Act
        var result = bookingId.ToString();

        // Assert
        Assert.Equal(guid.ToString(), result);
    }

    [Fact]
    public void TwoBookingIds_With_Same_Value_Should_Be_Equal()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = BookingId.From(guid);
        var id2 = BookingId.From(guid);

        // Act & Assert
        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
    }

    [Fact]
    public void TwoBookingIds_With_Different_Values_Should_Not_Be_Equal()
    {
        // Arrange
        var id1 = BookingId.New();
        var id2 = BookingId.New();

        // Act & Assert
        Assert.NotEqual(id1, id2);
        Assert.False(id1 == id2);
    }

    [Fact]
    public void ImplicitConversion_To_Guid_Should_Work()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var bookingId = BookingId.From(guid);

        // Act
        Guid convertedGuid = bookingId;

        // Assert
        Assert.Equal(guid, convertedGuid);
    }

    [Fact]
    public void ImplicitConversion_From_Guid_Should_Work()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        BookingId bookingId = guid;

        // Assert
        Assert.Equal(guid, bookingId.Value);
    }
}
