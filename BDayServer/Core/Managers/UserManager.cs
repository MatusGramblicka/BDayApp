using Contracts.Authorization;
using Contracts.DataTransferObjects.User;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Core.Services;
using Core.Services.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Repository.Services;
using Identity = Microsoft.AspNetCore.Identity;

namespace Core.Managers;

/// <summary>User administration (administrators only).</summary>
public class UserManager(
    Identity.UserManager<User> userManager,
    Identity.RoleManager<Identity.IdentityRole> roleManager,
    IGetUserProvider currentUser,
    IPersonMirror personMirror) : IUserManager
{
    public async Task<List<UserLite>> GetUsersAsync()
    {
        var query = userManager.Users
            .OrderBy(u => u.Email)
            .Select(u => new UserLite
            {
                Email = u.Email ?? string.Empty,
                IsAdmin = u.IsAdmin,
                TwoFactorEnabled = u.TwoFactorEnabled
            });

        return query.Provider is IAsyncQueryProvider ? await query.ToListAsync() : query.ToList();
    }

    /// <summary>Gives the user the administrator role.</summary>
    public async Task UpdateUser(UserLite userForUpdate)
    {
        var user = await FindUserAsync(userForUpdate.Email);

        await roleManager.EnsureRoleExistsAsync(AppRoles.Administrator);

        if (!await userManager.IsInRoleAsync(user, AppRoles.Administrator))
            (await userManager.AddToRoleAsync(user, AppRoles.Administrator)).ThrowIfFailed("Adding the administrator role");

        user.IsAdmin = true;
        (await userManager.UpdateAsync(user)).ThrowIfFailed("Updating the user");
    }

    public async Task RemoveAdminRole(UserLite userForUpdate)
    {
        var user = await FindUserAsync(userForUpdate.Email);

        await EnsureNotSelfOrLastAdministratorAsync(user,
            "remove the administrator role from your own account",
            "remove the administrator role from the last administrator");

        if (await userManager.IsInRoleAsync(user, AppRoles.Administrator))
            (await userManager.RemoveFromRoleAsync(user, AppRoles.Administrator))
                .ThrowIfFailed("Removing the administrator role");

        user.IsAdmin = false;
        (await userManager.UpdateAsync(user)).ThrowIfFailed("Updating the user");
    }

    public async Task DeleteUser(UserLite userForDeletion)
    {
        var user = await FindUserAsync(userForDeletion.Email);

        await EnsureNotSelfOrLastAdministratorAsync(user,
            "delete your own account",
            "delete the last administrator");

        foreach (var login in await userManager.GetLoginsAsync(user))
            (await userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey))
                .ThrowIfFailed("Removing a login");

        foreach (var role in await userManager.GetRolesAsync(user))
            (await userManager.RemoveFromRoleAsync(user, role)).ThrowIfFailed("Removing a role");

        (await userManager.DeleteAsync(user)).ThrowIfFailed("Deleting the user");

        // Otherwise the birthday e-mails would still be sent to the deleted user.
        await personMirror.DeleteUserAsync(user.Id);
    }

    public async Task SetTwoFactorAuthorization(UserLite2StepsAuthDto userForUpdate)
    {
        var user = await FindUserAsync(userForUpdate.Email);

        (await userManager.SetTwoFactorEnabledAsync(user, userForUpdate.TwoFactorEnabled))
            .ThrowIfFailed("Changing the 2-step verification");
    }

    private async Task<User> FindUserAsync(string email) =>
        await userManager.FindByNameAsync(email) ?? throw new UserNotExistException("User does not exist");

    private async Task EnsureNotSelfOrLastAdministratorAsync(User user, string selfAction, string lastAdminAction)
    {
        if (currentUser.UserName is not null &&
            (string.Equals(user.UserName, currentUser.UserName, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(user.Email, currentUser.UserName, StringComparison.OrdinalIgnoreCase)))
            throw new UserManagementException($"You cannot {selfAction}.");

        if (await userManager.IsInRoleAsync(user, AppRoles.Administrator) &&
            (await userManager.GetUsersInRoleAsync(AppRoles.Administrator)).Count <= 1)
            throw new UserManagementException($"You cannot {lastAdminAction}.");
    }
}
