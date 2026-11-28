using Contracts.DataTransferObjects.Event;

namespace BDayClient.Pocos;

public class EventForCreation : EventForManipulation
{
    public EventForCreationDto ToDto() => new()
    {
        Name = Name,
        Date = DateAtMidnight,
        ImageUrl = NullIfEmpty(ImageUrl)
    };
}
