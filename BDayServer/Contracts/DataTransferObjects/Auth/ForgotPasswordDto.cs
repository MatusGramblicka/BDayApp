using System.ComponentModel.DataAnnotations;

namespace Contracts.DataTransferObjects.Auth;

public record ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>Page that the reset link in the e-mail opens; must belong to this application.</summary>
    [Required]
    public string ClientUri { get; init; } = string.Empty;
}
