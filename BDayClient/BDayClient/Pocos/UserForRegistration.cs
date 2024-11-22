using System.ComponentModel.DataAnnotations;

namespace BDayClient.Pocos;

public class UserForRegistration
{
    [Required(ErrorMessage = "Email is required.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password),
        ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string ClientUri { get; set; } = string.Empty;
}