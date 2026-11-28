using BDayClient.Shared;
using Contracts.DataTransferObjects.User;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Components;

public partial class UsersTable
{
    /// <summary>The users to list; null while they are loading.</summary>
    [Parameter] public IReadOnlyList<UserLite>? Users { get; set; }

    [Parameter] public string? LoggedUser { get; set; }

    /// <summary>The admin role switch was turned on.</summary>
    [Parameter] public EventCallback<UserLite> OnGrantAdminRole { get; set; }

    /// <summary>The admin role switch was turned off.</summary>
    [Parameter] public EventCallback<UserLite> OnRemoveAdminRole { get; set; }

    [Parameter] public EventCallback<UserLite> OnDeleteUser { get; set; }

    [Parameter] public EventCallback<UserLite2StepsAuthDto> OnChange2StepsAuthorization { get; set; }

    private Confirmation? _confirmation;
    private UserLite? _userToDelete;

    private bool IsLoggedUser(UserLite user) =>
        string.Equals(user.Email, LoggedUser, StringComparison.OrdinalIgnoreCase);

    private Task OnAdminRoleChange(bool isAdmin, UserLite user) =>
        isAdmin ? OnGrantAdminRole.InvokeAsync(user) : OnRemoveAdminRole.InvokeAsync(user);

    private Task OnTwoFactorChange(bool enabled, UserLite user) =>
        OnChange2StepsAuthorization.InvokeAsync(new UserLite2StepsAuthDto
        {
            Email = user.Email,
            TwoFactorEnabled = enabled
        });

    private void CallConfirmationModal(UserLite userToDelete)
    {
        _userToDelete = userToDelete;
        _confirmation?.Show();
    }

    private async Task DeleteUser()
    {
        _confirmation?.Hide();

        if (_userToDelete is not null)
            await OnDeleteUser.InvokeAsync(_userToDelete);
    }
}
