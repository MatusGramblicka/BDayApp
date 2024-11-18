namespace Contracts.DataTransferObjects.Auth;

public record ResetPasswordResponseDto
{
    public bool IsResetPasswordSuccessful { get; init; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}