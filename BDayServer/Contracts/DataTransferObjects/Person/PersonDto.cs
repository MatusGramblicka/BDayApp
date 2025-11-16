namespace Contracts.DataTransferObjects.Person;

public record PersonDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;   
    public string Surname { get; init; } = string.Empty;
    public DateOnly? DayOfBirth { get; init; }
    public DateOnly DayOfNameDay { get; init; }
    public string? ImageUrl { get; init; }
}