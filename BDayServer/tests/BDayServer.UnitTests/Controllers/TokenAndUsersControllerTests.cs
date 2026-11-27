using BDayServer.Controllers;
using BDayServer.UnitTests.TestUtilities;
using Contracts.DataTransferObjects.Auth;
using Contracts.DataTransferObjects.User;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace BDayServer.UnitTests.Controllers;

public class TokenControllerTests
{
    private readonly Mock<ITokenManager> _manager = new();

    [Fact]
    public async Task Refresh_Valid_Returns200WithTheNewTokens()
    {
        _manager.Setup(m => m.Refresh(It.IsAny<RefreshTokenDto>())).ReturnsAsync(("jwt", "refresh"));

        var result = await new TokenController(_manager.Object).WithHttpContext().Refresh(new RefreshTokenDto());

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new AuthResponseDto
            { IsAuthSuccessful = true, Token = "jwt", RefreshToken = "refresh" });
    }

    [Fact]
    public async Task Refresh_Invalid_Returns400WithAFailedAuthResponse()
    {
        _manager.Setup(m => m.Refresh(It.IsAny<RefreshTokenDto>()))
            .ThrowsAsync(new TokenOperationException("Invalid client request"));

        var result = await new TokenController(_manager.Object).WithHttpContext().Refresh(new RefreshTokenDto());

        result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeEquivalentTo(new AuthResponseDto
            { IsAuthSuccessful = false, ErrorMessage = "Invalid client request" });
    }
}

public class UsersControllerTests
{
    private readonly Mock<IUserManager> _manager = new();
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        _sut = new UsersController(_manager.Object).WithHttpContext();
    }

    [Fact]
    public async Task GetUsers_Returns200WithTheUsers()
    {
        _manager.Setup(m => m.GetUsersAsync()).ReturnsAsync([new UserLite { Email = "a@test.com" }]);

        var result = await _sut.GetUsers();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeAssignableTo<List<UserLite>>()
            .Which.Should().ContainSingle();
    }

    [Fact]
    public async Task UnknownUser_Returns404()
    {
        _manager.Setup(m => m.DeleteUser(It.IsAny<UserLite>())).ThrowsAsync(new UserNotExistException("User does not exist"));

        var result = await _sut.DeleteUser(new UserLite { Email = "x@test.com" });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RuleViolation_Returns400WithTheReason()
    {
        _manager.Setup(m => m.RemoveAdminRole(It.IsAny<UserLite>()))
            .ThrowsAsync(new UserManagementException("You cannot remove the administrator role from the last administrator."));

        var result = await _sut.RemoveAdminRole(new UserLite { Email = "x@test.com" });

        result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be(
            "You cannot remove the administrator role from the last administrator.");
    }

    [Fact]
    public async Task Success_Returns204()
    {
        var result = await _sut.SetTwoFactorAuthorization(new UserLite2StepsAuthDto { Email = "x@test.com" });

        result.Should().BeOfType<NoContentResult>();
        _manager.Verify(m => m.SetTwoFactorAuthorization(It.IsAny<UserLite2StepsAuthDto>()), Times.Once);
    }
}
