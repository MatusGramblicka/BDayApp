using System.ComponentModel.DataAnnotations;

namespace Contracts.DataTransferObjects.User;

public record UserLite2StepsAuthDto
{
    [Required(ErrorMessage = "Email is required.")]
    public string Email { get; init; } = string.Empty;

    public bool TwoFactorEnabled { get; init; }
}
