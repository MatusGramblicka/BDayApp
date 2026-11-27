using System.Globalization;
using BDayServer.UnitTests.TestUtilities;
using Contracts.Authorization;
using Contracts.DataTransferObjects.Auth;
using Contracts.DataTransferObjects.User;
using Contracts.Exceptions;
using Core.Managers;
using Core.Services.Interfaces;
using EmailService.EmailServiceContracts;
using EmailService.Interfaces;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace BDayServer.UnitTests.Core;

public class AccountManagerTests
{
    private const string Email = "jane@test.com";
    private const string Password = "Secret123!";

    private readonly Mock<UserManager<User>> _userManager = MockUserManager.Create();
    private readonly Mock<RoleManager<IdentityRole>> _roleManager = MockRoleManager.Create();
    private readonly Mock<IAuthenticationService> _authService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly User _user = new() { Id = "user-1", Email = Email, UserName = Email };
    private readonly List<Message> _sent = [];
    private readonly AccountManager _sut;

    public AccountManagerTests()
    {
        _sut = new AccountManager(_userManager.Object, _roleManager.Object, _authService.Object, _emailSender.Object,
            NullLogger<AccountManager>.Instance);

        // Identity operations succeed unless a test says otherwise.
        _userManager.Setup(m => m.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AccessFailedAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetLockoutEndDateAsync(It.IsAny<User>(), null)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetTwoFactorEnabledAsync(It.IsAny<User>(), It.IsAny<bool>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.DeleteAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetAuthenticationTokenAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.RemoveAuthenticationTokenAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _roleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        _authService.Setup(s => s.GetToken(_user)).ReturnsAsync("jwt");
        _authService.Setup(s => s.GenerateRefreshToken()).Returns("refresh");
        _emailSender.Setup(s => s.SendEmailAsync(It.IsAny<Message>())).Callback<Message>(_sent.Add)
            .Returns(Task.CompletedTask);
    }

    private void SetupUser(bool emailConfirmed = true, bool lockedOut = false, bool passwordOk = true,
        bool twoFactor = false)
    {
        _userManager.Setup(m => m.FindByNameAsync(Email)).ReturnsAsync(_user);
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(_user);
        _userManager.Setup(m => m.IsEmailConfirmedAsync(_user)).ReturnsAsync(emailConfirmed);
        _userManager.Setup(m => m.IsLockedOutAsync(_user)).ReturnsAsync(lockedOut);
        _userManager.Setup(m => m.CheckPasswordAsync(_user, Password)).ReturnsAsync(passwordOk);
        _userManager.Setup(m => m.GetTwoFactorEnabledAsync(_user)).ReturnsAsync(twoFactor);
    }

    private void SetupPendingTwoStepLogin(DateTimeOffset? expiresAt)
    {
        _userManager.Setup(m => m.GetAuthenticationTokenAsync(_user, AccountManager.TwoFactorLoginProvider,
                AccountManager.TwoFactorLoginPendingToken))
            .ReturnsAsync(expiresAt?.ToString("O", CultureInfo.InvariantCulture));
    }

    private static UserForAuthenticationDto Credentials() => new() { Email = Email, Password = Password };

    private static UserForRegistrationDto Registration() => new()
    {
        Email = Email,
        Password = Password,
        ConfirmPassword = Password,
        ClientUri = "https://client.test/emailconfirmation"
    };

    private static TwoFactorVerificationDto Code(string code = "123456", string provider = "Email") =>
        new() { Email = Email, TwoFactorToken = code, Provider = provider };

    #region RegisterUser

    [Fact]
    public async Task RegisterUser_Null_Throws()
    {
        var act = () => _sut.RegisterUser(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RegisterUser_CreateFails_ThrowsIdentityErrorsAndSendsNothing()
    {
        _userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email taken" }));

        var act = () => _sut.RegisterUser(Registration());

        (await act.Should().ThrowAsync<AccountManagerErrorsException>()).Which.Errors.Should().Equal("Email taken");
        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterUser_Success_AddsViewerRoleEnablesTwoStepAndMailsConfirmationLink()
    {
        User? created = null;
        _userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), Password))
            .Callback<User, string>((u, _) => created = u)
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>())).ReturnsAsync("tok+en");

        await _sut.RegisterUser(Registration());

        created!.Email.Should().Be(Email);
        created.UserName.Should().Be(Email);
        created.RefreshToken.Should().BeEmpty("the column is NOT NULL");
        _userManager.Verify(m => m.AddToRoleAsync(created, AppRoles.Viewer));
        _userManager.Verify(m => m.SetTwoFactorEnabledAsync(created, true));
        _sent.Should().ContainSingle();
        _sent[0].Subject.Should().Be("Email confirmation token");
        _sent[0].To.Should().ContainSingle().Which.Address.Should().Be(Email);
        _sent[0].Content.Should().Be("https://client.test/emailconfirmation?token=tok%2Ben&email=jane@test.com");
    }

    [Fact]
    public async Task RegisterUser_ViewerRoleMissing_CreatesIt()
    {
        _userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), Password)).ReturnsAsync(IdentityResult.Success);
        _roleManager.Setup(m => m.RoleExistsAsync(AppRoles.Viewer)).ReturnsAsync(false);
        _roleManager.Setup(m => m.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);

        await _sut.RegisterUser(Registration());

        _roleManager.Verify(m => m.CreateAsync(It.Is<IdentityRole>(r => r.Name == AppRoles.Viewer)));
    }

    [Fact]
    public async Task RegisterUser_EmailFails_RemovesTheAccountAgain()
    {
        User? created = null;
        _userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), Password))
            .Callback<User, string>((u, _) => created = u)
            .ReturnsAsync(IdentityResult.Success);
        _emailSender.Setup(s => s.SendEmailAsync(It.IsAny<Message>())).ThrowsAsync(new IOException("SMTP down"));

        var act = () => _sut.RegisterUser(Registration());

        (await act.Should().ThrowAsync<AccountManagerErrorsException>())
            .Which.Errors.Should().Equal(AccountManager.RegistrationFailedMessage);
        _userManager.Verify(m => m.DeleteAsync(created!), Times.Once);
    }

    #endregion

    #region Login

    [Fact]
    public async Task Login_UnknownUser_GetsTheSameAnswerAsAWrongPassword()
    {
        var act = () => _sut.Login(Credentials());

        (await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>())
            .WithMessage(AccountManager.InvalidAuthenticationMessage);
    }

    [Fact]
    public async Task Login_LockedOut_ThrowsWithoutCheckingThePassword()
    {
        SetupUser(lockedOut: true);

        var act = () => _sut.Login(Credentials());

        (await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>())
            .WithMessage(AccountManager.LockedOutMessage);
        _userManager.Verify(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_WrongPassword_CountsTheFailure()
    {
        SetupUser(passwordOk: false);

        var act = () => _sut.Login(Credentials());

        (await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>())
            .WithMessage(AccountManager.InvalidAuthenticationMessage);
        _userManager.Verify(m => m.AccessFailedAsync(_user), Times.Once);
        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Login_WrongPasswordThatLocksTheAccount_SendsANotice()
    {
        SetupUser(passwordOk: false);
        _userManager.SetupSequence(m => m.IsLockedOutAsync(_user)).ReturnsAsync(false).ReturnsAsync(true);

        var act = () => _sut.Login(Credentials());

        (await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>())
            .WithMessage(AccountManager.LockedOutMessage);
        _sent.Should().ContainSingle().Which.Subject.Should().Be("Locked out account information");
    }

    [Fact]
    public async Task Login_UnconfirmedEmail_IsOnlyRevealedAfterTheRightPassword()
    {
        SetupUser(emailConfirmed: false, passwordOk: false);
        var wrongPassword = () => _sut.Login(Credentials());
        (await wrongPassword.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>())
            .WithMessage(AccountManager.InvalidAuthenticationMessage);

        SetupUser(emailConfirmed: false, passwordOk: true);
        var rightPassword = () => _sut.Login(Credentials());
        (await rightPassword.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>())
            .WithMessage(AccountManager.EmailNotConfirmedMessage);
    }

    [Fact]
    public async Task Login_Success_IssuesTokensAndResetsTheFailureCount()
    {
        SetupUser();

        var result = await _sut.Login(Credentials());

        result.Should().BeEquivalentTo(new AuthResponseDto
            { IsAuthSuccessful = true, Token = "jwt", RefreshToken = "refresh" });
        _user.RefreshToken.Should().Be("refresh");
        _user.RefreshTokenExpiryTime.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
        _userManager.Verify(m => m.UpdateAsync(_user), Times.Once);
        _userManager.Verify(m => m.ResetAccessFailedCountAsync(_user), Times.Once);
    }

    [Fact]
    public async Task Login_RefreshTokenCannotBeStored_Throws()
    {
        SetupUser();
        _userManager.Setup(m => m.UpdateAsync(_user)).ReturnsAsync(IdentityResult.Failed());

        var act = () => _sut.Login(Credentials());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Login_TwoStepEnabled_MailsACodeAndMarksTheLoginAsPending()
    {
        SetupUser(twoFactor: true);
        _userManager.Setup(m => m.GetValidTwoFactorProvidersAsync(_user)).ReturnsAsync(["Email"]);
        _userManager.Setup(m => m.GenerateTwoFactorTokenAsync(_user, "Email")).ReturnsAsync("123456");
        string? pendingUntil = null;
        _userManager.Setup(m => m.SetAuthenticationTokenAsync(_user, AccountManager.TwoFactorLoginProvider,
                AccountManager.TwoFactorLoginPendingToken, It.IsAny<string?>()))
            .Callback<User, string, string, string?>((_, _, _, value) => pendingUntil = value)
            .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.Login(Credentials());

        result.Is2StepVerificationRequired.Should().BeTrue();
        result.Provider.Should().Be("Email");
        result.Token.Should().BeEmpty();
        DateTimeOffset.Parse(pendingUntil!, CultureInfo.InvariantCulture)
            .Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(10), TimeSpan.FromMinutes(1));
        _sent.Should().ContainSingle().Which.Content.Should().Be("123456");
        _authService.Verify(s => s.GetToken(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region ForgotPassword / ResetPassword / EmailConfirmation

    [Fact]
    public async Task ForgotPassword_UnknownEmail_DoesNothingAndDoesNotThrow()
    {
        await _sut.ForgotPassword(new ForgotPasswordDto { Email = Email, ClientUri = "https://client.test/reset" });

        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_MailsAResetLink()
    {
        SetupUser();
        _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(_user)).ReturnsAsync("reset-token");

        await _sut.ForgotPassword(new ForgotPasswordDto { Email = Email, ClientUri = "https://client.test/reset" });

        _sent.Should().ContainSingle();
        _sent[0].Subject.Should().Be("Reset password token");
        _sent[0].Content.Should().Be("https://client.test/reset?token=reset-token&email=jane@test.com");
    }

    [Fact]
    public async Task ResetPassword_UnknownUser_GetsTheSameAnswerAsAWrongToken()
    {
        var act = () => _sut.ResetPassword(new ResetPasswordDto { Email = Email, Token = "t", Password = Password });

        (await act.Should().ThrowAsync<AccountManagerErrorsException>())
            .Which.Errors.Should().Equal(AccountManager.InvalidTokenMessage);
    }

    [Fact]
    public async Task ResetPassword_Rejected_ThrowsTheIdentityErrors()
    {
        SetupUser();
        _userManager.Setup(m => m.ResetPasswordAsync(_user, "t", Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var act = () => _sut.ResetPassword(new ResetPasswordDto { Email = Email, Token = "t", Password = Password });

        (await act.Should().ThrowAsync<AccountManagerErrorsException>()).Which.Errors.Should().Equal("Invalid token.");
    }

    [Fact]
    public async Task ResetPassword_Success_UnlocksTheAccountAndRevokesTheRefreshToken()
    {
        SetupUser();
        _user.RefreshToken = "stolen";
        _userManager.Setup(m => m.ResetPasswordAsync(_user, "t", Password)).ReturnsAsync(IdentityResult.Success);

        await _sut.ResetPassword(new ResetPasswordDto { Email = Email, Token = "t", Password = Password });

        _userManager.Verify(m => m.SetLockoutEndDateAsync(_user, null), Times.Once);
        _userManager.Verify(m => m.ResetAccessFailedCountAsync(_user), Times.Once);
        _userManager.Verify(m => m.UpdateAsync(It.Is<User>(u => u.RefreshToken == string.Empty)), Times.Once);
    }

    [Fact]
    public async Task EmailConfirmation_InvalidToken_Throws()
    {
        SetupUser();
        _userManager.Setup(m => m.ConfirmEmailAsync(_user, "bad")).ReturnsAsync(IdentityResult.Failed());

        var act = () => _sut.EmailConfirmation(Email, "bad");

        await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>();
    }

    [Fact]
    public async Task EmailConfirmation_ValidToken_Succeeds()
    {
        SetupUser();
        _userManager.Setup(m => m.ConfirmEmailAsync(_user, "good")).ReturnsAsync(IdentityResult.Success);

        await _sut.EmailConfirmation(Email, "good");
    }

    #endregion

    #region TwoStepVerification

    [Fact]
    public async Task TwoStepVerification_UnknownUser_Throws()
    {
        var act = () => _sut.TwoStepVerification(Code());

        (await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>())
            .WithMessage(AccountManager.InvalidVerificationMessage);
    }

    [Fact]
    public async Task TwoStepVerification_LockedOut_Throws()
    {
        SetupUser(lockedOut: true, twoFactor: true);
        SetupPendingTwoStepLogin(DateTimeOffset.UtcNow.AddMinutes(5));

        var act = () => _sut.TwoStepVerification(Code());

        (await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>())
            .WithMessage(AccountManager.LockedOutMessage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TwoStepVerification_WithoutARecentPasswordCheck_RejectsEvenAValidCode(bool expired)
    {
        SetupUser(twoFactor: true);
        SetupPendingTwoStepLogin(expired ? DateTimeOffset.UtcNow.AddMinutes(-1) : null);
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(_user, "Email", "123456")).ReturnsAsync(true);

        var act = () => _sut.TwoStepVerification(Code());

        (await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>())
            .WithMessage(AccountManager.VerificationExpiredMessage);
        _authService.Verify(s => s.GetToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task TwoStepVerification_WrongCode_CountsTheFailure()
    {
        SetupUser(twoFactor: true);
        SetupPendingTwoStepLogin(DateTimeOffset.UtcNow.AddMinutes(5));
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(_user, "Email", "000000")).ReturnsAsync(false);

        var act = () => _sut.TwoStepVerification(Code("000000"));

        (await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>())
            .WithMessage(AccountManager.InvalidVerificationMessage);
        _userManager.Verify(m => m.AccessFailedAsync(_user), Times.Once);
    }

    [Fact]
    public async Task TwoStepVerification_WrongCodeThatLocksTheAccount_EndsThePendingLogin()
    {
        SetupUser(twoFactor: true);
        SetupPendingTwoStepLogin(DateTimeOffset.UtcNow.AddMinutes(5));
        _userManager.SetupSequence(m => m.IsLockedOutAsync(_user)).ReturnsAsync(false).ReturnsAsync(true);
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(_user, "Email", "000000")).ReturnsAsync(false);

        var act = () => _sut.TwoStepVerification(Code("000000"));

        (await act.Should().ThrowAsync<AccountManagerUnauthorizedLoginException>())
            .WithMessage(AccountManager.LockedOutMessage);
        _userManager.Verify(m => m.RemoveAuthenticationTokenAsync(_user, AccountManager.TwoFactorLoginProvider,
            AccountManager.TwoFactorLoginPendingToken), Times.Once);
    }

    [Fact]
    public async Task TwoStepVerification_ValidCode_UsesTheEmailProviderAndIssuesTokens()
    {
        SetupUser(twoFactor: true);
        SetupPendingTwoStepLogin(DateTimeOffset.UtcNow.AddMinutes(5));
        _userManager.Setup(m => m.VerifyTwoFactorTokenAsync(_user, "Email", "123456")).ReturnsAsync(true);

        var result = await _sut.TwoStepVerification(Code(provider: "Phone"));

        result.Should().BeEquivalentTo(new AuthResponseDto
            { IsAuthSuccessful = true, Token = "jwt", RefreshToken = "refresh" });
        _userManager.Verify(m => m.VerifyTwoFactorTokenAsync(_user, "Phone", It.IsAny<string>()), Times.Never);
        _userManager.Verify(m => m.RemoveAuthenticationTokenAsync(_user, AccountManager.TwoFactorLoginProvider,
            AccountManager.TwoFactorLoginPendingToken), Times.Once);
        _userManager.Verify(m => m.ResetAccessFailedCountAsync(_user), Times.Once);
    }

    #endregion
}
