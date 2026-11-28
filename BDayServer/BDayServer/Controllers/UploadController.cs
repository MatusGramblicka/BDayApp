using BDayServer.Extensions;
using Contracts.Exceptions;
using Core.Managers;
using Core.Managers.ManagerInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BDayServer.Controllers;

[Route("api/upload")]
[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Upload)]
public class UploadController(IUploadManager uploadManager) : ControllerBase
{
    // The largest allowed image plus room for the multipart envelope; larger requests get 413 before they are read.
    private const long MaxRequestBytes = UploadManager.MaxFileSizeBytes + 64 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await uploadManager.UploadAsync(Request, cancellationToken));
        }
        catch (UploadException ex)
        {
            return BadRequest(ex.Message);
        }
        // Any other exception is logged and answered with a generic 500 by UseExceptionHandler in Program.cs,
        // instead of sending the exception details to the browser.
    }
}
