using Entities.Models;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

public partial class PersonTableAdmin
{
    [Parameter]
    public List<Person> Persons { get; set; }

    private bool DisplayPerson(DateOnly? dayOfBirth, DateOnly dayOfNameDay)
    {
        DateTime timeNow = DateTime.Today;

        // solve nullability
        var age = timeNow.Year - dayOfBirth.Value.Year;
        var numOfDays = (/*dayOfBirth*/dayOfBirth.Value.ToDateTime(TimeOnly.MinValue) - timeNow.AddYears(-age)).Days;

        var ageNameDay = timeNow.Year - dayOfNameDay.Year;
        var numOfDaysNameDay = (/*dayOfNameDay*/dayOfNameDay.ToDateTime(TimeOnly.MinValue) - timeNow.AddYears(-ageNameDay)).Days;

        var result = (numOfDays < 30 && numOfDays >= 0) || (numOfDaysNameDay < 30 && numOfDaysNameDay >= 0);
        return result;
    }
}