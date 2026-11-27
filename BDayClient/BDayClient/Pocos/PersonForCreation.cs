using Contracts.DataTransferObjects.Person;

namespace BDayClient.Pocos;

public class PersonForCreation : PersonForManipulation
{
    public PersonForCreationDto ToDto() => new()
    {
        Name = Name,
        Surname = NullIfEmpty(Surname),
        DayOfBirth = DayOfBirth,
        DayOfNameDay = DayOfNameDay,
        ImageUrl = NullIfEmpty(ImageUrl)
    };
}
