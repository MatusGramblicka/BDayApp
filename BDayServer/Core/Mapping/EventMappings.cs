using Contracts.DataTransferObjects.Event;
using Entities.Models;

namespace Core.Mapping;

/// <summary>Maps events to and from their DTOs (hand-written; replaced AutoMapper).</summary>
public static class EventMappings
{
    public static EventDto ToDto(this Event @event) => new()
    {
        Id = @event.Id,
        Name = @event.Name,
        Date = @event.Date,
        // EventDto.ImageUrl is declared non-nullable, but an event without an image is returned with null,
        // as AutoMapper did - not with "".
        ImageUrl = @event.ImageUrl!
    };

    /// <summary>A new event. The caller sets <see cref="Event.UserId"/>; EF Core assigns the Id when it is added.</summary>
    public static Event ToEntity(this EventForCreationDto dto) => new()
    {
        Name = dto.Name,
        Date = dto.Date,
        ImageUrl = dto.ImageUrl
    };

    /// <summary>Overwrites every field the user edits, also with null: a null ImageUrl clears it. Id and UserId are kept.</summary>
    public static void ApplyTo(this EventForUpdateDto dto, Event @event)
    {
        @event.Name = dto.Name;
        @event.Date = dto.Date;
        @event.ImageUrl = dto.ImageUrl;
    }
}
