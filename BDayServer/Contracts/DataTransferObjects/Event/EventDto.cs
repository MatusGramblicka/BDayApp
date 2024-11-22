namespace Contracts.DataTransferObjects.Event;

public record EventDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
}