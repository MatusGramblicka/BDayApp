namespace Contracts.EmailService;

public class RecipientMessage
{
    public string Message { get; set; } = string.Empty;
    public string? Recipient { get; set; }
    public DayType CelebrationType { get; set; }
}