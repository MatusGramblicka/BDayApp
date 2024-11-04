using System.ComponentModel.DataAnnotations;

namespace Entities.DataTransferObjects.User;

public record UserForRegistrationDto
{
    [Required(ErrorMessage = "Email is required.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; init; } = string.Empty;

    [Compare(nameof(Password),
        ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; init; } = string.Empty;

    public string ClientUri { get; init; } = string.Empty;
}