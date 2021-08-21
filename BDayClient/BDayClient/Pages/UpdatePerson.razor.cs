using AutoMapper;
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
	public partial class UpdatePerson : IDisposable
	{
		private Person _person;
		private PersonForUpdateDto _personForUpdateDto { get; set; } = new PersonForUpdateDto();
		private EditContext _editContext;
		private bool formInvalid = true;

		[Inject]
		public IPersonHttpRepository PersonRepo { get; set; }

		[Inject]
		public HttpInterceptorService Interceptor { get; set; }

		[Inject]
		public IToastService ToastService { get; set; }

		[Inject]
		public IMapper _mapper { get; set; }

		[Parameter]
		public Guid Id { get; set; }

		protected async override Task OnInitializedAsync()
		{
			_person = await PersonRepo.GetPerson(Id);
			_mapper.Map(_person, _personForUpdateDto);

			_editContext = new EditContext(_personForUpdateDto);
			_editContext.OnFieldChanged += HandleFieldChanged;
			Interceptor.RegisterEvent();
		}

		private void HandleFieldChanged(object sender, FieldChangedEventArgs e)
		{
			formInvalid = !_editContext.Validate();
			StateHasChanged();
		}

		private async Task Update()
		{
			await PersonRepo.UpdatePerson(_person.Id, _personForUpdateDto);

			ToastService.ShowSuccess($"Action successful. " +
				$"Person \"{_personForUpdateDto.Name}\" successfully updated.");
		}

		private void AssignImageUrl(string imgUrl)
		{
			formInvalid = _personForUpdateDto.ImageUrl == imgUrl ? true : false;
			_personForUpdateDto.ImageUrl = imgUrl;
			StateHasChanged();
		}
		

		public void Dispose()
		{
			Interceptor.DisposeEvent();
			_editContext.OnFieldChanged -= HandleFieldChanged;
		}
	}
}
