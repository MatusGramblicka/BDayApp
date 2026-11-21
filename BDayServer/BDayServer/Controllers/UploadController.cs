using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.Controllers;

[Route("api/upload")]
[ApiController]
[Authorize]
public class UploadController(IUploadManager uploadManager) : ControllerBase
{
    [HttpPost]
    public IActionResult Upload()
    {
        try
        {
            return Ok(uploadManager.Upload(Request));
        }
        catch (UploadException ex)
        {
            return BadRequest(ex.Message);
        }
        // Any other exception is logged and answered with a generic 500 by UseExceptionHandler in Program.cs,
        // instead of sending the exception details to the browser.
    }
}
