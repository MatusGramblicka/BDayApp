using Contracts.Exceptions;
using Interfaces.Managers;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.Controllers;

[Route("api/upload")]
[ApiController]
public class UploadController(IUploadManager uploadManager) : ControllerBase
{
    [HttpPost]
    public IActionResult Upload()
    {
        string? dbPath;

        try
        {
            dbPath = uploadManager.Upload(Request);
        }
        catch (UploadException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex}");
        }

        return Ok(dbPath);
    }
}