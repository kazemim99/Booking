using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// C4 create-then-pay coupling (end-to-end backend). A deposit-required booking is created Requested; when its
/// deposit payment is verified, the <c>ConfirmBookingOnDepositVerified</c> handler records the deposit and confirms
/// the booking. Proves the backend half of the customer-payment-experience on the finalized ledger.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class DepositCheckoutCouplingTests : ServiceCatalogIntegrationTestBase
{
    public DepositCheckoutCouplingTests(AsanRezerveHostFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Verifying_the_deposit_payment_confirms_a_deposit_required_booking()
    {
        var (provider, services) = await CreateProviderWithServicesAsync(1);

        // 1. Create a deposit-required booking (Requested) — create-then-pay.
        Guid bookingId;
        using (var scope = Factory.Services.CreateScope())
        {
            var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingWriteRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
            var policy = BookingPolicy.Create(1, 90, 24, 0m, true, 24, requireDeposit: true, depositPercentage: 20m);
            var booking = Booking.CreateBookingRequest(
                provider.OwnerId, provider.Id, services[0].Id, staffId: Guid.NewGuid(),
                startTime: DateTime.UtcNow.AddDays(2), duration: Duration.FromMinutes(60),
                totalPrice: Price.Create(100m, "USD"), policy: policy);
            bookingId = booking.Id.Value;
            await bookingRepo.SaveAsync(booking);
            await uow.CommitAsync();
        }

        // 2. Pay + verify the deposit → PaymentVerifiedEvent → ConfirmBookingOnDepositVerified handler.
        using (var scope = Factory.Services.CreateScope())
        {
            var paymentRepo = scope.ServiceProvider.GetRequiredService<IPaymentWriteRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
            var payment = Payment.CreateForBooking(
                BookingId.From(bookingId), provider.OwnerId, provider.Id,
                Money.Create(20m, "USD"), PaymentMethod.ZarinPal);
            payment.RecordPaymentRequest($"auth-{Guid.NewGuid():N}", "https://pay.test/authority");
            payment.VerifyPayment("REF-DEP", "6274********1234", 0); // -> raises PaymentVerifiedEvent
            await paymentRepo.AddAsync(payment);
            await uow.CommitAsync();
        }

        // 3. The booking is now Confirmed (the deposit gate was satisfied by the verified deposit).
        using (var scope = Factory.Services.CreateScope())
        {
            var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingWriteRepository>();
            var booking = await bookingRepo.GetByIdAsync(BookingId.From(bookingId));
            booking!.Status.Should().Be(BookingStatus.Confirmed, "a verified deposit confirms the booking (create-then-pay)");
            booking.PaymentInfo.IsDepositPaid().Should().BeTrue();
        }
    }
}
