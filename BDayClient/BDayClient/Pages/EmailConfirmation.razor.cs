using BDayClient.Http;
using BDayClient.HttpRepository;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.Pages;

public partial class EmailConfirmation
{
    private bool _showSuccess;

    // Null unless the confirmation failed; then the server's messages (possibly none, when a toast already explained it).
    private IReadOnlyList<string>? _errors;

    [Inject]
    public IAuthenticationService AuthService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

        var queryStrings = QueryHelpers.ParseQuery(uri.Query);
        if (!queryStrings.TryGetValue("email", out var email) ||
            !queryStrings.TryGetValue("token", out var token))
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        try
        {
            // Any 2xx is success (the API answers 204).
            var result = await AuthService.EmailConfirmation(email.ToString(), token.ToString());
            if (result.Succeeded)
                _showSuccess = true;
            else
                _errors = result.Errors;
        }
        catch (ApiException ex)
        {
            _errors = ex.Handled ? [] : ex.Errors;
        }
    }
}
