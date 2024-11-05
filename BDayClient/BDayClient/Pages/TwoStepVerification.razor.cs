using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Entities.DataTransferObjects.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.Pages;

public partial class TwoStepVerification
{
    private TwoFactorVerification _twoFactorVerification = new();

    private bool _showError;
    private string _error;

    [Inject] public IAuthenticationService AuthService { get; set; }

    [Inject] public NavigationManager NavigationManager { get; set; }

    protected override void OnInitialized()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

        var queryStrings = QueryHelpers.ParseQuery(uri.Query);
        if (queryStrings.TryGetValue("email", out var email) &&
            queryStrings.TryGetValue("provider", out var provider))
        {
            _twoFactorVerification.Email = email;
            _twoFactorVerification.Provider = provider;
        }
        else
            NavigationManager.NavigateTo("/");
    }

    private async Task Submit()
    {
        _showError = false;

        var twoFactorDto = new TwoFactorVerificationDto
        {
            Email = _twoFactorVerification.Email,
            Provider = _twoFactorVerification.Provider,
            TwoFactorToken = _twoFactorVerification.TwoFactorToken
        };

        var result = await AuthService.LoginVerification(twoFactorDto);
        if (result.IsAuthSuccessful)
            NavigationManager.NavigateTo("/");
        else
        {
            _error = result.ErrorMessage;
            _showError = true;
        }
    }
}