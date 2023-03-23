using AutoMapper;
using BDayClient.HttpInterceptor;
using BDayClient.HttpRepository;
using Blazored.Toast.Services;
using Entities.DataTransferObjects.Person;
using Entities.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Threading.Tasks;

namespace BDayClient.Pages
{
    public partial class UpdatePerson : IDisposable
    {
        private Person _person;
        private PersonForUpdateDto PersonForUpdateDto { get; set; } = new();
        private EditContext _editContext;
        private bool formInvalid = true;

        [Inject] public IPersonHttpRepository PersonRepo { get; set; }

        [Inject] public HttpInterceptorService Interceptor { get; set; }

        [Inject] public IToastService ToastService { get; set; }

        [Inject] public IMapper Mapper { get; set; }

        [Parameter] public Guid Id { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _person = await PersonRepo.GetPerson(Id);
            Mapper.Map(_person, PersonForUpdateDto);

            _editContext = new EditContext(PersonForUpdateDto);
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
            await PersonRepo.UpdatePerson(_person.Id, PersonForUpdateDto);

            ToastService.ShowSuccess($"Action successful. " +
                                     $"Person \"{PersonForUpdateDto.Name}\" successfully updated.");
        }

        private void AssignImageUrl(string imgUrl)
        {
            formInvalid = PersonForUpdateDto.ImageUrl == imgUrl ? true : false;
            PersonForUpdateDto.ImageUrl = imgUrl;
            StateHasChanged();
        }

        public void Dispose()
        {
            Interceptor.DisposeEvent();
            _editContext.OnFieldChanged -= HandleFieldChanged;
        }
    }
}
