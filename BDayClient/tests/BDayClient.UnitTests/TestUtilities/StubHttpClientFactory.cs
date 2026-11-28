namespace BDayClient.UnitTests.TestUtilities;

/// <summary>Hands out the given client, e.g. one built from a MockHttpMessageHandler.</summary>
internal sealed class StubHttpClientFactory(Func<HttpClient> createClient) : IHttpClientFactory
{
    public StubHttpClientFactory(HttpClient client) : this(() => client)
    {
    }

    public List<string> RequestedNames { get; } = [];

    public HttpClient CreateClient(string name)
    {
        RequestedNames.Add(name);
        return createClient();
    }
}
