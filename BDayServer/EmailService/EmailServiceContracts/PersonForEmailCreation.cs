namespace Contracts.EmailService;

public class PersonForEmailCreation
{
    public string? CreatorEmail { get; set; }
    public string Name { get; init; } = string.Empty;
    public string? Surname { get; init; }
    public DateOnly? DayOfBirth { get; init; }
    public DateOnly DayOfNameDay { get; init; }
}