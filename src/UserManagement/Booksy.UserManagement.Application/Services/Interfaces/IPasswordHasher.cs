// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
// ========================================
// Services - Interfaces
// ========================================

// Booksy.UserManagement.Application/Services/Interfaces/IPasswordHasher.cs
namespace Booksy.UserManagement.Application.Services.Interfaces
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        bool RequiresRehash(string hashedPassword);
    }
}


