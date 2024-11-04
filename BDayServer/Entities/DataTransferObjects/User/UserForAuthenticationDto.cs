using System.ComponentModel.DataAnnotations;

namespace Entities.DataTransferObjects.User;

public record UserForAuthenticationDto
{
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; init; } = string.Empty;
}