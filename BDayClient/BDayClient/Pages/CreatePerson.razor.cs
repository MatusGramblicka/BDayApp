using BDayClient.HttpInterceptor;
using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Blazored.Toast.Services;
using Entities.DataTransferObjects.Person;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BDayClient.Pages;

public partial class CreatePerson
{
    private PersonForCreation _person = new();
    private EditContext _editContext;
    private bool _formInvalid = true;        

    [Inject]
    public IPersonHttpRepository PersonRepo { get; set; }

    [Inject]
    public HttpInterceptorService Interceptor { get; set; }

    [Inject]
    public IToastService ToastService { get; set; }      

    protected override void OnInitialized()
    {          
        _person.DayOfBirth = new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
        _person.DayOfNameDay = new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
        _editContext = new EditContext(_person);
        _editContext.OnFieldChanged += HandleFieldChanged;
        Interceptor.RegisterEvent();
    }

    private void HandleFieldChanged(object sender, FieldChangedEventArgs e)
    {
        _formInvalid = !_editContext.Validate();
        StateHasChanged();
    }

    private async Task Create()
    {
        var personForCreationDto = new PersonForCreationDto
        {
            DayOfBirth = _person.DayOfBirth,
            DayOfNameDay = _person.DayOfNameDay,
            Name = _person.Name,
            Surname = _person.Surname,
            ImageUrl = _person.ImageUrl
        };

        await PersonRepo.CreatePerson(personForCreationDto);

        ToastService.ShowSuccess("Action successful. " +
                                 $"Person \"{_person.Name}\" successfully added.");
        _person = new PersonForCreation();
        _editContext.OnValidationStateChanged += ValidationChanged;
        _editContext.NotifyValidationStateChanged();
    }

    private void ValidationChanged(object sender, ValidationStateChangedEventArgs e)
    {
        _formInvalid = true;
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