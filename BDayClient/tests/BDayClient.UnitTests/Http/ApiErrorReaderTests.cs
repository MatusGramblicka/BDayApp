using System.Net;
using System.Net.Http.Json;
using System.Text;
using BDayClient.Http;
using Contracts.DataTransferObjects.Auth;

namespace BDayClient.UnitTests.Http;

public class ApiErrorReaderTests
{
    private static HttpResponseMessage Response(HttpStatusCode status, HttpContent? content = null) =>
        new(status) { Content = content ?? new StringContent(string.Empty) };

    private static StringContent Text(string text, string mediaType = "text/plain") =>
        new(text, Encoding.UTF8, mediaType);

    [Fact]
    public async Task AuthResponseDto_UsesErrorMessage()
    {
        var error = await ApiErrorReader.ReadAsync(Response(HttpStatusCode.Unauthorized,
            JsonContent.Create(new AuthResponseDto { ErrorMessage = "Invalid credentials" })));

        error.Errors.Should().Equal("Invalid credentials");
        error.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        error.Handled.Should().BeFalse();
    }

    [Fact]
    public async Task ResponseDto_UsesErrorsArray()
    {
        var error = await ApiErrorReader.ReadAsync(Response(HttpStatusCode.BadRequest,
            JsonContent.Create(new ResponseDto { Errors = ["Email taken", "Password too short"] })));

        error.Errors.Should().Equal("Email taken", "Password too short");
        error.Message.Should().Be("Email taken Password too short");
    }

    [Fact]
    public async Task ValidationProblemDetails_FlattensTheErrorsMap()
    {
        const string body = """
            {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.",
             "status":400,"errors":{"Email":["The Email field is required.","The Email field is not a valid e-mail address."],
             "Password":["The Password field is required."]},"traceId":"00-abc"}
            """;

        var error = await ApiErrorReader.ReadAsync(Response(HttpStatusCode.BadRequest, Text(body, "application/problem+json")));

        error.Errors.Should().Equal("The Email field is required.", "The Email field is not a valid e-mail address.",
            "The Password field is required.");
    }

    [Theory]
    [InlineData("""{"title":"An error occurred while processing your request.","status":500}""",
        "An error occurred while processing your request.")]
    [InlineData("""{"title":"Bad Request","status":400,"detail":"The token has expired."}""", "The token has expired.")]
    public async Task ProblemDetails_PrefersDetailOverTitle(string body, string expected)
    {
        var error = await ApiErrorReader.ReadAsync(Response(HttpStatusCode.InternalServerError,
            Text(body, "application/problem+json")));

        error.Errors.Should().Equal(expected);
    }

    [Fact]
    public async Task PlainText_IsTheMessage()
    {
        var error = await ApiErrorReader.ReadAsync(Response(HttpStatusCode.BadRequest, Text("Unspecified problem")));

        error.Errors.Should().Equal("Unspecified problem");
    }

    [Fact]
    public async Task JsonString_IsUnquoted()
    {
        var error = await ApiErrorReader.ReadAsync(Response(HttpStatusCode.BadRequest,
            Text("\"Object is null\"", "application/json")));

        error.Errors.Should().Equal("Object is null");
    }

    [Fact]
    public async Task HtmlErrorPage_GivesStatusMessage()
    {
        // IIS answers with an HTML page, e.g. 503 while the site restarts after a deploy.
        var error = await ApiErrorReader.ReadAsync(Response(HttpStatusCode.ServiceUnavailable,
            Text("<html><body>Service Unavailable</body></html>", "text/html")));

        error.Errors.Should().ContainSingle().Which.Should().Contain("503").And.Contain("Service Unavailable");
    }

    [Fact]
    public async Task EmptyBody429_GivesTooManyRequestsMessage()
    {
        var error = await ApiErrorReader.ReadAsync(Response(HttpStatusCode.TooManyRequests));

        error.Errors.Should().Equal("Too many requests - please try again in a minute");
    }

    [Fact]
    public async Task UnknownJson_GivesStatusMessage()
    {
        var error = await ApiErrorReader.ReadAsync(Response(HttpStatusCode.Conflict,
            Text("""{"isSuccessfulRegistration":false,"errors":[]}""", "application/json")));

        error.Errors.Should().ContainSingle().Which.Should().Contain("409");
    }

    [Fact]
    public async Task LongText_IsNotShownToTheUser()
    {
        var stackTrace = "System.InvalidOperationException: boom" + new string('x', 400);

        var error = await ApiErrorReader.ReadAsync(Response(HttpStatusCode.InternalServerError, Text(stackTrace)));

        error.Errors.Should().ContainSingle().Which.Should().Contain("500").And.NotContain("boom");
    }

    [Fact]
    public async Task EmptyReasonPhrase_UsesStandardPhrase()
    {
        // Browsers report an empty status text for HTTP/2 responses.
        var response = Response(HttpStatusCode.BadGateway);
        response.ReasonPhrase = string.Empty;

        var error = await ApiErrorReader.ReadAsync(response);

        error.Message.Should().Contain("502 Bad Gateway");
    }

    [Fact]
    public async Task HandledMarkOnTheRequest_IsReported()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test/api/persons");
        ApiErrorReader.MarkHandled(request);
        var response = Response(HttpStatusCode.TooManyRequests);
        response.RequestMessage = request;

        var error = await ApiErrorReader.ReadAsync(response);

        error.Handled.Should().BeTrue();
    }
}
