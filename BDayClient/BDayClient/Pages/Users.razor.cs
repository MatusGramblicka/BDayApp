using BDayClient.Http;
using BDayClient.HttpRepository;
using Blazored.Toast.Services;
using Contracts.DataTransferObjects.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace BDayClient.Pages;

public partial class Users
{
    // Null while loading.
    private List<UserLite>? _users;
    private string? _loggedUserName;
    private bool _loadFailed;

    [CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [Inject] public IUsersHttpRepository UsersHttpRepository { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask is not null)
            _loggedUserName = (await AuthenticationStateTask).User.Identity?.Name;

        await GetUsers();
    }

    private async Task GetUsers()
    {
        try
        {
            _users = await UsersHttpRepository.GetUsers();
            _loadFailed = false;
        }
        catch (ApiException ex)
        {
            _loadFailed = _users is null;
            ToastService.ShowApiError(ex);
        }
    }

    private Task UpdateUser(UserLite user) =>
        ChangeUser(() => UsersHttpRepository.UpdateUser(user), $"{user.Email} is now an administrator.");

    private Task RemoveAdminRole(UserLite user) =>
        ChangeUser(() => UsersHttpRepository.RemoveAdminRole(user), $"{user.Email} is no longer an administrator.");

    private Task DeleteUser(UserLite user) =>
        ChangeUser(() => UsersHttpRepository.DeleteUser(user), $"{user.Email} was deleted.");

    private Task Change2StepsAuthorization(UserLite2StepsAuthDto user2StepsAuth) =>
        ChangeUser(() => UsersHttpRepository.SetTwoFactorAuthorization(user2StepsAuth),
            $"Two-factor authentication is now {(user2StepsAuth.TwoFactorEnabled ? "on" : "off")} for {user2StepsAuth.Email}.");

    // Reloads the list after success and failure alike, so it always shows what the server has
    // (e.g. a refused two-factor change flips the switch back).
    private async Task ChangeUser(Func<Task> change, string successMessage)
    {
        try
        {
            await change();
            ToastService.ShowSuccess(successMessage);
        }
        catch (ApiException ex)
        {
            ToastService.ShowApiError(ex);
        }

        await GetUsers();
    }
}
