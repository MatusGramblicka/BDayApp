using System.ComponentModel.DataAnnotations;

namespace Contracts.DataTransferObjects.Auth;

public record RevokeTokenDto
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
