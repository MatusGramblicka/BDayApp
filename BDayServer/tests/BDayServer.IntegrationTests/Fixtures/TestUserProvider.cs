using Repository.Services;

namespace BDayServer.IntegrationTests.Fixtures;

/// <summary>Stands in for the HTTP-context based provider that drives the per-user query filters.</summary>
public sealed class TestUserProvider(string? userName) : IGetUserProvider
{
    public string UserName { get; } = userName!;
}
