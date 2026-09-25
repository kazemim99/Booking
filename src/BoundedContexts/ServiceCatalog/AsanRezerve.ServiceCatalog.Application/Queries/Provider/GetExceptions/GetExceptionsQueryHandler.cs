// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Provider/GetExceptions/GetExceptionsQueryHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetExceptions;

public sealed class GetExceptionsQueryHandler
    : IQueryHandler<GetExceptionsQuery, ExceptionsViewModel>
{
    private readonly IProviderWriteRepository _providerRepository;

    public GetExceptionsQueryHandler(IProviderWriteRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public async Task<ExceptionsViewModel> Handle(
        GetExceptionsQuery query,
        CancellationToken cancellationToken)
    {
        // Retrieve provider
        var provider = await _providerRepository.GetByIdAsync(
            ProviderId.From(query.ProviderId),
            cancellationToken);

        if (provider == null)
            throw new DomainValidationException("Provider not found");

        // Map exceptions to view model
        var exceptions = provider.Exceptions
            .OrderBy(e => e.Date)
            .Select(e => new ExceptionViewModel(
                Id: e.Id,
                Date: e.Date.ToString("yyyy-MM-dd"),
                OpenTime: e.OpenTime?.ToString("HH:mm"),
                CloseTime: e.CloseTime?.ToString("HH:mm"),
                Reason: e.Reason,
                IsClosed: e.IsClosed))
            .ToList();

        return new ExceptionsViewModel(exceptions);
    }
}
