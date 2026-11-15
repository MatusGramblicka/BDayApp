using BDayServer.UnitTests.TestUtilities;
using Contracts.DataTransferObjects.Auth;
using Contracts.DataTransferObjects.User;
using Contracts.Exceptions;
using Core.Managers;
using Core.Services.Interfaces;
using EmailService.EmailServiceContracts;
using EmailService.Interfaces;
using Entities.Models;
using Microsoft.AspNetCore.Identity;

namespace BDayServer.UnitTests.Core;

public class AccountManagerTests
{
    private const string Email = "jane@test.com";
    private const string Password = "Secret123!";

    private readonly Mock<UserManager<User>> _userManager = MockUserManager.Create();
    private readonly Mock<IAuthenticationService> _authService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly User _user = new() { Email = Email, UserName = Email };
    private readonly AccountManager _sut;

    public AccountManagerTests()
    {
        _sut = new AccountManager(_userManager.Object, _authService.Object, _emailSender.Object);
    }

    private void SetupValidUser(bool twoFactor = false)
    {
        _userManager.Setup(m => m.FindByNameAsync(Email)).ReturnsAsync(_user);
        _userManager.Setup(m => m.IsEmailConfirmedAsync(_user)).ReturnsAsync(true);
        _userManager.Setup(m => m.IsLockedOutAsync(_user)).ReturnsAsync(false);
        _userManager.Setup(m => m.CheckPasswordAsync(_user, Password)).ReturnsAsync(true);
        _userManager.Setup(m => m.GetTwoFactorEnabledAsync(_user)).ReturnsAsync(twoFactor);
    }

    private static UserForAuthenticationDto Credentials(string password = Password) =>
        new() { Email = Email, Password = password };

    #region RegisterUser

    [Fact]
    public async Task RegisterUser_Null_Throws()
    {
        var act = () => _sut.RegisterUser(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RegisterUser_CreateFails_ThrowsWithIdentityErrors()
    {
        _userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), Password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Password too weak" },
                new IdentityError { Description = "Email taken" }));

        var act = () => _sut.RegisterUser(new UserForRegistrationDto { Email = Email, Password = Password });

        var exception = await act.Should().ThrowAsync<AccountManagerErrorsException>();
        exception.Which.Errors.Should().BeEquivalentTo("Password too weak", "Email taken");
        _emailSender.Verify(s => s.SendEmailAsync(It.IsAny<Message>()), Times.Never);
    }

    [Fact]
    public async Task RegisterUser_Success_EnablesTwoFactorSendsConfirmationAndAssignsViewerRole()
    {
        _userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), Password)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>())).ReturnsAsync("confirm-token");

        Message? sent = null;
        _emailSender.Setup(s => s.SendEmailAsync(It.IsAny<Message>())).Callback<Message>(m => sent = m);

        await _sut.RegisterUser(new UserForRegistrationDto
        {
            Email = Email,
            Password = Password,
            ClientUri = "https://client.test/confirm"
        });

        _userManager.Verify(m => m.CreateAsync(It.Is<User>(u => u.Email == Email && u.UserName == Email), Password));
        _userManager.Verify(m => m.SetTwoFactorEnabledAsync(It.IsAny<User>(), true));
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), "Viewer"));

        sent.Should().NotBeNull();
        sent!.Subject.Should().Be("Email confirmation token");
        sent.To.Should().ContainSingle().Which.Address.Should().Be(Email);
        sent.Content.Should().StartWith("https://client.test/confirm?")
            .And.Contain("token=confirm-token")
            .And.Contain($"email={Email}");
    }

    #endregion

    #region Login

    [Fact]
    public async Task Login_UnknownUser_Throws()
    {
        var act = () => _sut.Login(Credentials());

        await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>().WithMessage("Invalid Request");
    }

    [Fact]
    public async Task Login_EmailNotConfirmed_Throws()
    {
        SetupValidUser();
        _userManager.Setup(m => m.IsEmailConfirmedAsync(_user)).ReturnsAsync(false);

        var act = () => _sut.Login(Credentials());

        await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>().WithMessage("Email is not confirmed");
    }

    [Fact]
    public async Task Login_AlreadyLockedOut_Throws()
    {
        SetupValidUser();
        _userManager.Setup(m => m.IsLockedOutAsync(_user)).ReturnsAsync(true);

        var act = () => _sut.Login(Credentials());

        await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>().WithMessage("The account is locked out");
        _userManager.Verify(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_WrongPassword_RegistersFailedAttempt()
    {
        SetupValidUser();

        var act = () => _sut.Login(Credentials("wrong"));

        await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>().WithMessage("Invalid Authentication");
        _userManager.Verify(m => m.AccessFailedAsync(_user), Times.Once);
        _emailSender.Verify(s => s.SendEmailAsync(It.IsAny<Message>()), Times.Never);
    }

    [Fact]
    public async Task Login_WrongPasswordCausingLockout_SendsLockoutEmail()
    {
        SetupValidUser();
        _userManager.SetupSequence(m => m.IsLockedOutAsync(_user))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var act = () => _sut.Login(Credentials("wrong"));

        await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>().WithMessage("The account is locked out");
        _emailSender.Verify(s => s.SendEmailAsync(
            It.Is<Message>(m => m.Subject == "Locked out account information")), Times.Once);
    }

    [Fact]
    public async Task Login_TwoFactorEnabled_SendsOtpAndRequiresSecondStep()
    {
        SetupValidUser(twoFactor: true);
        _userManager.Setup(m => m.GetValidTwoFactorProvidersAsync(_user)).ReturnsAsync(["Email"]);
        _userManager.Setup(m => m.GenerateTwoFactorTokenAsync(_user, "Email")).ReturnsAsync("123456");

        var result = await _sut.Login(Credentials());

        result.Is2StepVerificationRequired.Should().BeTrue();
        result.Provider.Should().Be("Email");
        result.IsAuthSuccessful.Should().BeFalse();
        _emailSender.Verify(s => s.SendEmailAsync(
            It.Is<Message>(m => m.Subject == "Authentication token" && m.Content == "123456")), Times.Once);
        _authService.Verify(a => a.GetToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_TwoFactorWithoutEmailProvider_Throws()
    {
        SetupValidUser(twoFactor: true);
        _userManager.Setup(m => m.GetValidTwoFactorProvidersAsync(_user)).ReturnsAsync(["Authenticator"]);

        var act = () => _sut.Login(Credentials());

        await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>()
            .WithMessage("Invalid 2-Step Verification Provider");
    }

    [Fact]
    public async Task Login_Success_ReturnsTokensAndPersistsRefreshToken()
    {
        SetupValidUser();
        _authService.Setup(a => a.GetToken(_user)).ReturnsAsync("jwt");
        _authService.Setup(a => a.GenerateRefreshToken()).Returns("refresh");

        var result = await _sut.Login(Credentials());

        result.Should().BeEquivalentTo(new AuthResponseDto
        {
            IsAuthSuccessful = true,
            Token = "jwt",
            RefreshToken = "refresh"
        });
        _user.RefreshToken.Should().Be("refresh");
        _user.RefreshTokenExpiryTime.Should().BeCloseTo(DateTime.Now.AddDays(7), TimeSpan.FromMinutes(1));
        _userManager.Verify(m => m.UpdateAsync(_user), Times.Once);
        _userManager.Verify(m => m.ResetAccessFailedCountAsync(_user), Times.Once);
    }

    #endregion

    #region Password, email confirmation and 2FA

    [Fact]
    public async Task ForgotPassword_UnknownEmail_Throws()
    {
        var act = () => _sut.ForgotPassword(new ForgotPasswordDto { Email = Email, ClientUri = "https://c" });

        await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>();
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_SendsResetLink()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(_user);
        _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(_user)).ReturnsAsync("reset-token");

        await _sut.ForgotPassword(new ForgotPasswordDto { Email = Email, ClientUri = "https://client.test/reset" });

        _emailSender.Verify(s => s.SendEmailAsync(It.Is<Message>(m =>
            m.Subject == "Reset password token" &&
            m.Content.StartsWith("https://client.test/reset?") &&
            m.Content.Contains("token=reset-token"))), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_Failure_ThrowsWithErrors()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(_user);
        _userManager.Setup(m => m.ResetPasswordAsync(_user, "t", Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        var act = () => _sut.ResetPassword(new ResetPasswordDto { Email = Email, Token = "t", Password = Password });

        (await act.Should().ThrowAsync<AccountManagerErrorsException>())
            .Which.Errors.Should().Equal("Invalid token");
        _userManager.Verify(m => m.SetLockoutEndDateAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset?>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_Success_ClearsLockout()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(_user);
        _userManager.Setup(m => m.ResetPasswordAsync(_user, "t", Password)).ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ResetPassword(new ResetPasswordDto { Email = Email, Token = "t", Password = Password });

        result.IsResetPasswordSuccessful.Should().BeTrue();
        _userManager.Verify(m => m.SetLockoutEndDateAsync(_user, null), Times.Once);
    }

    [Fact]
    public async Task EmailConfirmation_InvalidToken_Throws()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(_user);
        _userManager.Setup(m => m.ConfirmEmailAsync(_user, "bad"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError()));

        var act = () => _sut.EmailConfirmation(Email, "bad");

        await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>();
    }

    [Fact]
    public async Task EmailConfirmation_ValidToken_Succeeds()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(_user);
        _userManager.Setup(m => m.ConfirmEmailAsync(_user, "good")).ReturnsAsync(IdentityResult.Success);

        var act = () => _sut.EmailConfirmation(Email, "good");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TwoStepVerification_InvalidToken_Throws()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(_user);
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(_user, "Email", "000000")).ReturnsAsync(false);

        var act = () => _sut.TwoStepVerification(new TwoFactorVerificationDto
        {
            Email = Email, Provider = "Email", TwoFactorToken = "000000"
        });

        await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>()
            .WithMessage("Invalid Token Verification");
    }

    [Fact]
    public async Task TwoStepVerification_ValidToken_ReturnsTokens()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(_user);
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(_user, "Email", "123456")).ReturnsAsync(true);
        _authService.Setup(a => a.GetToken(_user)).ReturnsAsync("jwt");
        _authService.Setup(a => a.GenerateRefreshToken()).Returns("refresh");

        var result = await _sut.TwoStepVerification(new TwoFactorVerificationDto
        {
            Email = Email, Provider = "Email", TwoFactorToken = "123456"
        });

        result.IsAuthSuccessful.Should().BeTrue();
        result.Token.Should().Be("jwt");
        result.RefreshToken.Should().Be("refresh");
        _userManager.Verify(m => m.UpdateAsync(_user), Times.Once);
    }

    #endregion
}
