using System.ComponentModel.DataAnnotations;

namespace Entities.DataTransferObjects.Auth;

public record ResetPasswordDto
{
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; init; } = string.Empty;
    [Compare(nameof(Password),
        ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}