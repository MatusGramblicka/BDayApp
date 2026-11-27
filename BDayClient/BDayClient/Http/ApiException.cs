using System.Net;

namespace BDayClient.Http;

/// <summary>The one exception type the HTTP layer throws for failed API calls. Pages catch it and show <see cref="Exception.Message"/>.</summary>
public sealed class ApiException(ApiError error, Exception? innerException = null)
    : Exception(error.Message, innerException)
{
    public ApiError Error { get; } = error;

    public HttpStatusCode? StatusCode => Error.StatusCode;

    public IReadOnlyList<string> Errors => Error.Errors;

    /// <summary>True when the user has already been notified by <see cref="ApiErrorHandler"/>.</summary>
    public bool Handled => Error.Handled;
}
