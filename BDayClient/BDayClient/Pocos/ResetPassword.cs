using System.ComponentModel.DataAnnotations;

namespace BDayClient.Pocos;

public class ResetPassword
{
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
    [Compare(nameof(Password),
        ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}