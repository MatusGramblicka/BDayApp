namespace EmailService.Contracts.Models;

public class ReceipientMessage
{
    public string Message { get; set; }
    public string Receipient { get; set; }
    public DayType CelebrationType { get; set; }
}