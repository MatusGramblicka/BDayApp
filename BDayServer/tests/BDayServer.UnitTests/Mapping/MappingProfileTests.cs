using AutoMapper;
using BDayServer.UnitTests.TestUtilities;
using Contracts.DataTransferObjects.Person;
using Contracts.DataTransferObjects.User;
using Entities.Models;

namespace BDayServer.UnitTests.Mapping;

public class MappingProfileTests
{
    private readonly IMapper _mapper = TestMapper.Create();

    [Fact]
    public void Person_MapsToPersonDto()
    {
        var person = new Person
        {
            Id = Guid.NewGuid(),
            Name = "Anna",
            Surname = "Novak",
            DayOfBirth = new DateOnly(1990, 5, 1),
            DayOfNameDay = new DateOnly(2000, 7, 26),
            ImageUrl = "StaticFiles/Images/anna.png",
            UserId = "u1"
        };

        var dto = _mapper.Map<PersonDto>(person);

        dto.Should().BeEquivalentTo(new
        {
            person.Id, person.Name, person.Surname, person.DayOfBirth, person.DayOfNameDay, person.ImageUrl
        });
    }

    [Fact]
    public void PersonForCreationDto_MapsToPerson()
    {
        var dto = new PersonForCreationDto
        {
            Name = "Boris",
            Surname = "Kral",
            DayOfBirth = null,
            DayOfNameDay = new DateOnly(2000, 5, 2),
            ImageUrl = null
        };

        var person = _mapper.Map<Person>(dto);

        person.Name.Should().Be("Boris");
        person.Surname.Should().Be("Kral");
        person.DayOfBirth.Should().BeNull();
        person.DayOfNameDay.Should().Be(new DateOnly(2000, 5, 2));
        person.Id.Should().BeEmpty();
    }

    [Fact]
    public void PersonForUpdateDto_MapsOntoExistingPerson_KeepingIdentity()
    {
        var id = Guid.NewGuid();
        var person = new Person { Id = id, Name = "Old", UserId = "u1" };

        _mapper.Map(new PersonForUpdateDto { Name = "New", DayOfNameDay = new DateOnly(2000, 3, 3) }, person);

        person.Id.Should().Be(id);
        person.UserId.Should().Be("u1");
        person.Name.Should().Be("New");
        person.DayOfNameDay.Should().Be(new DateOnly(2000, 3, 3));
    }

    [Fact]
    public void User_MapsToUserLite()
    {
        var user = new User { Email = "a@test.com", IsAdmin = true, TwoFactorEnabled = true };

        var lite = _mapper.Map<UserLite>(user);

        lite.Should().BeEquivalentTo(new UserLite { Email = "a@test.com", IsAdmin = true, TwoFactorEnabled = true });
    }
}
