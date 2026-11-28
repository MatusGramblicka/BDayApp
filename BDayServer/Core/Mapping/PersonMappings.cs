using Contracts.DataTransferObjects.Person;
using Entities.Models;

namespace Core.Mapping;

/// <summary>Maps persons to and from their DTOs (hand-written; replaced AutoMapper).</summary>
public static class PersonMappings
{
    public static PersonDto ToDto(this Person person) => new()
    {
        Id = person.Id,
        Name = person.Name,
        Surname = person.Surname,
        DayOfBirth = person.DayOfBirth,
        DayOfNameDay = person.DayOfNameDay,
        ImageUrl = person.ImageUrl
    };

    /// <summary>A new person. The caller sets <see cref="Person.UserId"/>; EF Core assigns the Id when it is added.</summary>
    public static Person ToEntity(this PersonForCreationDto dto) => new()
    {
        Name = dto.Name,
        Surname = dto.Surname,
        DayOfBirth = dto.DayOfBirth,
        DayOfNameDay = dto.DayOfNameDay,
        ImageUrl = dto.ImageUrl
    };

    /// <summary>
    /// Overwrites every field the user edits, also with null: a null Surname, DayOfBirth or ImageUrl clears it.
    /// Id and UserId are kept.
    /// </summary>
    public static void ApplyTo(this PersonForUpdateDto dto, Person person)
    {
        person.Name = dto.Name;
        person.Surname = dto.Surname;
        person.DayOfBirth = dto.DayOfBirth;
        person.DayOfNameDay = dto.DayOfNameDay;
        person.ImageUrl = dto.ImageUrl;
    }
}
