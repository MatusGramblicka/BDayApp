using System.Collections.Concurrent;
using Blazored.LocalStorage;

namespace BDayClient.UnitTests.TestUtilities;

/// <summary>A mocked <see cref="ILocalStorageService"/> that keeps string items in <see cref="Items"/>.</summary>
internal sealed class LocalStorageMock
{
    public LocalStorageMock()
    {
        StorageMock.Setup(s => s.GetItemAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string key, CancellationToken _) => ValueTask.FromResult(Items.GetValueOrDefault(key)));

        StorageMock.Setup(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string key, string value, CancellationToken _) => Items[key] = value)
            .Returns(ValueTask.CompletedTask);

        StorageMock.Setup(s => s.RemoveItemAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string key, CancellationToken _) => Items.TryRemove(key, out string? _))
            .Returns(ValueTask.CompletedTask);
    }

    public Mock<ILocalStorageService> StorageMock { get; } = new();

    public ILocalStorageService Object => StorageMock.Object;

    public ConcurrentDictionary<string, string?> Items { get; } = new();

    public string? AccessToken => Items.GetValueOrDefault("authToken");

    public string? RefreshToken => Items.GetValueOrDefault("refreshToken");

    public void StoreTokens(string? accessToken, string? refreshToken = "refresh-token")
    {
        Items["authToken"] = accessToken;
        Items["refreshToken"] = refreshToken;
    }
}
