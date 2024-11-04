namespace Entities.DataTransferObjects.Auth;

public record AuthResponseDto
{
    public bool IsAuthSuccessful { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public bool Is2StepVerificationRequired { get; init; }
    public string Provider { get; init; } = string.Empty;
}