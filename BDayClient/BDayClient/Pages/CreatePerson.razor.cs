using BDayClient.HttpInterceptor;
using BDayClient.HttpRepository;
using Blazored.Toast.Services;
using Entities.DataTransferObjects;
using Entities.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BDayClient.Pages
{
    public partial class CreatePerson
    {
		private PersonForCreationDto _person = new PersonForCreationDto();
		private EditContext _editContext;
		private bool formInvalid = true;

		[Inject]
		public IPersonHttpRepository PersonRepo { get; set; }

		[Inject]
		public HttpInterceptorService Interceptor { get; set; }

		[Inject]
		public IToastService ToastService { get; set; }

		protected override void OnInitialized()
		{
			_person.DayOfBirth = new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
			_editContext = new EditContext(_person);
			_editContext.OnFieldChanged += HandleFieldChanged;
			Interceptor.RegisterEvent();
		}

		private void HandleFieldChanged(object sender, FieldChangedEventArgs e)
		{
			formInvalid = !_editContext.Validate();
			StateHasChanged();
		}

		private async Task Create()
		{
			await PersonRepo.CreatePerson(_person);

			ToastService.ShowSuccess($"Action successful. " +
				$"Person \"{_person.Name}\" successfully added.");
			_person = new PersonForCreationDto();
			_editContext.OnValidationStateChanged += ValidationChanged;
			_editContext.NotifyValidationStateChanged();
		}

		private void ValidationChanged(object sender, ValidationStateChangedEventArgs e)
		{
			formInvalid = true;
			_editContext.OnFieldChanged -= HandleFieldChanged;
			_editContext = new EditContext(_person);
			_editContext.OnFieldChanged += HandleFieldChanged;
			_editContext.OnValidationStateChanged -= ValidationChanged;
		}

		private void AssignImageUrl(string imgUrl)
			=> _person.ImageUrl = imgUrl;

		public void Dispose()
		{
			Interceptor.DisposeEvent();
			_editContext.OnFieldChanged -= HandleFieldChanged;
			_editContext.OnValidationStateChanged -= ValidationChanged;
		}
	}
}
