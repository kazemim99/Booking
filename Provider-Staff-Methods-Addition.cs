// ========================================
// ADD THESE METHODS TO Provider.cs AFTER RemoveStaff() method (line 165)
// Replace the existing AddStaff and RemoveStaff methods with this complete section
// ========================================

        #region Staff Management (DDD Aggregate Root Pattern)

        /// <summary>
        /// Adds a new staff member to the provider (DDD-compliant)
        /// Returns the created Staff entity for further configuration
        /// </summary>
        public Staff AddStaff(string firstName, string lastName, Email email, StaffRole role, PhoneNumber? phone = null)
        {
            // Business rule: Cannot add staff if provider is not active
            if (Status == ProviderStatus.Inactive)
                throw new InvalidProviderException("Cannot add staff to inactive provider");

            // Business rule: Email must be unique among active staff
            if (_staff.Any(s => s.Email.Value.Equals(email.Value, StringComparison.OrdinalIgnoreCase) && s.IsActive))
                throw new InvalidProviderException($"Active staff member with email '{email.Value}' already exists");

            var staff = Entities.Staff.Create(firstName, lastName, email, role, phone);
            staff.ProviderId = Id;  // Ensure ProviderId is set
            _staff.Add(staff);

            RaiseDomainEvent(new StaffAddedEvent(Id, staff.Id, staff.FullName, role, DateTime.UtcNow));

            return staff;  // Return for further configuration (e.g., notes)
        }

        /// <summary>
        /// Updates an existing staff member's information
        /// </summary>
        public void UpdateStaff(Guid staffId, Email email, PhoneNumber? phone, StaffRole role)
        {
            var staff = GetStaffById(staffId);

            // Business rule: Email must be unique among active staff (excluding current)
            if (_staff.Any(s => s.Id != staffId &&
                           s.Email.Value.Equals(email.Value, StringComparison.OrdinalIgnoreCase) &&
                           s.IsActive))
            {
                throw new InvalidProviderException($"Another staff member with email '{email.Value}' already exists");
            }

            // Update staff through its methods (maintains encapsulation)
            staff.UpdateContactInfo(email, phone);
            staff.UpdateRole(role);

            // Raise domain event for auditing/notifications
            RaiseDomainEvent(new StaffUpdatedEvent(Id, staff.Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Activates a deactivated staff member
        /// </summary>
        public void ActivateStaff(Guid staffId)
        {
            var staff = GetStaffById(staffId);

            if (staff.IsActive)
                throw new InvalidProviderException("Staff member is already active");

            staff.Reactivate();

            RaiseDomainEvent(new StaffActivatedEvent(Id, staff.Id, DateTime.UtcNow));
        }

        /// <summary>
        /// Deactivates a staff member with a reason
        /// </summary>
        public void DeactivateStaff(Guid staffId, string reason)
        {
            var staff = GetStaffById(staffId);

            if (!staff.IsActive)
                throw new InvalidProviderException("Staff member is already inactive");

            // Business rule: Cannot deactivate if they're the only active staff member
            var activeStaffCount = _staff.Count(s => s.IsActive);
            if (activeStaffCount <= 1)
                throw new InvalidProviderException("Cannot deactivate the last active staff member");

            staff.Deactivate(reason);

            RaiseDomainEvent(new StaffDeactivatedEvent(Id, staff.Id, reason, DateTime.UtcNow));
        }

        /// <summary>
        /// Removes a staff member (soft delete via deactivation)
        /// </summary>
        public void RemoveStaff(Guid staffId, string reason)
        {
            // Delegate to DeactivateStaff to reuse business rules
            DeactivateStaff(staffId, reason);
        }

        /// <summary>
        /// Gets a staff member by ID (throws if not found)
        /// </summary>
        public Staff GetStaffById(Guid staffId)
        {
            var staff = _staff.FirstOrDefault(s => s.Id == staffId);
            if (staff == null)
                throw new InvalidProviderException($"Staff member '{staffId}' not found in provider '{Id}'");

            return staff;
        }

        /// <summary>
        /// Gets all active staff members
        /// </summary>
        public IEnumerable<Staff> GetActiveStaff()
        {
            return _staff.Where(s => s.IsActive);
        }

        /// <summary>
        /// Checks if a staff member with the given email exists (for validation)
        /// </summary>
        public bool HasStaffWithEmail(Email email, Guid? excludeStaffId = null)
        {
            return _staff.Any(s =>
                s.Email.Value.Equals(email.Value, StringComparison.OrdinalIgnoreCase) &&
                s.IsActive &&
                (!excludeStaffId.HasValue || s.Id != excludeStaffId.Value));
        }

        /// <summary>
        /// Updates notes for a staff member
        /// </summary>
        public void UpdateStaffNotes(Guid staffId, string? notes)
        {
            var staff = GetStaffById(staffId);
            if (!string.IsNullOrWhiteSpace(notes))
            {
                staff.UpdateNotes(notes);
            }
        }

        /// <summary>
        /// Gets the count of active staff members
        /// </summary>
        public int GetActiveStaffCount()
        {
            return _staff.Count(s => s.IsActive);
        }

        #endregion
