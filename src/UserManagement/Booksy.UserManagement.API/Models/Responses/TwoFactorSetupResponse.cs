
namespace Booksy.UserManagement.API.Models.Responses;

public class TwoFactorSetupResponse
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
    public string[] BackupCodes { get; set; } = Array.Empty<string>();
}
