using BDayServer.UnitTests.TestUtilities;
using Contracts.Authorization;
using Contracts.DataTransferObjects.User;
using Contracts.Exceptions;
using Core.Services.Interfaces;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Repository.Services;
using UserManager = Core.Managers.UserManager;

namespace BDayServer.UnitTests.Core;

public class UserManagerTests
{
    private const string AdminEmail = "admin@test.com";
    private const string OtherEmail = "other@test.com";

    private readonly Mock<UserManager<User>> _identity = MockUserManager.Create();
    private readonly Mock<RoleManager<IdentityRole>> _roleManager = MockRoleManager.Create();
    private readonly Mock<IGetUserProvider> _currentUser = new();
    private readonly Mock<IPersonMirror> _mirror = new();
    private readonly User _admin = new() { Id = "admin", Email = AdminEmail, UserName = AdminEmail, IsAdmin = true };
    private readonly User _other = new() { Id = "other", Email = OtherEmail, UserName = OtherEmail };
    private readonly UserManager _sut;

    public UserManagerTests()
    {
        _sut = new UserManager(_identity.Object, _roleManager.Object, _currentUser.Object, _mirror.Object);

        _currentUser.Setup(c => c.UserName).Returns(AdminEmail);
        _identity.Setup(m => m.FindByNameAsync(AdminEmail)).ReturnsAsync(_admin);
        _identity.Setup(m => m.FindByNameAsync(OtherEmail)).ReturnsAsync(_other);
        _identity.Setup(m => m.IsInRoleAsync(_admin, AppRoles.Administrator)).ReturnsAsync(true);
        _identity.Setup(m => m.IsInRoleAsync(_other, AppRoles.Administrator)).ReturnsAsync(false);
        _identity.Setup(m => m.GetUsersInRoleAsync(AppRoles.Administrator)).ReturnsAsync([_admin]);
        _identity.Setup(m => m.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        _identity.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _identity.Setup(m => m.RemoveFromRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _identity.Setup(m => m.DeleteAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        _identity.Setup(m => m.GetLoginsAsync(It.IsAny<User>())).ReturnsAsync([]);
        _identity.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync([]);
        _roleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
    }

    private void MakeOtherAnAdministratorToo()
    {
        _other.IsAdmin = true;
        _identity.Setup(m => m.IsInRoleAsync(_other, AppRoles.Administrator)).ReturnsAsync(true);
        _identity.Setup(m => m.GetUsersInRoleAsync(AppRoles.Administrator)).ReturnsAsync([_admin, _other]);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsAllUsersOrderedByEmail()
    {
        _identity.Setup(m => m.Users).Returns(new[] { _other, _admin }.AsQueryable());

        var users = await _sut.GetUsersAsync();

        users.Select(u => u.Email).Should().Equal(AdminEmail, OtherEmail);
        users[0].IsAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUser_UnknownUser_ThrowsNotFound()
    {
        var act = () => _sut.UpdateUser(new UserLite { Email = "nobody@test.com" });

        await act.Should().ThrowAsync<UserNotExistException>();
    }

    [Fact]
    public async Task UpdateUser_MakesTheUserAnAdministrator()
    {
        await _sut.UpdateUser(new UserLite { Email = OtherEmail });

        _identity.Verify(m => m.AddToRoleAsync(_other, AppRoles.Administrator), Times.Once);
        _identity.Verify(m => m.UpdateAsync(It.Is<User>(u => u == _other && u.IsAdmin)), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_AdministratorRoleMissing_CreatesIt()
    {
        _roleManager.Setup(m => m.RoleExistsAsync(AppRoles.Administrator)).ReturnsAsync(false);
        _roleManager.Setup(m => m.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);

        await _sut.UpdateUser(new UserLite { Email = OtherEmail });

        _roleManager.Verify(m => m.CreateAsync(It.Is<IdentityRole>(r => r.Name == AppRoles.Administrator)));
    }

    [Fact]
    public async Task UpdateUser_AlreadyAnAdministrator_DoesNotAddTheRoleTwice()
    {
        MakeOtherAnAdministratorToo();

        await _sut.UpdateUser(new UserLite { Email = OtherEmail });

        _identity.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAdminRole_OwnAccount_IsRefused()
    {
        MakeOtherAnAdministratorToo();

        var act = () => _sut.RemoveAdminRole(new UserLite { Email = AdminEmail });

        await act.Should().ThrowAsync<UserManagementException>();
        _identity.Verify(m => m.RemoveFromRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAdminRole_LastAdministrator_IsRefused()
    {
        _currentUser.Setup(c => c.UserName).Returns("someone-else@test.com");

        var act = () => _sut.RemoveAdminRole(new UserLite { Email = AdminEmail });

        (await act.Should().ThrowAsync<UserManagementException>()).WithMessage("*last administrator*");
    }

    [Fact]
    public async Task RemoveAdminRole_AnotherAdministrator_RemovesRoleAndFlag()
    {
        MakeOtherAnAdministratorToo();

        await _sut.RemoveAdminRole(new UserLite { Email = OtherEmail });

        _identity.Verify(m => m.RemoveFromRoleAsync(_other, AppRoles.Administrator), Times.Once);
        _identity.Verify(m => m.UpdateAsync(It.Is<User>(u => u == _other && !u.IsAdmin)), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_OwnAccount_IsRefused()
    {
        var act = () => _sut.DeleteUser(new UserLite { Email = AdminEmail });

        await act.Should().ThrowAsync<UserManagementException>();
        _identity.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUser_RemovesLoginsRolesTheUserAndTheMirroredCopy()
    {
        _identity.Setup(m => m.GetLoginsAsync(_other)).ReturnsAsync([new UserLoginInfo("Google", "key", "Google")]);
        _identity.Setup(m => m.GetRolesAsync(_other)).ReturnsAsync([AppRoles.Viewer]);
        _identity.Setup(m => m.RemoveLoginAsync(_other, "Google", "key")).ReturnsAsync(IdentityResult.Success);

        await _sut.DeleteUser(new UserLite { Email = OtherEmail });

        _identity.Verify(m => m.RemoveLoginAsync(_other, "Google", "key"), Times.Once);
        _identity.Verify(m => m.RemoveFromRoleAsync(_other, AppRoles.Viewer), Times.Once);
        _identity.Verify(m => m.DeleteAsync(_other), Times.Once);
        _mirror.Verify(m => m.DeleteUserAsync(_other.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_DeleteFails_ThrowsAndKeepsTheMirroredCopy()
    {
        _identity.Setup(m => m.DeleteAsync(_other)).ReturnsAsync(IdentityResult.Failed());

        var act = () => _sut.DeleteUser(new UserLite { Email = OtherEmail });

        await act.Should().ThrowAsync<InvalidOperationException>();
        _mirror.Verify(m => m.DeleteUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetTwoFactorAuthorization_SetsTheFlag()
    {
        _identity.Setup(m => m.SetTwoFactorEnabledAsync(_other, false)).ReturnsAsync(IdentityResult.Success);

        await _sut.SetTwoFactorAuthorization(new UserLite2StepsAuthDto { Email = OtherEmail, TwoFactorEnabled = false });

        _identity.Verify(m => m.SetTwoFactorEnabledAsync(_other, false), Times.Once);
    }

    [Fact]
    public async Task SetTwoFactorAuthorization_UnknownUser_ThrowsNotFound()
    {
        var act = () => _sut.SetTwoFactorAuthorization(new UserLite2StepsAuthDto { Email = "nobody@test.com" });

        await act.Should().ThrowAsync<UserNotExistException>();
    }
}
