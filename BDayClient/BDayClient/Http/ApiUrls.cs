using Entities.Configuration;
using Microsoft.Extensions.Options;

namespace BDayClient.Http;

/// <summary>Addresses of the BDay API, derived once from <see cref="ApiConfiguration.BaseAddress"/>.</summary>
public sealed class ApiUrls
{
    /// <summary>Name of the <see cref="HttpClient"/> that talks to the API (see Program.cs).</summary>
    public const string HttpClientName = "PersonsAPI";

    private static readonly string[] AuthEndpointPrefixes = ["account/", "token/"];

    public ApiUrls(IOptions<ApiConfiguration> configuration)
        : this(configuration.Value.BaseAddress)
    {
    }

    public ApiUrls(string baseAddress)
    {
        // A configured address may or may not end with '/': "https://host" and "https://host/" both give "https://host/api/".
        ServerBase = new Uri(baseAddress.TrimEnd('/') + "/", UriKind.Absolute);
        ApiBase = new Uri(ServerBase, "api/");
    }

    /// <summary>The server root, e.g. <c>https://host/</c>. Uploaded images are served below it.</summary>
    public Uri ServerBase { get; }

    /// <summary>The API root, e.g. <c>https://host/api/</c>.</summary>
    public Uri ApiBase { get; }

    /// <summary>The request path relative to <see cref="ApiBase"/> (e.g. "persons/1"), or null when the URI is not an API URI.</summary>
    public string? GetApiRelativePath(Uri? requestUri)
    {
        if (requestUri is null || !requestUri.IsAbsoluteUri)
            return null;

        if (Uri.Compare(requestUri, ApiBase, UriComponents.SchemeAndServer, UriFormat.Unescaped,
                StringComparison.OrdinalIgnoreCase) != 0)
            return null;

        var path = requestUri.AbsolutePath;
        return path.StartsWith(ApiBase.AbsolutePath, StringComparison.OrdinalIgnoreCase)
            ? path[ApiBase.AbsolutePath.Length..]
            : null;
    }

    public bool IsApiRequest(Uri? requestUri) => GetApiRelativePath(requestUri) is not null;

    /// <summary>
    /// True for the anonymous account/... and token/... endpoints (login, registration, token refresh, ...).
    /// The check is a prefix match on the path below the API root, so e.g. "persons?searchTerm=account" is not one.
    /// </summary>
    public bool IsAuthEndpoint(Uri? requestUri)
    {
        var relativePath = GetApiRelativePath(requestUri);
        return relativePath is not null &&
               AuthEndpointPrefixes.Any(prefix => relativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The address to load a person image from. New uploads store the path the server returns
    /// ("StaticFiles/Images/name.png"), which is resolved against the server root; older records contain absolute URLs,
    /// which are used as they are.
    /// </summary>
    public string? GetImageSource(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        var trimmed = imageUrl.Trim();

        // Scheme check: on Linux and in the browser a path such as "/StaticFiles/x.png" also parses as an absolute file: URI.
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttps || absolute.Scheme == Uri.UriSchemeHttp))
            return absolute.ToString();

        return new Uri(ServerBase, trimmed.TrimStart('/')).ToString();
    }
}
