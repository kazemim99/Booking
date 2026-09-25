using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Commands.Booking.AutoCompleteBooking;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.BackgroundJobs;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AsanRezerve.ServiceCatalog.Infrastructure.UnitTests.BackgroundJobs;

/// <summary>
/// One pass of the auto-completion job (openspec/changes/_inline/reviews-and-reschedule-round2 D3): it asks for the
/// confirmed bookings 12 hours past their end on the salon's clock, and completes each in its own command — one
/// failure does not stop the rest. The clock is passed in.
/// </summary>
public class AutoCompleteBookingsJobTests
{
    private static readonly DateTime UtcNow = new(2026, 9, 25, 20, 30, 0, DateTimeKind.Utc);

    private readonly IBookingReadRepository _bookings = Substitute.For<IBookingReadRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();

    private AutoCompleteBookingsJob Job()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_bookings);
        services.AddSingleton(_sender);
        var provider = services.BuildServiceProvider();
        return new AutoCompleteBookingsJob(
            provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<AutoCompleteBookingsJob>.Instance);
    }

    private void Due(params BookingId[] ids) =>
        _bookings.GetConfirmedEndedByAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ids);

    [Fact]
    public async Task It_asks_for_bookings_that_ended_twelve_hours_ago_on_the_salons_clock()
    {
        Due();

        await Job().RunAsync(UtcNow);

        var expected = SalonTime.FromUtc(UtcNow) - BookingAutoCompletion.After;
        await _bookings.Received(1).GetConfirmedEndedByAsync(expected, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Each_due_booking_is_completed_by_its_own_command_at_the_passes_moment()
    {
        var first = BookingId.New();
        var second = BookingId.New();
        Due(first, second);
        _sender.Send(Arg.Any<AutoCompleteBookingCommand>(), Arg.Any<CancellationToken>()).Returns(true);

        var completed = await Job().RunAsync(UtcNow);

        completed.Should().Be(2);
        await _sender.Received(1).Send(new AutoCompleteBookingCommand(first.Value, UtcNow), Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(new AutoCompleteBookingCommand(second.Value, UtcNow), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task One_booking_that_fails_does_not_stop_the_others()
    {
        var broken = BookingId.New();
        var fine = BookingId.New();
        Due(broken, fine);
        _sender.Send(new AutoCompleteBookingCommand(broken.Value, UtcNow), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));
        _sender.Send(new AutoCompleteBookingCommand(fine.Value, UtcNow), Arg.Any<CancellationToken>()).Returns(true);

        var completed = await Job().RunAsync(UtcNow);

        completed.Should().Be(1);
    }

    [Fact]
    public async Task A_booking_someone_else_completed_first_is_not_counted()
    {
        Due(BookingId.New());
        _sender.Send(Arg.Any<AutoCompleteBookingCommand>(), Arg.Any<CancellationToken>()).Returns(false);

        (await Job().RunAsync(UtcNow)).Should().Be(0);
    }

    [Fact]
    public async Task Nothing_due_sends_nothing()
    {
        Due();

        (await Job().RunAsync(UtcNow)).Should().Be(0);
        await _sender.DidNotReceiveWithAnyArgs().Send(Arg.Any<AutoCompleteBookingCommand>(), Arg.Any<CancellationToken>());
    }
}
