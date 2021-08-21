using BDayClient.HttpRepository;
using Entities.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BDayClient.Pages
{
    public partial class PersonDetails
    {
		public Person Person { get; set; } = new Person();

		[Inject]
		public IPersonHttpRepository PersonRepo { get; set; }

		[Parameter]
		public Guid PersonId { get; set; }

		protected async override Task OnInitializedAsync()
		{
			Person = await PersonRepo.GetPerson(PersonId);
		}
	}
}
