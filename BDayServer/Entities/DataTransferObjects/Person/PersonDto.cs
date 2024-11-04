namespace Entities.DataTransferObjects.Person;

public record PersonDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;   
    public string Surname { get; init; } = string.Empty;
    public DateTime DayOfBirth { get; init; }
    public DateTime DayOfNameDay { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
}