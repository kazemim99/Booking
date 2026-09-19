using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Customers
{
    // A salon's own customer book (openspec/changes/provider-customer-book).

    public sealed record ProviderCustomerDto(
        Guid Id,
        string FirstName,
        string LastName,
        string FullName,
        string PhoneNumber,
        string? Notes,
        string Source,
        DateTime CreatedAt,
        int TotalBookings = 0,
        int UpcomingBookings = 0,
        DateTime? LastBookingAt = null)
    {
        public static ProviderCustomerDto From(ProviderCustomer c, ProviderCustomerBookingStats? stats = null) => new(
            c.Id, c.FirstName, c.LastName, c.FullName, c.PhoneNumber.Value, c.Notes, c.Source.ToString(), c.CreatedAt,
            stats?.Total ?? 0, stats?.Upcoming ?? 0, stats?.LastBookingAt);
    }

    internal static class CustomerPhone
    {
        /// <summary>Any spelling of a number to its normalized form, or a 400 naming the problem.</summary>
        public static PhoneNumber Parse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new DomainValidationException(nameof(PhoneNumber), "شماره تلفن الزامی است");
            try
            {
                return PhoneNumber.From(raw);
            }
            catch (ArgumentException)
            {
                throw new DomainValidationException(nameof(PhoneNumber), "شماره تلفن معتبر نیست");
            }
        }
    }

    // ---- add ----

    public sealed record AddProviderCustomerCommand(
        Guid ProviderId, string FirstName, string? LastName, string PhoneNumber, string? Notes, Guid? IdempotencyKey = null)
        : ICommand<ProviderCustomerDto>;

    public sealed class AddProviderCustomerCommandHandler : ICommandHandler<AddProviderCustomerCommand, ProviderCustomerDto>
    {
        private readonly IProviderCustomerRepository _customers;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;

        public AddProviderCustomerCommandHandler(IProviderCustomerRepository customers, IServiceCatalogUnitOfWork unitOfWork)
        {
            _customers = customers;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProviderCustomerDto> Handle(AddProviderCustomerCommand request, CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);
            var phone = CustomerPhone.Parse(request.PhoneNumber);

            // Adding never silently overwrites a saved entry: the provider is told who already has the number.
            var existing = await _customers.GetByPhoneAsync(providerId, phone, cancellationToken);
            if (existing != null)
                throw new ConflictException($"این شماره قبلاً برای «{existing.FullName}» ثبت شده است");

            var customer = ProviderCustomer.Create(
                providerId, request.FirstName, request.LastName, phone, request.Notes, CustomerSource.Manual);
            await _customers.AddAsync(customer, cancellationToken);
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
            return ProviderCustomerDto.From(customer);
        }
    }

    // ---- update ----

    public sealed record UpdateProviderCustomerCommand(
        Guid ProviderId, Guid CustomerId, string FirstName, string? LastName, string PhoneNumber, string? Notes, Guid? IdempotencyKey = null)
        : ICommand<ProviderCustomerDto>;

    public sealed class UpdateProviderCustomerCommandHandler : ICommandHandler<UpdateProviderCustomerCommand, ProviderCustomerDto>
    {
        private readonly IProviderCustomerRepository _customers;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;

        public UpdateProviderCustomerCommandHandler(IProviderCustomerRepository customers, IServiceCatalogUnitOfWork unitOfWork)
        {
            _customers = customers;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProviderCustomerDto> Handle(UpdateProviderCustomerCommand request, CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);
            var customer = await _customers.GetAsync(providerId, request.CustomerId, cancellationToken)
                ?? throw new NotFoundException("مشتری پیدا نشد");
            var phone = CustomerPhone.Parse(request.PhoneNumber);

            var owner = await _customers.GetByPhoneAsync(providerId, phone, cancellationToken);
            if (owner != null && owner.Id != customer.Id)
                throw new ConflictException($"این شماره قبلاً برای «{owner.FullName}» ثبت شده است");

            customer.Update(request.FirstName, request.LastName, phone, request.Notes);
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
            return ProviderCustomerDto.From(customer);
        }
    }

    // ---- remove ----

    public sealed record RemoveProviderCustomerCommand(Guid ProviderId, Guid CustomerId, Guid? IdempotencyKey = null) : ICommand<bool>;

    public sealed class RemoveProviderCustomerCommandHandler : ICommandHandler<RemoveProviderCustomerCommand, bool>
    {
        private readonly IProviderCustomerRepository _customers;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;

        public RemoveProviderCustomerCommandHandler(IProviderCustomerRepository customers, IServiceCatalogUnitOfWork unitOfWork)
        {
            _customers = customers;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(RemoveProviderCustomerCommand request, CancellationToken cancellationToken)
        {
            // Past bookings are not touched: they keep the name and number they were made with.
            var customer = await _customers.GetAsync(ProviderId.From(request.ProviderId), request.CustomerId, cancellationToken)
                ?? throw new NotFoundException("مشتری پیدا نشد");
            _customers.Remove(customer);
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);
            return true;
        }
    }

    // ---- import (only the contacts the provider ticked) ----

    public sealed record ImportCustomerRow(string? FirstName, string? LastName, string? PhoneNumber);

    public sealed record ImportCustomerRowResult(string? PhoneNumber, string Outcome, string? Message);

    public sealed record ImportProviderCustomersResult(
        int Added, int AlreadySaved, int Invalid, IReadOnlyList<ImportCustomerRowResult> Rows);

    public sealed record ImportProviderCustomersCommand(Guid ProviderId, IReadOnlyList<ImportCustomerRow> Customers, Guid? IdempotencyKey = null)
        : ICommand<ImportProviderCustomersResult>;

    public sealed class ImportProviderCustomersCommandHandler
        : ICommandHandler<ImportProviderCustomersCommand, ImportProviderCustomersResult>
    {
        public const int MaxRows = 500;

        private readonly IProviderCustomerRepository _customers;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;

        public ImportProviderCustomersCommandHandler(IProviderCustomerRepository customers, IServiceCatalogUnitOfWork unitOfWork)
        {
            _customers = customers;
            _unitOfWork = unitOfWork;
        }

        public async Task<ImportProviderCustomersResult> Handle(ImportProviderCustomersCommand request, CancellationToken cancellationToken)
        {
            var input = request.Customers ?? Array.Empty<ImportCustomerRow>();
            if (input.Count > MaxRows)
                throw new DomainValidationException(nameof(request.Customers), $"حداکثر {MaxRows} مخاطب در هر بار");

            var providerId = ProviderId.From(request.ProviderId);
            var rows = new List<ImportCustomerRowResult>();
            var parsed = new List<(string First, string? Last, PhoneNumber Phone)>();

            foreach (var row in input)
            {
                if (row is null)
                    continue;

                // A contact with only one name: that name is the first name.
                var hasFirst = !string.IsNullOrWhiteSpace(row.FirstName);
                var first = hasFirst ? row.FirstName!.Trim() : row.LastName?.Trim();
                var last = hasFirst ? row.LastName : null;
                if (string.IsNullOrWhiteSpace(first))
                {
                    rows.Add(new ImportCustomerRowResult(row.PhoneNumber, "Invalid", "نام مشتری الزامی است"));
                    continue;
                }

                try
                {
                    parsed.Add((first, last, CustomerPhone.Parse(row.PhoneNumber)));
                }
                catch (DomainValidationException ex)
                {
                    rows.Add(new ImportCustomerRowResult(row.PhoneNumber, "Invalid", ex.Message));
                }
            }

            var alreadySaved = await _customers.ExistingPhonesAsync(
                providerId, parsed.Select(p => p.Phone.Value).Distinct().ToList(), cancellationToken);
            var seenInBatch = new HashSet<string>();

            foreach (var (first, last, phone) in parsed)
            {
                // Never overwrite what the salon saved, and never add one number twice from one batch.
                if (alreadySaved.Contains(phone.Value) || !seenInBatch.Add(phone.Value))
                {
                    rows.Add(new ImportCustomerRowResult(phone.Value, "AlreadySaved", null));
                    continue;
                }

                try
                {
                    await _customers.AddAsync(
                        ProviderCustomer.Create(providerId, first, last, phone, notes: null, CustomerSource.Contacts),
                        cancellationToken);
                    rows.Add(new ImportCustomerRowResult(phone.Value, "Added", null));
                }
                catch (DomainValidationException ex)
                {
                    rows.Add(new ImportCustomerRowResult(phone.Value, "Invalid", ex.Message));
                }
            }

            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

            return new ImportProviderCustomersResult(
                rows.Count(r => r.Outcome == "Added"),
                rows.Count(r => r.Outcome == "AlreadySaved"),
                rows.Count(r => r.Outcome == "Invalid"),
                rows);
        }
    }

    // ---- list / search ----

    public sealed record GetProviderCustomersQuery(Guid ProviderId, string? Search)
        : IQuery<IReadOnlyList<ProviderCustomerDto>>;

    public sealed class GetProviderCustomersQueryHandler
        : IQueryHandler<GetProviderCustomersQuery, IReadOnlyList<ProviderCustomerDto>>
    {
        private readonly IProviderCustomerRepository _customers;

        public GetProviderCustomersQueryHandler(IProviderCustomerRepository customers) => _customers = customers;

        public async Task<IReadOnlyList<ProviderCustomerDto>> Handle(GetProviderCustomersQuery request, CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);
            var customers = await _customers.ListAsync(providerId, request.Search, cancellationToken);
            var stats = await _customers.BookingStatsAsync(providerId, DateTime.UtcNow, cancellationToken);
            return customers.Select(c => ProviderCustomerDto.From(c, stats.GetValueOrDefault(c.Id))).ToList();
        }
    }
}
