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

        bool birthDayPartialResult = false;
        bool nameDayPartialResult = false;

        if (dayOfBirth is not null)
        {
            var age = timeNow.Year - dayOfBirth.Value.Year;
            var numOfDays = (dayOfBirth.Value.ToDateTime(TimeOnly.MinValue) - timeNow.AddYears(-age)).Days;

            birthDayPartialResult = numOfDays < 30 && numOfDays >= 0;
        }

        var ageNameDay = timeNow.Year - dayOfNameDay.Year;
        var numOfDaysNameDay = (dayOfNameDay.ToDateTime(TimeOnly.MinValue) - timeNow.AddYears(-ageNameDay)).Days;

        nameDayPartialResult = numOfDaysNameDay < 30 && numOfDaysNameDay >= 0;

        var result = birthDayPartialResult || nameDayPartialResult;
        return result;
    }
}