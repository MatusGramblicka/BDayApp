using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BDayServer.UnitTests.TestUtilities;

internal static class ControllerTestExtensions
{
    /// <summary>Gives the controller a request to https://{host} and what ValidationProblem() needs.</summary>
    public static T WithHttpContext<T>(this T controller, string host = "app.test") where T : ControllerBase
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString(host);

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.ProblemDetailsFactory = new SimpleProblemDetailsFactory();

        return controller;
    }

    private sealed class SimpleProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null,
            string? title = null, string? type = null, string? detail = null, string? instance = null) =>
            new() { Status = statusCode ?? 500, Title = title, Detail = detail };

        public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext,
            ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null,
            string? type = null, string? detail = null, string? instance = null) =>
            new(modelStateDictionary) { Status = statusCode ?? 400 };
    }
}
