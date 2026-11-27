using Contracts.DataTransferObjects.Person;

namespace BDayClient.Pocos;

public class PersonForUpdate : PersonForManipulation
{
    public static PersonForUpdate FromDto(PersonDto person) => new()
    {
        Name = person.Name,
        Surname = person.Surname,
        DayOfBirth = person.DayOfBirth,
        DayOfNameDay = person.DayOfNameDay,
        ImageUrl = person.ImageUrl
    };

    public PersonForUpdate Copy() => (PersonForUpdate)MemberwiseClone();

    public PersonForUpdateDto ToDto() => new()
    {
        Name = Name,
        Surname = NullIfEmpty(Surname),
        DayOfBirth = DayOfBirth,
        DayOfNameDay = DayOfNameDay,
        ImageUrl = NullIfEmpty(ImageUrl)
    };
}
