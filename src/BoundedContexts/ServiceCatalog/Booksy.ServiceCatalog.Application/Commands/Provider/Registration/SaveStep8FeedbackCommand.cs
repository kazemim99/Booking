using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

/// <summary>
/// Step 8: Save optional feedback from provider
/// This is an optional step - providers can skip providing feedback
/// </summary>
public sealed record SaveStep8FeedbackCommand(
    Guid ProviderId,
    string? FeedbackText,
    Guid? IdempotencyKey = null
) : ICommand<SaveStep8FeedbackResult>;

public sealed record SaveStep8FeedbackResult(
    Guid ProviderId,
    int RegistrationStep,
    string Message
);
