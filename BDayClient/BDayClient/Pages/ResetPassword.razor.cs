using BDayClient.Http;
using BDayClient.HttpRepository;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.Pages;

public partial class ResetPassword
{
    private BDayClient.Pocos.ResetPassword _resetPassword = new();

    private bool _showSuccess;
    private IReadOnlyList<string> _errors = [];

    [Inject] public IAuthenticationService AuthService { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var queryStrings = QueryHelpers.ParseQuery(uri.Query);
        if (queryStrings.TryGetValue("email", out var email) &&
            queryStrings.TryGetValue("token", out var token))
        {
            _resetPassword.Email = email.ToString();
            _resetPassword.Token = token.ToString();
        }
        else
        {
            NavigationManager.NavigateTo("/");
        }
    }

    private async Task Submit()
    {
        _showSuccess = false;
        _errors = [];

        try
        {
            var result = await AuthService.ResetPassword(_resetPassword);
            if (result.IsResetPasswordSuccessful)
                _showSuccess = true;
            else
                _errors = result.Errors.ToList();
        }
        catch (ApiException ex)
        {
            if (!ex.Handled)
                _errors = ex.Errors;
        }
    }
}
