using System.ComponentModel.DataAnnotations;

namespace Entities.DataTransferObjects.Auth;

public record TwoFactorVerificationDto
{
    public string Email { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    [Required(ErrorMessage = "Token is required")]
    public string TwoFactorToken { get; init; } = string.Empty;
}