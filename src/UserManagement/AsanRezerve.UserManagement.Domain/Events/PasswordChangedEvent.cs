// ========================================
// AsanRezerve.UserManagement.Domain/Enums/UserStatus.cs
// ========================================


using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Domain.Aggregates;

namespace AsanRezerve.UserManagement.Domain.Events
{
    public sealed record PasswordChangedEvent(UserId UserId, DateTime ChangedAt) : 
        DomainEvent("User", UserId.ToString())
    {

       
    }
}

