using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace BDayClient.Http;

/// <summary>Sending the user to the login page and back to where they were afterwards.</summary>
public static class LoginNavigation
{
    public const string LoginPath = "/login";

    // Pages that make no sense to come back to after logging in.
    private static readonly string[] NonReturnablePaths =
    [
        "login", "logout", "register", "twostepverification", "forgotpassword", "resetpassword",
        "emailconfirmation", "unauthorized", "404", "error"
    ];

    /// <summary>Navigates to the login page, which returns to the current page after a successful login.</summary>
    public static void NavigateToLogin(this NavigationManager navigationManager)
    {
        var relativeUri = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        var path = PathOf(relativeUri);

        // Already there (e.g. redirected by RedirectToLogin a moment ago): keep that page and its returnUrl.
        if (path.Equals("login", StringComparison.OrdinalIgnoreCase))
            return;

        navigationManager.NavigateTo(GetLoginUri(IsReturnable(path) ? "/" + relativeUri : null));
    }

    public static string GetLoginUri(string? returnUrl) =>
        string.IsNullOrEmpty(returnUrl) || returnUrl == "/"
            ? LoginPath
            : QueryHelpers.AddQueryString(LoginPath, "returnUrl", returnUrl);

    /// <summary>
    /// The page to continue at after logging in: <paramref name="returnUrl"/> when it is a path within this app, otherwise "/".
    /// Anything else (absolute or protocol-relative URLs, backslashes, control characters) would be an open redirect.
    /// </summary>
    public static string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl) ||
            !returnUrl.StartsWith('/') ||
            returnUrl.StartsWith("//", StringComparison.Ordinal) ||
            returnUrl.Contains('\\') ||
            returnUrl.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
            return "/";

        return IsReturnable(PathOf(returnUrl)) ? returnUrl : "/";
    }

    private static string PathOf(string relativeUri) => relativeUri.Split('?', '#')[0].Trim('/');

    private static bool IsReturnable(string path) =>
        path.Length > 0 && !NonReturnablePaths.Contains(path, StringComparer.OrdinalIgnoreCase);
}
