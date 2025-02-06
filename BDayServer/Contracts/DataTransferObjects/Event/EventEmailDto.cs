namespace Contracts.DataTransferObjects.Event;

public class EventEmailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? ImageUrl { get; set; }
    public string EventCreator { get; set; } = string.Empty;
}