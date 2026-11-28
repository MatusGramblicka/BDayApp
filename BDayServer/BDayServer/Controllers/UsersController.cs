using Contracts.Authorization;
using Contracts.DataTransferObjects.User;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.Controllers;

[Route("api/users")]
[ApiController]
[Authorize(Roles = AppRoles.Administrator)]
public class UsersController(IUserManager userManager) : ControllerBase
{
    [HttpGet("Users")]
    public async Task<IActionResult> GetUsers() => Ok(await userManager.GetUsersAsync());

    /// <summary>Gives the user the administrator role.</summary>
    [HttpPost("UpdateUser")]
    public Task<IActionResult> UpdateUser([FromBody] UserLite userForUpdate) =>
        RunAsync(() => userManager.UpdateUser(userForUpdate));

    [HttpPost("RemoveAdminRole")]
    public Task<IActionResult> RemoveAdminRole([FromBody] UserLite userForUpdate) =>
        RunAsync(() => userManager.RemoveAdminRole(userForUpdate));

    [HttpPost("DeleteUser")]
    public Task<IActionResult> DeleteUser([FromBody] UserLite userForDeletion) =>
        RunAsync(() => userManager.DeleteUser(userForDeletion));

    [HttpPost("SetTwoFactorAuthorization")]
    public Task<IActionResult> SetTwoFactorAuthorization([FromBody] UserLite2StepsAuthDto userForUpdate) =>
        RunAsync(() => userManager.SetTwoFactorAuthorization(userForUpdate));

    private async Task<IActionResult> RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (UserNotExistException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UserManagementException ex)
        {
            return BadRequest(ex.Message);
        }

        return NoContent();
    }
}
