using System.ComponentModel.DataAnnotations;

namespace BDayClient.Pocos;

public class ForgotPassword
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string ClientUri { get; set; } = string.Empty;
}