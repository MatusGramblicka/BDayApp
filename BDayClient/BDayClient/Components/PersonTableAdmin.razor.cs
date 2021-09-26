using Entities.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;

namespace BDayClient.Components
{
    public partial class PersonTableAdmin
	{
		[Parameter]
		public List<Person> Persons { get; set; }		

		public DateTime timeNow = DateTime.Today;
		public int age;
		public int ageNameDay;

		private bool DisplayPerson(DateTime DayOfBirth, DateTime DayOfNameDay)
		{
			age = timeNow.Year - DayOfBirth.Year;
			int numOfDays = (DayOfBirth - timeNow.AddYears(-age)).Days;

			ageNameDay = timeNow.Year - DayOfNameDay.Year;
			int numOfDaysNameDay = (DayOfNameDay - timeNow.AddYears(-ageNameDay)).Days;

			bool result = ((numOfDays < 30) && (numOfDays >= 0)) || ((numOfDaysNameDay < 30) && (numOfDaysNameDay >= 0));
			return result;
		}
	}
}
