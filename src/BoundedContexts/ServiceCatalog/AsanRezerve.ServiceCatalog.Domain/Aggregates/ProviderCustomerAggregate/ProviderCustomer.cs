using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Aggregates
{
    /// <summary>
    /// A customer in one salon's own customer book: someone the provider added by hand or picked from
    /// their phone's contacts, so booking a regular is a tap instead of typing their details again.
    ///
    /// <para>Belongs to exactly one salon. The same phone is one customer per salon (enforced by a
    /// unique index); another salon may know the same person under its own entry.</para>
    /// </summary>
    public sealed class ProviderCustomer : AggregateRoot<Guid>
    {
        public const int MaxNameLength = 100;
        public const int MaxNotesLength = 500;

        public ProviderId ProviderId { get; private set; } = null!;
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;

        /// <summary>Normalized (E.164, +98...), so every spelling of a number is the same customer.</summary>
        public PhoneNumber PhoneNumber { get; private set; } = null!;

        public string? Notes { get; private set; }
        public CustomerSource Source { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public string FullName => $"{FirstName} {LastName}".Trim();

        private ProviderCustomer()
        {
        }

        public static ProviderCustomer Create(
            ProviderId providerId,
            string firstName,
            string? lastName,
            PhoneNumber phoneNumber,
            string? notes,
            CustomerSource source)
        {
            var customer = new ProviderCustomer
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId ?? throw new DomainValidationException(nameof(ProviderId), "A customer belongs to a salon"),
                PhoneNumber = phoneNumber ?? throw new DomainValidationException(nameof(PhoneNumber), "شماره تلفن الزامی است"),
                Source = source,
                CreatedAt = DateTime.UtcNow,
            };
            customer.SetDetails(firstName, lastName, notes);
            return customer;
        }

        /// <summary>Edits the customer. The phone may change; the handler checks it stays unique.</summary>
        public void Update(string firstName, string? lastName, PhoneNumber phoneNumber, string? notes)
        {
            PhoneNumber = phoneNumber ?? throw new DomainValidationException(nameof(PhoneNumber), "شماره تلفن الزامی است");
            SetDetails(firstName, lastName, notes);
            UpdatedAt = DateTime.UtcNow;
        }

        private void SetDetails(string firstName, string? lastName, string? notes)
        {
            var first = firstName?.Trim() ?? string.Empty;
            if (first.Length == 0)
                throw new DomainValidationException(nameof(FirstName), "نام مشتری الزامی است");
            if (first.Length > MaxNameLength)
                throw new DomainValidationException(nameof(FirstName), $"نام حداکثر {MaxNameLength} کاراکتر است");

            var last = lastName?.Trim() ?? string.Empty;
            if (last.Length > MaxNameLength)
                throw new DomainValidationException(nameof(LastName), $"نام خانوادگی حداکثر {MaxNameLength} کاراکتر است");

            var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            if (trimmedNotes is { Length: > MaxNotesLength })
                throw new DomainValidationException(nameof(Notes), $"یادداشت حداکثر {MaxNotesLength} کاراکتر است");

            FirstName = first;
            LastName = last;
            Notes = trimmedNotes;
        }
    }
}
