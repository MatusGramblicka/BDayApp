using AutoMapper;
using BDayClient.Pocos;
using Entities.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace BDayClient.UnitTests;

public class MappingProfileTests
{
    [Fact]
    public void Person_MapsToPersonForUpdate()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance)
            .CreateMapper();
        var person = new Person
        {
            Name = "Anna",
            Surname = "Novak",
            DayOfBirth = new DateOnly(1990, 5, 1),
            DayOfNameDay = new DateOnly(2000, 7, 26),
            ImageUrl = "img.png"
        };

        var update = mapper.Map<PersonForUpdate>(person);

        update.Should().BeEquivalentTo(new
        {
            person.Name, person.Surname, person.DayOfBirth, person.DayOfNameDay, person.ImageUrl
        });
    }
}
