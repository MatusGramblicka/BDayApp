namespace Contracts.DataTransferObjects.Person;

public record PersonEmailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Surname { get; init; } = string.Empty;
    public DateTime DayOfBirth { get; init; }
    public DateTime DayOfNameDay { get; init; }
    public string? ImageUrl { get; init; }
    public string PersonCreator { get; init; } = string.Empty;
}