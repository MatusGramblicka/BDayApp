using System.ComponentModel.DataAnnotations;

namespace Contracts.DataTransferObjects.Auth;

public record TwoFactorVerificationDto
{
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; init; } = string.Empty;

    /// <summary>Ignored by the server: codes are always sent by e-mail.</summary>
    public string Provider { get; init; } = string.Empty;

    [Required(ErrorMessage = "Token is required")]
    public string TwoFactorToken { get; init; } = string.Empty;
}
