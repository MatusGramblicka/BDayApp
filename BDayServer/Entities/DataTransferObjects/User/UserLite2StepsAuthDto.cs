namespace Entities.DataTransferObjects.User;

public record UserLite2StepsAuthDto
{
    public string Email { get; init; } = string.Empty;

    public bool TwoFactorEnabled { get; init; }
}