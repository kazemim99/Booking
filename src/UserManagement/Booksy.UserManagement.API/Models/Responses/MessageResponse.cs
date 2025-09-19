
// ========================================

namespace Booksy.UserManagement.API.Models.Responses;

public class MessageResponse
{
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }

    public MessageResponse(string message)
    {
        Message = message;
        Timestamp = DateTime.UtcNow;
    }
}
