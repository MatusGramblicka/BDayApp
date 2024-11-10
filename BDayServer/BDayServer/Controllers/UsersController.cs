using Entities;
using Entities.DataTransferObjects.Auth;
using Entities.DataTransferObjects.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.Controllers;

[Route("api/users")]
[ApiController]
[Authorize(Roles = "Administrator")]
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    public UsersController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("Users")]
    public IActionResult GetUsers()
    {
        var allUsersLite = _userManager.Users.Select(u => new UserLite
        {
            Email = u.Email,
            IsAdmin = u.IsAdmin,
            TwoFactorEnabled = u.TwoFactorEnabled
        });
        //var usersAdministrator = await _userManager.GetUsersInRoleAsync("Administrator");                       

        return Ok(allUsersLite);
    }

    [HttpPost("UpdateUser")]
    public async Task<IActionResult> UpdateUser([FromBody] UserLite userForUpdate)
    {
        var user = await _userManager.FindByNameAsync(userForUpdate.Email);

        if (user is null)
            return Unauthorized(new AuthResponseDto
            {
                ErrorMessage = "Invalid Request"
            });

        await _userManager.AddToRoleAsync(user, "Administrator");
        user.IsAdmin = true;
        await _userManager.UpdateAsync(user);

        return Ok();
    }

    [HttpPost("RemoveAdminRole")]
    public async Task<IActionResult> RemoveAdminRole([FromBody] UserLite userForUpdate)
    {
        var user = await _userManager.FindByNameAsync(userForUpdate.Email);

        if (user is null)
            return Unauthorized(new AuthResponseDto
            {
                ErrorMessage = "Invalid Request"
            });

        await _userManager.RemoveFromRoleAsync(user, "Administrator");
        user.IsAdmin = false;
        await _userManager.UpdateAsync(user);

        return Ok();
    }

    [HttpPost("DeleteUser")]
    public async Task<IActionResult> DeleteUser([FromBody] UserLite userForDeletion)
    {
        var user = await _userManager.FindByNameAsync(userForDeletion.Email);

        if (user is null)
            return Unauthorized(new AuthResponseDto
            {
                ErrorMessage = "Invalid Request"
            });

        var logins = await _userManager.GetLoginsAsync(user);

        var rolesForUser = await _userManager.GetRolesAsync(user);

        foreach (var login in logins)
            await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        
        if (rolesForUser.Count > 0)
        {
            foreach (var item in rolesForUser)
                await _userManager.RemoveFromRoleAsync(user, item);
        }

        await _userManager.DeleteAsync(user);

        return Ok();
    }

    [HttpPost("SetTwoFactorAuthorization")]
    public async Task<IActionResult> SetTwoFactorAuthorization([FromBody] UserLite2StepsAuthDto userForUpdate)
    {
        var user = await _userManager.FindByNameAsync(userForUpdate.Email);

        if (user is null)
            return Unauthorized(new AuthResponseDto
            {
                ErrorMessage = "Invalid Request"
            });

        await _userManager.SetTwoFactorEnabledAsync(user, userForUpdate.TwoFactorEnabled);

        return Ok();
    }
}