namespace Contracts.DataTransferObjects.Auth;

public record RefreshTokenDto
{
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}