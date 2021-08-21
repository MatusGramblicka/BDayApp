using BDayClient.Shared;
using Entities.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BDayClient.Components
{
	public partial class PersonTable
	{
		[Parameter]
		public List<Person> Persons { get; set; }

		[Parameter]
		public EventCallback<Guid> OnDelete { get; set; }

		[Parameter]
		public bool hideButtons { get; set; } = false;

		public DateTime timeNow = DateTime.Today;
		public int age;

		private Confirmation _confirmation;
		private Guid _personIdToDelete;

		private void CallConfirmationModal(Guid id)
		{
			_personIdToDelete = id;
			_confirmation.Show();
		}

		private async Task DeletePerson()
		{
			_confirmation.Hide();
			await OnDelete.InvokeAsync(_personIdToDelete);
		}

		private bool DisplayPerson(DateTime DayOfBirth)
		{
			if (hideButtons)
			{
				age =  timeNow.Year-DayOfBirth.Year;				
				int numOfDays = (DayOfBirth - timeNow.AddYears(-age)).Days;
				bool result = (numOfDays < 30) && (numOfDays >= 0);
				return result;
			}
			return true;
		}		
	}
}
