using Entities.DataTransferObjects.Person;
using System.Linq.Expressions;
using Entities.Models;

namespace Repository.Projections;

public static class PersonProjection
{
    public static Expression<Func<Person, PersonDto>> GetPersonSelected()
    {
        return person => new PersonDto
        {
            Id = person.Id,
            DayOfBirth = person.DayOfBirth,
            DayOfNameDay = person.DayOfNameDay,
            ImageUrl = person.ImageUrl,
            Name = person.Name,
            Surname = person.Surname
        };
    }
}