using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.Http;

/// <summary>
/// Turns the body of a failed response into user-facing messages, whatever the server sent: our DTOs
/// (<c>AuthResponseDto.ErrorMessage</c>, <c>ResponseDto.Errors</c>), RFC 7807 problem details (<c>detail</c>/<c>title</c>,
/// and the <c>errors</c> map of validation problem details), plain text, or an HTML error page (e.g. IIS while the site restarts).
/// </summary>
public static class ApiErrorReader
{
    public const string TooManyRequestsMessage = "Too many requests - please try again in a minute";

    // Longer text is most likely a stack trace or a page, not a message meant for the user.
    private const int MaxPlainTextLength = 300;

    private static readonly string[] MessagePropertyNames = ["errorMessage", "message", "detail", "title"];

    // Set by ApiErrorHandler on requests whose failure it has already reported to the user.
    private static readonly HttpRequestOptionsKey<bool> HandledOption = new("BDayClient.ErrorHandled");

    public static async Task<ApiError> ReadAsync(HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        string content;
        try
        {
            content = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or ObjectDisposedException)
        {
            content = string.Empty;
        }

        var messages = ParseMessages(content);
        if (messages.Count == 0)
            messages = [GetDefaultMessage(response.StatusCode, response.ReasonPhrase)];

        return new ApiError(response.StatusCode, messages, IsHandled(response));
    }

    /// <summary>The messages contained in an error body; empty when it holds none (empty body, HTML page, unknown JSON).</summary>
    public static IReadOnlyList<string> ParseMessages(string? content)
    {
        var text = content?.Trim();
        if (string.IsNullOrEmpty(text))
            return [];

        if (text[0] is '{' or '[' or '"')
        {
            try
            {
                using var document = JsonDocument.Parse(text);
                return FromJson(document.RootElement);
            }
            catch (JsonException)
            {
                // Not JSON after all - treated as text below.
            }
        }

        return text.Length <= MaxPlainTextLength && !text.StartsWith('<') ? [text] : [];
    }

    public static string GetDefaultMessage(HttpStatusCode statusCode, string? reasonPhrase)
    {
        var code = (int)statusCode;
        // Browsers report an empty status text for HTTP/2 responses.
        var reason = string.IsNullOrWhiteSpace(reasonPhrase) ? ReasonPhrases.GetReasonPhrase(code) : reasonPhrase;

        return statusCode switch
        {
            HttpStatusCode.BadRequest => "The request was not accepted. Please check your input.",
            HttpStatusCode.Unauthorized => "Please log in again.",
            HttpStatusCode.Forbidden => "You are not allowed to do this.",
            HttpStatusCode.NotFound => "The requested item was not found.",
            HttpStatusCode.RequestEntityTooLarge => "The file is too large.",
            HttpStatusCode.TooManyRequests => TooManyRequestsMessage,
            _ when code >= 500 => $"The server answered {code} {reason}. Please try again in a moment.",
            _ => $"The server answered {code} {reason}."
        };
    }

    internal static void MarkHandled(HttpRequestMessage request) => request.Options.Set(HandledOption, true);

    internal static bool IsHandled(HttpResponseMessage response) =>
        response.RequestMessage is not null &&
        response.RequestMessage.Options.TryGetValue(HandledOption, out var handled) &&
        handled;

    private static IReadOnlyList<string> FromJson(JsonElement root) => root.ValueKind switch
    {
        JsonValueKind.String => NonEmpty(AsString(root)),
        JsonValueKind.Array => Strings(root),
        JsonValueKind.Object => FromObject(root),
        _ => []
    };

    private static IReadOnlyList<string> FromObject(JsonElement root)
    {
        // ResponseDto / ResetPasswordResponseDto: "errors": ["..."].
        // ValidationProblemDetails: "errors": { "Email": ["...", ...], ... }.
        if (TryGetProperty(root, "errors", out var errors))
        {
            var messages = errors.ValueKind switch
            {
                JsonValueKind.Array => Strings(errors),
                JsonValueKind.Object => errors.EnumerateObject()
                    .SelectMany(field => field.Value.ValueKind == JsonValueKind.Array
                        ? Strings(field.Value)
                        : NonEmpty(AsString(field.Value)))
                    .Distinct()
                    .ToList(),
                _ => NonEmpty(AsString(errors))
            };

            if (messages.Count > 0)
                return messages;
        }

        // AuthResponseDto: "errorMessage"; ProblemDetails: "detail", then the more generic "title".
        foreach (var name in MessagePropertyNames)
        {
            if (TryGetProperty(root, name, out var value) && AsString(value) is { Length: > 0 } message)
                return [message];
        }

        return [];
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? AsString(JsonElement element) =>
        element.ValueKind == JsonValueKind.String ? element.GetString()?.Trim() : null;

    private static List<string> Strings(JsonElement array) =>
        array.EnumerateArray()
            .Select(AsString)
            .Where(message => !string.IsNullOrEmpty(message))
            .Select(message => message!)
            .ToList();

    private static List<string> NonEmpty(string? message) =>
        string.IsNullOrEmpty(message) ? [] : [message];
}
