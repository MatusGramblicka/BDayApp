using Microsoft.AspNetCore.Identity;

namespace BDayServer.UnitTests.TestUtilities;

internal static class MockRoleManager
{
    public static Mock<RoleManager<IdentityRole>> Create() =>
        new(Mock.Of<IRoleStore<IdentityRole>>(), null!, null!, null!, null!);
}
