using System.Net;

namespace BDayClient.Http;

/// <summary>A failed API call: the status (null when the server could not be reached) and the messages to show.</summary>
/// <param name="StatusCode">The response status, or null when no response was received.</param>
/// <param name="Errors">At least one user-facing message.</param>
/// <param name="Handled">
/// True when <see cref="ApiErrorHandler"/> has already told the user (toast or navigation), so pages must not show it again.
/// </param>
public sealed record ApiError(HttpStatusCode? StatusCode, IReadOnlyList<string> Errors, bool Handled)
{
    public string Message => string.Join(" ", Errors);
}
