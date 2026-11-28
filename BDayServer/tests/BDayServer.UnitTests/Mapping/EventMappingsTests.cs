using Contracts.DataTransferObjects.Event;
using Core.Mapping;
using Entities.Models;

namespace BDayServer.UnitTests.Mapping;

public class EventMappingsTests
{
    private static Event StoredEvent() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Party",
        Date = new DateTime(2026, 12, 24, 18, 0, 0),
        ImageUrl = "StaticFiles/Images/party.png",
        UserId = "u1"
    };

    [Fact]
    public void ToDto_CopiesEveryField()
    {
        var @event = StoredEvent();

        @event.ToDto().Should().Be(new EventDto
        {
            Id = @event.Id,
            Name = "Party",
            Date = new DateTime(2026, 12, 24, 18, 0, 0),
            ImageUrl = "StaticFiles/Images/party.png"
        });
    }

    [Fact]
    public void ToDto_WithoutImage_KeepsImageUrlNull()
    {
        var @event = StoredEvent();
        @event.ImageUrl = null;

        // Returned as "imageUrl": null, as before - not as an empty string.
        @event.ToDto().ImageUrl.Should().BeNull();
    }

    [Fact]
    public void ToEntity_CopiesEveryFieldAndLeavesIdAndOwnerEmpty()
    {
        var dto = new EventForCreationDto
        {
            Name = "Trip", Date = new DateTime(2027, 1, 2), ImageUrl = "StaticFiles/Images/trip.png"
        };

        dto.ToEntity().Should().BeEquivalentTo(new
        {
            Id = Guid.Empty,
            Name = "Trip",
            Date = new DateTime(2027, 1, 2),
            ImageUrl = "StaticFiles/Images/trip.png",
            UserId = string.Empty
        });
    }

    [Fact]
    public void ToEntity_WithoutImage_KeepsImageUrlNull()
    {
        var @event = new EventForCreationDto { Name = "Trip", Date = new DateTime(2027, 1, 2) }.ToEntity();

        @event.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void ApplyTo_OverwritesEveryEditableFieldAndKeepsIdAndOwner()
    {
        var @event = StoredEvent();
        var id = @event.Id;

        new EventForUpdateDto
        {
            Name = "Wedding", Date = new DateTime(2027, 6, 1, 14, 30, 0), ImageUrl = "StaticFiles/Images/wedding.png"
        }.ApplyTo(@event);

        @event.Should().BeEquivalentTo(new
        {
            Id = id,
            Name = "Wedding",
            Date = new DateTime(2027, 6, 1, 14, 30, 0),
            ImageUrl = "StaticFiles/Images/wedding.png",
            UserId = "u1"
        });
    }

    [Fact]
    public void ApplyTo_NullImageUrl_ClearsIt()
    {
        var @event = StoredEvent();

        new EventForUpdateDto { Name = "Party", Date = new DateTime(2026, 12, 24, 18, 0, 0) }.ApplyTo(@event);

        @event.ImageUrl.Should().BeNull();
    }
}
