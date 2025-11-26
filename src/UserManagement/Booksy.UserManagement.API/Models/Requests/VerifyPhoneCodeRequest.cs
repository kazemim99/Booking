using System.ComponentModel.DataAnnotations;

namespace Booksy.UserManagement.API.Models.Requests;

/// <summary>
/// Request to verify phone number with code
/// </summary>
public class VerifyPhoneCodeRequest
{
    /// <summary>
    /// Phone number being verified
    /// </summary>
    [Required(ErrorMessage = "شماره موبایل الزامی است")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل باید با 09 شروع شود و 11 رقم باشد")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 6-digit verification code
    /// </summary>
    [Required(ErrorMessage = "کد تأیید الزامی است")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "کد تأیید باید 6 رقم باشد")]
    public string VerificationCode { get; set; } = string.Empty;
}
