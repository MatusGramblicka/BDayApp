using Contracts.DataTransferObjects.Person;
using Core.Mapping;
using Entities.Models;

namespace BDayServer.UnitTests.Mapping;

public class PersonMappingsTests
{
    private static Person StoredPerson() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Anna",
        Surname = "Novak",
        DayOfBirth = new DateOnly(1990, 5, 1),
        DayOfNameDay = new DateOnly(2000, 7, 26),
        ImageUrl = "StaticFiles/Images/anna.png",
        UserId = "u1"
    };

    [Fact]
    public void ToDto_CopiesEveryField()
    {
        var person = StoredPerson();

        person.ToDto().Should().Be(new PersonDto
        {
            Id = person.Id,
            Name = "Anna",
            Surname = "Novak",
            DayOfBirth = new DateOnly(1990, 5, 1),
            DayOfNameDay = new DateOnly(2000, 7, 26),
            ImageUrl = "StaticFiles/Images/anna.png"
        });
    }

    [Fact]
    public void ToDto_KeepsMissingOptionalValuesNull()
    {
        var person = new Person { Id = Guid.NewGuid(), Name = "Boris", DayOfNameDay = new DateOnly(2000, 5, 2) };

        var dto = person.ToDto();

        dto.Surname.Should().BeNull();
        dto.DayOfBirth.Should().BeNull();
        dto.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void ToEntity_CopiesEveryFieldAndLeavesIdAndOwnerEmpty()
    {
        var dto = new PersonForCreationDto
        {
            Name = "Boris",
            Surname = "Kral",
            DayOfBirth = new DateOnly(1985, 3, 4),
            DayOfNameDay = new DateOnly(2000, 5, 2),
            ImageUrl = "StaticFiles/Images/boris.png"
        };

        dto.ToEntity().Should().BeEquivalentTo(new
        {
            Id = Guid.Empty,
            Name = "Boris",
            Surname = "Kral",
            DayOfBirth = new DateOnly(1985, 3, 4),
            DayOfNameDay = new DateOnly(2000, 5, 2),
            ImageUrl = "StaticFiles/Images/boris.png",
            UserId = string.Empty
        });
    }

    [Fact]
    public void ToEntity_KeepsMissingOptionalValuesNull()
    {
        var person = new PersonForCreationDto { Name = "Boris", DayOfNameDay = new DateOnly(2000, 5, 2) }.ToEntity();

        person.Surname.Should().BeNull();
        person.DayOfBirth.Should().BeNull();
        person.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void ApplyTo_OverwritesEveryEditableFieldAndKeepsIdAndOwner()
    {
        var person = StoredPerson();
        var id = person.Id;

        new PersonForUpdateDto
        {
            Name = "Hana",
            Surname = "Kralova",
            DayOfBirth = new DateOnly(1991, 6, 2),
            DayOfNameDay = new DateOnly(2000, 5, 31),
            ImageUrl = "StaticFiles/Images/hana.png"
        }.ApplyTo(person);

        person.Should().BeEquivalentTo(new
        {
            Id = id,
            Name = "Hana",
            Surname = "Kralova",
            DayOfBirth = new DateOnly(1991, 6, 2),
            DayOfNameDay = new DateOnly(2000, 5, 31),
            ImageUrl = "StaticFiles/Images/hana.png",
            UserId = "u1"
        });
    }

    [Fact]
    public void ApplyTo_NullOptionalValues_ClearThem()
    {
        var person = StoredPerson();

        new PersonForUpdateDto { Name = "Anna", DayOfNameDay = new DateOnly(2000, 7, 26) }.ApplyTo(person);

        person.Surname.Should().BeNull();
        person.DayOfBirth.Should().BeNull();
        person.ImageUrl.Should().BeNull();
    }
}
