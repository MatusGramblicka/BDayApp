using System.ComponentModel.DataAnnotations;

namespace Entities.DataTransferObjects.Auth;

public record ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
    public string ClientUri { get; init; } = string.Empty;
}