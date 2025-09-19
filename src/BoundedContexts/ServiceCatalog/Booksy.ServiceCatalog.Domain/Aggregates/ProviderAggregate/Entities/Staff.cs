﻿// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/Staff.cs
// ========================================
namespace Booksy.ServiceCatalog.Domain.Entities
{
    /// <summary>
    /// Staff member entity within Provider aggregate
    /// </summary>
    public sealed class Staff : Entity<Guid>
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public Email Email { get; private set; }
        public PhoneNumber? Phone { get; private set; }
        public StaffRole Role { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime HiredAt { get; private set; }
        public DateTime? TerminatedAt { get; private set; }
        public string? TerminationReason { get; private set; }
        public string? Notes { get; private set; }
        public ProviderId ProviderId { get; set; }
        // Calculated property
        public string FullName => $"{FirstName} {LastName}";

        // Private constructor for EF Core
        private Staff() : base() { }

        internal static Staff Create(string firstName, string lastName, Email email, StaffRole role, PhoneNumber? phone = null)
        {
            return new Staff
            {
                Id =Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
                Role = role,
                IsActive = true,
                HiredAt = DateTime.UtcNow
            };
        }

        public void UpdateContactInfo(Email email, PhoneNumber? phone)
        {
            Email = email;
            Phone = phone;
        }

        public void UpdateRole(StaffRole newRole)
        {
            Role = newRole;
        }

        public void Deactivate(string reason)
        {
            IsActive = false;
            TerminatedAt = DateTime.UtcNow;
            TerminationReason = reason;
        }

        public void Reactivate()
        {
            IsActive = true;
            TerminatedAt = null;
            TerminationReason = null;
        }

        public void UpdateNotes(string notes)
        {
            Notes = notes;
        }
    }
}