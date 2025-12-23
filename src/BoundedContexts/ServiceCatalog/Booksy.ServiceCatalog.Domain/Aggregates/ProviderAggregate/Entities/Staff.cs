//// ========================================
//// Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/Staff.cs
//// ========================================
//namespace Booksy.ServiceCatalog.Domain.Entities
//{
//    /// <summary>
//    /// Staff member entity within Provider aggregate
//    /// </summary>
//    public sealed class Staff : Entity<Guid>
//    {
//        public string FirstName { get; private set; }
//        public string LastName { get; private set; }
//        public Email? Email { get; private set; }
//        public PhoneNumber? Phone { get; private set; }
//        public StaffRole Role { get; private set; }
//        public bool IsActive { get; private set; }
//        public DateTime HiredAt { get; private set; }
//        public DateTime? TerminatedAt { get; private set; }
//        public string? TerminationReason { get; private set; }
//        public string? Notes { get; private set; }
//        public string? ProfilePhotoUrl { get; private set; }
//        public string? Biography { get; private set; }
//        public ProviderId ProviderId { get; private set; }
//        // Calculated property
//        public string FullName => $"{FirstName} {LastName}";


//        // Private constructor for EF Core
//        private Staff() : base() { }

//        internal static Staff Create(string firstName, string lastName, StaffRole role,
//                    ProviderId providerId
//            , PhoneNumber? phone = null)
//        {
//            return new Staff
//            {
//                Id = Guid.NewGuid(),
//                FirstName = firstName,
//                LastName = lastName,
//                Phone = phone,
//                Role = role,
//                ProviderId = providerId,
//                IsActive = true,
//                HiredAt = DateTime.UtcNow
//            };
//        }

//        public void UpdateContactInfo(string firstname, string lastName, Email email, PhoneNumber? phone)
//        {
//            Email = email;
//            Phone = phone;
//            FirstName = firstname;
//            LastName = lastName;
//        }

//        public void UpdateRole(StaffRole newRole)
//        {
//            Role = newRole;
//        }

//        public void Deactivate(string reason)
//        {
//            IsActive = false;
//            TerminatedAt = DateTime.UtcNow;
//            TerminationReason = reason;
//        }

//        public void Reactivate()
//        {
//            IsActive = true;
//            TerminatedAt = null;
//            TerminationReason = null;
//        }

//        public void UpdateNotes(string? notes)
//        {
//            if (notes != null && notes.Length > 1000)
//                throw new ArgumentException("Notes cannot exceed 1000 characters", nameof(notes));

//            Notes = notes;
//        }

//        public void UpdateProfilePhoto(string? photoUrl)
//        {
//            ProfilePhotoUrl = photoUrl;
//        }

//        public void UpdateBiography(string? biography)
//        {
//            if (biography != null && biography.Length > 500)
//                throw new ArgumentException("Biography cannot exceed 500 characters", nameof(biography));

//            Biography = biography;
//        }
//    }
//}