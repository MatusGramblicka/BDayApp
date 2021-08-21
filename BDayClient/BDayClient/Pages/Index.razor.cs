using BDayClient.HttpInterceptor;
using BDayClient.HttpRepository;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BDayClient.Pages
{
    public partial class Index
    {
		public List<Person> PersonList { get; set; } = new List<Person>();
		public MetaData MetaData { get; set; } = new MetaData();

		public PersonParameters _personParameters = new PersonParameters();

		[Inject]
		public IPersonHttpRepository PersonRepo { get; set; }

		[Inject]
		public HttpInterceptorService Interceptor { get; set; }

		protected async override Task OnInitializedAsync()
		{
			Interceptor.RegisterEvent();
			await GetPersons();
		}	

		private async Task GetPersons()
		{
			var pagingResponse = await PersonRepo.GetPersons(_personParameters);

			PersonList = pagingResponse.Items;
			MetaData = pagingResponse.MetaData;
		}	

		public void Dispose() => Interceptor.DisposeEvent();
	}
}
