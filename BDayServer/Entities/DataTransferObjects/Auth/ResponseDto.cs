namespace Entities.DataTransferObjects.Auth;

public record ResponseDto
{
    public bool IsSuccessfulRegistration { get; init; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}