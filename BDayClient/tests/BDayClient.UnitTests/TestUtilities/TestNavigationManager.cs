using Microsoft.AspNetCore.Components;

namespace BDayClient.UnitTests.TestUtilities;

internal sealed class TestNavigationManager : NavigationManager
{
    public const string Base = "https://app.test/";

    public TestNavigationManager() => Initialize(Base, Base);

    public List<string> NavigatedTo { get; } = [];

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        NavigatedTo.Add(uri);
        Uri = ToAbsoluteUri(uri).ToString();
    }
}
