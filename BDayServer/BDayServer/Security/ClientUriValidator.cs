using Microsoft.Extensions.Options;

namespace BDayServer.Security;

/// <summary>
/// Origins, besides the API's own origin, that links in confirmation and password-reset e-mails may point to -
/// for example the Blazor client's own dev server. Setting: "ClientApp:AllowedOrigins".
/// </summary>
public sealed class ClientAppOptions
{
    public const string SectionName = "ClientApp";

    public string[] AllowedOrigins { get; set; } = [];
}

/// <summary>
/// Confirmation and password-reset e-mails contain a link to the ClientUri that the browser sends, and the link carries
/// a valid token. ClientUri must therefore point at this app: otherwise anyone could make the server send a genuine
/// reset token to any user, in a link to the attacker's own site.
/// </summary>
public sealed class ClientUriValidator(IOptions<ClientAppOptions> options, IHostEnvironment environment)
{
    public bool IsAllowed(string? clientUri, HttpRequest request)
    {
        if (!Uri.TryCreate(clientUri, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(uri.UserInfo))
            return false;

        // Our own host, whatever the scheme and port: behind Azure's front end the request may not reveal how the
        // browser reached us, and what matters is that the link can't point to somebody else's site.
        if (string.Equals(uri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase))
            return true;

        var origin = uri.GetLeftPart(UriPartial.Authority);

        if (options.Value.AllowedOrigins.Any(allowed =>
                string.Equals(origin, Origin(allowed), StringComparison.OrdinalIgnoreCase)))
            return true;

        // Locally the client may run on its own dev server (another localhost port).
        return environment.IsDevelopment() && uri.IsLoopback;
    }

    private static string? Origin(string? uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var parsed) ? parsed.GetLeftPart(UriPartial.Authority) : null;
}
