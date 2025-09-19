// ========================================
// Booksy.UserManagement.Domain/Enums/UserStatus.cs
// ========================================


using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;

namespace Booksy.UserManagement.Domain.Events
{
    public sealed record PasswordChangedEvent(UserId UserId, DateTime ChangedAt) : 
        DomainEvent("User", UserId.ToString())
    {

       
    }
}

