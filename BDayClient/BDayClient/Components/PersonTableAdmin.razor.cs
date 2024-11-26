﻿using Entities.Models;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

public partial class PersonTableAdmin
{
    [Parameter]
    public List<Person> Persons { get; set; }

    private bool DisplayPerson(DateTime dayOfBirth, DateTime dayOfNameDay)
    {
        DateTime timeNow = DateTime.Today;

        var age = timeNow.Year - dayOfBirth.Year;
        var numOfDays = (dayOfBirth - timeNow.AddYears(-age)).Days;

        var ageNameDay = timeNow.Year - dayOfNameDay.Year;
        var numOfDaysNameDay = (dayOfNameDay - timeNow.AddYears(-ageNameDay)).Days;

        var result = (numOfDays < 30 && numOfDays >= 0) || (numOfDaysNameDay < 30 && numOfDaysNameDay >= 0);
        return result;
    }
}