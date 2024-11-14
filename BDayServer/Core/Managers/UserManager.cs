using Contracts.Exceptions;
using Contracts.Managers;
using Entities;
using Entities.DataTransferObjects.User;
using Identity =  Microsoft.AspNetCore.Identity;

namespace Core.Managers;

public class UserManager(Identity.UserManager<User> userManager) : IUserManager
{
    public IQueryable<UserLite> GetUsers()
    {
        var allUsersLite = userManager.Users.Select(u => new UserLite
        {
            Email = u.Email,
            IsAdmin = u.IsAdmin,
            TwoFactorEnabled = u.TwoFactorEnabled
        });

        return allUsersLite;
    }

    public async Task UpdateUser(UserLite userForUpdate)
    {
        var user = await userManager.FindByNameAsync(userForUpdate.Email);

        if (user is null)
            throw new UnauthorizedUserException("Invalid Request");

        await userManager.AddToRoleAsync(user, "Administrator");
        user.IsAdmin = true;

        await userManager.UpdateAsync(user);
    }

    public async Task RemoveAdminRole(UserLite userForUpdate)
    {
        var user = await userManager.FindByNameAsync(userForUpdate.Email);

        if (user is null)
            throw new UnauthorizedUserException("Invalid Request");

        await userManager.RemoveFromRoleAsync(user, "Administrator");
        user.IsAdmin = false;

        await userManager.UpdateAsync(user);
    }

    public async Task DeleteUser(UserLite userForDeletion)
    {
        var user = await userManager.FindByNameAsync(userForDeletion.Email);

        if (user is null)
            throw new UnauthorizedUserException("Invalid Request");

        var logins = await userManager.GetLoginsAsync(user);

        var rolesForUser = await userManager.GetRolesAsync(user);

        foreach (var login in logins)
            await userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);

        foreach (var role in rolesForUser)
            await userManager.RemoveFromRoleAsync(user, role);

        await userManager.DeleteAsync(user);
    }

    public async Task SetTwoFactorAuthorization(UserLite2StepsAuthDto userForUpdate)
    {
        var user = await userManager.FindByNameAsync(userForUpdate.Email);

        if (user is null)
            throw new UnauthorizedUserException("Invalid Request");

        await userManager.SetTwoFactorEnabledAsync(user, userForUpdate.TwoFactorEnabled);
    }
}