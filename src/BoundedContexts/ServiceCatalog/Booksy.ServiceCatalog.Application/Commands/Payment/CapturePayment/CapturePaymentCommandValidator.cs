// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/CapturePayment/CapturePaymentCommandValidator.cs
// ========================================
using FluentValidation;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.CapturePayment
{
    public sealed class CapturePaymentCommandValidator : AbstractValidator<CapturePaymentCommand>
    {
        public CapturePaymentCommandValidator()
        {
            RuleFor(x => x.PaymentId)
                .NotEmpty()
                .WithMessage("Payment ID is required");

            RuleFor(x => x.AmountToCapture)
                .GreaterThan(0)
                .When(x => x.AmountToCapture.HasValue)
                .WithMessage("Capture amount must be greater than zero when specified");
        }
    }
}
