namespace BDayServer.Extensions;

/// <summary>Names of the rate limiting policies registered in <see cref="ServiceExtensions.ConfigureRateLimiting"/>.</summary>
public static class RateLimitPolicies
{
    public const string Account = "account";
    public const string TokenRefresh = "token-refresh";
    public const string Upload = "upload";
}
