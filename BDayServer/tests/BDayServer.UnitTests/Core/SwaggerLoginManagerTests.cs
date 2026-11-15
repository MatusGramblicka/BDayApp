using BDayServer.UnitTests.TestUtilities;
using Contracts.DataTransferObjects.Auth;
using Contracts.Exceptions;
using Core.Managers;
using Core.Services.Interfaces;
using Entities.Models;
using Microsoft.AspNetCore.Identity;

namespace BDayServer.UnitTests.Core;

public class SwaggerLoginManagerTests
{
    private readonly Mock<UserManager<User>> _userManager = MockUserManager.Create();
    private readonly Mock<IAuthenticationService> _authService = new();
    private readonly SwaggerLoginManager _sut;

    public SwaggerLoginManagerTests()
    {
        _sut = new SwaggerLoginManager(_userManager.Object, _authService.Object);
    }

    [Theory]
    [InlineData("", "pwd")]
    [InlineData("a@b.c", "")]
    public async Task Login_MissingCredentials_Throws(string email, string password)
    {
        var act = () => _sut.Login(new SwaggerLoginDto { Email = email, Password = password });

        await act.Should().ThrowAsync<SwaggerLoginAuthenticationException>()
            .WithMessage("Username and/or Password not specified");
    }

    [Fact]
    public async Task Login_UnknownUser_Throws()
    {
        var act = () => _sut.Login(new SwaggerLoginDto { Email = "a@b.c", Password = "pwd" });

        await act.Should().ThrowAsync<SwaggerLoginAuthenticationException>().WithMessage("Bad credentials");
    }

    [Fact]
    public async Task Login_WrongPassword_Throws()
    {
        var user = new User { Email = "a@b.c" };
        _userManager.Setup(m => m.FindByEmailAsync("a@b.c")).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "pwd")).ReturnsAsync(false);

        var act = () => _sut.Login(new SwaggerLoginDto { Email = "a@b.c", Password = "pwd" });

        await act.Should().ThrowAsync<SwaggerLoginAuthenticationException>().WithMessage("Bad credentials");
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var user = new User { Email = "a@b.c" };
        _userManager.Setup(m => m.FindByEmailAsync("a@b.c")).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "pwd")).ReturnsAsync(true);
        _authService.Setup(a => a.GetToken(user)).ReturnsAsync("jwt");

        var token = await _sut.Login(new SwaggerLoginDto { Email = "a@b.c", Password = "pwd" });

        token.Should().Be("jwt");
    }
}
