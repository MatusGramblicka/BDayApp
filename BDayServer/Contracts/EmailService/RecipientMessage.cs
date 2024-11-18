namespace Contracts.EmailService;

public class RecipientMessage
{
    public string Message { get; set; }
    public string Recipient { get; set; }
    public DayType CelebrationType { get; set; }
}