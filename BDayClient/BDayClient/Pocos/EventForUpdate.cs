using Contracts.DataTransferObjects.Event;

namespace BDayClient.Pocos;

public class EventForUpdate : EventForManipulation
{
    public static EventForUpdate FromDto(EventDto eventDto) => new()
    {
        Name = eventDto.Name,
        Date = DateOnly.FromDateTime(eventDto.Date),
        ImageUrl = eventDto.ImageUrl
    };

    public EventForUpdate Copy() => (EventForUpdate)MemberwiseClone();

    public EventForUpdateDto ToDto() => new()
    {
        Name = Name,
        Date = DateAtMidnight,
        ImageUrl = NullIfEmpty(ImageUrl)
    };
}
