using System.Text.Json;

namespace BDayClient.UnitTests.TestUtilities;

internal static class TestJson
{
    /// <summary>Matches what <c>PostAsJsonAsync</c>/<c>PutAsJsonAsync</c> send (camelCase, case-insensitive).</summary>
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}
