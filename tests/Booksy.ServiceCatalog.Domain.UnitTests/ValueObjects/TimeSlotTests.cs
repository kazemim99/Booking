using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ValueObjects;

public class TimeSlotTests
{
    [Fact]
    public void Create_Should_Create_TimeSlot_With_Start_And_End()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = startTime.AddHours(1);

        // Act
        var timeSlot = TimeSlot.Create(startTime, endTime);

        // Assert
        Assert.NotNull(timeSlot);
        Assert.Equal(startTime, timeSlot.StartTime);
        Assert.Equal(endTime, timeSlot.EndTime);
        Assert.Equal(60, timeSlot.Duration.Value);
    }

    [Fact]
    public void Create_Should_Create_TimeSlot_With_Duration()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var duration = Duration.FromMinutes(90);

        // Act
        var timeSlot = TimeSlot.Create(startTime, duration);

        // Assert
        Assert.NotNull(timeSlot);
        Assert.Equal(startTime, timeSlot.StartTime);
        Assert.Equal(startTime.AddMinutes(90), timeSlot.EndTime);
        Assert.Equal(90, timeSlot.Duration.Value);
    }

    [Fact]
    public void Create_Should_Throw_When_Start_Is_After_End()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = startTime.AddHours(-1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeSlot.Create(startTime, endTime));
    }

    [Fact]
    public void Create_Should_Throw_When_Start_Equals_End()
    {
        // Arrange
        var time = DateTime.UtcNow.AddHours(2);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TimeSlot.Create(time, time));
    }

    // Note: Past dates are now allowed for TimeSlots to support historical bookings in the database
    // The validation of booking timing rules is handled at the Booking aggregate level, not TimeSlot

    [Fact]
    public void OverlapsWith_Should_Return_True_For_Overlapping_Slots()
    {
        // Arrange
        var slot1 = TimeSlot.Create(
            DateTime.UtcNow.AddHours(2),
            DateTime.UtcNow.AddHours(4));

        var slot2 = TimeSlot.Create(
            DateTime.UtcNow.AddHours(3),
            DateTime.UtcNow.AddHours(5));

        // Act & Assert
        Assert.True(slot1.OverlapsWith(slot2));
        Assert.True(slot2.OverlapsWith(slot1));
    }

    [Fact]
    public void OverlapsWith_Should_Return_False_For_Non_Overlapping_Slots()
    {
        // Arrange
        var slot1 = TimeSlot.Create(
            DateTime.UtcNow.AddHours(2),
            DateTime.UtcNow.AddHours(3));

        var slot2 = TimeSlot.Create(
            DateTime.UtcNow.AddHours(4),
            DateTime.UtcNow.AddHours(5));

        // Act & Assert
        Assert.False(slot1.OverlapsWith(slot2));
        Assert.False(slot2.OverlapsWith(slot1));
    }

    [Fact]
    public void OverlapsWith_Should_Return_True_For_Contained_Slot()
    {
        // Arrange
        var outerSlot = TimeSlot.Create(
            DateTime.UtcNow.AddHours(2),
            DateTime.UtcNow.AddHours(5));

        var innerSlot = TimeSlot.Create(
            DateTime.UtcNow.AddHours(3),
            DateTime.UtcNow.AddHours(4));

        // Act & Assert
        Assert.True(outerSlot.OverlapsWith(innerSlot));
        Assert.True(innerSlot.OverlapsWith(outerSlot));
    }

    [Fact]
    public void OverlapsWith_Should_Return_False_For_Adjacent_Slots()
    {
        // Arrange
        var endTime = DateTime.UtcNow.AddHours(3);
        var slot1 = TimeSlot.Create(
            DateTime.UtcNow.AddHours(2),
            endTime);

        var slot2 = TimeSlot.Create(
            endTime,
            DateTime.UtcNow.AddHours(4));

        // Act & Assert
        Assert.False(slot1.OverlapsWith(slot2));
        Assert.False(slot2.OverlapsWith(slot1));
    }

    [Fact]
    public void Contains_Should_Return_True_For_Time_Within_Slot()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = DateTime.UtcNow.AddHours(4);
        var timeSlot = TimeSlot.Create(startTime, endTime);
        var midTime = DateTime.UtcNow.AddHours(3);

        // Act & Assert
        Assert.True(timeSlot.Contains(midTime));
    }

    [Fact]
    public void Contains_Should_Return_True_For_Start_Time()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = DateTime.UtcNow.AddHours(4);
        var timeSlot = TimeSlot.Create(startTime, endTime);

        // Act & Assert
        Assert.True(timeSlot.Contains(startTime));
    }

    [Fact]
    public void Contains_Should_Return_False_For_End_Time()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = DateTime.UtcNow.AddHours(4);
        var timeSlot = TimeSlot.Create(startTime, endTime);

        // Act & Assert
        Assert.False(timeSlot.Contains(endTime)); // End is exclusive
    }

    [Fact]
    public void Contains_Should_Return_False_For_Time_Before_Slot()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var timeSlot = TimeSlot.Create(startTime, DateTime.UtcNow.AddHours(4));
        var beforeTime = DateTime.UtcNow.AddHours(1);

        // Act & Assert
        Assert.False(timeSlot.Contains(beforeTime));
    }

    [Fact]
    public void Contains_Should_Return_False_For_Time_After_Slot()
    {
        // Arrange
        var timeSlot = TimeSlot.Create(DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(4));
        var afterTime = DateTime.UtcNow.AddHours(5);

        // Act & Assert
        Assert.False(timeSlot.Contains(afterTime));
    }

    [Fact]
    public void IsAdjacentTo_Should_Return_True_For_Back_To_Back_Slots()
    {
        // Arrange
        var midTime = DateTime.UtcNow.AddHours(3);
        var slot1 = TimeSlot.Create(DateTime.UtcNow.AddHours(2), midTime);
        var slot2 = TimeSlot.Create(midTime, DateTime.UtcNow.AddHours(4));

        // Act & Assert
        Assert.True(slot1.IsAdjacentTo(slot2));
        Assert.True(slot2.IsAdjacentTo(slot1));
    }

    [Fact]
    public void IsAdjacentTo_Should_Return_False_For_Overlapping_Slots()
    {
        // Arrange
        var slot1 = TimeSlot.Create(DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(4));
        var slot2 = TimeSlot.Create(DateTime.UtcNow.AddHours(3), DateTime.UtcNow.AddHours(5));

        // Act & Assert
        Assert.False(slot1.IsAdjacentTo(slot2));
    }

    [Fact]
    public void IsAdjacentTo_Should_Return_False_For_Slots_With_Gap()
    {
        // Arrange
        var slot1 = TimeSlot.Create(DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3));
        var slot2 = TimeSlot.Create(DateTime.UtcNow.AddHours(4), DateTime.UtcNow.AddHours(5));

        // Act & Assert
        Assert.False(slot1.IsAdjacentTo(slot2));
    }

    [Fact]
    public void WithBuffer_Should_Extend_End_Time()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = DateTime.UtcNow.AddHours(3);
        var timeSlot = TimeSlot.Create(startTime, endTime);
        var buffer = Duration.FromMinutes(15);

        // Act
        var bufferedSlot = timeSlot.WithBuffer(buffer);

        // Assert
        Assert.Equal(startTime, bufferedSlot.StartTime);
        Assert.Equal(endTime.AddMinutes(15), bufferedSlot.EndTime);
        Assert.Equal(75, bufferedSlot.Duration.Value); // 60 + 15
    }

    [Fact]
    public void TwoTimeSlots_With_Same_Times_Should_Be_Equal()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = DateTime.UtcNow.AddHours(3);
        var slot1 = TimeSlot.Create(startTime, endTime);
        var slot2 = TimeSlot.Create(startTime, endTime);

        // Act & Assert
        Assert.Equal(slot1, slot2);
    }

    [Fact]
    public void TwoTimeSlots_With_Different_Times_Should_Not_Be_Equal()
    {
        // Arrange
        var slot1 = TimeSlot.Create(DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3));
        var slot2 = TimeSlot.Create(DateTime.UtcNow.AddHours(3), DateTime.UtcNow.AddHours(4));

        // Act & Assert
        Assert.NotEqual(slot1, slot2);
    }

    [Fact]
    public void ToString_Should_Return_Readable_Format()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var endTime = startTime.AddMinutes(90);
        var timeSlot = TimeSlot.Create(startTime, endTime);

        // Act
        var result = timeSlot.ToString();

        // Assert
        Assert.Contains("10:00", result);
        Assert.Contains("11:30", result);
        Assert.Contains("1h 30m", result);
    }
}
