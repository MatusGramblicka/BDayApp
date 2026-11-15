using BDayServer.UnitTests.TestUtilities;
using Contracts.DataTransferObjects.User;
using Contracts.Exceptions;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using CoreUserManager = Core.Managers.UserManager;

namespace BDayServer.UnitTests.Core;

public class UserManagerTests
{
    private const string Email = "john@test.com";

    private readonly Mock<UserManager<User>> _identity = MockUserManager.Create();
    private readonly User _user = new() { Email = Email, UserName = Email };
    private readonly CoreUserManager _sut;

    public UserManagerTests()
    {
        _sut = new CoreUserManager(_identity.Object);
    }

    private void SetupUserExists() => _identity.Setup(m => m.FindByNameAsync(Email)).ReturnsAsync(_user);

    [Fact]
    public void GetUsers_ProjectsToUserLite()
    {
        _identity.Setup(m => m.Users).Returns(new[]
        {
            new User { Email = "a@test.com", IsAdmin = true, TwoFactorEnabled = false },
            new User { Email = "b@test.com", IsAdmin = false, TwoFactorEnabled = true }
        }.AsQueryable());

        var users = _sut.GetUsers().ToList();

        users.Should().BeEquivalentTo(new[]
        {
            new UserLite { Email = "a@test.com", IsAdmin = true, TwoFactorEnabled = false },
            new UserLite { Email = "b@test.com", IsAdmin = false, TwoFactorEnabled = true }
        });
    }

    [Fact]
    public async Task UpdateUser_UnknownUser_Throws()
    {
        var act = () => _sut.UpdateUser(new UserLite { Email = Email });

        await act.Should().ThrowAsync<UnauthorizedUserException>();
    }

    [Fact]
    public async Task UpdateUser_PromotesToAdministrator()
    {
        SetupUserExists();

        await _sut.UpdateUser(new UserLite { Email = Email });

        _user.IsAdmin.Should().BeTrue();
        _identity.Verify(m => m.AddToRoleAsync(_user, "Administrator"), Times.Once);
        _identity.Verify(m => m.UpdateAsync(_user), Times.Once);
    }

    [Fact]
    public async Task RemoveAdminRole_DemotesUser()
    {
        _user.IsAdmin = true;
        SetupUserExists();

        await _sut.RemoveAdminRole(new UserLite { Email = Email });

        _user.IsAdmin.Should().BeFalse();
        _identity.Verify(m => m.RemoveFromRoleAsync(_user, "Administrator"), Times.Once);
        _identity.Verify(m => m.UpdateAsync(_user), Times.Once);
    }

    [Fact]
    public async Task RemoveAdminRole_UnknownUser_Throws()
    {
        var act = () => _sut.RemoveAdminRole(new UserLite { Email = Email });

        await act.Should().ThrowAsync<UnauthorizedUserException>();
    }

    [Fact]
    public async Task DeleteUser_RemovesLoginsAndRolesBeforeDeleting()
    {
        SetupUserExists();
        _identity.Setup(m => m.GetLoginsAsync(_user)).ReturnsAsync(
        [
            new UserLoginInfo("Google", "g-key", "Google"),
            new UserLoginInfo("Microsoft", "m-key", "Microsoft")
        ]);
        _identity.Setup(m => m.GetRolesAsync(_user)).ReturnsAsync(["Viewer", "Administrator"]);

        await _sut.DeleteUser(new UserLite { Email = Email });

        _identity.Verify(m => m.RemoveLoginAsync(_user, "Google", "g-key"), Times.Once);
        _identity.Verify(m => m.RemoveLoginAsync(_user, "Microsoft", "m-key"), Times.Once);
        _identity.Verify(m => m.RemoveFromRoleAsync(_user, "Viewer"), Times.Once);
        _identity.Verify(m => m.RemoveFromRoleAsync(_user, "Administrator"), Times.Once);
        _identity.Verify(m => m.DeleteAsync(_user), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_UnknownUser_Throws()
    {
        var act = () => _sut.DeleteUser(new UserLite { Email = Email });

        await act.Should().ThrowAsync<UnauthorizedUserException>();
        _identity.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetTwoFactorAuthorization_SetsFlag(bool enabled)
    {
        SetupUserExists();

        await _sut.SetTwoFactorAuthorization(new UserLite2StepsAuthDto { Email = Email, TwoFactorEnabled = enabled });

        _identity.Verify(m => m.SetTwoFactorEnabledAsync(_user, enabled), Times.Once);
    }
}
