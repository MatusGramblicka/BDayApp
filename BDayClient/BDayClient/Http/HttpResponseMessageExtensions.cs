using System.Text.Json;

namespace BDayClient.Http;

internal static class HttpResponseMessageExtensions
{
    // camelCase and case-insensitive, like the API (and PostAsJsonAsync).
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Any 2xx is success; otherwise throws an <see cref="ApiException"/> with the server's message.</summary>
    public static async Task EnsureSuccessAsync(this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
            return;

        throw new ApiException(await ApiErrorReader.ReadAsync(response, cancellationToken));
    }

    /// <summary>The JSON body as <typeparamref name="T"/>, or default when the body is empty or not that JSON.</summary>
    public static async Task<T?> ReadJsonOrDefaultAsync<T>(this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>An <see cref="ApiException"/> for a 2xx response whose body is not what the API documents.</summary>
    public static ApiException UnexpectedResponse(this HttpResponseMessage response) =>
        new(new ApiError(response.StatusCode, ["The server sent an unexpected response. Please try again."],
            Handled: false));
}
