using BDayClient.Http;
using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Contracts.DataTransferObjects.Auth;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class Registration
{
    private UserForRegistration _userForRegistration = new();
    private bool _registered;

    [Inject] public IAuthenticationService AuthenticationService { get; set; } = default!;

    public bool ShowRegistrationErrors { get; set; }
    public IEnumerable<string> Errors { get; set; } = [];

    public async Task Register()
    {
        ShowRegistrationErrors = false;

        ResponseDto result;
        try
        {
            result = await AuthenticationService.RegisterUser(_userForRegistration);
        }
        catch (ApiException ex)
        {
            if (!ex.Handled)
                ShowErrors(ex.Errors);
            return;
        }

        if (result.IsSuccessfulRegistration)
            _registered = true; // the account can only be used after the email address was confirmed
        else
            ShowErrors(result.Errors);
    }

    private void ShowErrors(IEnumerable<string> errors)
    {
        Errors = errors;
        ShowRegistrationErrors = true;
    }
}
