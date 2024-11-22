using System.ComponentModel.DataAnnotations;

namespace BDayClient.Pocos;

public class TwoFactorVerification
{
    public string Email { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    [Required(ErrorMessage = "Token is required")]
    public string TwoFactorToken { get; set; } = string.Empty;
}