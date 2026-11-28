using System.ComponentModel.DataAnnotations;

namespace Contracts.DataTransferObjects.Auth;

public record RefreshTokenDto
{
    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
