using BDayClient.Pocos;
using Contracts.DataTransferObjects.Person;

namespace BDayClient.UnitTests.Pocos;

/// <summary>The hand-written mappings that replaced AutoMapper.</summary>
public class PersonMappingTests
{
    [Fact]
    public void PersonDto_MapsToPersonForUpdate()
    {
        var person = new PersonDto
        {
            Id = Guid.NewGuid(),
            Name = "Anna",
            Surname = "Novak",
            DayOfBirth = new DateOnly(1990, 5, 1),
            DayOfNameDay = new DateOnly(2000, 7, 26),
            ImageUrl = "StaticFiles/Images/anna.png"
        };

        var update = PersonForUpdate.FromDto(person);

        update.Should().BeEquivalentTo(new
        {
            person.Name, person.Surname, person.DayOfBirth, person.DayOfNameDay, person.ImageUrl
        });
    }

    [Fact]
    public void PersonForUpdate_MapsToDto_WithEmptyOptionalValuesAsNull()
    {
        var update = new PersonForUpdate
        {
            Name = "Anna", Surname = " ", DayOfBirth = null, DayOfNameDay = new DateOnly(2000, 7, 26), ImageUrl = ""
        };

        update.ToDto().Should().BeEquivalentTo(new PersonForUpdateDto
        {
            Name = "Anna", Surname = null, DayOfBirth = null, DayOfNameDay = new DateOnly(2000, 7, 26), ImageUrl = null
        });
    }

    [Fact]
    public void PersonForCreation_MapsToDto()
    {
        var creation = new PersonForCreation
        {
            Name = "Anna", Surname = "Novak", DayOfBirth = new DateOnly(1990, 5, 1),
            DayOfNameDay = new DateOnly(2000, 7, 26), ImageUrl = "StaticFiles/Images/anna.png"
        };

        creation.ToDto().Should().BeEquivalentTo(new PersonForCreationDto
        {
            Name = "Anna", Surname = "Novak", DayOfBirth = new DateOnly(1990, 5, 1),
            DayOfNameDay = new DateOnly(2000, 7, 26), ImageUrl = "StaticFiles/Images/anna.png"
        });
    }

    [Fact]
    public void PersonForUpdate_Copy_IsASeparateObjectWithTheSameValues()
    {
        var update = new PersonForUpdate { Name = "Anna", DayOfNameDay = new DateOnly(2000, 7, 26) };

        var copy = update.Copy();

        copy.Should().NotBeSameAs(update);
        copy.Should().BeEquivalentTo(update);
    }
}
