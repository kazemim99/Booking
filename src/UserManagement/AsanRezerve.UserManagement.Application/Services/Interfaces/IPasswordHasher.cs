// ========================================
// Event Handlers - Domain Events
// ========================================

// AsanRezerve.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
// ========================================
// Services - Interfaces
// ========================================

// AsanRezerve.UserManagement.Application/Services/Interfaces/IPasswordHasher.cs
namespace AsanRezerve.UserManagement.Application.Services.Interfaces
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        bool RequiresRehash(string hashedPassword);
    }
}


