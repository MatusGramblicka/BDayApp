using BDayServer.Controllers;
using BDayServer.Security;
using BDayServer.UnitTests.TestUtilities;
using Contracts.DataTransferObjects.Auth;
using Contracts.DataTransferObjects.User;
using Contracts.Exceptions;
using Core.Managers.ManagerInterfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BDayServer.UnitTests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<IAccountManager> _manager = new();
    private readonly AccountController _sut;

    public AccountControllerTests()
    {
        var environment = Mock.Of<IHostEnvironment>(e => e.EnvironmentName == Environments.Production);
        var validator = new ClientUriValidator(Options.Create(new ClientAppOptions()), environment);

        _sut = new AccountController(_manager.Object, validator).WithHttpContext("app.test");
    }

    private static UserForRegistrationDto Registration(string clientUri = "https://app.test/emailconfirmation") =>
        new() { Email = "a@test.com", Password = "Secret1!", ConfirmPassword = "Secret1!", ClientUri = clientUri };

    [Fact]
    public async Task Register_LinkToAnotherSite_Returns400AndRegistersNobody()
    {
        var result = await _sut.RegisterUser(Registration("https://evil.test/emailconfirmation"));

        result.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(400);
        ((ObjectResult)result).Value.Should().BeOfType<ValidationProblemDetails>()
            .Which.Errors.Should().ContainKey(nameof(UserForRegistrationDto.ClientUri));
        _manager.Verify(m => m.RegisterUser(It.IsAny<UserForRegistrationDto>()), Times.Never);
    }

    [Fact]
    public async Task Register_Success_Returns201()
    {
        var result = await _sut.RegisterUser(Registration());

        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Register_Rejected_Returns400WithTheErrors()
    {
        _manager.Setup(m => m.RegisterUser(It.IsAny<UserForRegistrationDto>()))
            .ThrowsAsync(new AccountManagerErrorsException(["Email taken"]));

        var result = await _sut.RegisterUser(Registration());

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<ResponseDto>().Which.Errors.Should().Equal("Email taken");
    }

    [Fact]
    public async Task Login_Rejected_Returns401WithTheMessage()
    {
        _manager.Setup(m => m.Login(It.IsAny<UserForAuthenticationDto>()))
            .ThrowsAsync(new AccountManagerUnauthorizedLoginException("Invalid Authentication"));

        var result = await _sut.Login(new UserForAuthenticationDto { Email = "a@test.com", Password = "x" });

        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().BeOfType<AuthResponseDto>().Which.ErrorMessage.Should().Be("Invalid Authentication");
    }

    [Fact]
    public async Task ForgotPassword_Returns204()
    {
        var dto = new ForgotPasswordDto { Email = "a@test.com", ClientUri = "https://app.test/resetpassword" };

        var result = await _sut.ForgotPassword(dto);

        result.Should().BeOfType<NoContentResult>();
        _manager.Verify(m => m.ForgotPassword(dto), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_LinkToAnotherSite_Returns400AndSendsNothing()
    {
        var result = await _sut.ForgotPassword(new ForgotPasswordDto
            { Email = "a@test.com", ClientUri = "https://evil.test/resetpassword" });

        result.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(400);
        _manager.Verify(m => m.ForgotPassword(It.IsAny<ForgotPasswordDto>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_Rejected_Returns400WithResetPasswordResponse()
    {
        _manager.Setup(m => m.ResetPassword(It.IsAny<ResetPasswordDto>()))
            .ThrowsAsync(new AccountManagerErrorsException(["Invalid token."]));

        var result = await _sut.ResetPassword(new ResetPasswordDto());

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<ResetPasswordResponseDto>()
            .Which.Errors.Should().Equal("Invalid token.");
    }

    [Fact]
    public async Task ResetPassword_Success_Returns204()
    {
        var result = await _sut.ResetPassword(new ResetPasswordDto());

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task EmailConfirmation_Rejected_Returns400()
    {
        _manager.Setup(m => m.EmailConfirmation("a@test.com", "bad"))
            .ThrowsAsync(new AccountManagerUnauthorizedLoginException("Invalid Request"));

        var result = await _sut.EmailConfirmation("a@test.com", "bad");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task EmailConfirmation_Success_Returns204()
    {
        var result = await _sut.EmailConfirmation("a@test.com", "good");

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task TwoStepVerification_Rejected_Returns401WithTheMessage()
    {
        _manager.Setup(m => m.TwoStepVerification(It.IsAny<TwoFactorVerificationDto>()))
            .ThrowsAsync(new AccountManagerUnauthorizedLoginException("Invalid Token Verification"));

        var result = await _sut.TwoStepVerification(new TwoFactorVerificationDto());

        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().BeOfType<AuthResponseDto>()
            .Which.ErrorMessage.Should().Be("Invalid Token Verification");
    }
}
