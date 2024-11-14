using BDayServer.ActionFilters;
using Contracts.Exceptions;
using Contracts.Managers;
using Entities.DataTransferObjects.Auth;
using Entities.DataTransferObjects.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.Controllers;

[Route("api/users")]
[ApiController]
[Authorize(Roles = "Administrator")]
public class UsersController(IUserManager userManager) : ControllerBase
{
    [HttpGet("Users")]
    public IActionResult GetUsers()
    {
        var allUsersLite = userManager.GetUsers();

        return Ok(allUsersLite);
    }

    [HttpPost("UpdateUser")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> UpdateUser([FromBody] UserLite userForUpdate)
    {
        try
        {
            await userManager.UpdateUser(userForUpdate);
        }
        catch (UnauthorizedUserException ex)
        {
            return Unauthorized(new AuthResponseDto
            {
                ErrorMessage = ex.Message
            });
        }
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return NoContent();
    }

    [HttpPost("RemoveAdminRole")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> RemoveAdminRole([FromBody] UserLite userForUpdate)
    {
        try
        {
            await userManager.RemoveAdminRole(userForUpdate);
        }
        catch (UnauthorizedUserException ex)
        {
            return Unauthorized(new AuthResponseDto
            {
                ErrorMessage = ex.Message
            });
        }
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return NoContent();
    }

    [HttpPost("DeleteUser")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> DeleteUser([FromBody] UserLite userForDeletion)
    {
        try
        {
            await userManager.DeleteUser(userForDeletion);
        }
        catch (UnauthorizedUserException ex)
        {
            return Unauthorized(new AuthResponseDto
            {
                ErrorMessage = ex.Message
            });
        }
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return NoContent();
    }

    [HttpPost("SetTwoFactorAuthorization")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> SetTwoFactorAuthorization([FromBody] UserLite2StepsAuthDto userForUpdate)
    {
        try
        {
            await userManager.SetTwoFactorAuthorization(userForUpdate);
        }
        catch (UnauthorizedUserException ex)
        {
            return Unauthorized(new AuthResponseDto
            {
                ErrorMessage = ex.Message
            });
        }
        catch (Exception)
        {
            return BadRequest("Unspecified problem");
        }

        return NoContent();
    }
}