//===========================================
// Supporting Request/Response Models and Extensions
//===========================================

//===========================================
// Models/Requests/SearchServicesRequest.cs
//===========================================
//===========================================
// Models/Responses/MessageResponse.cs
//===========================================
namespace Booksy.ServiceCatalog.Api.Models.Responses
{
    public sealed class MessageResponse
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }

        public MessageResponse(string message)
        {
            Message = message;
            Timestamp = DateTime.UtcNow;
        }
    }
}

