namespace Contracts.EmailService;

public class PersonForEmailCreation
{
    public string? CreatorEmail { get; set; }
    public string Name { get; init; } = string.Empty;
    public string Surname { get; init; } = string.Empty;
    public DateTime DayOfBirth { get; init; }
    public DateTime DayOfNameDay { get; init; }
}