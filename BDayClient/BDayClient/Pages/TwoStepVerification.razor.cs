using BDayClient.Http;
using BDayClient.HttpRepository;
using BDayClient.Pocos;
using Contracts.DataTransferObjects.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.Pages;

public partial class TwoStepVerification
{
    private TwoFactorVerification _twoFactorVerification = new();
    private string? _returnUrl;

    private bool _showError;
    private string? _error;

    [Inject] public IAuthenticationService AuthService { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

        var queryStrings = QueryHelpers.ParseQuery(uri.Query);
        if (queryStrings.TryGetValue("email", out var email) &&
            queryStrings.TryGetValue("provider", out var provider))
        {
            _twoFactorVerification.Email = email.ToString();
            _twoFactorVerification.Provider = provider.ToString();
            _returnUrl = queryStrings.TryGetValue("returnUrl", out var returnUrl) ? returnUrl.ToString() : null;
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

        AuthResponseDto result;
        try
        {
            result = await AuthService.LoginVerification(twoFactorDto);
        }
        catch (ApiException ex)
        {
            if (!ex.Handled)
                ShowError(ex.Message);
            return;
        }

        if (result.IsAuthSuccessful)
            NavigationManager.NavigateTo(LoginNavigation.GetSafeReturnUrl(_returnUrl));
        else
            ShowError(result.ErrorMessage);
    }

    private void ShowError(string message)
    {
        _error = message;
        _showError = true;
    }
}
