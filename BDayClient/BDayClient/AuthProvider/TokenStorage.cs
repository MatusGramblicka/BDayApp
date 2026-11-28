using Blazored.LocalStorage;

namespace BDayClient.AuthProvider;

/// <summary>The access and refresh token, kept in the browser's local storage.</summary>
public sealed class TokenStorage(ILocalStorageService localStorage)
{
    public const string AccessTokenKey = "authToken";
    public const string RefreshTokenKey = "refreshToken";

    public async Task<string?> GetAccessTokenAsync() => await localStorage.GetItemAsync<string>(AccessTokenKey);

    public async Task<string?> GetRefreshTokenAsync() => await localStorage.GetItemAsync<string>(RefreshTokenKey);

    public async Task StoreAsync(string accessToken, string refreshToken)
    {
        await localStorage.SetItemAsync(AccessTokenKey, accessToken);
        await localStorage.SetItemAsync(RefreshTokenKey, refreshToken);
    }

    public async Task ClearAsync()
    {
        await localStorage.RemoveItemAsync(AccessTokenKey);
        await localStorage.RemoveItemAsync(RefreshTokenKey);
    }
}
