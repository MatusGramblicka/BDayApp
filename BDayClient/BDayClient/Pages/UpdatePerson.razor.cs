using AutoMapper;
using BDayClient.HttpInterceptor;
using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Blazored.Toast.Services;
using Contracts.DataTransferObjects.Person;
using Entities.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BDayClient.Pages;

public partial class UpdatePerson : IDisposable
{
    // todo do not use dto, only after map in update
    //private PersonForUpdateDto PersonForUpdateDto { get; set; } = new();
    private PersonForUpdate _personForUpdate = new();

    private Person _person;
    private EditContext _editContext;
    private bool formInvalid = true;
    private bool _alreadyDisposed;

    [Inject] public IPersonHttpRepository PersonRepo { get; set; }

    [Inject] public HttpInterceptorService Interceptor { get; set; }

    [Inject] public IToastService ToastService { get; set; }

    [Inject] public IMapper Mapper { get; set; }

    [Parameter] public Guid Id { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        _person = await PersonRepo.GetPerson(Id);
        Mapper.Map(_person, _personForUpdate); 

        _editContext = new EditContext(_personForUpdate);
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
        var personForUpdateDto = new PersonForUpdateDto
        {
            DayOfBirth = _personForUpdate.DayOfBirth,
            DayOfNameDay = _personForUpdate.DayOfNameDay,
            ImageUrl = _personForUpdate.ImageUrl,
            Name = _personForUpdate.Name,
            Surname = _personForUpdate.Surname
        };
     
        await PersonRepo.UpdatePerson(_person.Id, personForUpdateDto);

        ToastService.ShowSuccess("Action successful. " +
                                 $"Person \"{personForUpdateDto.Name}\" successfully updated.");
    }

    private void AssignImageUrl(string imgUrl)
    {
        formInvalid = _personForUpdate.ImageUrl == imgUrl;
        _personForUpdate.ImageUrl = imgUrl;
        StateHasChanged();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_alreadyDisposed)
            return;

        if (disposing)
        {
            Interceptor.DisposeEvent();
            _editContext.OnFieldChanged -= HandleFieldChanged;
            _alreadyDisposed = true;
        }
    }

    ~UpdatePerson()
    {
        Dispose(disposing: false);
    }
}